using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace LiteClip
{
    internal static class Program
    {
        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        [STAThread]
        private static int Main(string[] args)
        {
            try { SetProcessDPIAware(); } catch { }

            if (args != null && args.Length > 0 && args[0] == "--self-test")
            {
                return RunAutomatedSelfTest();
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
            return 0;
        }

        private static int RunAutomatedSelfTest()
        {
            Console.WriteLine("=== LiteClip Engine Self-Test & RAM Benchmark ===");
            var cfg = new RecorderConfig();
            cfg.ApplyPreset(QualityPreset.Balanced);

            string testOutDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestRecordings");
            Directory.CreateDirectory(testOutDir);
            cfg.OutputFolder = testOutDir;

            string testMp4 = Path.Combine(testOutDir, "SelfTest_Balanced_1080p60.mp4");
            if (File.Exists(testMp4)) File.Delete(testMp4);

            using (var recorder = new RecorderCore(cfg))
            {
                recorder.OnStatusMessage += msg => Console.WriteLine("[STATUS] " + msg);
                bool started = recorder.StartRecording(cfg, testMp4);
                if (!started)
                {
                    Console.WriteLine("[ERROR] Failed to start recording.");
                    return 1;
                }

                for (int i = 0; i < 4; i++)
                {
                    Thread.Sleep(500);
                    RecorderTelemetry t = recorder.GetTelemetry();
                    Console.WriteLine(string.Format(
                        System.Globalization.CultureInfo.InvariantCulture,
                        "[TELEMETRY t={0:F1}s] Frames={1} | FPS={2:F1} | WorkingSet={3:F2} MB ({4:F1}% of OBS 300MB) | Private={5:F2} MB",
                        (i + 1) * 0.5, t.EncodedFrames, t.CurrentFps, t.WorkingSetMB, t.ObsComparisonPercent, t.PrivateBytesMB));
                }

                string saved = recorder.StopRecording();
                if (!File.Exists(testMp4))
                {
                    Console.WriteLine("[ERROR] Output MP4 was not created.");
                    return 2;
                }

                long fileBytes = new FileInfo(testMp4).Length;
                Console.WriteLine(string.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    "[SUCCESS] Output file: {0} ({1:F2} KB)", testMp4, fileBytes / 1024.0));
                return (fileBytes > 1024) ? 0 : 3;
            }
        }
    }
}
