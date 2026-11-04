using System;
using System.Runtime.InteropServices;

namespace LiteClip
{
    internal static class NativeMF
    {
        public const int MF_SDK_VERSION = 0x0002;
        public const int MF_API_VERSION = 0x0070;
        public const int MF_VERSION = (MF_SDK_VERSION << 16) | MF_API_VERSION;
        public const uint MFSTARTUP_NOSOCKET = 0x1;

        public static readonly Guid MF_MT_MAJOR_TYPE = new Guid("48eba18e-f8c9-4687-bf11-0a74c9f96a8f");
        public static readonly Guid MF_MT_SUBTYPE = new Guid("f7e34c9a-42e8-4714-b74b-cb29d72c35e5");
        public static readonly Guid MF_MT_AVG_BITRATE = new Guid("20332624-fb0d-4d9e-bd0d-cbf6786c102e");
        public static readonly Guid MF_MT_INTERLACE_MODE = new Guid("e2724bb8-e676-4806-b4b2-a8d6efb44ccd");
        public static readonly Guid MF_MT_FRAME_SIZE = new Guid("1652c33d-d6b2-4012-b834-72030849a37d");
        public static readonly Guid MF_MT_FRAME_RATE = new Guid("c459a2e8-3d2c-4e44-b132-fee5156c7bb0");
        public static readonly Guid MF_MT_PIXEL_ASPECT_RATIO = new Guid("c6376a1e-8d0a-4027-be45-6d9a0ad39bb6");
        public static readonly Guid MF_MT_ALL_SAMPLES_INDEPENDENT = new Guid("c9173739-5e56-461c-b713-46fb995cb95f");
        public static readonly Guid MF_MT_DEFAULT_STRIDE = new Guid("644b4e48-1e02-4516-b0eb-c01ca9d49ac6");
        public static readonly Guid MF_MT_FIXED_SIZE_SAMPLES = new Guid("b8ebefaf-b718-4e04-b0a9-116775e3321b");
        public static readonly Guid MF_MT_SAMPLE_SIZE = new Guid("dad3ab78-1990-408b-bce2-eba673dacc10");
        public static readonly Guid MF_MT_MPEG2_PROFILE = new Guid("ad76a80b-2d5c-4e0b-b375-64e520137036");

        // Audio Media Type Attributes
        public static readonly Guid MF_MT_AUDIO_NUM_CHANNELS = new Guid("37e48bf5-645e-4c5b-89de-ada9e29b696a");
        public static readonly Guid MF_MT_AUDIO_SAMPLES_PER_SECOND = new Guid("5faeeae7-0290-4c31-9e8a-c534f68d9dba");
        public static readonly Guid MF_MT_AUDIO_AVG_BYTES_PER_SECOND = new Guid("1aab75c8-cfef-451c-ab95-ac034b8e1731");
        public static readonly Guid MF_MT_AUDIO_BLOCK_ALIGNMENT = new Guid("322de230-9eeb-43bd-ab7a-ff412251541d");
        public static readonly Guid MF_MT_AUDIO_BITS_PER_SAMPLE = new Guid("f2deb57f-40fa-4764-aa33-ed4f2d1ff669");
        public static readonly Guid MF_MT_AAC_PAYLOAD_TYPE = new Guid("bfbabe79-7434-4d1c-94f0-72a3b9e17188");

        // SinkWriter Attributes
        public static readonly Guid MF_READWRITE_ENABLE_HARDWARE_TRANSFORMS = new Guid("a634a91c-822b-41b9-a494-4de4643612b0");
        public static readonly Guid MF_LOW_LATENCY = new Guid("9c27891a-ed7a-40e1-88e8-b22727a024ee");
        public static readonly Guid MF_SINK_WRITER_DISABLE_THROTTLING = new Guid("08b845d8-2b74-4afe-9d53-be16d2d5ae4f");

        // Major Media Types
        public static readonly Guid MFMediaType_Video = new Guid("73646976-0000-0010-8000-00AA00389B71");
        public static readonly Guid MFMediaType_Audio = new Guid("73647561-0000-0010-8000-00AA00389B71");

        // Audio Subtypes
        public static readonly Guid MFAudioFormat_PCM = new Guid("00000001-0000-0010-8000-00AA00389B71");
        public static readonly Guid MFAudioFormat_AAC = new Guid("00001610-0000-0010-8000-00AA00389B71");

