using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace LiteClip
{
    internal sealed class BufferedCanvas : Panel
    {
        public BufferedCanvas()
        {
            this.DoubleBuffered = true;
            this.SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.Opaque,
                true);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            // Handled inside OnPaint backbuffer to prevent any background erase flicker
        }
    }

    /// <summary>
    /// Minimalist vector header logo matching LiteClip.ico (L-viewfinder + capture lens).
    /// </summary>
    internal sealed class MinimalLogoControl : Control
    {
        public MinimalLogoControl()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Size = new Size(30, 30);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            float s = Math.Min(Width, Height);
            RectangleF disc = new RectangleF(1f, 1f, s - 2f, s - 2f);

            using (var bg = new LinearGradientBrush(disc, Color.FromArgb(22, 27, 38), Color.FromArgb(13, 16, 24), 45f))
            using (var ringPen = new Pen(Color.FromArgb(56, 189, 248), 1.8f))
            {
                g.FillEllipse(bg, disc);
                g.DrawEllipse(ringPen, 2f, 2f, s - 4f, s - 4f);
            }

            using (var lPen = new Pen(Color.FromArgb(56, 189, 248), 2.4f))
            using (var cPen = new Pen(Color.FromArgb(234, 237, 242), 1.9f))
            using (var coreBrush = new SolidBrush(Color.FromArgb(56, 189, 248)))
            {
                lPen.StartCap = LineCap.Round;
                lPen.EndCap = LineCap.Round;
                lPen.LineJoin = LineJoin.Round;

                cPen.StartCap = LineCap.Round;
                cPen.EndCap = LineCap.Round;
                cPen.LineJoin = LineJoin.Round;

                PointF[] lPts =
                {
                    new PointF(s * 0.31f, s * 0.31f),
                    new PointF(s * 0.31f, s * 0.69f),
                    new PointF(s * 0.69f, s * 0.69f)
                };
                g.DrawLines(lPen, lPts);

                PointF[] rPts =
                {
                    new PointF(s * 0.50f, s * 0.31f),
                    new PointF(s * 0.69f, s * 0.31f),
                    new PointF(s * 0.69f, s * 0.50f)
                };
                g.DrawLines(cPen, rPts);

                float r = s * 0.17f;
                g.FillEllipse(coreBrush, (s - r) * 0.5f, (s - r) * 0.5f, r, r);
            }
        }
    }

    /// <summary>
    /// FlashClip-matched DarkComboBox with neutral dark utility palette.
    /// </summary>
    internal sealed class DarkComboBox : ComboBox
    {
        private static readonly Color ControlBg = Color.FromArgb(34, 37, 44);
        private static readonly Color CardBg = Color.FromArgb(27, 29, 34);
        private static readonly Color CardBorder = Color.FromArgb(47, 51, 59);
        private static readonly Color PrimaryFill = Color.FromArgb(37, 74, 158);
        private static readonly Color PrimaryBorder = Color.FromArgb(58, 98, 188);
        private static readonly Color TextPrimary = Color.FromArgb(234, 237, 242);
        private static readonly Color TextMuted = Color.FromArgb(148, 154, 166);

        public DarkComboBox()
        {
            this.DropDownStyle = ComboBoxStyle.DropDownList;
            this.DrawMode = DrawMode.OwnerDrawFixed;
            this.FlatStyle = FlatStyle.Flat;
            this.BackColor = ControlBg;
            this.ForeColor = TextPrimary;
            this.ItemHeight = 22;
            this.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
        }

        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= Items.Count) return;

            bool selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            bool inComboBoxEdit = (e.State & DrawItemState.ComboBoxEdit) == DrawItemState.ComboBoxEdit;

            Color bg = inComboBoxEdit ? ControlBg : (selected ? PrimaryFill : CardBg);

            using (var b = new SolidBrush(bg))
            {
                e.Graphics.FillRectangle(b, e.Bounds);
            }

            string text = Items[e.Index].ToString();
            TextRenderer.DrawText(
                e.Graphics,
                text,
                this.Font,
                new Rectangle(e.Bounds.X + 7, e.Bounds.Y, e.Bounds.Width - 12, e.Bounds.Height),
                TextPrimary,
                bg,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            const int WM_PAINT = 0x000F;
            if (m.Msg == WM_PAINT)
            {
                using (Graphics g = Graphics.FromHwnd(this.Handle))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    Rectangle client = new Rectangle(0, 0, this.Width, this.Height);

                    using (var bgBrush = new SolidBrush(ControlBg))
                    {
                        g.FillRectangle(bgBrush, client);
                    }

                    string text = (this.SelectedIndex >= 0 && this.SelectedIndex < this.Items.Count)
                        ? this.Items[this.SelectedIndex].ToString()
                        : "";

                    TextRenderer.DrawText(
                        g,
                        text,
                        this.Font,
                        new Rectangle(8, 1, this.Width - 30, this.Height - 2),
                        TextPrimary,
                        ControlBg,
                        TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);

                    int cx = this.Width - 15;
                    int cy = this.Height / 2 - 2;
                    using (var arrowPen = new Pen(TextMuted, 1.6f))
                    {
                        g.DrawLine(arrowPen, cx - 4, cy, cx, cy + 4);
                        g.DrawLine(arrowPen, cx, cy + 4, cx + 4, cy);
                    }

                    Color borderCol = (this.Focused || this.DroppedDown) ? PrimaryBorder : CardBorder;
                    using (var borderPen = new Pen(borderCol, 1f))
                    {
                        g.DrawRectangle(borderPen, 0, 0, this.Width - 1, this.Height - 1);
                    }
                }
            }
        }
    }

    /// <summary>
    /// FlashClip-matched DarkCheckBox replacing bright white Win32 squares.
    /// </summary>
    internal sealed class DarkCheckBox : CheckBox
    {
        private static readonly Color CardBg = Color.FromArgb(27, 29, 34);
        private static readonly Color CardBorder = Color.FromArgb(47, 51, 59);
        private static readonly Color ControlBg = Color.FromArgb(34, 37, 44);
        private static readonly Color ControlHover = Color.FromArgb(43, 47, 56);
        private static readonly Color PrimaryFill = Color.FromArgb(37, 74, 158);
        private static readonly Color PrimaryBorder = Color.FromArgb(58, 98, 188);
        private static readonly Color TextPrimary = Color.FromArgb(234, 237, 242);

        private bool _hover;

        public DarkCheckBox()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            BackColor = CardBg;
            ForeColor = TextPrimary;
            Cursor = Cursors.Hand;
            Size = new Size(340, 22);
        }

        protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            Color panelBg = Parent != null ? Parent.BackColor : CardBg;
            g.Clear(panelBg);

            int boxSize = 15;
            int boxY = (Height - boxSize) / 2;
            var boxRect = new Rectangle(0, boxY, boxSize, boxSize);

            Color boxFill = Checked ? PrimaryFill : (_hover ? ControlHover : ControlBg);
            Color boxBorder = Checked ? PrimaryBorder : (_hover ? Color.FromArgb(72, 78, 92) : CardBorder);

            using (var fillBrush = new SolidBrush(boxFill))
            using (var borderPen = new Pen(boxBorder))
            {
                g.FillRectangle(fillBrush, boxRect);
                g.DrawRectangle(borderPen, boxRect);
            }

            if (Checked)
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (var checkPen = new Pen(Color.FromArgb(245, 247, 250), 1.8f))
                {
                    g.DrawLine(checkPen, 3, boxY + 8, 6, boxY + 11);
                    g.DrawLine(checkPen, 6, boxY + 11, 12, boxY + 4);
                }
                g.SmoothingMode = SmoothingMode.None;
            }

            TextRenderer.DrawText(
                g,
                Text,
                Font,
                new Rectangle(boxSize + 8, 0, Width - boxSize - 8, Height),
                ForeColor,
                panelBg,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
        }
    }

    public sealed class MainForm : Form
    {
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const int HOTKEY_ID_RECORD = 9001; // F9
        private const int HOTKEY_ID_PAUSE = 9002;  // F10

        private readonly string _configPath;
        private readonly RecorderConfig _config;
        private readonly RecorderCore _recorder;
        private bool _suppressUiEvents;

        // Top Bar Controls
        private Button _btnPresetBalanced;
        private Button _btnPresetHighQ;
        private Button _btnPresetUHQ;
        private Button _btnPresetUltraLight;
        private Button _btnPresetCustom;
        private Label _lblRamBadge;
        private Panel _ramMeterFill;

        // Left Stage: Preview & Transport
        private BufferedCanvas _previewCanvas;
        private Button _btnRecord;
        private Button _btnPause;
        private DarkCheckBox _chkEcoPreview;
        private DarkCheckBox _chkDisablePreview;
        private Label _lblStatusBanner;

        // Right Stage: Dark Custom Tab Buttons & Pages
        private readonly Button[] _tabButtons = new Button[4];
        private readonly Panel[] _tabPages = new Panel[4];

        // Right Stage: Customization Controls
        private DarkComboBox _cmbCaptureMode;
        private DarkComboBox _cmbMonitor;
        private DarkComboBox _cmbWindowList;
        private Button _btnRefreshWindows;
        private Button _btnPickRegion;
        private Label _lblRegionSummary;
        private DarkCheckBox _chkCaptureCursor;
        private DarkCheckBox _chkHighlightClicks;
        private DarkCheckBox _chkDesktopAudio;
        private DarkCheckBox _chkMicAudio;
        private DarkComboBox _cmbMicDevice;
        private TrackBar _trkMicVolume;
        private Label _lblMicVolVal;

        private DarkComboBox _cmbResolution;
        private DarkComboBox _cmbFps;
        private DarkComboBox _cmbRateControl;
        private TrackBar _trkBitrate;
        private NumericUpDown _numBitrate;
        private TrackBar _trkQuality;
        private Label _lblQualityVal;

        private DarkComboBox _cmbEncoder;
        private DarkComboBox _cmbDecoderPipeline;
        private DarkCheckBox _chkLowLatency;
        private DarkCheckBox _chkAggressiveRam;

        private TextBox _txtOutputFolder;
        private ListBox _lstClips;

        // Bottom Telemetry Bar
        private Label _lblTelemetryFps;
        private Label _lblTelemetryFrames;
        private Label _lblTelemetryBitrate;
        private Label _lblTelemetryTimer;
        private Label _lblTelemetryRam;

        private System.Windows.Forms.Timer _uiTimer;
        private int _tickCounter;

        // Exact FlashClip Neutral Dark Utility Palette
        private static readonly Color BgDark = Color.FromArgb(20, 21, 24);
        private static readonly Color BgPanel = Color.FromArgb(27, 29, 34);      // CardBg
        private static readonly Color BgCard = Color.FromArgb(34, 37, 44);       // ControlBg
        private static readonly Color BorderSubtle = Color.FromArgb(47, 51, 59); // CardBorder
        private static readonly Color PrimaryFill = Color.FromArgb(37, 74, 158);
        private static readonly Color PrimaryHover = Color.FromArgb(45, 86, 178);
        private static readonly Color PrimaryBorder = Color.FromArgb(58, 98, 188);
        private static readonly Color AccentSky = Color.FromArgb(56, 189, 248);  // FlashClip Icon Sky-Blue
        private static readonly Color StatusGreen = Color.FromArgb(62, 176, 118);
        private static readonly Color StatusAmber = Color.FromArgb(214, 162, 54);
        private static readonly Color StatusRed = Color.FromArgb(218, 88, 88);
        private static readonly Color TextPrimary = Color.FromArgb(234, 237, 242);
        private static readonly Color TextMuted = Color.FromArgb(148, 154, 166);

        public MainForm()
        {
            _configPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "LiteClip",
                "settings.json");

            _config = RecorderConfig.LoadFromFile(_configPath);
            Directory.CreateDirectory(_config.OutputFolder);

            _recorder = new RecorderCore(_config);
            _recorder.OnClipSaved += path =>
            {
                if (IsHandleCreated)
                {
                    BeginInvoke(new Action(() => RefreshClipsList(path)));
                }
            };
            _recorder.OnStatusMessage += msg =>
            {
                if (IsHandleCreated)
                {
                    BeginInvoke(new Action(() =>
                    {
                        if (_lblStatusBanner != null) _lblStatusBanner.Text = msg;
                    }));
                }
            };

            InitializeStudioUi();
            RefreshMonitorList();
            RefreshWindowList();
            RefreshMicList();
            SyncUiFromConfig();
            RefreshClipsList(null);

            _uiTimer = new System.Windows.Forms.Timer { Interval = 80 };
            _uiTimer.Tick += OnUiTimerTick;
            _uiTimer.Start();

            MemoryOptimizer.TrimProcessMemory(true);
        }

        private void InitializeStudioUi()
        {
            Text = "LiteClip";
            Size = new Size(1180, 740);
            MinimumSize = new Size(1040, 660);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = BgDark;
            ForeColor = TextPrimary;
            Font = new Font("Segoe UI", 9f, FontStyle.Regular);
            KeyPreview = true;
            try
            {
                this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch { }

            // 1. TOP PRESET & RAM EFFICIENCY BAR
            var topBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 56,
                BackColor = BgPanel,
                Padding = new Padding(14, 10, 14, 10)
            };
            topBar.Paint += (s, e) =>
            {
                using (var pen = new Pen(BorderSubtle, 1f))
                {
                    e.Graphics.DrawLine(pen, 0, topBar.Height - 1, topBar.Width, topBar.Height - 1);
                }
            };

            var logoMark = new MinimalLogoControl
            {
                Location = new Point(14, 13)
            };

            var lblBrand = new Label
            {
                Text = "LiteClip",
                Font = new Font("Segoe UI Semibold", 13f, FontStyle.Bold),
                ForeColor = TextPrimary,
                AutoSize = true,
                Location = new Point(50, 14),
                UseMnemonic = false
            };

            var lblPresetsTitle = new Label
            {
                Text = "PRESETS",
                Font = new Font("Segoe UI Semibold", 8f, FontStyle.Bold),
                ForeColor = TextMuted,
                AutoSize = true,
                Location = new Point(155, 21),
                UseMnemonic = false
            };

            _btnPresetBalanced = CreatePresetButton("Balanced", 220, () => SelectPreset(QualityPreset.Balanced));
            _btnPresetHighQ = CreatePresetButton("High Q", 310, () => SelectPreset(QualityPreset.HighQ));
            _btnPresetUHQ = CreatePresetButton("UHQ (Ultra)", 400, () => SelectPreset(QualityPreset.UHQ));
            _btnPresetUltraLight = CreatePresetButton("Ultra-Light", 504, () => SelectPreset(QualityPreset.UltraLight));
            _btnPresetCustom = CreatePresetButton("Custom", 604, () => SelectPreset(QualityPreset.Custom));

            // RAM vs OBS Widget on Top Right
            var ramBox = new Panel
            {
                Size = new Size(330, 36),
                Location = new Point(825, 10),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                BackColor = BgCard
            };
            ramBox.Paint += (s, e) =>
            {
                using (var pen = new Pen(BorderSubtle, 1f))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, ramBox.Width - 1, ramBox.Height - 1);
                }
            };
            _lblRamBadge = new Label
            {
                Text = "RAM: 24.0 MB (8.0% of OBS)",
                Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold),
                ForeColor = StatusGreen,
                Location = new Point(10, 4),
                Size = new Size(235, 18),
                UseMnemonic = false
            };
            var ramMeterBg = new Panel
            {
                Location = new Point(10, 25),
                Size = new Size(225, 5),
                BackColor = BgDark
            };
            _ramMeterFill = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(35, 5),
                BackColor = StatusGreen
            };
            ramMeterBg.Controls.Add(_ramMeterFill);

            var btnTrimNow = new Button
            {
                Text = "Trim RAM",
                Font = new Font("Segoe UI Semibold", 8f, FontStyle.Regular),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(43, 47, 56),
                ForeColor = TextPrimary,
                Location = new Point(245, 5),
                Size = new Size(76, 25),
                Cursor = Cursors.Hand,
                UseMnemonic = false
            };
            btnTrimNow.FlatAppearance.BorderSize = 1;
            btnTrimNow.FlatAppearance.BorderColor = BorderSubtle;
            btnTrimNow.Click += (s, e) =>
            {
                MemoryOptimizer.TrimProcessMemory(true);
                UpdateTelemetryBar();
            };

            ramBox.Controls.Add(_lblRamBadge);
            ramBox.Controls.Add(ramMeterBg);
            ramBox.Controls.Add(btnTrimNow);

            topBar.Controls.AddRange(new Control[]
            {
                logoMark, lblBrand, lblPresetsTitle,
                _btnPresetBalanced, _btnPresetHighQ, _btnPresetUHQ, _btnPresetUltraLight, _btnPresetCustom,
                ramBox
            });

            // 2. BOTTOM TELEMETRY STATUS BAR
            var bottomBar = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 32,
                BackColor = BgPanel,
                Padding = new Padding(14, 6, 14, 6)
            };
            bottomBar.Paint += (s, e) =>
            {
                using (var pen = new Pen(BorderSubtle, 1f))
                {
                    e.Graphics.DrawLine(pen, 0, 0, bottomBar.Width, 0);
                }
            };

            _lblTelemetryTimer = CreateTelemetryLabel("⏱ 00:00:00", 14, StatusGreen, true);
            _lblTelemetryFps = CreateTelemetryLabel("FPS: 60.0", 140, TextPrimary, false);
            _lblTelemetryFrames = CreateTelemetryLabel("Frames: 0 (Dropped: 0)", 250, TextMuted, false);
            _lblTelemetryBitrate = CreateTelemetryLabel("Bitrate: 12.0 Mbps", 450, TextPrimary, false);
            _lblTelemetryRam = CreateTelemetryLabel("Memory Footprint: ~24 MB vs OBS ~300 MB (8%)", 630, StatusGreen, false);

            bottomBar.Controls.AddRange(new Control[]
            {
                _lblTelemetryTimer, _lblTelemetryFps, _lblTelemetryFrames, _lblTelemetryBitrate, _lblTelemetryRam
            });

            // 3. MAIN SPLIT BODY: Left Preview & Transport + Right Dark-Themed Tabs
            var mainBody = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12)
            };

            var rightTabsPanel = new Panel
            {
                Dock = DockStyle.Right,
                Width = 435,
                BackColor = BgPanel,
                Padding = new Padding(10)
            };
            rightTabsPanel.Paint += (s, e) =>
            {
                using (var pen = new Pen(BorderSubtle, 1f))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, rightTabsPanel.Width - 1, rightTabsPanel.Height - 1);
                }
            };

            var leftStagePanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 0, 12, 0)
            };

            // Build Left Stage: Transport Dock at bottom + Preview Canvas filling center
            var transportDock = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 94,
                BackColor = BgPanel,
                Padding = new Padding(12)
            };
            transportDock.Paint += (s, e) =>
            {
                using (var pen = new Pen(BorderSubtle, 1f))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, transportDock.Width - 1, transportDock.Height - 1);
                }
            };

            _btnRecord = new Button
            {
                Text = "●  START RECORDING (F9)",
                Font = new Font("Segoe UI Semibold", 9.75f, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                BackColor = PrimaryFill,
                ForeColor = Color.FromArgb(245, 247, 250),
                Location = new Point(14, 12),
                Size = new Size(230, 40),
                Cursor = Cursors.Hand,
                UseMnemonic = false
            };
            _btnRecord.FlatAppearance.BorderSize = 1;
            _btnRecord.FlatAppearance.BorderColor = PrimaryBorder;
            _btnRecord.Click += (s, e) => ToggleRecording();

            _btnPause = new Button
            {
                Text = "❚❚ PAUSE (F10)",
                Font = new Font("Segoe UI Semibold", 9f, FontStyle.Regular),
                FlatStyle = FlatStyle.Flat,
                BackColor = BgCard,
                ForeColor = TextPrimary,
                Location = new Point(254, 12),
                Size = new Size(135, 40),
                Enabled = false,
                Cursor = Cursors.Hand,
                UseMnemonic = false
            };
            _btnPause.FlatAppearance.BorderSize = 1;
            _btnPause.FlatAppearance.BorderColor = BorderSubtle;
            _btnPause.Click += (s, e) =>
            {
                _recorder.TogglePause();
                UpdateTransportButtons();
            };

            _chkEcoPreview = new DarkCheckBox
            {
                Text = "Eco Preview (12 FPS Low-RAM)",
                Location = new Point(14, 62),
                Size = new Size(215, 22),
                Checked = _config.EcoPreviewMode
            };
            _chkEcoPreview.CheckedChanged += (s, e) =>
            {
                _config.EcoPreviewMode = _chkEcoPreview.Checked;
                _uiTimer.Interval = _config.EcoPreviewMode ? 80 : 33;
            };

            _chkDisablePreview = new DarkCheckBox
            {
                Text = "Zero-Preview Mode (Minimum GPU/RAM)",
                Location = new Point(240, 62),
                Size = new Size(265, 22),
                Checked = _config.DisablePreviewWhileRecording
            };
            _chkDisablePreview.CheckedChanged += (s, e) =>
            {
                _config.DisablePreviewWhileRecording = _chkDisablePreview.Checked;
                _previewCanvas.Invalidate();
            };

            _lblStatusBanner = new Label
            {
                Text = "Ready",
                ForeColor = StatusGreen,
                Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(405, 22),
                UseMnemonic = false
            };

            transportDock.Controls.AddRange(new Control[]
            {
                _btnRecord, _btnPause,
                _chkEcoPreview, _chkDisablePreview, _lblStatusBanner
            });

            _previewCanvas = new BufferedCanvas
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(14, 15, 18)
            };
            _previewCanvas.Paint += OnPreviewCanvasPaint;

            leftStagePanel.Controls.Add(_previewCanvas);
            leftStagePanel.Controls.Add(transportDock);

            // Build Right Stage: Sleek Dark Tab Header Strip + Dark Content Pages
            var tabStrip = new Panel
            {
                Dock = DockStyle.Top,
                Height = 36,
                BackColor = BgDark,
                Padding = new Padding(2)
            };

            string[] tabTitles = { "1. Capture Mode", "2. Video & Quality", "3. Codec & RAM", "4. Recordings" };
            int tabW = 103;
            for (int i = 0; i < 4; i++)
            {
                int idx = i;
                var btn = new Button
                {
                    Text = tabTitles[i],
                    FlatStyle = FlatStyle.Flat,
                    Location = new Point(2 + (i * tabW), 2),
                    Size = new Size(tabW - 2, 32),
                    Font = new Font("Segoe UI Semibold", 8.25f, FontStyle.Regular),
                    Cursor = Cursors.Hand,
                    UseMnemonic = false
                };
                btn.FlatAppearance.BorderSize = 1;
                btn.Click += (s, e) => SwitchRightTab(idx);
                _tabButtons[i] = btn;
                tabStrip.Controls.Add(btn);
            }

            var pageHost = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = BgPanel
            };

            _tabPages[0] = BuildCaptureTab();
            _tabPages[1] = BuildVideoQualityTab();
            _tabPages[2] = BuildEncoderDecoderTab();
            _tabPages[3] = BuildClipsAndOutputTab();

            for (int i = 0; i < 4; i++)
            {
                _tabPages[i].Dock = DockStyle.Fill;
                pageHost.Controls.Add(_tabPages[i]);
            }

            SwitchRightTab(0);

            rightTabsPanel.Controls.Add(pageHost);
            rightTabsPanel.Controls.Add(tabStrip);

            mainBody.Controls.Add(leftStagePanel);
            mainBody.Controls.Add(rightTabsPanel);

            Controls.Add(mainBody);
            Controls.Add(bottomBar);
            Controls.Add(topBar);
        }

        private void SwitchRightTab(int activeIndex)
        {
            for (int i = 0; i < 4; i++)
            {
                bool active = (i == activeIndex);
                _tabPages[i].Visible = active;
                _tabButtons[i].BackColor = active ? PrimaryFill : BgCard;
                _tabButtons[i].ForeColor = active ? TextPrimary : TextMuted;
                _tabButtons[i].FlatAppearance.BorderColor = active ? PrimaryBorder : BorderSubtle;
            }
        }

        private Panel BuildCaptureTab()
        {
            var page = new Panel { BackColor = BgPanel, ForeColor = TextPrimary, Padding = new Padding(10) };
            int y = 14;

            AddSectionHeader(page, "DESKTOP & GAME CAPTURING MODE", ref y);

            _cmbCaptureMode = new DarkComboBox
            {
                Location = new Point(12, y),
                Width = 390
            };
            _cmbCaptureMode.Items.AddRange(new object[]
            {
                "DXGI Desktop Duplication (GPU Direct Compositor)",
                "GDI Zero-Copy BitBlt (Ultra-Lightweight RAM)",
                "Window / Game Capture (Specific Application)",
                "Custom Screen Region (Interactive Rectangle)"
            });
            _cmbCaptureMode.SelectedIndexChanged += OnCaptureSettingChanged;
            page.Controls.Add(_cmbCaptureMode);
            y += 38;

            AddFieldLabel(page, "Target Display / Monitor:", 12, ref y);
            _cmbMonitor = new DarkComboBox
            {
                Location = new Point(12, y),
                Width = 390
            };
            _cmbMonitor.SelectedIndexChanged += OnCaptureSettingChanged;
            page.Controls.Add(_cmbMonitor);
            y += 38;

            AddFieldLabel(page, "Target Application / Game Window (for Window Capture):", 12, ref y);
            _cmbWindowList = new DarkComboBox
            {
                Location = new Point(12, y),
                Width = 304
            };
            _cmbWindowList.SelectedIndexChanged += OnWindowSelectionChanged;

            _btnRefreshWindows = CreateActionButton("↻ Refresh", 322, y, 80, 26, () => RefreshWindowList());
            page.Controls.Add(_cmbWindowList);
            page.Controls.Add(_btnRefreshWindows);
            y += 40;

            AddSectionHeader(page, "CUSTOM REGION & CURSOR OPTIONS", ref y);

            _btnPickRegion = CreateActionButton("Select Screen Region on Desktop...", 12, y, 246, 30, () => LaunchRegionPicker());
            _lblRegionSummary = new Label
            {
                Text = "1280x720 @ (100,100)",
                ForeColor = AccentSky,
                Location = new Point(268, y + 6),
                AutoSize = true,
                UseMnemonic = false
            };
            page.Controls.Add(_btnPickRegion);
            page.Controls.Add(_lblRegionSummary);
            y += 42;

            _chkCaptureCursor = new DarkCheckBox
            {
                Text = "Capture Hardware Mouse Cursor",
                Location = new Point(12, y),
                Size = new Size(380, 22),
                Checked = true
            };
            _chkCaptureCursor.CheckedChanged += OnCaptureSettingChanged;
            page.Controls.Add(_chkCaptureCursor);
            y += 25;

            _chkHighlightClicks = new DarkCheckBox
            {
                Text = "Highlight Mouse Clicks (Left/Right Click Ripples)",
                Location = new Point(12, y),
                Size = new Size(380, 22),
                Checked = true
            };
            _chkHighlightClicks.CheckedChanged += OnCaptureSettingChanged;
            page.Controls.Add(_chkHighlightClicks);
            y += 32;

            AddSectionHeader(page, "MICROPHONE & DESKTOP AUDIO", ref y);

            _chkDesktopAudio = new DarkCheckBox
            {
                Text = "Capture Desktop / Game Audio (WASAPI Loopback)",
                Location = new Point(12, y),
                Size = new Size(380, 22),
                Checked = true
            };
            _chkDesktopAudio.CheckedChanged += OnCaptureSettingChanged;
            page.Controls.Add(_chkDesktopAudio);
            y += 25;

            _chkMicAudio = new DarkCheckBox
            {
                Text = "Capture Microphone Input",
                Location = new Point(12, y),
                Size = new Size(380, 22),
                Checked = true
            };
            _chkMicAudio.CheckedChanged += OnCaptureSettingChanged;
            page.Controls.Add(_chkMicAudio);
            y += 28;

            AddFieldLabel(page, "Microphone Device:", 12, ref y);
            _cmbMicDevice = new DarkComboBox
            {
                Location = new Point(12, y),
                Width = 304
            };
            _cmbMicDevice.SelectedIndexChanged += OnCaptureSettingChanged;
            var btnRefreshMics = CreateActionButton("↻ Refresh", 322, y, 80, 26, () => RefreshMicList());
            page.Controls.Add(_cmbMicDevice);
            page.Controls.Add(btnRefreshMics);
            y += 34;

            AddFieldLabel(page, "Microphone Volume / Gain (0% - 200%):", 12, ref y);
            _trkMicVolume = new TrackBar
            {
                Minimum = 0,
                Maximum = 200,
                TickFrequency = 25,
                SmallChange = 5,
                LargeChange = 25,
                Value = 100,
                BackColor = BgPanel,
                Location = new Point(8, y - 2),
                Size = new Size(315, 32)
            };
            _lblMicVolVal = new Label
            {
                Text = "100%",
                ForeColor = AccentSky,
                Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold),
                Location = new Point(330, y + 2),
                AutoSize = true,
                UseMnemonic = false
            };
            _trkMicVolume.Scroll += (s, e) =>
            {
                _lblMicVolVal.Text = string.Format("{0}%", _trkMicVolume.Value);
                OnCaptureSettingChanged(s, e);
            };
            page.Controls.Add(_trkMicVolume);
            page.Controls.Add(_lblMicVolVal);

            return page;
        }

        private Panel BuildVideoQualityTab()
        {
            var page = new Panel { BackColor = BgPanel, ForeColor = TextPrimary, Padding = new Padding(10) };
            int y = 14;

            AddSectionHeader(page, "RESOLUTION & FRAMERATE", ref y);

            AddFieldLabel(page, "Output Video Resolution:", 12, ref y);
            _cmbResolution = new DarkComboBox
            {
                Location = new Point(12, y),
                Width = 390
            };
            _cmbResolution.Items.AddRange(new object[]
            {
                "Native Monitor Resolution (1:1 Zero-Scale)",
                "3840 x 2160 (4K UHD)",
                "2560 x 1440 (1440p QHD)",
                "1920 x 1080 (1080p Full HD)",
                "1600 x 900 (900p HD+)",
                "1280 x 720 (720p HD)",
                "854 x 480 (480p Fast SD)"
            });
            _cmbResolution.SelectedIndexChanged += OnVideoSettingChanged;
            page.Controls.Add(_cmbResolution);
            y += 38;

            AddFieldLabel(page, "Target Framerate (FPS):", 12, ref y);
            _cmbFps = new DarkComboBox
            {
                Location = new Point(12, y),
                Width = 390
            };
            _cmbFps.Items.AddRange(new object[]
            {
                "24 FPS (Cinematic)",
                "30 FPS (Standard Desktop)",
                "48 FPS (Smooth Film)",
                "60 FPS (High Smoothness Gaming)",
                "120 FPS (High-Refresh Esports)",
                "144 FPS (Ultra-Refresh Monitor)"
            });
            _cmbFps.SelectedIndexChanged += OnVideoSettingChanged;
            page.Controls.Add(_cmbFps);
            y += 42;

            AddSectionHeader(page, "BITRATE, RATE CONTROL & QUALITY", ref y);

            AddFieldLabel(page, "Rate Control Mode:", 12, ref y);
            _cmbRateControl = new DarkComboBox
            {
                Location = new Point(12, y),
                Width = 390
            };
            _cmbRateControl.Items.AddRange(new object[]
            {
                "CBR — Constant Bitrate (Stable Streaming/Clips)",
                "VBR Peak — Variable Bitrate (High Motion Detail)",
                "Quality-Based — Constant Visual Quality Factor"
            });
            _cmbRateControl.SelectedIndexChanged += OnVideoSettingChanged;
            page.Controls.Add(_cmbRateControl);
            y += 38;

            AddFieldLabel(page, "Target Video Bitrate (Kbps):", 12, ref y);
            _trkBitrate = new TrackBar
            {
                Minimum = 1000,
                Maximum = 80000,
                TickFrequency = 5000,
                SmallChange = 1000,
                LargeChange = 5000,
                Value = 12000,
                BackColor = BgPanel,
                Location = new Point(8, y),
                Size = new Size(280, 36)
            };
            _numBitrate = new NumericUpDown
            {
                Minimum = 1000,
                Maximum = 120000,
                Increment = 1000,
                Value = 12000,
                BackColor = BgCard,
                ForeColor = AccentSky,
                BorderStyle = BorderStyle.FixedSingle,
                Location = new Point(296, y + 2),
                Size = new Size(106, 26)
            };
            _trkBitrate.Scroll += (s, e) =>
            {
                _numBitrate.Value = _trkBitrate.Value;
                OnVideoSettingChanged(s, e);
            };
            _numBitrate.ValueChanged += (s, e) =>
            {
                int v = (int)Math.Min(_trkBitrate.Maximum, Math.Max(_trkBitrate.Minimum, _numBitrate.Value));
                _trkBitrate.Value = v;
                OnVideoSettingChanged(s, e);
            };
            page.Controls.Add(_trkBitrate);
            page.Controls.Add(_numBitrate);
            y += 42;

            AddFieldLabel(page, "Quantizer / Visual Quality Factor (1 - 100):", 12, ref y);
            _trkQuality = new TrackBar
            {
                Minimum = 20,
                Maximum = 100,
                TickFrequency = 10,
                Value = 78,
                BackColor = BgPanel,
                Location = new Point(8, y),
                Size = new Size(315, 36)
            };
            _lblQualityVal = new Label
            {
                Text = "78 / 100",
                ForeColor = AccentSky,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Location = new Point(328, y + 4),
                AutoSize = true
            };
            _trkQuality.Scroll += (s, e) =>
            {
                _lblQualityVal.Text = string.Format("{0} / 100", _trkQuality.Value);
                OnVideoSettingChanged(s, e);
            };
            page.Controls.Add(_trkQuality);
            page.Controls.Add(_lblQualityVal);

            return page;
        }

        private Panel BuildEncoderDecoderTab()
        {
            var page = new Panel { BackColor = BgPanel, ForeColor = TextPrimary, Padding = new Padding(10) };
            int y = 14;

            AddSectionHeader(page, "VIDEO ENCODER & DECODER PIPELINE", ref y);

            AddFieldLabel(page, "Video Encoder Backend (Hardware / Software):", 12, ref y);
            _cmbEncoder = new DarkComboBox
            {
                Location = new Point(12, y),
                Width = 390
            };
            _cmbEncoder.Items.AddRange(new object[]
            {
                "H.264 Hardware Accelerated (NVENC / AMF / QuickSync)",
                "HEVC / H.265 Hardware Accelerated (High Compression)",
                "H.264 Software MFT (CPU Multi-Threaded Fallback)",
                "Fast Low-Latency Stream (Minimum CPU Overhead)"
            });
            _cmbEncoder.SelectedIndexChanged += OnEncoderSettingChanged;
            page.Controls.Add(_cmbEncoder);
            y += 40;

            AddFieldLabel(page, "Decoder / Color Format Pipeline:", 12, ref y);
            _cmbDecoderPipeline = new DarkComboBox
            {
                Location = new Point(12, y),
                Width = 390
            };
            _cmbDecoderPipeline.Items.AddRange(new object[]
            {
                "RGB32 Direct (Full 32-bit BGRA -> Hardware Color MFT)",
                "NV12 Hardware 4:2:0 (50% Less RAM Bandwidth — Recommended)",
                "I420 Planar YUV (4:2:0 3-Plane Software/Hardware)",
                "RGB24 Compact Pipeline"
            });
            _cmbDecoderPipeline.SelectedIndexChanged += OnEncoderSettingChanged;
            page.Controls.Add(_cmbDecoderPipeline);
            y += 44;

            AddSectionHeader(page, "LIGHTWEIGHT MEMORY & LATENCY ARCHITECTURE", ref y);

            _chkLowLatency = new DarkCheckBox
            {
                Text = "Enable Hardware Low-Latency Pipeline (MF_LOW_LATENCY)",
                Location = new Point(12, y),
                Size = new Size(380, 22),
                Checked = true
            };
            _chkLowLatency.CheckedChanged += OnEncoderSettingChanged;
            page.Controls.Add(_chkLowLatency);
            y += 28;

            _chkAggressiveRam = new DarkCheckBox
            {
                Text = "Aggressive Working-Set Trimmer (Keep RAM at ~15% of OBS)",
                Location = new Point(12, y),
                Size = new Size(380, 22),
                Checked = true
            };
            _chkAggressiveRam.CheckedChanged += OnEncoderSettingChanged;
            page.Controls.Add(_chkAggressiveRam);
            y += 38;

            var archBox = new Label
            {
                Text = "Why LiteClip uses ~15% of OBS Studio's RAM:\n" +
                       "• 4-Buffer Ring Pool: Only 4 media samples are ever allocated and recycled across millions of frames.\n" +
                       "• Zero Managed Bitmaps: GDI/DXGI writes straight into unmanaged memory pointers.\n" +
                       "• NV12 Direct Parallel Converter: Halves frame buffer size before hitting the encoder.",
                ForeColor = TextMuted,
                BackColor = BgCard,
                Padding = new Padding(10),
                Location = new Point(12, y),
                Size = new Size(390, 130),
                UseMnemonic = false
            };
            page.Controls.Add(archBox);

            return page;
        }

        private Panel BuildClipsAndOutputTab()
        {
            var page = new Panel { BackColor = BgPanel, ForeColor = TextPrimary, Padding = new Padding(10) };
            int y = 14;

            AddSectionHeader(page, "OUTPUT FOLDER & GLOBAL HOTKEYS", ref y);

            _txtOutputFolder = new TextBox
            {
                Text = _config.OutputFolder,
                BackColor = BgCard,
                ForeColor = TextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Location = new Point(12, y),
                Width = 300
            };
            var btnOpenDir = CreateActionButton("Open Folder", 318, y - 1, 84, 25, () =>
            {
                try
                {
                    Directory.CreateDirectory(_txtOutputFolder.Text);
                    Process.Start("explorer.exe", _txtOutputFolder.Text);
                }
                catch { }
            });
            page.Controls.Add(_txtOutputFolder);
            page.Controls.Add(btnOpenDir);
            y += 34;

            var lblHotkeys = new Label
            {
                Text = "Hotkeys:  [F9] Start/Stop Recording   •   [F10] Pause/Resume",
                ForeColor = StatusAmber,
                Location = new Point(12, y),
                AutoSize = true,
                UseMnemonic = false
            };
            page.Controls.Add(lblHotkeys);
            y += 28;

            AddSectionHeader(page, "RECORDED VIDEOS (DOUBLE-CLICK TO PLAY)", ref y);

            _lstClips = new ListBox
            {
                BackColor = BgCard,
                ForeColor = TextPrimary,
                BorderStyle = BorderStyle.FixedSingle,
                Location = new Point(12, y),
                Size = new Size(390, 215)
            };
            _lstClips.DoubleClick += (s, e) => PlaySelectedClip();
            page.Controls.Add(_lstClips);
            y += 224;

            var btnPlay = CreateActionButton("▶ Play Selected Video", 12, y, 190, 30, () => PlaySelectedClip());
            var btnRefreshClips = CreateActionButton("↻ Refresh List", 212, y, 190, 30, () => RefreshClipsList(null));
            page.Controls.Add(btnPlay);
            page.Controls.Add(btnRefreshClips);

            return page;
        }

        private void SelectPreset(QualityPreset preset)
        {
            if (_recorder.IsRecording)
            {
                MessageBox.Show("Stop recording before switching presets.", "LiteClip", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _config.ApplyPreset(preset);
            SyncUiFromConfig();
            _recorder.UpdatePreviewConfig(_config);
            _config.SaveToFile(_configPath);
        }

        private void SyncUiFromConfig()
        {
            _suppressUiEvents = true;
            try
            {
                HighlightPresetButton(_config.Preset);

                _cmbCaptureMode.SelectedIndex = (int)_config.CaptureMode;
                _chkCaptureCursor.Checked = _config.CaptureCursor;
                _chkHighlightClicks.Checked = _config.HighlightMouseClicks;
                _chkDesktopAudio.Checked = _config.CaptureDesktopAudio;
                _chkMicAudio.Checked = _config.CaptureMicrophone;

                int mv = Math.Max(_trkMicVolume.Minimum, Math.Min(_trkMicVolume.Maximum, _config.MicrophoneVolumePercent));
                _trkMicVolume.Value = mv;
                _lblMicVolVal.Text = string.Format("{0}%", mv);

                _lblRegionSummary.Text = string.Format("{0}x{1} @ ({2},{3})",
                    _config.RegionWidth, _config.RegionHeight, _config.RegionX, _config.RegionY);

                // Resolution
                if (_config.UseNativeResolution)
                    _cmbResolution.SelectedIndex = 0;
                else if (_config.TargetWidth == 3840)
                    _cmbResolution.SelectedIndex = 1;
                else if (_config.TargetWidth == 2560)
                    _cmbResolution.SelectedIndex = 2;
                else if (_config.TargetWidth == 1920)
                    _cmbResolution.SelectedIndex = 3;
                else if (_config.TargetWidth == 1600)
                    _cmbResolution.SelectedIndex = 4;
                else if (_config.TargetWidth == 1280)
                    _cmbResolution.SelectedIndex = 5;
                else
                    _cmbResolution.SelectedIndex = 6;

                // FPS
                switch (_config.FPS)
                {
                    case 24: _cmbFps.SelectedIndex = 0; break;
                    case 30: _cmbFps.SelectedIndex = 1; break;
                    case 48: _cmbFps.SelectedIndex = 2; break;
                    case 60: _cmbFps.SelectedIndex = 3; break;
                    case 120: _cmbFps.SelectedIndex = 4; break;
                    case 144: _cmbFps.SelectedIndex = 5; break;
                    default: _cmbFps.SelectedIndex = 3; break;
                }

                _cmbRateControl.SelectedIndex = (int)_config.RateControl;
                int br = Math.Max((int)_numBitrate.Minimum, Math.Min((int)_numBitrate.Maximum, _config.BitrateKbps));
                _numBitrate.Value = br;
                _trkBitrate.Value = Math.Min(_trkBitrate.Maximum, br);

                int ql = Math.Max(_trkQuality.Minimum, Math.Min(_trkQuality.Maximum, _config.QualityLevel));
                _trkQuality.Value = ql;
                _lblQualityVal.Text = string.Format("{0} / 100", ql);

                _cmbEncoder.SelectedIndex = (int)_config.Encoder;
                _cmbDecoderPipeline.SelectedIndex = (int)_config.DecoderPipeline;
                _chkLowLatency.Checked = _config.LowLatencyMode;
                _chkAggressiveRam.Checked = _config.AggressiveRamTrim;
            }
            finally
            {
                _suppressUiEvents = false;
            }
        }

        private void HighlightPresetButton(QualityPreset active)
        {
            SetPresetBtnStyle(_btnPresetBalanced, active == QualityPreset.Balanced);
            SetPresetBtnStyle(_btnPresetHighQ, active == QualityPreset.HighQ);
            SetPresetBtnStyle(_btnPresetUHQ, active == QualityPreset.UHQ);
            SetPresetBtnStyle(_btnPresetUltraLight, active == QualityPreset.UltraLight);
            SetPresetBtnStyle(_btnPresetCustom, active == QualityPreset.Custom);
        }

        private static void SetPresetBtnStyle(Button btn, bool active)
        {
            if (btn == null) return;
            btn.BackColor = active ? PrimaryFill : BgCard;
            btn.ForeColor = active ? Color.FromArgb(245, 247, 250) : TextPrimary;
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = active ? PrimaryBorder : BorderSubtle;
            btn.Font = new Font("Segoe UI", 8.5f, active ? FontStyle.Bold : FontStyle.Regular);
        }

        private void OnCaptureSettingChanged(object sender, EventArgs e)
        {
            if (_suppressUiEvents) return;
            _config.CaptureMode = (DesktopCaptureMode)Math.Max(0, _cmbCaptureMode.SelectedIndex);
            _config.MonitorIndex = Math.Max(0, _cmbMonitor.SelectedIndex);
            _config.CaptureCursor = _chkCaptureCursor.Checked;
            _config.HighlightMouseClicks = _chkHighlightClicks.Checked;
            _config.CaptureDesktopAudio = _chkDesktopAudio.Checked;
            _config.CaptureMicrophone = _chkMicAudio.Checked;
            if (_cmbMicDevice.SelectedItem != null)
            {
                _config.MicrophoneDeviceName = _cmbMicDevice.SelectedItem.ToString();
            }
            _config.MicrophoneVolumePercent = _trkMicVolume.Value;
            _config.Preset = QualityPreset.Custom;
            HighlightPresetButton(QualityPreset.Custom);
            _recorder.UpdatePreviewConfig(_config);
            _config.SaveToFile(_configPath);
        }

        private void OnWindowSelectionChanged(object sender, EventArgs e)
        {
            if (_suppressUiEvents) return;
            var info = _cmbWindowList.SelectedItem as WindowTargetInfo;
            if (info != null)
            {
                _config.TargetWindowHandle = info.Handle;
                _config.TargetWindowTitle = info.Title;
                if (_config.CaptureMode == DesktopCaptureMode.WindowCapture)
                {
                    _recorder.UpdatePreviewConfig(_config);
                }
            }
        }

        private void OnVideoSettingChanged(object sender, EventArgs e)
        {
            if (_suppressUiEvents) return;

            switch (_cmbResolution.SelectedIndex)
            {
                case 0: _config.UseNativeResolution = true; break;
                case 1: _config.UseNativeResolution = false; _config.TargetWidth = 3840; _config.TargetHeight = 2160; break;
                case 2: _config.UseNativeResolution = false; _config.TargetWidth = 2560; _config.TargetHeight = 1440; break;
                case 3: _config.UseNativeResolution = false; _config.TargetWidth = 1920; _config.TargetHeight = 1080; break;
                case 4: _config.UseNativeResolution = false; _config.TargetWidth = 1600; _config.TargetHeight = 900; break;
                case 5: _config.UseNativeResolution = false; _config.TargetWidth = 1280; _config.TargetHeight = 720; break;
                case 6: _config.UseNativeResolution = false; _config.TargetWidth = 854; _config.TargetHeight = 480; break;
            }

            int[] fpsTable = { 24, 30, 48, 60, 120, 144 };
            if (_cmbFps.SelectedIndex >= 0 && _cmbFps.SelectedIndex < fpsTable.Length)
            {
                _config.FPS = fpsTable[_cmbFps.SelectedIndex];
            }

            _config.RateControl = (RateControlMode)Math.Max(0, _cmbRateControl.SelectedIndex);
            _config.BitrateKbps = (int)_numBitrate.Value;
            _config.QualityLevel = _trkQuality.Value;
            _config.Preset = QualityPreset.Custom;
            HighlightPresetButton(QualityPreset.Custom);

            _recorder.UpdatePreviewConfig(_config);
            _config.SaveToFile(_configPath);
        }

        private void OnEncoderSettingChanged(object sender, EventArgs e)
        {
            if (_suppressUiEvents) return;
            _config.Encoder = (VideoEncoderType)Math.Max(0, _cmbEncoder.SelectedIndex);
            _config.DecoderPipeline = (DecoderColorPipeline)Math.Max(0, _cmbDecoderPipeline.SelectedIndex);
            _config.LowLatencyMode = _chkLowLatency.Checked;
            _config.AggressiveRamTrim = _chkAggressiveRam.Checked;
            _config.Preset = QualityPreset.Custom;
            HighlightPresetButton(QualityPreset.Custom);
            _config.SaveToFile(_configPath);
        }

        private void LaunchRegionPicker()
        {
            var initial = new Rectangle(_config.RegionX, _config.RegionY, _config.RegionWidth, _config.RegionHeight);
            using (var overlay = new RegionSelectorOverlay(initial))
            {
                if (overlay.ShowDialog(this) == DialogResult.OK)
                {
                    Rectangle r = overlay.SelectedRegion;
                    _config.RegionX = r.X;
                    _config.RegionY = r.Y;
                    _config.RegionWidth = r.Width;
                    _config.RegionHeight = r.Height;
                    _config.CaptureMode = DesktopCaptureMode.CustomRegion;
                    SyncUiFromConfig();
                    _recorder.UpdatePreviewConfig(_config);
                    _config.SaveToFile(_configPath);
                }
            }
        }

        private void ToggleRecording()
        {
            if (!_recorder.IsRecording)
            {
                _config.OutputFolder = _txtOutputFolder.Text.Trim();
                _recorder.StartRecording(_config);
            }
            else
            {
                _recorder.StopRecording();
                _lblStatusBanner.Text = "Ready";
            }
            UpdateTransportButtons();
        }

        private void UpdateTransportButtons()
        {
            if (_recorder.IsRecording)
            {
                _btnRecord.Text = "■  STOP RECORDING (F9)";
                _btnRecord.BackColor = StatusRed;
                _btnRecord.FlatAppearance.BorderColor = Color.FromArgb(235, 110, 110);
                _btnPause.Enabled = true;
                _btnPause.Text = _recorder.IsPaused ? "▶ RESUME (F10)" : "❚❚ PAUSE (F10)";
            }
            else
            {
                _btnRecord.Text = "●  START RECORDING (F9)";
                _btnRecord.BackColor = PrimaryFill;
                _btnRecord.FlatAppearance.BorderColor = PrimaryBorder;
                _btnPause.Enabled = false;
                _btnPause.Text = "❚❚ PAUSE (F10)";
            }
        }

        private void OnUiTimerTick(object sender, EventArgs e)
        {
            _tickCounter++;

            bool skipPreview = _config.DisablePreviewWhileRecording;
            if (!skipPreview)
            {
                if (!_recorder.IsRecording)
                {
                    _recorder.StepIdlePreview();
                }
                _previewCanvas.Invalidate();
            }

            if ((_tickCounter & 3) == 0)
            {
                UpdateTelemetryBar();
            }
        }

        private void UpdateTelemetryBar()
        {
            RecorderTelemetry t = _recorder.GetTelemetry();
            var inv = System.Globalization.CultureInfo.InvariantCulture;
            _lblTelemetryTimer.Text = string.Format(inv, "{0} {1:00}:{2:00}:{3:00}",
                t.IsRecording ? (t.IsPaused ? "❚❚" : "● REC") : "⏱ IDLE",
                (int)t.Elapsed.TotalHours, t.Elapsed.Minutes, t.Elapsed.Seconds);
            _lblTelemetryTimer.ForeColor = t.IsRecording ? StatusRed : StatusGreen;

            _lblTelemetryFps.Text = string.Format(inv, "FPS: {0:F1}", t.CurrentFps);
            _lblTelemetryFrames.Text = string.Format(inv, "Frames: {0} (Dropped: {1})", t.EncodedFrames, t.DroppedFrames);
            _lblTelemetryBitrate.Text = string.Format(inv, "Bitrate: {0:F1} Mbps", t.ActualBitrateMbps);

            string ramText = string.Format(inv, "RAM: {0:F1} MB ({1:F1}% of OBS)", t.WorkingSetMB, t.ObsComparisonPercent);
            _lblRamBadge.Text = ramText;
            _lblTelemetryRam.Text = string.Format(inv, "Memory: {0:F1} MB Working Set vs OBS ~300 MB ({1:F1}%)", t.WorkingSetMB, t.ObsComparisonPercent);

            int barWidth = (int)Math.Max(8, Math.Min(225, (t.ObsComparisonPercent / 100.0) * 225.0));
            _ramMeterFill.Width = barWidth;
            _ramMeterFill.BackColor = (t.ObsComparisonPercent <= 25.0) ? StatusGreen : StatusAmber;
        }

        private void OnPreviewCanvasPaint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            int cw = _previewCanvas.ClientSize.Width;
            int ch = _previewCanvas.ClientSize.Height;
            if (cw <= 10 || ch <= 10) return;

            g.Clear(Color.FromArgb(14, 15, 18));

            ZeroCopyCaptureEngine eng = _recorder.ActiveCaptureEngine;
            if (!_config.DisablePreviewWhileRecording && eng != null && eng.Width > 0 && eng.Height > 0)
            {
                float scale = Math.Min((float)(cw - 16) / eng.Width, (float)(ch - 16) / eng.Height);
                int drawW = Math.Max(64, (int)(eng.Width * scale));
                int drawH = Math.Max(36, (int)(eng.Height * scale));
                int drawX = (cw - drawW) / 2;
                int drawY = (ch - drawH) / 2;

                eng.DrawPreviewToGraphics(g, drawX, drawY, drawW, drawH);

                using (var borderPen = new Pen(_recorder.IsRecording ? StatusRed : BorderSubtle, 1f))
                {
                    g.DrawRectangle(borderPen, drawX - 1, drawY - 1, drawW + 1, drawH + 1);
                }
            }
            else
            {
                using (var font = new Font("Segoe UI Semibold", 10.5f))
                {
                    g.DrawString("Zero-Preview Mode Active (0 GPU/UI Overhead)", font, Brushes.Gray, 24, ch / 2 - 12);
                }
            }

            // Draw HUD Overlay pill in top-left of preview
            using (var hudBrush = new SolidBrush(Color.FromArgb(205, 20, 21, 24)))
            using (var hudBorder = new Pen(BorderSubtle, 1f))
            using (var hudFont = new Font("Segoe UI Semibold", 8.5f))
            {
                int w = (eng != null) ? eng.Width : _config.TargetWidth;
                int h = (eng != null) ? eng.Height : _config.TargetHeight;
                string hudText = string.Format("{0}  |  {1}x{2} @ {3} FPS  |  {4}  |  {5}  |  Preset: {6}",
                    _recorder.IsRecording ? "● RECORDING" : "READY",
                    w, h, _config.FPS,
                    _config.CaptureMode,
                    _config.DecoderPipeline,
                    _config.Preset);

                SizeF sz = g.MeasureString(hudText, hudFont);
                RectangleF pill = new RectangleF(14, 14, sz.Width + 18, 25);
                g.FillRectangle(hudBrush, pill);
                g.DrawRectangle(hudBorder, 14, 14, (int)sz.Width + 18, 25);
                g.DrawString(hudText, hudFont, _recorder.IsRecording ? Brushes.RecRedBrush : Brushes.TextPrimaryBrush, 23, 18);
            }
        }

        private static class Brushes
        {
            public static readonly Brush Gray = new SolidBrush(TextMuted);
            public static readonly Brush TextPrimaryBrush = new SolidBrush(TextPrimary);
            public static readonly Brush RecRedBrush = new SolidBrush(StatusRed);
        }

        private void RefreshMonitorList()
        {
            _suppressUiEvents = true;
            try
            {
                _cmbMonitor.Items.Clear();
                Screen[] screens = Screen.AllScreens;
                for (int i = 0; i < screens.Length; i++)
                {
                    Screen s = screens[i];
                    _cmbMonitor.Items.Add(string.Format("Display #{0}: {1} ({2}x{3}){4}",
                        i + 1, s.DeviceName, s.Bounds.Width, s.Bounds.Height, s.Primary ? " [Primary]" : ""));
                }
                if (_cmbMonitor.Items.Count > 0)
                {
                    _cmbMonitor.SelectedIndex = Math.Min(_config.MonitorIndex, _cmbMonitor.Items.Count - 1);
                }
            }
            finally
            {
                _suppressUiEvents = false;
            }
        }

        private void RefreshWindowList()
        {
            var windows = ZeroCopyCaptureEngine.GetVisibleWindows();
            _cmbWindowList.Items.Clear();
            foreach (var w in windows)
            {
                _cmbWindowList.Items.Add(w);
            }
            if (_cmbWindowList.Items.Count > 0)
            {
                _cmbWindowList.SelectedIndex = 0;
            }
        }

        private void RefreshMicList()
        {
            _suppressUiEvents = true;
            try
            {
                var mics = WasapiAudioMixer.GetAvailableMicrophones();
                _cmbMicDevice.Items.Clear();
                int matchIdx = 0;
                for (int i = 0; i < mics.Count; i++)
                {
                    _cmbMicDevice.Items.Add(mics[i]);
                    if (!string.IsNullOrEmpty(_config.MicrophoneDeviceName) &&
                        string.Equals(mics[i], _config.MicrophoneDeviceName, StringComparison.OrdinalIgnoreCase))
                    {
                        matchIdx = i;
                    }
                    else if (string.IsNullOrEmpty(_config.MicrophoneDeviceName) &&
                             mics[i].IndexOf("G435", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        matchIdx = i;
                    }
                }
                if (_cmbMicDevice.Items.Count > 0)
                {
                    _cmbMicDevice.SelectedIndex = matchIdx;
                    _config.MicrophoneDeviceName = _cmbMicDevice.SelectedItem.ToString();
                }
            }
            finally
            {
                _suppressUiEvents = false;
            }
        }

        private void RefreshClipsList(string highlightPath)
        {
            if (_lstClips == null) return;
            _lstClips.Items.Clear();
            try
            {
                if (!Directory.Exists(_config.OutputFolder)) return;
                var dir = new DirectoryInfo(_config.OutputFolder);
                FileInfo[] files = dir.GetFiles("*.mp4");
                Array.Sort(files, (a, b) => b.LastWriteTime.CompareTo(a.LastWriteTime));
                foreach (FileInfo fi in files)
                {
                    double mb = fi.Length / (1024.0 * 1024.0);
                    _lstClips.Items.Add(string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}   ({1:F2} MB)", fi.Name, mb));
                }
                if (_lstClips.Items.Count > 0)
                {
                    _lstClips.SelectedIndex = 0;
                }
            }
            catch { }
        }

        private void PlaySelectedClip()
        {
            if (_lstClips.SelectedItem == null) return;
            string item = _lstClips.SelectedItem.ToString();
            int idx = item.IndexOf("   (");
            string fileName = (idx > 0) ? item.Substring(0, idx) : item;
            string fullPath = Path.Combine(_config.OutputFolder, fileName);
            if (File.Exists(fullPath))
            {
                try { Process.Start(fullPath); } catch { }
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            RegisterHotKey(this.Handle, HOTKEY_ID_RECORD, 0, (uint)Keys.F9);
            RegisterHotKey(this.Handle, HOTKEY_ID_PAUSE, 0, (uint)Keys.F10);
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_HOTKEY = 0x0312;
            if (m.Msg == WM_HOTKEY)
            {
                int id = m.WParam.ToInt32();
                if (id == HOTKEY_ID_RECORD)
                {
                    ToggleRecording();
                }
                else if (id == HOTKEY_ID_PAUSE)
                {
                    _recorder.TogglePause();
                    UpdateTransportButtons();
                }
            }
            base.WndProc(ref m);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _uiTimer.Stop();
            UnregisterHotKey(this.Handle, HOTKEY_ID_RECORD);
            UnregisterHotKey(this.Handle, HOTKEY_ID_PAUSE);
            _config.SaveToFile(_configPath);
            _recorder.Dispose();
            base.OnFormClosing(e);
        }

        private static Button CreatePresetButton(string text, int x, Action onClick)
        {
            var btn = new Button
            {
                Text = text,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(x, 12),
                Size = new Size(text.Length > 9 ? 96 : 82, 30),
                BackColor = BgCard,
                ForeColor = TextPrimary,
                Cursor = Cursors.Hand,
                UseMnemonic = false
            };
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = BorderSubtle;
            btn.Click += (s, e) => onClick();
            return btn;
        }

        private static Button CreateActionButton(string text, int x, int y, int w, int h, Action onClick)
        {
            var btn = new Button
            {
                Text = text,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(x, y),
                Size = new Size(w, h),
                BackColor = BgCard,
                ForeColor = TextPrimary,
                Cursor = Cursors.Hand,
                UseMnemonic = false
            };
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = BorderSubtle;
            btn.Click += (s, e) => onClick();
            return btn;
        }

        private static Label CreateTelemetryLabel(string text, int x, Color color, bool bold)
        {
            return new Label
            {
                Text = text,
                ForeColor = color,
                Font = new Font("Segoe UI", 8.75f, bold ? FontStyle.Bold : FontStyle.Regular),
                AutoSize = true,
                Location = new Point(x, 7),
                UseMnemonic = false
            };
        }

        private static void AddSectionHeader(Control parent, string title, ref int y)
        {
            var lbl = new Label
            {
                Text = title,
                Font = new Font("Segoe UI Semibold", 8f, FontStyle.Bold),
                ForeColor = AccentSky,
                AutoSize = true,
                Location = new Point(12, y),
                UseMnemonic = false
            };
            parent.Controls.Add(lbl);
            y += 22;
        }

        private static void AddFieldLabel(Control parent, string text, int x, ref int y)
        {
            var lbl = new Label
            {
                Text = text,
                ForeColor = TextMuted,
                AutoSize = true,
                Location = new Point(x, y),
                UseMnemonic = false
            };
            parent.Controls.Add(lbl);
            y += 20;
        }
    }
}
