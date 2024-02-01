using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace GI_Subtitles
{
    public class ScreenInfo
    {
        [DllImport("user32.dll")]
        public static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("gdi32.dll")]
        public static extern int GetDeviceCaps(IntPtr hdc, int nIndex);
        [DllImport("gdi32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr CreateDC(string lpszDriver, string lpszDevice, string lpszOutput, IntPtr lpInitData);

        [DllImport("gdi32.dll", CharSet = CharSet.Auto)]
        public static extern bool DeleteDC(IntPtr hdc);


        public static float GetScaleFactorX(int screenIndex)
        {
            if (screenIndex < 0 || screenIndex >= Screen.AllScreens.Length)
                throw new ArgumentOutOfRangeException(nameof(screenIndex), "Invalid screen index.");

            Screen screen = Screen.AllScreens[screenIndex];
            IntPtr hdc = CreateDC(screen.DeviceName, null, null, IntPtr.Zero);

            try
            {
                const int DESKTOPHORZRES = 118;
                const int HORZRES = 8;

                int t = GetDeviceCaps(hdc, DESKTOPHORZRES);
                int d = GetDeviceCaps(hdc, HORZRES);
                return (float)t / d;
            }
            finally
            {
                // Always release the DC after using it
                if (hdc != IntPtr.Zero)
                    DeleteDC(hdc);
            }
        }

        public static float GetScaleFactorY(int screenIndex)
        {
            if (screenIndex < 0 || screenIndex >= Screen.AllScreens.Length)
                throw new ArgumentOutOfRangeException(nameof(screenIndex), "Invalid screen index.");

            Screen screen = Screen.AllScreens[screenIndex];
            IntPtr hdc = CreateDC(screen.DeviceName, null, null, IntPtr.Zero);

            try
            {
                const int DESKTOPVERTRES = 117;
                const int VERTRES = 10;

                int t = GetDeviceCaps(hdc, DESKTOPVERTRES);
                int d = GetDeviceCaps(hdc, VERTRES);
                return (float)t / d;
            }
            finally
            {
                // Always release the DC after using it
                if (hdc != IntPtr.Zero)
                    DeleteDC(hdc);
            }
        }

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);
    }
}