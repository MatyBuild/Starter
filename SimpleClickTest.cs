using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace ButtonRecognitionTool
{
    // Simple test tool to click at specific coordinates - use this to test if basic clicking works
    public class SimpleClickTest
    {
        [DllImport("user32.dll")]
        static extern bool SetCursorPos(int x, int y);
        
        [DllImport("user32.dll")]
        static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, IntPtr dwExtraInfo);
        
        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);
        
        [DllImport("user32.dll")]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        
        [DllImport("user32.dll")]
        static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);
        
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
            public int Width => Right - Left;
            public int Height => Bottom - Top;
        }
        
        private const uint MOUSEEVENTF_LEFTDOWN = 0x02;
        private const uint MOUSEEVENTF_LEFTUP = 0x04;
        
        public static void TestSimpleClick()
        {
            Console.WriteLine("=== Simple Click Test ===");
            Console.WriteLine("This tool will click at specific coordinates on SimHub window");
            Console.WriteLine();
            
            // Find SimHub window
            IntPtr simHubWindow = FindWindow(null, "SimHub - Drivetech");
            if (simHubWindow == IntPtr.Zero)
            {
                simHubWindow = FindWindow(null, "SimHub");
            }
            
            if (simHubWindow == IntPtr.Zero)
            {
                Console.WriteLine("ERROR: SimHub window not found!");
                Console.WriteLine("Please make sure SimHub is running and window is visible.");
                return;
            }
            
            Console.WriteLine($"Found SimHub window: {simHubWindow}");
            
            // Get window bounds
            RECT windowRect;
            if (!GetWindowRect(simHubWindow, out windowRect))
            {
                Console.WriteLine("ERROR: Could not get window bounds!");
                return;
            }
            
            Console.WriteLine($"SimHub window bounds: ({windowRect.Left}, {windowRect.Top}) to ({windowRect.Right}, {windowRect.Bottom})");
            Console.WriteLine($"Window size: {windowRect.Width} x {windowRect.Height}");
            Console.WriteLine();
            
            // Calculate Activate button position based on your screenshot
            // From the screenshot, Activate button appears to be around 79.5% from left, 13.5% from top
            int activateX = windowRect.Left + (int)(windowRect.Width * 0.795f);
            int activateY = windowRect.Top + (int)(windowRect.Height * 0.135f);
            
            Console.WriteLine($"Calculated Activate button position: ({activateX}, {activateY})");
            Console.WriteLine();
            
            Console.WriteLine("In 5 seconds, I will:");
            Console.WriteLine("1. Bring SimHub window to front");
            Console.WriteLine("2. Move mouse to Activate button");
            Console.WriteLine("3. Click the button");
            Console.WriteLine();
            Console.WriteLine("Watch your SimHub window...");
            
            for (int i = 5; i > 0; i--)
            {
                Console.WriteLine($"Starting in {i} seconds...");
                Thread.Sleep(1000);
            }
            
            try
            {
                Console.WriteLine("Step 1: Activating SimHub window...");
                SetForegroundWindow(simHubWindow);
                Thread.Sleep(500);
                
                Console.WriteLine($"Step 2: Moving mouse to ({activateX}, {activateY})...");
                SetCursorPos(activateX, activateY);
                Thread.Sleep(500);
                
                Console.WriteLine("Step 3: Clicking...");
                mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, IntPtr.Zero);
                Thread.Sleep(100);
                mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, IntPtr.Zero);
                
                Console.WriteLine();
                Console.WriteLine("✓ Click performed!");
                Console.WriteLine("Did you see the Activate button get clicked in SimHub?");
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR during click test: {ex.Message}");
            }
            
            Console.WriteLine();
            Console.WriteLine("Test completed. Press any key to continue...");
            Console.ReadKey();
        }
    }
}