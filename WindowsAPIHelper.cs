using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace ButtonRecognitionTool
{
    public class WindowsAPIHelper
    {
        #region Windows API Constants
        public const int WM_COMMAND = 0x0111;
        public const int WM_LBUTTONDOWN = 0x0201;
        public const int WM_LBUTTONUP = 0x0202;
        public const int BN_CLICKED = 0;
        public const int GW_CHILD = 5;
        public const int GW_HWNDNEXT = 2;
        #endregion

        #region Windows API Declarations
        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        public static extern bool IsWindowEnabled(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);

        [DllImport("user32.dll")]
        public static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll")]
        public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, IntPtr dwExtraInfo);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        public const uint MOUSEEVENTF_LEFTDOWN = 0x02;
        public const uint MOUSEEVENTF_LEFTUP = 0x04;
        #endregion

        #region Helper Methods
        public static string GetWindowText(IntPtr hWnd)
        {
            StringBuilder sb = new StringBuilder(256);
            GetWindowText(hWnd, sb, sb.Capacity);
            return sb.ToString();
        }

        public static string GetClassName(IntPtr hWnd)
        {
            StringBuilder sb = new StringBuilder(256);
            GetClassName(hWnd, sb, sb.Capacity);
            return sb.ToString();
        }

        public static bool IsButton(IntPtr hWnd)
        {
            string className = GetClassName(hWnd);
            return className.ToLower().Contains("button") || 
                   className.ToLower() == "button" ||
                   className.ToLower() == "toolbarbutton32";
        }

        public static void ClickButton(IntPtr buttonHandle)
        {
            if (buttonHandle != IntPtr.Zero && IsWindowEnabled(buttonHandle))
            {
                // Method 1: Send BN_CLICKED message
                SendMessage(buttonHandle, WM_LBUTTONDOWN, IntPtr.Zero, IntPtr.Zero);
                SendMessage(buttonHandle, WM_LBUTTONUP, IntPtr.Zero, IntPtr.Zero);
                
                // Alternative method: Send WM_COMMAND to parent
                IntPtr parent = GetParent(buttonHandle);
                if (parent != IntPtr.Zero)
                {
                    int controlId = GetDlgCtrlID(buttonHandle);
                    IntPtr lParam = new IntPtr((BN_CLICKED << 16) | (controlId & 0xFFFF));
                    SendMessage(parent, WM_COMMAND, lParam, buttonHandle);
                }
            }
        }

        [DllImport("user32.dll")]
        public static extern IntPtr GetParent(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern int GetDlgCtrlID(IntPtr hWnd);

        public static void ClickButtonAtPosition(IntPtr buttonHandle)
        {
            if (buttonHandle != IntPtr.Zero && IsWindowEnabled(buttonHandle))
            {
                RECT rect;
                if (GetWindowRect(buttonHandle, out rect))
                {
                    int x = (rect.Left + rect.Right) / 2;
                    int y = (rect.Top + rect.Bottom) / 2;
                    
                    SetCursorPos(x, y);
                    mouse_event(MOUSEEVENTF_LEFTDOWN, (uint)x, (uint)y, 0, IntPtr.Zero);
                    System.Threading.Thread.Sleep(50);
                    mouse_event(MOUSEEVENTF_LEFTUP, (uint)x, (uint)y, 0, IntPtr.Zero);
                }
            }
        }
        #endregion
    }
}