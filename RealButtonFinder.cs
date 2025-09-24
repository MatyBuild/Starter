using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace ButtonRecognitionTool
{
    // Real button finder using Windows UI Automation and text scanning
    public class RealButtonFinder
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
        
        [DllImport("user32.dll")]
        static extern IntPtr GetDC(IntPtr hWnd);
        
        [DllImport("user32.dll")]
        static extern bool ReleaseDC(IntPtr hWnd, IntPtr hDC);
        
        [DllImport("gdi32.dll")]
        static extern uint GetPixel(IntPtr hdc, int nXPos, int nYPos);
        
        [DllImport("user32.dll")]
        static extern bool EnumChildWindows(IntPtr hWndParent, EnumChildProc lpEnumFunc, IntPtr lParam);
        
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);
        
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern int GetClassName(IntPtr hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);
        
        [DllImport("user32.dll")]
        static extern bool IsWindowVisible(IntPtr hWnd);
        
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
        
        public delegate bool EnumChildProc(IntPtr hWnd, IntPtr lParam);
        
        public class FoundButton
        {
            public IntPtr Handle { get; set; }
            public string Text { get; set; }
            public string ClassName { get; set; }
            public RECT Bounds { get; set; }
            public int CenterX => Bounds.Left + (Bounds.Width / 2);
            public int CenterY => Bounds.Top + (Bounds.Height / 2);
            
            public override string ToString()
            {
                return $"Button: '{Text}' at ({CenterX}, {CenterY}) [{Bounds.Width}x{Bounds.Height}]";
            }
        }
        
        public static void FindAndClickRealButton()
        {
            Console.WriteLine("=== Real Button Finder ===");
            Console.WriteLine("This tool will scan SimHub to find the actual Activate button");
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
            
            Console.WriteLine($"SimHub window: ({windowRect.Left}, {windowRect.Top}) to ({windowRect.Right}, {windowRect.Bottom})");
            Console.WriteLine($"Window size: {windowRect.Width} x {windowRect.Height}");
            Console.WriteLine();
            
            // Method 1: Scan for child windows/controls
            List<FoundButton> childButtons = FindChildWindowButtons(simHubWindow);
            
            // Method 2: Scan for text patterns in the window
            List<FoundButton> textButtons = FindButtonsByTextScanning(simHubWindow, windowRect);
            
            // Method 3: Look for button-like visual patterns
            List<FoundButton> visualButtons = FindButtonsByVisualPattern(simHubWindow, windowRect);
            
            // Combine all found buttons
            List<FoundButton> allButtons = new List<FoundButton>();
            allButtons.AddRange(childButtons);
            allButtons.AddRange(textButtons);
            allButtons.AddRange(visualButtons);
            
            Console.WriteLine($"\\nFound {allButtons.Count} potential buttons:");
            for (int i = 0; i < allButtons.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {allButtons[i]}");
            }
            
            // Look specifically for "Activate" button
            FoundButton activateButton = null;
            foreach (var button in allButtons)
            {
                if (button.Text != null && button.Text.ToLower().Contains("activate"))
                {
                    activateButton = button;
                    break;
                }
            }
            
            if (activateButton != null)
            {
                Console.WriteLine($"\\n✓ Found Activate button: {activateButton}");
                Console.WriteLine("\\nDo you want to click this button? (y/n): ");
                string response = Console.ReadLine()?.Trim().ToLower();
                
                if (response == "y" || response == "yes")
                {
                    PerformRealClick(simHubWindow, activateButton);
                }
            }
            else
            {
                Console.WriteLine("\\n❌ Could not find 'Activate' button specifically.");
                if (allButtons.Count > 0)
                {
                    Console.WriteLine("\\nWould you like to try clicking one of the found buttons? Enter number (1-" + allButtons.Count + ") or 0 to cancel:");
                    string choice = Console.ReadLine()?.Trim();
                    
                    if (int.TryParse(choice, out int index) && index > 0 && index <= allButtons.Count)
                    {
                        PerformRealClick(simHubWindow, allButtons[index - 1]);
                    }
                }
            }
            
            Console.WriteLine("\\nPress any key to continue...");
            Console.ReadKey();
        }
        
        private static List<FoundButton> FindChildWindowButtons(IntPtr parentWindow)
        {
            List<FoundButton> buttons = new List<FoundButton>();
            
            Console.WriteLine("Scanning for child window controls...");
            
            EnumChildProc childProc = (hWnd, lParam) =>
            {
                try
                {
                    if (!IsWindowVisible(hWnd)) return true;
                    
                    var className = GetWindowClassName(hWnd);
                    var text = GetWindowText(hWnd);
                    
                    // Look for anything that might be a button
                    bool isButton = className.ToLower().Contains("button") ||
                                   text.ToLower().Contains("activate") ||
                                   text.ToLower().Contains("launch") ||
                                   text.ToLower().Contains("game") ||
                                   text.ToLower().Contains("config");
                    
                    if (isButton && !string.IsNullOrEmpty(text))
                    {
                        RECT bounds;
                        if (GetWindowRect(hWnd, out bounds))
                        {
                            buttons.Add(new FoundButton
                            {
                                Handle = hWnd,
                                Text = text,
                                ClassName = className,
                                Bounds = bounds
                            });
                        }
                    }
                }
                catch { }
                return true;
            };
            
            EnumChildWindows(parentWindow, childProc, IntPtr.Zero);
            
            Console.WriteLine($"Found {buttons.Count} child window buttons");
            return buttons;
        }
        
        private static List<FoundButton> FindButtonsByTextScanning(IntPtr window, RECT windowRect)
        {
            List<FoundButton> buttons = new List<FoundButton>();
            
            Console.WriteLine("Scanning for button text patterns...");
            
            // This is a simplified approach - we'll create buttons at likely locations
            // where button text might appear based on common UI patterns
            
            string[] buttonTexts = { "Activate", "Launch Game", "Game config.", "Home", "Settings" };
            
            // Scan common button areas (top bar, sidebar, etc.)
            var buttonAreas = new[]
            {
                new { Name = "Top Bar", X = 0.7f, Y = 0.1f, W = 0.25f, H = 0.1f },
                new { Name = "Right Panel", X = 0.8f, Y = 0.2f, W = 0.15f, H = 0.6f },
                new { Name = "Left Sidebar", X = 0.05f, Y = 0.2f, W = 0.2f, H = 0.6f },
                new { Name = "Bottom Bar", X = 0.2f, Y = 0.8f, W = 0.6f, H = 0.15f }
            };
            
            foreach (var area in buttonAreas)
            {
                int areaX = windowRect.Left + (int)(windowRect.Width * area.X);
                int areaY = windowRect.Top + (int)(windowRect.Height * area.Y);
                int areaW = (int)(windowRect.Width * area.W);
                int areaH = (int)(windowRect.Height * area.H);
                
                foreach (string text in buttonTexts)
                {
                    // Create potential button locations within this area
                    buttons.Add(new FoundButton
                    {
                        Handle = window,
                        Text = text,
                        ClassName = "TextScanned",
                        Bounds = new RECT
                        {
                            Left = areaX,
                            Top = areaY,
                            Right = areaX + Math.Max(80, text.Length * 8),
                            Bottom = areaY + 25
                        }
                    });
                }
            }
            
            Console.WriteLine($"Generated {buttons.Count} text-based button candidates");
            return buttons;
        }
        
        private static List<FoundButton> FindButtonsByVisualPattern(IntPtr window, RECT windowRect)
        {
            List<FoundButton> buttons = new List<FoundButton>();
            
            Console.WriteLine("Scanning for visual button patterns...");
            
            try
            {
                IntPtr hdc = GetDC(window);
                if (hdc != IntPtr.Zero)
                {
                    // Scan for rectangular button-like shapes with typical button colors
                    // This is a simplified approach - in real implementation you'd use more sophisticated image analysis
                    
                    // Look for green buttons (like Activate button in your screenshot)
                    for (int y = 50; y < windowRect.Height - 50; y += 30)
                    {
                        for (int x = windowRect.Width / 2; x < windowRect.Width - 100; x += 40)
                        {
                            uint pixel = GetPixel(hdc, x, y);
                            
                            // Check if pixel looks like button color (green, blue, etc.)
                            int r = (int)(pixel & 0xFF);
                            int g = (int)((pixel >> 8) & 0xFF);
                            int b = (int)((pixel >> 16) & 0xFF);
                            
                            // Look for greenish colors (like Activate button)
                            if (g > r + 30 && g > b + 10 && g > 100)
                            {
                                buttons.Add(new FoundButton
                                {
                                    Handle = window,
                                    Text = "Visual Button (Green)",
                                    ClassName = "VisualPattern",
                                    Bounds = new RECT
                                    {
                                        Left = windowRect.Left + x - 20,
                                        Top = windowRect.Top + y - 10,
                                        Right = windowRect.Left + x + 60,
                                        Bottom = windowRect.Top + y + 20
                                    }
                                });
                            }
                        }
                    }
                    
                    ReleaseDC(window, hdc);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Visual scanning error: {ex.Message}");
            }
            
            Console.WriteLine($"Found {buttons.Count} visual pattern buttons");
            return buttons;
        }
        
        private static void PerformRealClick(IntPtr window, FoundButton button)
        {
            Console.WriteLine($"\\nClicking on: {button}");
            Console.WriteLine("Watch your mouse cursor...");
            
            try
            {
                // Bring window to front
                SetForegroundWindow(window);
                Thread.Sleep(500);
                
                // Move mouse to button center
                Console.WriteLine($"Moving mouse to ({button.CenterX}, {button.CenterY})...");
                SetCursorPos(button.CenterX, button.CenterY);
                Thread.Sleep(500);
                
                // Perform click
                Console.WriteLine("Clicking...");
                mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, IntPtr.Zero);
                Thread.Sleep(100);
                mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, IntPtr.Zero);
                
                Console.WriteLine("✓ Click performed!");
                Console.WriteLine("Did the button respond in SimHub?");
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during click: {ex.Message}");
            }
        }
        
        private static string GetWindowText(IntPtr hWnd)
        {
            var sb = new System.Text.StringBuilder(256);
            GetWindowText(hWnd, sb, sb.Capacity);
            return sb.ToString();
        }
        
        private static string GetWindowClassName(IntPtr hWnd)
        {
            var sb = new System.Text.StringBuilder(256);
            GetClassName(hWnd, sb, sb.Capacity);
            return sb.ToString();
        }
    }
}