        // Video Subtypes (FourCC embedded in base GUID 00000000-0000-0010-8000-00AA00389B71)
        public static readonly Guid MFVideoFormat_RGB32 = new Guid(22, 0x0000, 0x0010, 0x80, 0x00, 0x00, 0xaa, 0x00, 0x38, 0x9b, 0x71);
        public static readonly Guid MFVideoFormat_ARGB32 = new Guid(21, 0x0000, 0x0010, 0x80, 0x00, 0x00, 0xaa, 0x00, 0x38, 0x9b, 0x71);
        public static readonly Guid MFVideoFormat_NV12 = MakeFourCC('N', 'V', '1', '2');
        public static readonly Guid MFVideoFormat_I420 = MakeFourCC('I', '4', '2', '0');
        public static readonly Guid MFVideoFormat_YV12 = MakeFourCC('Y', 'V', '1', '2');
        public static readonly Guid MFVideoFormat_YUY2 = MakeFourCC('Y', 'U', 'Y', '2');
        public static readonly Guid MFVideoFormat_H264 = MakeFourCC('H', '2', '6', '4');
        public static readonly Guid MFVideoFormat_HEVC = MakeFourCC('H', 'E', 'V', 'C');
        public static readonly Guid MFVideoFormat_WVC1 = MakeFourCC('W', 'V', 'C', '1');
        public static readonly Guid MFVideoFormat_WMV3 = MakeFourCC('W', 'M', 'V', '3');
        public static readonly Guid MFVideoFormat_MJPG = MakeFourCC('M', 'J', 'P', 'G');

        public static Guid MakeFourCC(char c1, char c2, char c3, char c4)
        {
            uint fourcc = (uint)c1 | ((uint)c2 << 8) | ((uint)c3 << 16) | ((uint)c4 << 24);
            return new Guid(fourcc, 0x0000, 0x0010, 0x80, 0x00, 0x00, 0xaa, 0x00, 0x38, 0x9b, 0x71);
        }

        [DllImport("mfplat.dll", ExactSpelling = true)]
        public static extern int MFStartup(int Version, uint dwFlags);

        [DllImport("mfplat.dll", ExactSpelling = true)]
        public static extern int MFShutdown();

        [DllImport("mfplat.dll", ExactSpelling = true)]
        public static extern int MFCreateMediaType(out IMFMediaType ppMFType);

        [DllImport("mfplat.dll", ExactSpelling = true)]
        public static extern int MFCreateAttributes(out IMFAttributes ppMFAttributes, uint cInitialSize);

        [DllImport("mfplat.dll", ExactSpelling = true)]
        public static extern int MFCreateMemoryBuffer(uint cbMaxLength, out IMFMediaBuffer ppBuffer);

        [DllImport("mfplat.dll", ExactSpelling = true)]
        public static extern int MFCreateSample(out IMFSample ppIMFSample);

