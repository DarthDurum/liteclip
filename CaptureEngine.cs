using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LiteClip
{
    public sealed class WindowTargetInfo
    {
        public IntPtr Handle { get; set; }
        public string Title { get; set; }
        public Rectangle Bounds { get; set; }

        public override string ToString()
        {
            return string.Format("{0}  [{1}x{2}]", Title, Bounds.Width, Bounds.Height);
        }
    }

    internal sealed unsafe class ZeroCopyCaptureEngine : IDisposable
    {
        #region Win32 P/Invoke
        [DllImport("user32.dll")]
        private static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, uint nFlags);

        [DllImport("user32.dll")]
        private static extern bool GetCursorInfo(ref CURSORINFO pci);

        [DllImport("user32.dll")]
        private static extern bool DrawIconEx(IntPtr hdc, int xLeft, int yTop, IntPtr hIcon, int cxWidth, int cyWidth, uint istepIfAniCur, IntPtr hbrFlickerFreeDraw, uint diFlags);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        [DllImport("gdi32.dll")]
        private static extern bool BitBlt(IntPtr hdcDest, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, int dwRop);

        [DllImport("gdi32.dll")]
        private static extern bool StretchBlt(IntPtr hdcDest, int nXOriginDest, int nYOriginDest, int nWidthDest, int nHeightDest, IntPtr hdcSrc, int nXOriginSrc, int nYOriginSrc, int nWidthSrc, int nHeightSrc, int dwRop);

        [DllImport("gdi32.dll")]
        private static extern int SetStretchBltMode(IntPtr hdc, int iStretchMode);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateDIBSection(IntPtr hdc, ref BITMAPINFO pbmi, uint iUsage, out IntPtr ppvBits, IntPtr hSection, uint dwOffset);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreatePen(int fnPenStyle, int nWidth, uint crColor);

        [DllImport("gdi32.dll")]
        private static extern IntPtr GetStockObject(int fnObject);

        [DllImport("gdi32.dll")]
        private static extern bool Ellipse(IntPtr hdc, int nLeftRect, int nTopRect, int nRightRect, int nBottomRect);

        private const int SRCCOPY = 0x00CC0020;
        private const int CAPTUREBLT = 0x40000000;
        private const int COLORONCOLOR = 3;
        private const int HALFTONE = 4;
        private const uint PW_RENDERFULLCONTENT = 0x00000002;
        private const int CURSOR_SHOWING = 0x00000001;
        private const uint DI_NORMAL = 0x0003;
        private const int NULL_BRUSH = 5;

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct CURSORINFO
        {
            public int cbSize;
            public int flags;
            public IntPtr hCursor;
            public POINT ptScreenPos;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct BITMAPINFOHEADER
        {
            public uint biSize;
            public int biWidth;
            public int biHeight;
            public ushort biPlanes;
            public ushort biBitCount;
            public uint biCompression;
            public uint biSizeImage;
            public int biXPelsPerMeter;
            public int biYPelsPerMeter;
            public uint biClrUsed;
            public uint biClrImportant;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct BITMAPINFO
        {
            public BITMAPINFOHEADER bmiHeader;
            public uint bmiColors;
        }
        #endregion

        private readonly int _targetWidth;
        private readonly int _targetHeight;
        private readonly DesktopCaptureMode _mode;
        private readonly int _monitorIndex;
        private readonly IntPtr _targetWindow;
        private readonly Rectangle _sourceBounds;
        private readonly bool _captureCursor;
        private readonly bool _highlightClicks;

        // Zero-copy unmanaged GDI / DIBSection surfaces
        private readonly object _gdiLock = new object();
        private IntPtr _memDC;
        private IntPtr _dibBitmap;
        private IntPtr _oldBitmap;
        private IntPtr _dibBits; // Direct pointer to BGRA32 pixels (top-down or bottom-up)
        private Bitmap _previewWrapperBitmap; // Zero-copy wrapper around _dibBits for flicker-free GDI+ BufferedGraphics preview

        // Secondary source DC/Bitmap only when scaling is needed
        private bool _needsScaling;
        private IntPtr _srcMemDC;
        private IntPtr _srcDibBitmap;
        private IntPtr _srcOldBitmap;
        private IntPtr _srcDibBits;

        // Reusable pens for click highlights (zero per-frame GDI allocations)
        private IntPtr _leftClickPen;
        private IntPtr _rightClickPen;
        private IntPtr _nullBrush;

        public int Width { get { return _targetWidth; } }
        public int Height { get { return _targetHeight; } }
        public IntPtr RawBgraPointer { get { return _dibBits; } }
        public int Stride { get { return _targetWidth * 4; } }

        public ZeroCopyCaptureEngine(RecorderConfig config)
        {
            _mode = config.CaptureMode;
            _monitorIndex = config.MonitorIndex;
            _targetWindow = config.TargetWindowHandle;
            _captureCursor = config.CaptureCursor;
            _highlightClicks = config.HighlightMouseClicks;

            Screen[] screens = Screen.AllScreens;
            Screen activeScreen = (_monitorIndex >= 0 && _monitorIndex < screens.Length)
                ? screens[_monitorIndex]
                : Screen.PrimaryScreen;

            if (_mode == DesktopCaptureMode.CustomRegion)
            {
                _sourceBounds = new Rectangle(
                    config.RegionX,
                    config.RegionY,
                    Math.Max(64, config.RegionWidth & ~1),
                    Math.Max(64, config.RegionHeight & ~1));
            }
            else if (_mode == DesktopCaptureMode.WindowCapture && _targetWindow != IntPtr.Zero)
            {
                RECT rc;
                if (GetWindowRect(_targetWindow, out rc) && (rc.Right - rc.Left) > 32 && (rc.Bottom - rc.Top) > 32)
                {
                    _sourceBounds = new Rectangle(rc.Left, rc.Top, (rc.Right - rc.Left) & ~1, (rc.Bottom - rc.Top) & ~1);
                }
                else
                {
                    _sourceBounds = activeScreen.Bounds;
                }
            }
            else
            {
                _sourceBounds = activeScreen.Bounds;
            }

            if (config.UseNativeResolution || _mode == DesktopCaptureMode.CustomRegion)
            {
                _targetWidth = _sourceBounds.Width & ~1;
                _targetHeight = _sourceBounds.Height & ~1;
            }
            else
            {
                _targetWidth = Math.Max(128, config.TargetWidth & ~1);
                _targetHeight = Math.Max(128, config.TargetHeight & ~1);
            }

            _needsScaling = (_targetWidth != _sourceBounds.Width || _targetHeight != _sourceBounds.Height);

            InitializeSurfaces();
        }

        private void InitializeSurfaces()
        {
            IntPtr screenDC = GetDC(IntPtr.Zero);
            try
            {
                _memDC = CreateCompatibleDC(screenDC);
                SetStretchBltMode(_memDC, COLORONCOLOR);

                var bmi = new BITMAPINFO();
                bmi.bmiHeader.biSize = (uint)Marshal.SizeOf(typeof(BITMAPINFOHEADER));
                bmi.bmiHeader.biWidth = _targetWidth;
                // Negative height = Top-Down DIB so row 0 is top of screen!
                bmi.bmiHeader.biHeight = -_targetHeight;
                bmi.bmiHeader.biPlanes = 1;
                bmi.bmiHeader.biBitCount = 32;
                bmi.bmiHeader.biCompression = 0; // BI_RGB
                bmi.bmiHeader.biSizeImage = (uint)(_targetWidth * _targetHeight * 4);

                _dibBitmap = CreateDIBSection(screenDC, ref bmi, 0, out _dibBits, IntPtr.Zero, 0);
                _oldBitmap = SelectObject(_memDC, _dibBitmap);
                _previewWrapperBitmap = new Bitmap(
                    _targetWidth,
                    _targetHeight,
                    _targetWidth * 4,
                    System.Drawing.Imaging.PixelFormat.Format32bppRgb,
                    _dibBits);

                if (_needsScaling && _mode == DesktopCaptureMode.WindowCapture)
                {
                    _srcMemDC = CreateCompatibleDC(screenDC);
                    var srcBmi = bmi;
                    srcBmi.bmiHeader.biWidth = _sourceBounds.Width;
                    srcBmi.bmiHeader.biHeight = -_sourceBounds.Height;
                    srcBmi.bmiHeader.biSizeImage = (uint)(_sourceBounds.Width * _sourceBounds.Height * 4);
                    _srcDibBitmap = CreateDIBSection(screenDC, ref srcBmi, 0, out _srcDibBits, IntPtr.Zero, 0);
                    _srcOldBitmap = SelectObject(_srcMemDC, _srcDibBitmap);
                }

                _leftClickPen = CreatePen(0, 3, 0x005555FF);  // Red-Orange BGR
                _rightClickPen = CreatePen(0, 3, 0x00FFAA00); // Cyan-Blue BGR
                _nullBrush = GetStockObject(NULL_BRUSH);
            }
            finally
            {
                ReleaseDC(IntPtr.Zero, screenDC);
            }
        }

        /// <summary>
        /// Captures a frame directly into the unmanaged _dibBits buffer (0 managed heap allocations).
        /// </summary>
        public bool CaptureFrame()
        {
            if (_dibBits == IntPtr.Zero) return false;

            lock (_gdiLock)
            {
                if (_dibBits == IntPtr.Zero) return false;

                if (_mode == DesktopCaptureMode.WindowCapture && _targetWindow != IntPtr.Zero)
                {
                    if (_needsScaling && _srcMemDC != IntPtr.Zero)
                    {
                        PrintWindow(_targetWindow, _srcMemDC, PW_RENDERFULLCONTENT);
                        StretchBlt(_memDC, 0, 0, _targetWidth, _targetHeight,
                            _srcMemDC, 0, 0, _sourceBounds.Width, _sourceBounds.Height, SRCCOPY);
                    }
                    else
                    {
                        PrintWindow(_targetWindow, _memDC, PW_RENDERFULLCONTENT);
                    }
                }
                else
                {
                    IntPtr desktopDC = GetDC(IntPtr.Zero);
                    try
                    {
                        int rop = (_mode == DesktopCaptureMode.DXGIDesktopDuplication)
                            ? (SRCCOPY | CAPTUREBLT)
                            : SRCCOPY;

                        if (_needsScaling)
                        {
                            StretchBlt(_memDC, 0, 0, _targetWidth, _targetHeight,
                                desktopDC, _sourceBounds.X, _sourceBounds.Y, _sourceBounds.Width, _sourceBounds.Height, rop);
                        }
                        else
                        {
                            BitBlt(_memDC, 0, 0, _targetWidth, _targetHeight,
                                desktopDC, _sourceBounds.X, _sourceBounds.Y, rop);
                        }
                    }
                    finally
                    {
                        ReleaseDC(IntPtr.Zero, desktopDC);
                    }
                }

                if (_captureCursor)
                {
                    DrawCursorAndClicks();
                }
            }

            return true;
        }

        private void DrawCursorAndClicks()
        {
            var ci = new CURSORINFO();
            ci.cbSize = Marshal.SizeOf(typeof(CURSORINFO));
            if (GetCursorInfo(ref ci) && (ci.flags & CURSOR_SHOWING) != 0)
            {
                int relX = ci.ptScreenPos.x - _sourceBounds.X;
                int relY = ci.ptScreenPos.y - _sourceBounds.Y;

                if (_needsScaling && _sourceBounds.Width > 0 && _sourceBounds.Height > 0)
                {
                    relX = (relX * _targetWidth) / _sourceBounds.Width;
                    relY = (relY * _targetHeight) / _sourceBounds.Height;
                }

                if (relX >= 0 && relX < _targetWidth && relY >= 0 && relY < _targetHeight)
                {
                    if (_highlightClicks)
                    {
                        bool leftDown = (GetAsyncKeyState(0x01) & 0x8000) != 0;
                        bool rightDown = (GetAsyncKeyState(0x02) & 0x8000) != 0;
                        if (leftDown || rightDown)
                        {
                            IntPtr pen = leftDown ? _leftClickPen : _rightClickPen;
                            IntPtr oldPen = SelectObject(_memDC, pen);
                            IntPtr oldBrush = SelectObject(_memDC, _nullBrush);
                            Ellipse(_memDC, relX - 14, relY - 14, relX + 14, relY + 14);
                            SelectObject(_memDC, oldBrush);
                            SelectObject(_memDC, oldPen);
                        }
                    }

                    DrawIconEx(_memDC, relX, relY, ci.hCursor, 0, 0, 0, IntPtr.Zero, DI_NORMAL);
                }
            }
        }

        /// <summary>
        /// Converts or copies the captured frame directly into an IMFMediaBuffer pointer with zero GC allocations.
        /// </summary>
        public void WriteToMediaBuffer(IntPtr destPtr, DecoderColorPipeline pipeline)
        {
            byte* src = (byte*)_dibBits;
            byte* dst = (byte*)destPtr;
            int width = _targetWidth;
            int height = _targetHeight;

            if (pipeline == DecoderColorPipeline.RGB32_Direct || pipeline == DecoderColorPipeline.RGB24_Compact)
            {
                // Media Foundation RGB32 expects bottom-up rows by default when stride is positive,
                // or we can flip rows directly during the single memory copy pass:
                int rowBytes = width * 4;
                Parallel.For(0, height, y =>
                {
                    byte* srcRow = src + (y * rowBytes);
                    byte* dstRow = dst + ((height - 1 - y) * rowBytes);
                    Buffer.MemoryCopy(srcRow, dstRow, rowBytes, rowBytes);
                });
            }
            else if (pipeline == DecoderColorPipeline.NV12_Hardware420)
            {
                // Fast parallel BGRA32 (top-down) -> NV12 (Y plane + interleaved UV plane)
                byte* yPlane = dst;
                byte* uvPlane = dst + (width * height);
                int srcStride = width * 4;

                Parallel.For(0, height / 2, halfY =>
                {
                    int y0 = halfY * 2;
                    int y1 = y0 + 1;

                    byte* srcRow0 = src + (y0 * srcStride);
                    byte* srcRow1 = src + (y1 * srcStride);
                    byte* dstY0 = yPlane + (y0 * width);
                    byte* dstY1 = yPlane + (y1 * width);
                    byte* dstUV = uvPlane + (halfY * width);

                    for (int x = 0; x < width; x += 2)
                    {
                        int x4 = x * 4;
                        byte b00 = srcRow0[x4];
                        byte g00 = srcRow0[x4 + 1];
                        byte r00 = srcRow0[x4 + 2];

                        byte b01 = srcRow0[x4 + 4];
                        byte g01 = srcRow0[x4 + 5];
                        byte r01 = srcRow0[x4 + 6];

                        byte b10 = srcRow1[x4];
                        byte g10 = srcRow1[x4 + 1];
                        byte r10 = srcRow1[x4 + 2];

                        byte b11 = srcRow1[x4 + 4];
                        byte g11 = srcRow1[x4 + 5];
                        byte r11 = srcRow1[x4 + 6];

                        // BT.601 integer luminance
                        dstY0[x] = (byte)(((66 * r00 + 129 * g00 + 25 * b00 + 128) >> 8) + 16);
                        dstY0[x + 1] = (byte)(((66 * r01 + 129 * g01 + 25 * b01 + 128) >> 8) + 16);
                        dstY1[x] = (byte)(((66 * r10 + 129 * g10 + 25 * b10 + 128) >> 8) + 16);
                        dstY1[x + 1] = (byte)(((66 * r11 + 129 * g11 + 25 * b11 + 128) >> 8) + 16);

                        // Average 2x2 block for chroma
                        int rAvg = (r00 + r01 + r10 + r11 + 2) >> 2;
                        int gAvg = (g00 + g01 + g10 + g11 + 2) >> 2;
                        int bAvg = (b00 + b01 + b10 + b11 + 2) >> 2;

                        int u = ((-38 * rAvg - 74 * gAvg + 112 * bAvg + 128) >> 8) + 128;
                        int v = ((112 * rAvg - 94 * gAvg - 18 * bAvg + 128) >> 8) + 128;

                        dstUV[x] = (byte)(u < 0 ? 0 : (u > 255 ? 255 : u));
                        dstUV[x + 1] = (byte)(v < 0 ? 0 : (v > 255 ? 255 : v));
                    }
                });
            }
            else // I420_PlanarYUV
            {
                byte* yPlane = dst;
                int ySize = width * height;
                int uvStride = width / 2;
                byte* uPlane = dst + ySize;
                byte* vPlane = uPlane + (ySize / 4);
                int srcStride = width * 4;

                Parallel.For(0, height / 2, halfY =>
                {
                    int y0 = halfY * 2;
                    int y1 = y0 + 1;

                    byte* srcRow0 = src + (y0 * srcStride);
                    byte* srcRow1 = src + (y1 * srcStride);
                    byte* dstY0 = yPlane + (y0 * width);
                    byte* dstY1 = yPlane + (y1 * width);
                    byte* dstU = uPlane + (halfY * uvStride);
                    byte* dstV = vPlane + (halfY * uvStride);

                    for (int x = 0; x < width; x += 2)
                    {
                        int x4 = x * 4;
                        byte b00 = srcRow0[x4];
                        byte g00 = srcRow0[x4 + 1];
                        byte r00 = srcRow0[x4 + 2];

                        byte b01 = srcRow0[x4 + 4];
                        byte g01 = srcRow0[x4 + 5];
                        byte r01 = srcRow0[x4 + 6];

                        byte b10 = srcRow1[x4];
                        byte g10 = srcRow1[x4 + 1];
                        byte r10 = srcRow1[x4 + 2];

                        byte b11 = srcRow1[x4 + 4];
                        byte g11 = srcRow1[x4 + 5];
                        byte r11 = srcRow1[x4 + 6];

                        dstY0[x] = (byte)(((66 * r00 + 129 * g00 + 25 * b00 + 128) >> 8) + 16);
                        dstY0[x + 1] = (byte)(((66 * r01 + 129 * g01 + 25 * b01 + 128) >> 8) + 16);
                        dstY1[x] = (byte)(((66 * r10 + 129 * g10 + 25 * b10 + 128) >> 8) + 16);
                        dstY1[x + 1] = (byte)(((66 * r11 + 129 * g11 + 25 * b11 + 128) >> 8) + 16);

                        int rAvg = (r00 + r01 + r10 + r11 + 2) >> 2;
                        int gAvg = (g00 + g01 + g10 + g11 + 2) >> 2;
                        int bAvg = (b00 + b01 + b10 + b11 + 2) >> 2;

                        int u = ((-38 * rAvg - 74 * gAvg + 112 * bAvg + 128) >> 8) + 128;
                        int v = ((112 * rAvg - 94 * gAvg - 18 * bAvg + 128) >> 8) + 128;

                        int halfX = x >> 1;
                        dstU[halfX] = (byte)(u < 0 ? 0 : (u > 255 ? 255 : u));
                        dstV[halfX] = (byte)(v < 0 ? 0 : (v > 255 ? 255 : v));
                    }
                });
            }
        }

        /// <summary>
        /// Renders a scaled preview directly into the BufferedGraphics backbuffer with zero flickering or HDC contention.
        /// </summary>
        public void DrawPreviewToGraphics(Graphics g, int destX, int destY, int destW, int destH)
        {
            lock (_gdiLock)
            {
                if (_previewWrapperBitmap == null || _dibBits == IntPtr.Zero) return;
                var oldInterp = g.InterpolationMode;
                var oldComp = g.CompositingMode;
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Bilinear;
                g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                g.DrawImage(_previewWrapperBitmap, destX, destY, destW, destH);
                g.CompositingMode = oldComp;
                g.InterpolationMode = oldInterp;
            }
        }

        public static List<WindowTargetInfo> GetVisibleWindows()
        {
            var list = new List<WindowTargetInfo>();
            EnumWindows((hWnd, lParam) =>
            {
                if (!IsWindowVisible(hWnd)) return true;
                int len = GetWindowTextLength(hWnd);
                if (len <= 0) return true;

                RECT rc;
                if (!GetWindowRect(hWnd, out rc)) return true;
                int w = rc.Right - rc.Left;
                int h = rc.Bottom - rc.Top;
                if (w < 120 || h < 90) return true;

                var sb = new StringBuilder(len + 1);
                GetWindowText(hWnd, sb, sb.Capacity);
                string title = sb.ToString().Trim();
                if (string.IsNullOrEmpty(title) || title == "Program Manager" || title == "Windows Input Experience")
                    return true;

                list.Add(new WindowTargetInfo
                {
                    Handle = hWnd,
                    Title = title,
                    Bounds = new Rectangle(rc.Left, rc.Top, w, h)
                });
                return true;
            }, IntPtr.Zero);
            return list;
        }

        public void Dispose()
        {
            lock (_gdiLock)
            {
                if (_previewWrapperBitmap != null)
                {
                    _previewWrapperBitmap.Dispose();
                    _previewWrapperBitmap = null;
                }
                if (_leftClickPen != IntPtr.Zero) { DeleteObject(_leftClickPen); _leftClickPen = IntPtr.Zero; }
                if (_rightClickPen != IntPtr.Zero) { DeleteObject(_rightClickPen); _rightClickPen = IntPtr.Zero; }

                if (_srcMemDC != IntPtr.Zero)
                {
                    if (_srcOldBitmap != IntPtr.Zero) SelectObject(_srcMemDC, _srcOldBitmap);
                    if (_srcDibBitmap != IntPtr.Zero) DeleteObject(_srcDibBitmap);
                    DeleteDC(_srcMemDC);
                    _srcMemDC = IntPtr.Zero;
                }

                if (_memDC != IntPtr.Zero)
                {
                    if (_oldBitmap != IntPtr.Zero) SelectObject(_memDC, _oldBitmap);
                    if (_dibBitmap != IntPtr.Zero) DeleteObject(_dibBitmap);
                    DeleteDC(_memDC);
                    _memDC = IntPtr.Zero;
                    _dibBits = IntPtr.Zero;
                }
            }
        }
    }

    /// <summary>
    /// Interactive translucent overlay allowing the user to drag-select a custom screen region.
    /// </summary>
    internal sealed class RegionSelectorOverlay : Form
    {
        private Point _startPoint;
        private Rectangle _selectedRect;
        private bool _dragging;

        public Rectangle SelectedRegion { get { return _selectedRect; } }

        public RegionSelectorOverlay(Rectangle initial)
        {
            _selectedRect = initial;
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            Bounds = SystemInformation.VirtualScreen;
            TopMost = true;
            DoubleBuffered = true;
            BackColor = Color.Black;
            Opacity = 0.45;
            Cursor = Cursors.Cross;
            KeyPreview = true;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _dragging = true;
                _startPoint = new Point(e.X + Bounds.X, e.Y + Bounds.Y);
                _selectedRect = new Rectangle(_startPoint.X, _startPoint.Y, 64, 64);
                Invalidate();
            }
            else if (e.Button == MouseButtons.Right)
            {
                DialogResult = DialogResult.Cancel;
                Close();
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (_dragging)
            {
                int curX = e.X + Bounds.X;
                int curY = e.Y + Bounds.Y;
                int x = Math.Min(_startPoint.X, curX);
                int y = Math.Min(_startPoint.Y, curY);
                int w = Math.Max(64, Math.Abs(curX - _startPoint.X) & ~1);
                int h = Math.Max(64, Math.Abs(curY - _startPoint.Y) & ~1);
                _selectedRect = new Rectangle(x, y, w, h);
                Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (_dragging && e.Button == MouseButtons.Left)
            {
                _dragging = false;
                DialogResult = DialogResult.OK;
                Close();
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                DialogResult = DialogResult.Cancel;
                Close();
            }
            else if (e.KeyCode == Keys.Enter)
            {
                DialogResult = DialogResult.OK;
                Close();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Rectangle localRect = new Rectangle(
                _selectedRect.X - Bounds.X,
                _selectedRect.Y - Bounds.Y,
                _selectedRect.Width,
                _selectedRect.Height);

            using (var pen = new Pen(Color.FromArgb(0, 230, 180), 2f))
            using (var fill = new SolidBrush(Color.FromArgb(40, 0, 230, 180)))
            using (var font = new Font("Segoe UI Semibold", 11f))
            {
                e.Graphics.FillRectangle(fill, localRect);
                e.Graphics.DrawRectangle(pen, localRect);
                string label = string.Format("LiteClip Region: {0} x {1} at ({2}, {3})  —  Release Mouse or Press Enter (ESC to cancel)",
                    _selectedRect.Width, _selectedRect.Height, _selectedRect.X, _selectedRect.Y);
                e.Graphics.DrawString(label, font, Brushes.White, localRect.X + 6, Math.Max(8, localRect.Y - 26));
            }
        }
    }
}
