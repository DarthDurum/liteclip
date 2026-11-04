using System;
using System.IO;
using System.Text;

namespace LiteClip
{
    public enum QualityPreset
    {
        Balanced,
        HighQ,
        UHQ,
        UltraLight,
        Custom
    }

    public enum DesktopCaptureMode
    {
        DXGIDesktopDuplication, // Hardware GPU Direct Desktop Duplication (with fast GDI fallback)
        GDIZeroCopyBitBlt,      // Ultra-lightweight Zero-Allocation DIBSection BitBlt
        WindowCapture,          // Specific App / Game Window (PrintWindow / DWM)
        CustomRegion            // User-selected rectangle on screen
    }

    public enum VideoEncoderType
    {
        H264_Hardware,          // NVENC / AMF / QuickSync Hardware H.264 (MFT)
        HEVC_Hardware,          // Hardware H.265 / HEVC (MFT)
        H264_Software,          // CPU Software H.264 MFT
        WMV3_Fast               // Ultra-low CPU Windows Media Video 9
    }

    public enum DecoderColorPipeline
    {
        RGB32_Direct,           // Direct 32-bit BGRA Uncompressed Input -> Hardware MFT Color Converter
        NV12_Hardware420,       // Pre-converted NV12 4:2:0 YUV Planar (12 bits/pixel - halves memory bandwidth!)
        I420_PlanarYUV,         // I420 4:2:0 YUV Planar Pipeline
        RGB24_Compact           // Compact RGB32 Top-Down Low-Bandwidth Pipeline
    }

    public enum RateControlMode
    {
        CBR,                    // Constant Bitrate
        VBR_Peak,               // Variable Bitrate
        QualityBased            // Constant Quality (CRF / CQP equivalent)
    }

    public sealed class RecorderConfig
    {
        public QualityPreset Preset = QualityPreset.Balanced;
        public DesktopCaptureMode CaptureMode = DesktopCaptureMode.DXGIDesktopDuplication;
        public VideoEncoderType Encoder = VideoEncoderType.H264_Hardware;
        public DecoderColorPipeline DecoderPipeline = DecoderColorPipeline.NV12_Hardware420;
        public RateControlMode RateControl = RateControlMode.CBR;

        // Resolution (0 = Native Monitor Resolution)
        public int TargetWidth = 1920;
        public int TargetHeight = 1080;
        public bool UseNativeResolution = false;

        // Framerate & Bitrate
        public int FPS = 60;
        public int BitrateKbps = 12000; // 12 Mbps default for Balanced
        public int QualityLevel = 78;   // 1 - 100
        public int KeyframeIntervalSec = 2;
        public bool LowLatencyMode = true;

        // Capture Source Details
        public int MonitorIndex = 0;
        public IntPtr TargetWindowHandle = IntPtr.Zero;
        public string TargetWindowTitle = "";
        public int RegionX = 100;
        public int RegionY = 100;
        public int RegionWidth = 1280;
        public int RegionHeight = 720;

        // Cursor & Visuals
        public bool CaptureCursor = true;
        public bool HighlightMouseClicks = true;

        // Audio Settings
        public bool CaptureDesktopAudio = true;
        public bool CaptureMicrophone = true;
        public string MicrophoneDeviceName = "";
        public int MicrophoneVolumePercent = 100;
        public int AudioBitrateKbps = 160;

        // Lightweight / RAM Optimization Flags
        public bool AggressiveRamTrim = true;
        public bool EcoPreviewMode = true; // Updates preview at 12 FPS instead of 60 FPS to save ~60% UI RAM/CPU
        public bool DisablePreviewWhileRecording = false;

        // Output Directory
        public string OutputFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
            "LiteClip");

