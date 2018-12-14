using System;
using System.Runtime.InteropServices;
using System.Text;

namespace handler.util
{
    class HwndUtil
    {
        public static int WM_GETTEXT = 0x000D;
        const int WM_CLOSE = 0x0010;
        public const int SW_SHOWNORMAL = 1;
        public const int SW_SHOWMINIMIZED = 2;
        public const int SW_SHOWMAXIMIZED = 3;

        [DllImport("User32.dll", EntryPoint = "FindWindow")]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("User32.dll", EntryPoint = "FindWindowEx")]
        public static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpClassName, string lpWindowName);

        [DllImport("User32.dll", EntryPoint = "SendMessage")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, string lParam);

        [DllImport("user32.dll", EntryPoint = "SendMessage", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int SendMessage2(IntPtr hwnd, uint wMsg, int wParam, int lParam);

        [DllImport("user32.dll")]
        public static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int nMaxCount);
       
        [DllImport("user32.dll", EntryPoint = "SetWindowText", CharSet = CharSet.Ansi)]
        public static extern int SetWindowText(int hwnd, string lpString);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int x, int y, int Width, int Height, int flags);

        [DllImport("user32.dll")]
        public static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll", EntryPoint = "SendMessageA")]
        private static extern int SendMessage(IntPtr hwnd, int wMsg, int wParam, StringBuilder lParam);
      

        public static void clickHwnd(IntPtr hWnd)
        {
            SendMessage(hWnd, 0xF5, IntPtr.Zero, null);
        }

        public static void setText(IntPtr hwnd,string text)
        {
            SendMessage(hwnd, 0x0C, IntPtr.Zero, text);
        }

        public static void closeHwnd(IntPtr hwnd)
        {
            SendMessage(hwnd, WM_CLOSE, IntPtr.Zero, "");
        }

        public static int getEdit(IntPtr hwnd)
        {
            const int buffer_size = 1024;
            StringBuilder buffer = new StringBuilder(buffer_size);
            return SendMessage(hwnd, WM_GETTEXT, buffer_size, buffer);
        }
    }
}
