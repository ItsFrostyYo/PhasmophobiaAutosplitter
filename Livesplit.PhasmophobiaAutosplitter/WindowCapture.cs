using System;
using System.Runtime.InteropServices;

namespace LiveSplit.PhasmophobiaAutosplitter
{
    /// <summary>
    /// Captures pixels from a window's client area for load-removal detection.
    /// Uses GDI for fast, consistent pixel reads.
    /// </summary>
    internal static class WindowCapture
    {
        private const int SRCCOPY = 0x00CC0020;

        [DllImport("user32.dll")]
        private static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);

        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);

        [DllImport("gdi32.dll")]
        private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

        [DllImport("gdi32.dll")]
        private static extern bool BitBlt(IntPtr hdcDest, int nXDest, int nYDest, int nWidth, int nHeight,
            IntPtr hdcSrc, int nXSrc, int nYSrc, int dwRop);

        [DllImport("gdi32.dll")]
        private static extern uint GetPixel(IntPtr hdc, int nXPos, int nYPos);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteDC(IntPtr hdc);

        [DllImport("user32.dll")]
        private static extern bool PrintWindow(IntPtr hwnd, IntPtr hdcBlt, uint nFlags);

        private const uint PW_RENDERFULLCONTENT = 0x02;

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;

            public int Width => Right - Left;
            public int Height => Bottom - Top;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        /// <summary>
        /// Gets the client rectangle of the window in screen coordinates.
        /// Returns (0,0,0,0) if the window is invalid.
        /// </summary>
        public static bool GetClientRectScreen(IntPtr hWnd, out int x, out int y, out int width, out int height)
        {
            x = y = width = height = 0;
            if (hWnd == IntPtr.Zero)
                return false;

            if (!GetClientRect(hWnd, out RECT rect))
                return false;

            var pt = new POINT { X = rect.Left, Y = rect.Top };
            if (!ClientToScreen(hWnd, ref pt))
                return false;

            x = pt.X;
            y = pt.Y;
            width = rect.Width;
            height = rect.Height;
            return width > 0 && height > 0;
        }

        /// <summary>
        /// Samples a single pixel at client coordinates (px, py).
        /// Tries screen DC first (works for windowed/borderless). If that returns black (0,0,0), tries PrintWindow (for exclusive fullscreen).
        /// </summary>
        public static bool SampleClientPixel(IntPtr hWnd, int px, int py, out int r, out int g, out int b)
        {
            r = g = b = 0;
            if (hWnd == IntPtr.Zero)
                return false;

            if (!GetClientRectScreen(hWnd, out int clientX, out int clientY, out int clientW, out int clientH))
                return false;

            if (px < 0 || px >= clientW || py < 0 || py >= clientH)
                return false;

            // 1) Prefer PrintWindow: reads the window's actual framebuffer (correct for fullscreen/DirectX).
            if (SampleClientPixelViaPrintWindow(hWnd, clientX, clientY, clientW, clientH, px, py, out r, out g, out b))
                return true;

            // 2) Fallback: screen DC at window client position (if PrintWindow fails).
            IntPtr hdcScreen = GetDC(IntPtr.Zero);
            if (hdcScreen == IntPtr.Zero)
                return false;
            try
            {
                IntPtr hdcMem = CreateCompatibleDC(hdcScreen);
                if (hdcMem == IntPtr.Zero)
                    return false;
                try
                {
                    IntPtr hBitmap = CreateCompatibleBitmap(hdcScreen, 1, 1);
                    if (hBitmap == IntPtr.Zero)
                        return false;
                    try
                    {
                        IntPtr oldBitmap = SelectObject(hdcMem, hBitmap);
                        if (oldBitmap == IntPtr.Zero)
                            return false;
                        int screenX = clientX + px;
                        int screenY = clientY + py;
                        bool ok = BitBlt(hdcMem, 0, 0, 1, 1, hdcScreen, screenX, screenY, SRCCOPY);
                        SelectObject(hdcMem, oldBitmap);
                        if (!ok)
                            return false;
                        uint pixel = GetPixel(hdcMem, 0, 0);
                        b = (int)(pixel & 0xFF);
                        g = (int)((pixel >> 8) & 0xFF);
                        r = (int)((pixel >> 16) & 0xFF);
                        return true;
                    }
                    finally { DeleteObject(hBitmap); }
                }
                finally { DeleteDC(hdcMem); }
            }
            finally { ReleaseDC(IntPtr.Zero, hdcScreen); }
        }

        private static bool SampleClientPixelViaPrintWindow(IntPtr hWnd, int clientX, int clientY, int clientW, int clientH, int px, int py, out int r, out int g, out int b)
        {
            r = g = b = 0;
            if (clientW <= 0 || clientH <= 0 || px < 0 || px >= clientW || py < 0 || py >= clientH)
                return false;
            if (!GetWindowRect(hWnd, out RECT winRect))
                return false;
            int winW = winRect.Right - winRect.Left;
            int winH = winRect.Bottom - winRect.Top;
            if (winW <= 0 || winH <= 0)
                return false;
            int clientOffsetX = clientX - winRect.Left;
            int clientOffsetY = clientY - winRect.Top;
            int sampleX = clientOffsetX + px;
            int sampleY = clientOffsetY + py;
            if (sampleX < 0 || sampleX >= winW || sampleY < 0 || sampleY >= winH)
                return false;
            IntPtr hdcScreen = GetDC(IntPtr.Zero);
            if (hdcScreen == IntPtr.Zero)
                return false;
            try
            {
                IntPtr hdcMem = CreateCompatibleDC(hdcScreen);
                if (hdcMem == IntPtr.Zero)
                    return false;
                try
                {
                    IntPtr hBitmap = CreateCompatibleBitmap(hdcScreen, winW, winH);
                    if (hBitmap == IntPtr.Zero)
                        return false;
                    try
                    {
                        IntPtr oldBitmap = SelectObject(hdcMem, hBitmap);
                        if (oldBitmap == IntPtr.Zero)
                            return false;
                        if (!PrintWindow(hWnd, hdcMem, PW_RENDERFULLCONTENT))
                        {
                            SelectObject(hdcMem, oldBitmap);
                            return false;
                        }
                        uint pixel = GetPixel(hdcMem, sampleX, sampleY);
                        b = (int)(pixel & 0xFF);
                        g = (int)((pixel >> 8) & 0xFF);
                        r = (int)((pixel >> 16) & 0xFF);
                        SelectObject(hdcMem, oldBitmap);
                        return true;
                    }
                    finally { DeleteObject(hBitmap); }
                }
                finally { DeleteDC(hdcMem); }
            }
            finally { ReleaseDC(IntPtr.Zero, hdcScreen); }
        }

        // Reference points for loading screen (1920x1080); scale for other resolutions.
        private const int BlackRefWidth = 1920;
        private const int BlackRefHeight = 1080;
        private const int BlackRefX = 1825;
        private const int BlackRefY = 83;

        /// <summary>One window capture (one PrintWindow). Sample from it then call Release.</summary>
        public sealed class WindowCaptureFrame
        {
            internal IntPtr Hdc;
            internal IntPtr Bitmap;
            internal IntPtr OldBitmap;
            internal IntPtr ScreenDc;
            internal int ClientOffsetX, ClientOffsetY, WinW, WinH;

            public bool Sample(int clientPx, int clientPy, out int r, out int g, out int b)
            {
                r = g = b = 0;
                int sx = ClientOffsetX + clientPx;
                int sy = ClientOffsetY + clientPy;
                if (sx < 0 || sx >= WinW || sy < 0 || sy >= WinH) return false;
                uint pixel = GetPixel(Hdc, sx, sy);
                b = (int)(pixel & 0xFF);
                g = (int)((pixel >> 8) & 0xFF);
                r = (int)((pixel >> 16) & 0xFF);
                return true;
            }
        }

        /// <summary>Capture the window once (one PrintWindow). Use Sample then Release to avoid doing multiple captures per frame.</summary>
        public static WindowCaptureFrame TryCaptureWindow(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero) return null;
            if (!GetClientRectScreen(hWnd, out int clientX, out int clientY, out int clientW, out int clientH)) return null;
            if (!GetWindowRect(hWnd, out RECT winRect)) return null;
            int winW = winRect.Right - winRect.Left;
            int winH = winRect.Bottom - winRect.Top;
            if (winW <= 0 || winH <= 0) return null;
            IntPtr hdcScreen = GetDC(IntPtr.Zero);
            if (hdcScreen == IntPtr.Zero) return null;
            IntPtr hdcMem = CreateCompatibleDC(hdcScreen);
            if (hdcMem == IntPtr.Zero) { ReleaseDC(IntPtr.Zero, hdcScreen); return null; }
            IntPtr hBitmap = CreateCompatibleBitmap(hdcScreen, winW, winH);
            if (hBitmap == IntPtr.Zero) { DeleteDC(hdcMem); ReleaseDC(IntPtr.Zero, hdcScreen); return null; }
            IntPtr oldBitmap = SelectObject(hdcMem, hBitmap);
            if (oldBitmap == IntPtr.Zero) { DeleteObject(hBitmap); DeleteDC(hdcMem); ReleaseDC(IntPtr.Zero, hdcScreen); return null; }
            if (!PrintWindow(hWnd, hdcMem, PW_RENDERFULLCONTENT))
            { SelectObject(hdcMem, oldBitmap); DeleteObject(hBitmap); DeleteDC(hdcMem); ReleaseDC(IntPtr.Zero, hdcScreen); return null; }
            return new WindowCaptureFrame
            {
                Hdc = hdcMem,
                Bitmap = hBitmap,
                OldBitmap = oldBitmap,
                ScreenDc = hdcScreen,
                ClientOffsetX = clientX - winRect.Left,
                ClientOffsetY = clientY - winRect.Top,
                WinW = winW,
                WinH = winH
            };
        }

        public static void ReleaseCapture(WindowCaptureFrame frame)
        {
            if (frame == null) return;
            SelectObject(frame.Hdc, frame.OldBitmap);
            DeleteObject(frame.Bitmap);
            DeleteDC(frame.Hdc);
            ReleaseDC(IntPtr.Zero, frame.ScreenDc);
        }

        /// <summary>Returns true if loading screen (black at 1825,83). Use with a single capture frame to avoid lag.</summary>
        public static bool IsTopRightRegionBlack(WindowCaptureFrame frame, int clientW, int clientH, int blackThreshold = 1)
        {
            if (frame == null || clientW < 100 || clientH < 100) return false;
            int px = (BlackRefX * clientW) / BlackRefWidth;
            int py = (BlackRefY * clientH) / BlackRefHeight;
            return IsPixelBlack(frame, px, py, blackThreshold);
        }

        /// <summary>Returns true if the pixel at (px, py) is black (R,G,B all &lt;= blackThreshold). Used for first load (1825,83) and second load (808,945).</summary>
        public static bool IsPixelBlack(WindowCaptureFrame frame, int px, int py, int blackThreshold = 1)
        {
            if (frame == null) return false;
            if (!frame.Sample(px, py, out int r, out int g, out int b)) return false;
            return r <= blackThreshold && g <= blackThreshold && b <= blackThreshold;
        }
    }
}