        public void ApplyPreset(QualityPreset preset)
        {
            Preset = preset;
            switch (preset)
            {
                case QualityPreset.UHQ:
                    // Ultra High Quality: Native / 1440p-4K ready, 60 FPS, 45 Mbps, Quality 96
                    UseNativeResolution = true;
                    TargetWidth = 2560;
                    TargetHeight = 1440;
                    FPS = 60;
                    BitrateKbps = 45000;
                    QualityLevel = 96;
                    Encoder = VideoEncoderType.H264_Hardware;
                    DecoderPipeline = DecoderColorPipeline.RGB32_Direct;
                    RateControl = RateControlMode.VBR_Peak;
                    AudioBitrateKbps = 320;
                    LowLatencyMode = false;
                    AggressiveRamTrim = true;
                    break;

                case QualityPreset.HighQ:
                    // High Quality: 1080p60, 24 Mbps, Quality 88, Hardware H.264 + NV12
                    UseNativeResolution = false;
                    TargetWidth = 1920;
                    TargetHeight = 1080;
                    FPS = 60;
                    BitrateKbps = 24000;
                    QualityLevel = 88;
                    Encoder = VideoEncoderType.H264_Hardware;
                    DecoderPipeline = DecoderColorPipeline.NV12_Hardware420;
                    RateControl = RateControlMode.CBR;
                    AudioBitrateKbps = 192;
                    LowLatencyMode = true;
                    AggressiveRamTrim = true;
                    break;

                case QualityPreset.Balanced:
                    // Balanced: 1080p60, 12 Mbps, Quality 78, NV12 zero-copy pipeline
                    UseNativeResolution = false;
                    TargetWidth = 1920;
                    TargetHeight = 1080;
                    FPS = 60;
                    BitrateKbps = 12000;
                    QualityLevel = 78;
                    Encoder = VideoEncoderType.H264_Hardware;
                    DecoderPipeline = DecoderColorPipeline.NV12_Hardware420;
                    RateControl = RateControlMode.CBR;
                    AudioBitrateKbps = 160;
                    LowLatencyMode = true;
                    AggressiveRamTrim = true;
                    break;

                case QualityPreset.UltraLight:
                    // Ultra-Light: 720p30, 5 Mbps, GDI Zero-Copy + NV12, minimum RAM & CPU
                    UseNativeResolution = false;
                    TargetWidth = 1280;
                    TargetHeight = 720;
                    FPS = 30;
                    BitrateKbps = 5000;
                    QualityLevel = 68;
                    CaptureMode = DesktopCaptureMode.GDIZeroCopyBitBlt;
                    Encoder = VideoEncoderType.H264_Hardware;
                    DecoderPipeline = DecoderColorPipeline.NV12_Hardware420;
                    RateControl = RateControlMode.CBR;
                    AudioBitrateKbps = 128;
                    LowLatencyMode = true;
                    AggressiveRamTrim = true;
                    EcoPreviewMode = true;
                    break;
            }
        }

        public RecorderConfig Clone()
        {
            return (RecorderConfig)this.MemberwiseClone();
        }

