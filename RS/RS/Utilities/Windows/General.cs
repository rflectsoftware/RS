using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace RS.Utilities.Windows
{
    public static class General
    {
        #region Win32 Imports

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, UIntPtr wParam, UIntPtr lParam);

        [DllImport("user32.dll", SetLastError = false)]
        static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

        [DllImport("user32.dll", SetLastError = true)]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        #endregion

        #region Variables

        private static int Level;

        #endregion

        #region Constants

        private const uint GW_HWNDNEXT = 2;
        private const uint GW_CHILD = 5;
        private const uint WM_CLOSE = 0x10;

        #endregion

        #region Public

        public static void CloseWindow(IntPtr Window)
        {
            SendMessage(Window, WM_CLOSE, UIntPtr.Zero, UIntPtr.Zero);
        }

        public static IntPtr GetWindowHandle(string ClassName, string WindowText)
        {
            IntPtr hWnd = FindWindow(ClassName, WindowText);
            return hWnd;
        }

        public static List<IntPtr> FindExistingWindows(string WindowText, string ClassName)
        {
            List<IntPtr> list = new List<IntPtr>();
            IntPtr start = GetDesktopWindow();
            IntPtr hWnd = FindWindow(ClassName, WindowText);

            while (hWnd != IntPtr.Zero)
            {
                list.Add(hWnd);
                hWnd = FindWindowEx(start, hWnd, ClassName, WindowText);
            }

            return list;
        }

        public static List<IntPtr> FindWindowLike(IntPtr hWndStart, string WindowText, string ClassName)
        {
            List<IntPtr> MyResult = new List<IntPtr>();

            IntPtr hWnd = IntPtr.Zero;
            StringBuilder windowText = new StringBuilder(256);

            if (Level == 0)
            {
                if (hWndStart == IntPtr.Zero) hWndStart = GetDesktopWindow();
            }

            Level++;

            hWnd = GetWindow(hWndStart, GW_CHILD);

            while (hWnd != IntPtr.Zero)
            {
                FindWindowLike(hWnd, WindowText, ClassName);

                GetWindowText(hWnd, windowText, 255);

                if (windowText.Length > 0)
                {
                    if (windowText.ToString().Contains(WindowText))
                    {
                        MyResult.Add(hWnd);
                    }
                }

                hWnd = GetWindow(hWnd, GW_HWNDNEXT);
            }

            Level--;

            return MyResult;
        }

        #endregion
    }
}
