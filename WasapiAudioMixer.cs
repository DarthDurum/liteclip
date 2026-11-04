using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace LiteClip
{
    internal sealed class WasapiAudioMixer : IDisposable
    {
        private static readonly PROPERTYKEY PKEY_Device_FriendlyName = new PROPERTYKEY
        {
            fmtid = new Guid("a45c254e-df1c-4efd-8020-67d146a850e0"),
            pid = 14
        };

        private sealed class EndpointCapture : IDisposable
        {
            public IAudioClient Client;
            public IAudioCaptureClient Capture;
            public uint SampleRate;
            public int Channels;
            public int BitsPerSample;
            public bool IsFloat;
            public string FriendlyName;
            public bool Invalidated;

            public void Dispose()
            {
                try { if (Client != null) Client.Stop(); } catch { }
                if (Capture != null) { try { Marshal.ReleaseComObject(Capture); } catch { } Capture = null; }
                if (Client != null) { try { Marshal.ReleaseComObject(Client); } catch { } Client = null; }
            }
        }

        public const uint TargetSampleRate = 48000;
        public const int AacFrameSamples = 1024;
        public const int PcmFrameBytes = AacFrameSamples * 4; // 16-bit stereo = 4 bytes/sample

        private IMMDeviceEnumerator _devEnum;
        private EndpointCapture _desk;
        private EndpointCapture _mic;
        private readonly bool _enableDesktop;
        private readonly bool _enableMic;
        private readonly string _preferredMic;
        private readonly float _micGain;

        // Pre-allocated zero-GC mixing accumulators
        private readonly float[] _deskAccum = new float[65536 * 2];
        private int _deskCount;
        private readonly float[] _micAccum = new float[65536 * 2];
        private int _micCount;
        private readonly float[] _resampleScratch = new float[16384 * 2];
        private readonly byte[] _pcmBytes = new byte[PcmFrameBytes];
        private bool _micPrimed;
        private long _nextAudioPtsHns;
        private readonly long _frameDurHns = (AacFrameSamples * 10000000L) / TargetSampleRate;

        // 4-slot reusable Media Foundation sample pool
        private const int POOL_SIZE = 4;
        private readonly IMFMediaBuffer[] _audioBufPool = new IMFMediaBuffer[POOL_SIZE];
        private readonly IMFSample[] _audioSamplePool = new IMFSample[POOL_SIZE];
        private int _poolCursor;

        public float LastMicPeak { get; private set; }
        public float LastDesktopPeak { get; private set; }
        public string ActiveMicName { get; private set; }

        public WasapiAudioMixer(bool enableDesktop, bool enableMic, string preferredMicName, int micVolumePercent)
        {
            _enableDesktop = enableDesktop;
            _enableMic = enableMic;
            _preferredMic = preferredMicName ?? "";
            _micGain = Math.Max(0f, Math.Min(3.0f, micVolumePercent / 100.0f));
            ActiveMicName = "None";

            try
            {
                _devEnum = (IMMDeviceEnumerator)new MMDeviceEnumeratorComObj();
                if (_enableDesktop)
                {
                    _desk = OpenEndpoint(_devEnum, true, null);
                }
                if (_enableMic)
                {
                    _mic = OpenEndpoint(_devEnum, false, _preferredMic);
                    if (_mic != null) ActiveMicName = _mic.FriendlyName;
                }

                for (int i = 0; i < POOL_SIZE; i++)
                {
                    NativeMF.MFCreateMemoryBuffer((uint)PcmFrameBytes, out _audioBufPool[i]);
                    NativeMF.MFCreateSample(out _audioSamplePool[i]);
                    _audioSamplePool[i].AddBuffer(_audioBufPool[i]);
                }
            }
            catch { }
        }

        public static List<string> GetAvailableMicrophones()
        {
            var list = new List<string>();
            list.Add("Default System Microphone");
            IMMDeviceEnumerator devEnum = null;
            IMMDeviceCollection col = null;
            try
            {
                devEnum = (IMMDeviceEnumerator)new MMDeviceEnumeratorComObj();
                if (devEnum.EnumAudioEndpoints(1, 1, out col) == 0 && col != null) // eCapture = 1, ACTIVE = 1
                {
                    uint count;
                    if (col.GetCount(out count) == 0)
                    {
                        for (uint i = 0; i < count; i++)
                        {
                            IMMDevice dev;
                            if (col.Item(i, out dev) == 0 && dev != null)
                            {
                                string name = GetDeviceFriendlyName(dev);
                                if (!string.IsNullOrEmpty(name) && !list.Contains(name))
                                {
                                    list.Add(name);
                                }
                                Marshal.ReleaseComObject(dev);
                            }
                        }
                    }
                }
            }
            catch { }
            finally
            {
                if (col != null) Marshal.ReleaseComObject(col);
                if (devEnum != null) Marshal.ReleaseComObject(devEnum);
            }
            return list;
        }

        /// <summary>
        /// Drains WASAPI endpoints, mixes desktop + microphone audio up to videoTimestampHns, and writes AAC samples to IMFSinkWriter.
        /// </summary>
        public void PumpAudioToSinkWriter(IMFSinkWriter writer, uint audioStreamIndex, long videoTimestampHns)
        {
            if (!_enableDesktop && !_enableMic) return;

            if (_desk != null && !_desk.Invalidated)
            {
                _deskCount = DrainEndpointToStereo(_desk, TargetSampleRate, _deskAccum, _deskCount, _resampleScratch);
            }
            if (_mic != null && !_mic.Invalidated)
            {
                _micCount = DrainEndpointToStereo(_mic, TargetSampleRate, _micAccum, _micCount, _resampleScratch);
            }

            // Synthesize master-clock-paced silence when desktop audio is idle or disabled (so mic-only paces smoothly)
            long masterFramesAvail = (_desk != null) ? _deskCount : 0;
            long bufferedHns = (masterFramesAvail * 10000000L) / TargetSampleRate;
            long behindHns = videoTimestampHns - (_nextAudioPtsHns + bufferedHns);
            if (behindHns > _frameDurHns)
            {
                int fillFrames = (int)((behindHns * TargetSampleRate) / 10000000L);
                int maxAdd = (_deskAccum.Length / 2) - _deskCount;
                if (fillFrames > maxAdd) fillFrames = maxAdd;
                if (fillFrames > 0)
                {
                    Array.Clear(_deskAccum, _deskCount * 2, fillFrames * 2);
                    _deskCount += fillFrames;
                }
            }

            // Cushion microphone packets (~15ms) and bound drift (<60ms)
            if (_mic != null)
            {
                if (!_micPrimed && _micCount >= AacFrameSamples + 480)
                    _micPrimed = true;

                if (_micCount > _deskCount + 2880)
                {
                    int drop = _micCount - (_deskCount + 960);
                    _micCount -= drop;
                    Array.Copy(_micAccum, drop * 2, _micAccum, 0, _micCount * 2);
                }
            }

            while (_deskCount >= AacFrameSamples)
            {
                int bi = 0;
                int mixMicSamples = (_mic != null && _micPrimed) ? Math.Min(AacFrameSamples, _micCount) : 0;
                if (_mic != null && _micPrimed && mixMicSamples < AacFrameSamples)
                    _micPrimed = false;

                float deskPeak = 0f;
                float micPeak = 0f;

                for (int f = 0; f < AacFrameSamples; f++)
                {
                    int idx = f * 2;
                    float dl = _enableDesktop ? _deskAccum[idx] : 0f;
                    float dr = _enableDesktop ? _deskAccum[idx + 1] : 0f;
                    float dAbs = Math.Abs(dl) > Math.Abs(dr) ? Math.Abs(dl) : Math.Abs(dr);
                    if (dAbs > deskPeak) deskPeak = dAbs;

                    float ml = 0f, mr = 0f;
                    if (f < mixMicSamples)
                    {
                        ml = _micAccum[idx] * _micGain;
                        mr = _micAccum[idx + 1] * _micGain;
                        float mAbs = Math.Abs(ml) > Math.Abs(mr) ? Math.Abs(ml) : Math.Abs(mr);
                        if (mAbs > micPeak) micPeak = mAbs;
                    }

                    float l = SoftLimit(dl + ml);
                    float r = SoftLimit(dr + mr);

                    short sl = (short)(l * 32767.0f);
                    short sr = (short)(r * 32767.0f);
                    _pcmBytes[bi++] = (byte)(sl & 0xFF);
                    _pcmBytes[bi++] = (byte)((sl >> 8) & 0xFF);
                    _pcmBytes[bi++] = (byte)(sr & 0xFF);
                    _pcmBytes[bi++] = (byte)((sr >> 8) & 0xFF);
                }

                LastDesktopPeak = deskPeak;
                LastMicPeak = micPeak;

                _deskCount -= AacFrameSamples;
                if (_deskCount > 0)
                    Array.Copy(_deskAccum, AacFrameSamples * 2, _deskAccum, 0, _deskCount * 2);

                if (mixMicSamples > 0)
                {
                    _micCount -= mixMicSamples;
                    if (_micCount > 0)
                        Array.Copy(_micAccum, mixMicSamples * 2, _micAccum, 0, _micCount * 2);
                }

                IMFMediaBuffer buf = _audioBufPool[_poolCursor];
                IMFSample smp = _audioSamplePool[_poolCursor];
                _poolCursor = (_poolCursor + 1) & (POOL_SIZE - 1);

                IntPtr pIn;
                uint maxLen, curLen;
                if (buf.Lock(out pIn, out maxLen, out curLen) >= 0)
                {
                    Marshal.Copy(_pcmBytes, 0, pIn, PcmFrameBytes);
                    buf.Unlock();
                    buf.SetCurrentLength((uint)PcmFrameBytes);

                    smp.SetSampleTime(_nextAudioPtsHns);
                    smp.SetSampleDuration(_frameDurHns);
                    _nextAudioPtsHns += _frameDurHns;

                    writer.WriteSample(audioStreamIndex, smp);
                }
            }
        }

        private static float SoftLimit(float x)
        {
            if (x > 0.85f)
            {
                float ex = x - 0.85f;
                return 0.85f + 0.15f * (ex / (0.15f + ex));
            }
            if (x < -0.85f)
            {
                float ex = -0.85f - x;
                return -(0.85f + 0.15f * (ex / (0.15f + ex)));
            }
            return x;
        }

        private static EndpointCapture OpenEndpoint(IMMDeviceEnumerator devEnum, bool isLoopback, string preferredMicName)
        {
            IMMDevice dev = null;
            IntPtr pFmt = IntPtr.Zero;
            try
            {
                string friendly = "";
                if (isLoopback)
                {
                    if (devEnum.GetDefaultAudioEndpoint(0, 1, out dev) != 0 || dev == null)
                        return null;
                    friendly = GetDeviceFriendlyName(dev);
                }
                else
                {
                    dev = FindMicDevice(devEnum, preferredMicName, out friendly);
                    if (dev == null) return null;
                }

                Guid iidClient = new Guid("1CB9AD4C-DBFA-4c32-B178-C2F568A703B2");
                IntPtr pClient;
                if (dev.Activate(ref iidClient, 1, IntPtr.Zero, out pClient) != 0 || pClient == IntPtr.Zero)
                    return null;

                var client = (IAudioClient)Marshal.GetObjectForIUnknown(pClient);
                Marshal.Release(pClient);

                if (client.GetMixFormat(out pFmt) != 0 || pFmt == IntPtr.Zero)
                {
                    Marshal.ReleaseComObject(client);
                    return null;
                }

                var wfx = (WAVEFORMATEX)Marshal.PtrToStructure(pFmt, typeof(WAVEFORMATEX));
                bool isFloat = (wfx.wFormatTag == 3);
                if (wfx.wFormatTag == 0xFFFE && wfx.cbSize >= 22)
                {
                    int subTag = Marshal.ReadInt32(pFmt, 24);
                    isFloat = (subTag == 3);
                }

                uint flags = isLoopback ? 0x00020000u : 0u; // AUDCLNT_STREAMFLAGS_LOOPBACK
                if (client.Initialize(0, flags, 500000, 0, pFmt, IntPtr.Zero) != 0)
                {
                    Marshal.ReleaseComObject(client);
                    return null;
                }

                Guid iidCap = new Guid("C8ADBD64-E71E-48a0-A4DE-185C395CD317");
                IntPtr pCap;
                if (client.GetService(ref iidCap, out pCap) != 0 || pCap == IntPtr.Zero)
                {
                    Marshal.ReleaseComObject(client);
                    return null;
                }

                var cap = (IAudioCaptureClient)Marshal.GetObjectForIUnknown(pCap);
                Marshal.Release(pCap);

                client.Start();
                return new EndpointCapture
                {
                    Client = client,
                    Capture = cap,
                    SampleRate = wfx.nSamplesPerSec,
                    Channels = wfx.nChannels,
                    BitsPerSample = wfx.wBitsPerSample,
                    IsFloat = isFloat,
                    FriendlyName = friendly
                };
            }
            catch
            {
                return null;
            }
            finally
            {
                if (pFmt != IntPtr.Zero) NativeMF.CoTaskMemFree(pFmt);
                if (dev != null) Marshal.ReleaseComObject(dev);
            }
        }

        private static IMMDevice FindMicDevice(IMMDeviceEnumerator devEnum, string preferredName, out string friendlyName)
        {
            friendlyName = "";
            IMMDeviceCollection col = null;
            try
            {
                string search = (!string.IsNullOrEmpty(preferredName) && preferredName != "Default System Microphone")
                    ? preferredName
                    : "G435";

                if (devEnum.EnumAudioEndpoints(1, 1, out col) == 0 && col != null)
                {
                    uint count;
                    if (col.GetCount(out count) == 0)
                    {
                        for (uint i = 0; i < count; i++)
                        {
                            IMMDevice cand;
                            if (col.Item(i, out cand) == 0 && cand != null)
                            {
                                string name = GetDeviceFriendlyName(cand);
                                if (name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    friendlyName = name;
                                    return cand;
                                }
                                Marshal.ReleaseComObject(cand);
                            }
                        }
                    }
                }
            }
            catch { }
            finally
            {
                if (col != null) Marshal.ReleaseComObject(col);
            }

            IMMDevice defDev;
            if (devEnum.GetDefaultAudioEndpoint(1, 2, out defDev) == 0 && defDev != null)
            {
                friendlyName = GetDeviceFriendlyName(defDev);
                return defDev;
            }
            if (devEnum.GetDefaultAudioEndpoint(1, 1, out defDev) == 0 && defDev != null)
            {
                friendlyName = GetDeviceFriendlyName(defDev);
                return defDev;
            }
            return null;
        }

        private static string GetDeviceFriendlyName(IMMDevice dev)
        {
            if (dev == null) return "";
            IPropertyStore store = null;
            try
            {
                if (dev.OpenPropertyStore(0, out store) != 0 || store == null) return "";
                PROPERTYKEY key = PKEY_Device_FriendlyName;
                PROPVARIANT pv;
                if (store.GetValue(ref key, out pv) == 0)
                {
                    try
                    {
                        if (pv.vt == 31 && pv.ptrVal != IntPtr.Zero)
                            return Marshal.PtrToStringUni(pv.ptrVal) ?? "";
                    }
                    finally
                    {
                        NativeMF.PropVariantClear(ref pv);
                    }
                }
            }
            catch { }
            finally
            {
                if (store != null) Marshal.ReleaseComObject(store);
            }
            return "";
        }

        private static unsafe int DrainEndpointToStereo(EndpointCapture ep, uint targetRate, float[] dstStereo, int dstFrames, float[] scratch)
        {
            int maxFrames = dstStereo.Length / 2;
            uint pktSize;
            while (true)
            {
                int hrNext = ep.Capture.GetNextPacketSize(out pktSize);
                if (hrNext != 0)
                {
                    if (hrNext == unchecked((int)0x88890004))
                        ep.Invalidated = true;
                    break;
                }
                if (pktSize == 0) break;

                IntPtr pData;
                uint numFrames, flags;
                ulong devPos, qpcPos;
                int hrBuf = ep.Capture.GetBuffer(out pData, out numFrames, out flags, out devPos, out qpcPos);
                if (hrBuf != 0)
                {
                    if (hrBuf == unchecked((int)0x88890004))
                        ep.Invalidated = true;
                    break;
                }

                bool silent = (flags & 2) != 0;
                int outFrames = (ep.SampleRate == targetRate)
                    ? (int)numFrames
                    : (int)((long)numFrames * targetRate / ep.SampleRate);

                if (dstFrames + outFrames <= maxFrames && outFrames > 0)
                {
                    if (silent || pData == IntPtr.Zero)
                    {
                        Array.Clear(dstStereo, dstFrames * 2, outFrames * 2);
                    }
                    else if (ep.SampleRate == targetRate)
                    {
                        ConvertToStereoFloat(pData, (int)numFrames, ep.Channels, ep.IsFloat, ep.BitsPerSample, dstStereo, dstFrames * 2);
                    }
                    else
                    {
                        int needed = (int)numFrames * 2;
                        float[] tmp = (needed <= scratch.Length) ? scratch : new float[needed];
                        ConvertToStereoFloat(pData, (int)numFrames, ep.Channels, ep.IsFloat, ep.BitsPerSample, tmp, 0);
                        double step = (double)numFrames / outFrames;
                        int di = dstFrames * 2;
                        for (int i = 0; i < outFrames; i++)
                        {
                            int si = (int)(i * step);
                            if (si >= (int)numFrames) si = (int)numFrames - 1;
                            dstStereo[di++] = tmp[si * 2];
                            dstStereo[di++] = tmp[si * 2 + 1];
                        }
                    }
                    dstFrames += outFrames;
                }

                ep.Capture.ReleaseBuffer(numFrames);
            }
            return dstFrames;
        }

        private static unsafe void ConvertToStereoFloat(IntPtr pSrc, int frames, int ch, bool isFloat, int bits, float[] dst, int dstIndex)
        {
            if (isFloat && bits == 32)
            {
                float* pf = (float*)pSrc;
                if (ch == 2)
                {
                    Marshal.Copy(pSrc, dst, dstIndex, frames * 2);
                }
                else if (ch == 1)
                {
                    for (int i = 0; i < frames; i++)
                    {
                        float s = pf[i];
                        dst[dstIndex++] = s;
                        dst[dstIndex++] = s;
                    }
                }
                else if (ch >= 6)
                {
                    for (int i = 0; i < frames; i++)
                    {
                        int b = i * ch;
                        float fc = pf[b + 2] * 0.707f;
                        float l = pf[b] + fc + pf[b + 4] * 0.707f;
                        float r = pf[b + 1] + fc + pf[b + 5] * 0.707f;
                        dst[dstIndex++] = l;
                        dst[dstIndex++] = r;
                    }
                }
                else
                {
                    for (int i = 0; i < frames; i++)
                    {
                        int b = i * ch;
                        dst[dstIndex++] = pf[b];
                        dst[dstIndex++] = pf[b + 1];
                    }
                }
            }
            else if (bits == 16)
            {
                short* ps = (short*)pSrc;
                const float inv = 1.0f / 32768.0f;
                if (ch == 1)
                {
                    for (int i = 0; i < frames; i++)
                    {
                        float s = ps[i] * inv;
                        dst[dstIndex++] = s;
                        dst[dstIndex++] = s;
                    }
                }
                else
                {
                    for (int i = 0; i < frames; i++)
                    {
                        int b = i * ch;
                        dst[dstIndex++] = ps[b] * inv;
                        dst[dstIndex++] = ps[b + 1] * inv;
                    }
                }
            }
            else
            {
                Array.Clear(dst, dstIndex, frames * 2);
            }
        }

        public void Dispose()
        {
            for (int i = 0; i < POOL_SIZE; i++)
            {
                if (_audioSamplePool[i] != null) { try { Marshal.ReleaseComObject(_audioSamplePool[i]); } catch { } _audioSamplePool[i] = null; }
                if (_audioBufPool[i] != null) { try { Marshal.ReleaseComObject(_audioBufPool[i]); } catch { } _audioBufPool[i] = null; }
            }
            if (_desk != null) { _desk.Dispose(); _desk = null; }
            if (_mic != null) { _mic.Dispose(); _mic = null; }
            if (_devEnum != null) { try { Marshal.ReleaseComObject(_devEnum); } catch { } _devEnum = null; }
        }
    }
}