        public void SaveToFile(string path)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                var sb = new StringBuilder();
                sb.AppendLine("{");
                sb.AppendLine(string.Format("  \"Preset\": \"{0}\",", Preset));
                sb.AppendLine(string.Format("  \"CaptureMode\": \"{0}\",", CaptureMode));
                sb.AppendLine(string.Format("  \"Encoder\": \"{0}\",", Encoder));
                sb.AppendLine(string.Format("  \"DecoderPipeline\": \"{0}\",", DecoderPipeline));
                sb.AppendLine(string.Format("  \"RateControl\": \"{0}\",", RateControl));
                sb.AppendLine(string.Format("  \"UseNativeResolution\": {0},", UseNativeResolution.ToString().ToLowerInvariant()));
                sb.AppendLine(string.Format("  \"TargetWidth\": {0},", TargetWidth));
                sb.AppendLine(string.Format("  \"TargetHeight\": {0},", TargetHeight));
                sb.AppendLine(string.Format("  \"FPS\": {0},", FPS));
                sb.AppendLine(string.Format("  \"BitrateKbps\": {0},", BitrateKbps));
                sb.AppendLine(string.Format("  \"QualityLevel\": {0},", QualityLevel));
                sb.AppendLine(string.Format("  \"MonitorIndex\": {0},", MonitorIndex));
                sb.AppendLine(string.Format("  \"RegionX\": {0},", RegionX));
                sb.AppendLine(string.Format("  \"RegionY\": {0},", RegionY));
                sb.AppendLine(string.Format("  \"RegionWidth\": {0},", RegionWidth));
                sb.AppendLine(string.Format("  \"RegionHeight\": {0},", RegionHeight));
                sb.AppendLine(string.Format("  \"CaptureCursor\": {0},", CaptureCursor.ToString().ToLowerInvariant()));
                sb.AppendLine(string.Format("  \"HighlightMouseClicks\": {0},", HighlightMouseClicks.ToString().ToLowerInvariant()));
                sb.AppendLine(string.Format("  \"CaptureDesktopAudio\": {0},", CaptureDesktopAudio.ToString().ToLowerInvariant()));
                sb.AppendLine(string.Format("  \"CaptureMicrophone\": {0},", CaptureMicrophone.ToString().ToLowerInvariant()));
                sb.AppendLine(string.Format("  \"MicrophoneDeviceName\": \"{0}\",", (MicrophoneDeviceName ?? "").Replace("\\", "\\\\").Replace("\"", "")));
                sb.AppendLine(string.Format("  \"MicrophoneVolumePercent\": {0},", MicrophoneVolumePercent));
                sb.AppendLine(string.Format("  \"AudioBitrateKbps\": {0},", AudioBitrateKbps));
                sb.AppendLine(string.Format("  \"AggressiveRamTrim\": {0},", AggressiveRamTrim.ToString().ToLowerInvariant()));
                sb.AppendLine(string.Format("  \"EcoPreviewMode\": {0},", EcoPreviewMode.ToString().ToLowerInvariant()));
                sb.AppendLine(string.Format("  \"OutputFolder\": \"{0}\"", OutputFolder.Replace("\\", "\\\\")));
                sb.AppendLine("}");
                File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            }
            catch { }
        }

        public static RecorderConfig LoadFromFile(string path)
        {
            var cfg = new RecorderConfig();
            try
            {
                if (!File.Exists(path)) return cfg;
                string[] lines = File.ReadAllLines(path, Encoding.UTF8);
                foreach (string raw in lines)
                {
                    string line = raw.Trim().TrimEnd(',');
                    int colon = line.IndexOf(':');
                    if (colon <= 0) continue;
                    string key = line.Substring(0, colon).Trim().Trim('"');
                    string val = line.Substring(colon + 1).Trim().Trim('"');

                    switch (key)
                    {
                        case "Preset":
                            QualityPreset qp;
                            if (Enum.TryParse(val, out qp)) cfg.Preset = qp;
                            break;
                        case "CaptureMode":
                            DesktopCaptureMode cm;
                            if (Enum.TryParse(val, out cm)) cfg.CaptureMode = cm;
                            break;
                        case "Encoder":
                            VideoEncoderType enc;
                            if (Enum.TryParse(val, out enc)) cfg.Encoder = enc;
                            break;
                        case "DecoderPipeline":
                            DecoderColorPipeline dec;
                            if (Enum.TryParse(val, out dec)) cfg.DecoderPipeline = dec;
                            break;
                        case "RateControl":
                            RateControlMode rc;
                            if (Enum.TryParse(val, out rc)) cfg.RateControl = rc;
                            break;
                        case "UseNativeResolution":
                            cfg.UseNativeResolution = val.Equals("true", StringComparison.OrdinalIgnoreCase);
                            break;
                        case "TargetWidth":
                            int tw; if (int.TryParse(val, out tw)) cfg.TargetWidth = tw;
                            break;
                        case "TargetHeight":
                            int th; if (int.TryParse(val, out th)) cfg.TargetHeight = th;
                            break;
                        case "FPS":
                            int fps; if (int.TryParse(val, out fps)) cfg.FPS = fps;
                            break;
                        case "BitrateKbps":
                            int br; if (int.TryParse(val, out br)) cfg.BitrateKbps = br;
                            break;
                        case "QualityLevel":
                            int ql; if (int.TryParse(val, out ql)) cfg.QualityLevel = ql;
                            break;
                        case "MonitorIndex":
                            int mi; if (int.TryParse(val, out mi)) cfg.MonitorIndex = mi;
                            break;
                        case "RegionX":
                            int rx; if (int.TryParse(val, out rx)) cfg.RegionX = rx;
                            break;
                        case "RegionY":
                            int ry; if (int.TryParse(val, out ry)) cfg.RegionY = ry;
                            break;
                        case "RegionWidth":
                            int rw; if (int.TryParse(val, out rw)) cfg.RegionWidth = rw;
                            break;
                        case "RegionHeight":
                            int rh; if (int.TryParse(val, out rh)) cfg.RegionHeight = rh;
                            break;
                        case "CaptureCursor":
                            cfg.CaptureCursor = val.Equals("true", StringComparison.OrdinalIgnoreCase);
                            break;
                        case "HighlightMouseClicks":
                            cfg.HighlightMouseClicks = val.Equals("true", StringComparison.OrdinalIgnoreCase);
                            break;
                        case "CaptureDesktopAudio":
                            cfg.CaptureDesktopAudio = val.Equals("true", StringComparison.OrdinalIgnoreCase);
                            break;
                        case "CaptureMicrophone":
                            cfg.CaptureMicrophone = val.Equals("true", StringComparison.OrdinalIgnoreCase);
                            break;
                        case "MicrophoneDeviceName":
                            cfg.MicrophoneDeviceName = val ?? "";
                            break;
                        case "MicrophoneVolumePercent":
                            int mv; if (int.TryParse(val, out mv)) cfg.MicrophoneVolumePercent = mv;
                            break;
                        case "AudioBitrateKbps":
                            int ab; if (int.TryParse(val, out ab)) cfg.AudioBitrateKbps = ab;
                            break;
                        case "AggressiveRamTrim":
                            cfg.AggressiveRamTrim = val.Equals("true", StringComparison.OrdinalIgnoreCase);
                            break;
                        case "EcoPreviewMode":
                            cfg.EcoPreviewMode = val.Equals("true", StringComparison.OrdinalIgnoreCase);
                            break;
                        case "OutputFolder":
                            if (!string.IsNullOrWhiteSpace(val))
                                cfg.OutputFolder = val.Replace("\\\\", "\\");
                            break;
                    }
                }
            }
            catch { }
            return cfg;
        }
    }
}