        [DllImport("mfreadwrite.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
        public static extern int MFCreateSinkWriterFromURL(
            string pwszOutputURL,
            IntPtr pByteStream,
            IMFAttributes pAttributes,
            out IMFSinkWriter ppSinkWriter);

        [DllImport("ole32.dll")]
        public static extern void CoTaskMemFree(IntPtr pv);

        [DllImport("ole32.dll")]
        public static extern int PropVariantClear(ref PROPVARIANT pvar);

        public static ulong Pack2UInt32AsUInt64(uint high, uint low)
        {
            return ((ulong)high << 32) | low;
        }
    }

    // =========================================================================
    // WASAPI CoreAudio COM Interfaces
    // =========================================================================
    [ComImport, Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
    internal class MMDeviceEnumeratorComObj { }

    [ComImport, Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMMDeviceEnumerator
    {
        [PreserveSig] int EnumAudioEndpoints(int dataFlow, uint dwStateMask, out IMMDeviceCollection ppDevices);
        [PreserveSig] int GetDefaultAudioEndpoint(int dataFlow, int role, out IMMDevice ppEndpoint);
        [PreserveSig] int GetDevice([MarshalAs(UnmanagedType.LPWStr)] string pwstrId, out IMMDevice ppDevice);
    }

    [ComImport, Guid("0BD7A1BE-7A1A-44DB-8397-CC5392387B5E"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMMDeviceCollection
    {
        [PreserveSig] int GetCount(out uint pcDevices);
        [PreserveSig] int Item(uint nDevice, out IMMDevice ppDevice);
    }

    [ComImport, Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMMDevice
    {
        [PreserveSig] int Activate(ref Guid iid, uint dwClsCtx, IntPtr pActivationParams, out IntPtr ppInterface);
        [PreserveSig] int OpenPropertyStore(uint stgmAccess, out IPropertyStore ppProperties);
        [PreserveSig] int GetId([MarshalAs(UnmanagedType.LPWStr)] out string ppstrId);
    }

    [ComImport, Guid("886d8eeb-8cf2-4446-8d02-cdba1dbdcf99"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IPropertyStore
    {
        [PreserveSig] int GetCount(out uint cProps);
        [PreserveSig] int GetAt(uint iProp, out PROPERTYKEY pkey);
        [PreserveSig] int GetValue(ref PROPERTYKEY key, out PROPVARIANT pv);
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct PROPERTYKEY
    {
        public Guid fmtid;
        public uint pid;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct PROPVARIANT
    {
        public ushort vt;
        public ushort wReserved1;
        public ushort wReserved2;
        public ushort wReserved3;
        public IntPtr ptrVal;
        public int pad2;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct WAVEFORMATEX
    {
        public ushort wFormatTag;
        public ushort nChannels;
        public uint nSamplesPerSec;
        public uint nAvgBytesPerSec;
        public ushort nBlockAlign;
        public ushort wBitsPerSample;
        public ushort cbSize;
    }

    [ComImport, Guid("1CB9AD4C-DBFA-4c32-B178-C2F568A703B2"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IAudioClient
    {
        [PreserveSig] int Initialize(int ShareMode, uint StreamFlags, long hnsBufferDuration, long hnsPeriodicity, IntPtr pFormat, IntPtr AudioSessionGuid);
        [PreserveSig] int GetBufferSize(out uint pNumBufferFrames);
        [PreserveSig] int GetStreamLatency(out long phnsLatency);
        [PreserveSig] int GetCurrentPadding(out uint pNumPaddingFrames);
        [PreserveSig] int IsFormatSupported(int ShareMode, IntPtr pFormat, out IntPtr ppClosestMatch);
        [PreserveSig] int GetMixFormat(out IntPtr ppDeviceFormat);
        [PreserveSig] int GetDevicePeriod(out long phnsDefaultDevicePeriod, out long phnsMinimumDevicePeriod);
        [PreserveSig] int Start();
        [PreserveSig] int Stop();
        [PreserveSig] int Reset();
        [PreserveSig] int SetEventHandle(IntPtr eventHandle);
        [PreserveSig] int GetService(ref Guid riid, out IntPtr ppv);
    }

    [ComImport, Guid("C8ADBD64-E71E-48a0-A4DE-185C395CD317"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IAudioCaptureClient
    {
        [PreserveSig] int GetBuffer(out IntPtr ppData, out uint pNumFramesToRead, out uint pdwFlags, out ulong pu64DevicePosition, out ulong pu64QPCPosition);
        [PreserveSig] int ReleaseBuffer(uint NumFramesRead);
        [PreserveSig] int GetNextPacketSize(out uint pNumFramesInNextPacket);
    }

    // =========================================================================
    // Media Foundation COM Interfaces
    // =========================================================================
    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("2cd2d921-c447-44a7-a13c-4adabfc247e3")]
    internal interface IMFAttributes
    {
        int GetItem([In] ref Guid guidKey, [In, Out] IntPtr pValue);
        int GetItemType([In] ref Guid guidKey, out int pType);
        int CompareItem([In] ref Guid guidKey, IntPtr Value, out bool pbResult);
        int Compare(IMFAttributes pTheirs, int MatchType, out bool pbResult);
        int GetUINT32([In] ref Guid guidKey, out uint punValue);
        int GetUINT64([In] ref Guid guidKey, out ulong punValue);
        int GetDouble([In] ref Guid guidKey, out double pfValue);
        int GetGUID([In] ref Guid guidKey, out Guid pguidValue);
        int GetStringLength([In] ref Guid guidKey, out uint pcchLength);
        int GetString([In] ref Guid guidKey, [Out, MarshalAs(UnmanagedType.LPWStr)] out string pwszValue, uint cchBufSize, out uint pcchLength);
        int GetAllocatedString([In] ref Guid guidKey, [MarshalAs(UnmanagedType.LPWStr)] out string ppwszValue, out uint pcchLength);
        int GetBlobSize([In] ref Guid guidKey, out uint pcbBlobSize);
        int GetBlob([In] ref Guid guidKey, [Out] byte[] pBuf, uint cbBufSize, out uint pcbBlobSize);
        int GetAllocatedBlob([In] ref Guid guidKey, out IntPtr ppBuf, out uint pcbSize);
        int GetUnknown([In] ref Guid guidKey, [In] ref Guid riid, [MarshalAs(UnmanagedType.IUnknown)] out object ppv);
        int SetItem([In] ref Guid guidKey, IntPtr Value);
        int DeleteItem([In] ref Guid guidKey);
        int DeleteAllItems();
        int SetUINT32([In] ref Guid guidKey, uint unValue);
        int SetUINT64([In] ref Guid guidKey, ulong unValue);
        int SetDouble([In] ref Guid guidKey, double fValue);
        int SetGUID([In] ref Guid guidKey, [In] ref Guid guidValue);
        int SetString([In] ref Guid guidKey, [In, MarshalAs(UnmanagedType.LPWStr)] string wszValue);
        int SetBlob([In] ref Guid guidKey, [In] byte[] pBuf, uint cbBufSize);
        int SetUnknown([In] ref Guid guidKey, [In, MarshalAs(UnmanagedType.IUnknown)] object pUnknown);
        int LockStore();
        int UnlockStore();
        int GetCount(out uint pcItems);
        int GetItemByIndex(uint unIndex, out Guid pguidKey, [In, Out] IntPtr pValue);
        int CopyAllItems(IMFAttributes pDest);
    }

    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("44ae0fa8-ea31-4109-8d2e-4cae4997c555")]
    internal interface IMFMediaType : IMFAttributes
    {
        new int GetItem([In] ref Guid guidKey, [In, Out] IntPtr pValue);
        new int GetItemType([In] ref Guid guidKey, out int pType);
        new int CompareItem([In] ref Guid guidKey, IntPtr Value, out bool pbResult);
        new int Compare(IMFAttributes pTheirs, int MatchType, out bool pbResult);
        new int GetUINT32([In] ref Guid guidKey, out uint punValue);
        new int GetUINT64([In] ref Guid guidKey, out ulong punValue);
        new int GetDouble([In] ref Guid guidKey, out double pfValue);
        new int GetGUID([In] ref Guid guidKey, out Guid pguidValue);
        new int GetStringLength([In] ref Guid guidKey, out uint pcchLength);
        new int GetString([In] ref Guid guidKey, [Out, MarshalAs(UnmanagedType.LPWStr)] out string pwszValue, uint cchBufSize, out uint pcchLength);
        new int GetAllocatedString([In] ref Guid guidKey, [MarshalAs(UnmanagedType.LPWStr)] out string ppwszValue, out uint pcchLength);
        new int GetBlobSize([In] ref Guid guidKey, out uint pcbBlobSize);
        new int GetBlob([In] ref Guid guidKey, [Out] byte[] pBuf, uint cbBufSize, out uint pcbBlobSize);
        new int GetAllocatedBlob([In] ref Guid guidKey, out IntPtr ppBuf, out uint pcbSize);
        new int GetUnknown([In] ref Guid guidKey, [In] ref Guid riid, [MarshalAs(UnmanagedType.IUnknown)] out object ppv);
        new int SetItem([In] ref Guid guidKey, IntPtr Value);
        new int DeleteItem([In] ref Guid guidKey);
        new int DeleteAllItems();
        new int SetUINT32([In] ref Guid guidKey, uint unValue);
        new int SetUINT64([In] ref Guid guidKey, ulong unValue);
        new int SetDouble([In] ref Guid guidKey, double fValue);
        new int SetGUID([In] ref Guid guidKey, [In] ref Guid guidValue);
        new int SetString([In] ref Guid guidKey, [In, MarshalAs(UnmanagedType.LPWStr)] string wszValue);
        new int SetBlob([In] ref Guid guidKey, [In] byte[] pBuf, uint cbBufSize);
        new int SetUnknown([In] ref Guid guidKey, [In, MarshalAs(UnmanagedType.IUnknown)] object pUnknown);
        new int LockStore();
        new int UnlockStore();
        new int GetCount(out uint pcItems);
        new int GetItemByIndex(uint unIndex, out Guid pguidKey, [In, Out] IntPtr pValue);
        new int CopyAllItems(IMFAttributes pDest);

        int GetMajorType(out Guid pguidMajorType);
        int IsCompressedFormat(out bool pfCompressed);
        int IsEqual(IMFMediaType pIMediaType, out uint pdwFlags);
        int GetRepresentation(Guid guidRepresentation, out IntPtr ppvRepresentation);
        int FreeRepresentation(Guid guidRepresentation, IntPtr pvRepresentation);
    }

    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("045fa593-8799-42b8-bc8d-8968c6453507")]
    internal interface IMFMediaBuffer
    {
        int Lock(out IntPtr ppbBuffer, out uint pcbMaxLength, out uint pcbCurrentLength);
        int Unlock();
        int GetCurrentLength(out uint pcbCurrentLength);
        int SetCurrentLength(uint cbCurrentLength);
        int GetMaxLength(out uint pcbMaxLength);
    }

    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("c40a00f2-b93a-4d80-ae8c-5a1c634f58e4")]
    internal interface IMFSample : IMFAttributes
    {
        new int GetItem([In] ref Guid guidKey, [In, Out] IntPtr pValue);
        new int GetItemType([In] ref Guid guidKey, out int pType);
        new int CompareItem([In] ref Guid guidKey, IntPtr Value, out bool pbResult);
        new int Compare(IMFAttributes pTheirs, int MatchType, out bool pbResult);
        new int GetUINT32([In] ref Guid guidKey, out uint punValue);
        new int GetUINT64([In] ref Guid guidKey, out ulong punValue);
        new int GetDouble([In] ref Guid guidKey, out double pfValue);
        new int GetGUID([In] ref Guid guidKey, out Guid pguidValue);
        new int GetStringLength([In] ref Guid guidKey, out uint pcchLength);
        new int GetString([In] ref Guid guidKey, [Out, MarshalAs(UnmanagedType.LPWStr)] out string pwszValue, uint cchBufSize, out uint pcchLength);
        new int GetAllocatedString([In] ref Guid guidKey, [MarshalAs(UnmanagedType.LPWStr)] out string ppwszValue, out uint pcchLength);
        new int GetBlobSize([In] ref Guid guidKey, out uint pcbBlobSize);
        new int GetBlob([In] ref Guid guidKey, [Out] byte[] pBuf, uint cbBufSize, out uint pcbBlobSize);
        new int GetAllocatedBlob([In] ref Guid guidKey, out IntPtr ppBuf, out uint pcbSize);
        new int GetUnknown([In] ref Guid guidKey, [In] ref Guid riid, [MarshalAs(UnmanagedType.IUnknown)] out object ppv);
        new int SetItem([In] ref Guid guidKey, IntPtr Value);
        new int DeleteItem([In] ref Guid guidKey);
        new int DeleteAllItems();
        new int SetUINT32([In] ref Guid guidKey, uint unValue);
        new int SetUINT64([In] ref Guid guidKey, ulong unValue);
        new int SetDouble([In] ref Guid guidKey, double fValue);
        new int SetGUID([In] ref Guid guidKey, [In] ref Guid guidValue);
        new int SetString([In] ref Guid guidKey, [In, MarshalAs(UnmanagedType.LPWStr)] string wszValue);
        new int SetBlob([In] ref Guid guidKey, [In] byte[] pBuf, uint cbBufSize);
        new int SetUnknown([In] ref Guid guidKey, [In, MarshalAs(UnmanagedType.IUnknown)] object pUnknown);
        new int LockStore();
        new int UnlockStore();
        new int GetCount(out uint pcItems);
        new int GetItemByIndex(uint unIndex, out Guid pguidKey, [In, Out] IntPtr pValue);
        new int CopyAllItems(IMFAttributes pDest);

        int GetSampleFlags(out uint pdwSampleFlags);
        int SetSampleFlags(uint dwSampleFlags);
        int GetSampleTime(out long phnsSampleTime);
        int SetSampleTime(long hnsSampleTime);
        int GetSampleDuration(out long phnsSampleDuration);
        int SetSampleDuration(long hnsSampleDuration);
        int GetBufferCount(out uint pdwBufferCount);
        int GetBufferByIndex(uint dwIndex, out IMFMediaBuffer ppBuffer);
        int ConvertToContiguousBuffer(out IMFMediaBuffer ppBuffer);
        int AddBuffer(IMFMediaBuffer pBuffer);
        int RemoveBufferByIndex(uint dwIndex);
        int RemoveAllBuffers();
        int GetTotalLength(out uint pcbTotalLength);
        int CopyToBuffer(IMFMediaBuffer pBuffer);
    }

    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("3137f1cd-fe5e-4805-a5d8-fb477448cb3d")]
    internal interface IMFSinkWriter
    {
        int AddStream(IMFMediaType pTargetMediaType, out uint pdwStreamIndex);
        int SetInputMediaType(uint dwStreamIndex, IMFMediaType pInputMediaType, IMFAttributes pEncodingParameters);
        int BeginWriting();
        int WriteSample(uint dwStreamIndex, IMFSample pSample);
        int SendStreamTick(uint dwStreamIndex, long llTimestamp);
        int PlaceMarker(uint dwStreamIndex, IntPtr pvContext);
        int NotifyEndOfSegment(uint dwStreamIndex);
        int Flush(uint dwStreamIndex);
        int Finalize_();
        int GetServiceForStream(uint dwStreamIndex, [In] ref Guid guidService, [In] ref Guid riid, out IntPtr ppvObject);
        int GetStatistics(uint dwStreamIndex, IntPtr pStats);
    }
}
