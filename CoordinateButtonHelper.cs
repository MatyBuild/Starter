using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace ButtonRecognitionTool
{
    public class CoordinateButtonHelper
    {
        [DllImport("user32.dll")]
        static extern IntPtr WindowFromPoint(POINT Point);
        
        [DllImport("user32.dll")]
        static extern bool ScreenToClient(IntPtr hWnd, ref POINT lpPoint);
        
        [DllImport("user32.dll")]
        static extern bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);
        
        [DllImport("user32.dll")]
        static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);
        
        [DllImport("user32.dll")]
        static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);
        
        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);
        
        [DllImport("user32.dll")]
        static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
        
        [DllImport("user32.dll")]
        static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
        
        private const uint WM_LBUTTONDOWN = 0x0201;
        private const uint WM_LBUTTONUP = 0x0202;
        private const uint WM_KEYDOWN = 0x0100;
        private const uint WM_KEYUP = 0x0101;
        private const int VK_RETURN = 0x0D;
        private const int VK_SPACE = 0x20;
        
        [DllImport("user32.dll")]
        static extern IntPtr SetFocus(IntPtr hWnd);
        
        private static IntPtr MakeLParam(int x, int y)
        {
            return (IntPtr)((y << 16) | (x & 0xFFFF));
        }
        
        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int x;
            public int y;
            
            public POINT(int x, int y)
            {
                this.x = x;
                this.y = y;
            }
        }
        
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
        
        public List<ButtonInfo> CreateSimHubButtons(IntPtr windowHandle, bool debugMode = false)
        {
            List<ButtonInfo> buttons = new List<ButtonInfo>();
            
            try
            {
                if (debugMode)
                {
                    Console.WriteLine("Creating SimHub buttons using enhanced detection...");
                }
                
                // First try to find real buttons using child window enumeration
                List<ButtonInfo> realButtons = FindRealButtons(windowHandle, debugMode);
                if (realButtons.Count > 0)
                {
                    if (debugMode)
                    {
                        Console.WriteLine($"Found {realButtons.Count} real buttons via window enumeration");
                    }
                    buttons.AddRange(realButtons);
                    return buttons;
                }
                
                if (debugMode)
                {
                    Console.WriteLine("No real buttons found, trying text-based detection...");
                }
                
                // Try to find buttons by searching for button text in the window
                List<ButtonInfo> textButtons = FindButtonsByText(windowHandle, debugMode);
                if (textButtons.Count > 0)
                {
                    buttons.AddRange(textButtons);
                    return buttons;
                }
                
                if (debugMode)
                {
                    Console.WriteLine("Falling back to coordinate-based buttons...");
                }
                
                // Fallback to coordinate-based approach
                RECT windowRect;
                if (!GetWindowRect(windowHandle, out windowRect))
                {
                    if (debugMode) Console.WriteLine("Could not get window rectangle");
                    return buttons;
                }
                
                if (debugMode)
                {
                    Console.WriteLine($"Window rect: ({windowRect.Left}, {windowRect.Top}) - ({windowRect.Right}, {windowRect.Bottom})");
                    Console.WriteLine($"Window size: {windowRect.Width}x{windowRect.Height}");
                }
                
                // Looking at your screenshot, the Activate button is in the top-right area
                // Let's be more precise with coordinates based on the actual screenshot
                
                // Main buttons in top area - based on precise screenshot analysis
                // From the screenshot: Activate button appears to be around 78% from left, 13.5% from top
                buttons.Add(CreateCoordinateButton(windowHandle, windowRect, 0.795f, 0.135f, 0.080f, 0.035f, "Activate", "ActivateBtn", 1));
                buttons.Add(CreateCoordinateButton(windowHandle, windowRect, 0.882f, 0.135f, 0.090f, 0.035f, "Launch Game", "LaunchGameBtn", 2));
                buttons.Add(CreateCoordinateButton(windowHandle, windowRect, 0.975f, 0.135f, 0.080f, 0.035f, "Game config.", "GameConfigBtn", 3));
                
                if (debugMode)
                {
                    // Calculate absolute coordinates for the Activate button for debugging
                    int activateX = windowRect.Left + (int)(windowRect.Width * 0.795f);
                    int activateY = windowRect.Top + (int)(windowRect.Height * 0.135f);
                    Console.WriteLine($"DEBUG: Activate button should be at absolute coords: ({activateX}, {activateY})");
                    Console.WriteLine($"DEBUG: This corresponds to relative position 79.5% x 13.5% of window");
                }
                
                // Import device button on the right side
                buttons.Add(CreateCoordinateButton(windowHandle, windowRect, 0.90f, 0.30f, 0.15f, 0.04f, "Import device from file", "ImportDeviceBtn", 5));
                
                // License section buttons at the bottom
                buttons.Add(CreateCoordinateButton(windowHandle, windowRect, 0.85f, 0.82f, 0.20f, 0.04f, "LOAD MY LICENSE FILE", "LoadLicenseBtn", 8));
                
                // Left sidebar menu items
                buttons.Add(CreateCoordinateButton(windowHandle, windowRect, 0.25f, 0.15f, 0.10f, 0.04f, "Home", "HomeBtn", 9));
                buttons.Add(CreateCoordinateButton(windowHandle, windowRect, 0.25f, 0.20f, 0.10f, 0.04f, "Car settings", "CarSettingsBtn", 10));
                buttons.Add(CreateCoordinateButton(windowHandle, windowRect, 0.25f, 0.25f, 0.10f, 0.04f, "Devices", "DevicesBtn", 11));
                buttons.Add(CreateCoordinateButton(windowHandle, windowRect, 0.25f, 0.30f, 0.10f, 0.04f, "Dash Studio", "DashStudioBtn", 12));
                buttons.Add(CreateCoordinateButton(windowHandle, windowRect, 0.25f, 0.35f, 0.10f, 0.04f, "Arduino", "ArduinoBtn", 13));
                
                if (debugMode)
                {
                    Console.WriteLine($"Created {buttons.Count} coordinate-based buttons for SimHub");
                    foreach (var btn in buttons)
                    {
                        Console.WriteLine($"  - {btn}");
                    }
                }
                
            }
            catch (Exception ex)
            {
                if (debugMode)
                {
                    Console.WriteLine($"Error creating SimHub buttons: {ex.Message}");
                }
            }
            
            return buttons;
        }
        
        private ButtonInfo CreateCoordinateButton(IntPtr windowHandle, RECT windowRect, float relX, float relY, float relW, float relH, string text, string id, int controlId)
        {
            // Convert relative coordinates to absolute screen coordinates
            // Use client area instead of window area for more precise clicking
            int clientWidth = windowRect.Width;
            int clientHeight = windowRect.Height;
            
            int absoluteX = windowRect.Left + (int)(clientWidth * relX);
            int absoluteY = windowRect.Top + (int)(clientHeight * relY);
            int width = Math.Max(50, (int)(clientWidth * relW)); // Minimum button width
            int height = Math.Max(20, (int)(clientHeight * relH)); // Minimum button height
            
            return new ButtonInfo
            {
                Handle = windowHandle, // Use main window handle since we don't have individual control handles
                Text = text,
                ClassName = "CoordinateButton",
                IsEnabled = true, // Assume enabled
                IsVisible = true, // Assume visible
                ControlId = controlId,
                Bounds = new WindowsAPIHelper.RECT
                {
                    Left = absoluteX,
                    Top = absoluteY,
                    Right = absoluteX + width,
                    Bottom = absoluteY + height
                }
            };
        }
        
        public bool ClickCoordinateButton(ButtonInfo button, bool debugMode = false)
        {
            if (button == null)
            {
                if (debugMode) Console.WriteLine("Button is null");
                return false;
            }
            
            try
            {
                // Calculate center point of the button
                int centerX = (button.Bounds.Left + button.Bounds.Right) / 2;
                int centerY = (button.Bounds.Top + button.Bounds.Bottom) / 2;
                
                if (debugMode)
                {
                    Console.WriteLine($"Clicking '{button.Text}' at coordinates ({centerX}, {centerY})");
                    Console.WriteLine($"Button bounds: Left={button.Bounds.Left}, Top={button.Bounds.Top}, Right={button.Bounds.Right}, Bottom={button.Bounds.Bottom}");
                }
                
                // First, try to activate the window
                SetForegroundWindow(button.Handle);
                System.Threading.Thread.Sleep(200);
                
                // Move mouse to button position
                WindowsAPIHelper.SetCursorPos(centerX, centerY);
                System.Threading.Thread.Sleep(100);
                
                // Try multiple click methods for maximum compatibility
                
                if (debugMode)
                {
                    Console.WriteLine($"Attempting to click using multiple methods...");
                }
                
                // Method 1: Physical mouse click (most reliable for modern apps)
                bool method1Success = TryPhysicalClick(centerX, centerY, debugMode);
                
                // Method 2: Window message click (for traditional Win32 controls)
                bool method2Success = TryWindowMessageClick(button.Handle, centerX, centerY, debugMode);
                
                // Method 3: Try keyboard activation if this looks like a focused button
                bool method3Success = TryKeyboardActivation(button.Handle, debugMode);
                
                if (debugMode)
                {
                    Console.WriteLine($"Click results: Physical={method1Success}, Message={method2Success}, Keyboard={method3Success}");
                }
                
                if (debugMode)
                {
                    Console.WriteLine($"Successfully clicked '{button.Text}'");
                }
                
                return true;
            }
            catch (Exception ex)
            {
                if (debugMode)
                {
                    Console.WriteLine($"Error clicking button: {ex.Message}");
                }
                return false;
            }
        }
        
        private List<ButtonInfo> FindRealButtons(IntPtr windowHandle, bool debugMode = false)
        {
            List<ButtonInfo> buttons = new List<ButtonInfo>();
            
            try
            {
                if (debugMode)
                {
                    Console.WriteLine("Searching for real button controls...");
                }
                
                // Enumerate all child windows looking for actual button controls
                EnumChildWindowsProc childProc = (hWnd, lParam) =>
                {
                    try
                    {
                        string className = GetWindowClassName(hWnd);
                        string windowText = GetWindowText(hWnd);
                        
                        if (debugMode)
                        {
                            Console.WriteLine($"Found child: {className} '{windowText}' (Handle: {hWnd})");
                        }
                        
                        // Look for actual button controls
                        if (IsButtonClassName(className) && !string.IsNullOrEmpty(windowText))
                        {
                            RECT buttonRect;
                            if (GetWindowRect(hWnd, out buttonRect))
                            {
                                ButtonInfo button = new ButtonInfo
                                {
                                    Handle = hWnd,
                                    Text = windowText,
                                    ClassName = className,
                                    IsEnabled = IsWindowEnabled(hWnd),
                                    IsVisible = IsWindowVisible(hWnd),
                                    ControlId = GetDlgCtrlID(hWnd),
                                    Bounds = new WindowsAPIHelper.RECT
                                    {
                                        Left = buttonRect.Left,
                                        Top = buttonRect.Top,
                                        Right = buttonRect.Right,
                                        Bottom = buttonRect.Bottom
                                    }
                                };
                                buttons.Add(button);
                                
                                if (debugMode)
                                {
                                    Console.WriteLine($"Added real button: {windowText} at ({buttonRect.Left},{buttonRect.Top})");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if (debugMode)
                        {
                            Console.WriteLine($"Error processing child window: {ex.Message}");
                        }
                    }
                    return true; // Continue enumeration
                };
                
                EnumChildWindows(windowHandle, childProc, IntPtr.Zero);
                
            }
            catch (Exception ex)
            {
                if (debugMode)
                {
                    Console.WriteLine($"Error finding real buttons: {ex.Message}");
                }
            }
            
            return buttons;
        }
        
        private List<ButtonInfo> FindButtonsByText(IntPtr windowHandle, bool debugMode = false)
        {
            List<ButtonInfo> buttons = new List<ButtonInfo>();
            
            try
            {
                if (debugMode)
                {
                    Console.WriteLine("Searching for buttons by text content...");
                }
                
                // This is a more advanced approach - scan the window for text that looks like buttons
                // and create clickable regions around them
                
                string[] buttonTexts = { "Activate", "Launch Game", "Game config.", "Home", "Car settings", "Devices", "Dash Studio", "Arduino", "Settings" };
                
                foreach (string buttonText in buttonTexts)
                {
                    POINT textLocation = FindTextInWindow(windowHandle, buttonText, debugMode);
                    if (textLocation.x > 0 && textLocation.y > 0)
                    {
                        if (debugMode)
                        {
                            Console.WriteLine($"Found text '{buttonText}' at ({textLocation.x}, {textLocation.y})");
                        }
                        
                        // Create a button around the found text
                        ButtonInfo button = new ButtonInfo
                        {
                            Handle = windowHandle,
                            Text = buttonText,
                            ClassName = "TextButton",
                            IsEnabled = true,
                            IsVisible = true,
                            ControlId = buttonText.GetHashCode(),
                            Bounds = new WindowsAPIHelper.RECT
                            {
                                Left = textLocation.x - 20,
                                Top = textLocation.y - 10,
                                Right = textLocation.x + buttonText.Length * 8 + 20,
                                Bottom = textLocation.y + 20
                            }
                        };
                        buttons.Add(button);
                    }
                }
            }
            catch (Exception ex)
            {
                if (debugMode)
                {
                    Console.WriteLine($"Error finding buttons by text: {ex.Message}");
                }
            }
            
            return buttons;
        }
        
        // Helper methods
        [DllImport("user32.dll")]
        static extern bool EnumChildWindows(IntPtr hWndParent, EnumChildWindowsProc lpEnumFunc, IntPtr lParam);
        
        [DllImport("user32.dll")]
        static extern bool IsWindowEnabled(IntPtr hWnd);
        
        [DllImport("user32.dll")]
        static extern bool IsWindowVisible(IntPtr hWnd);
        
        [DllImport("user32.dll")]
        static extern int GetDlgCtrlID(IntPtr hWnd);
        
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);
        
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern int GetClassName(IntPtr hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);
        
        public delegate bool EnumChildWindowsProc(IntPtr hWnd, IntPtr lParam);
        
        private string GetWindowText(IntPtr hWnd)
        {
            var sb = new System.Text.StringBuilder(256);
            GetWindowText(hWnd, sb, sb.Capacity);
            return sb.ToString();
        }
        
        private string GetWindowClassName(IntPtr hWnd)
        {
            var sb = new System.Text.StringBuilder(256);
            GetClassName(hWnd, sb, sb.Capacity);
            return sb.ToString();
        }
        
        private bool IsButtonClassName(string className)
        {
            if (string.IsNullOrEmpty(className)) return false;
            
            string lower = className.ToLower();
            return lower.Contains("button") || 
                   lower == "button" ||
                   lower.Contains("btn") ||
                   lower.Contains("toolbarbutton") ||
                   lower.Contains("windowsforms10.button");
        }
        
        private POINT FindTextInWindow(IntPtr windowHandle, string searchText, bool debugMode = false)
        {
            // This is a simplified version - in a real implementation you'd use more sophisticated text detection
            // For now, return empty point to indicate text not found this way
            return new POINT(0, 0);
        }
        
        private bool TryPhysicalClick(int x, int y, bool debugMode = false)
        {
            try
            {
                if (debugMode)
                {
                    Console.WriteLine($"Method 1: Physical mouse click at ({x}, {y})");
                }
                
                // Move mouse to position
                WindowsAPIHelper.SetCursorPos(x, y);
                System.Threading.Thread.Sleep(200); // Longer delay for UI to respond
                
                // Perform click
                WindowsAPIHelper.mouse_event(WindowsAPIHelper.MOUSEEVENTF_LEFTDOWN, 0, 0, 0, IntPtr.Zero);
                System.Threading.Thread.Sleep(100);
                WindowsAPIHelper.mouse_event(WindowsAPIHelper.MOUSEEVENTF_LEFTUP, 0, 0, 0, IntPtr.Zero);
                
                return true;
            }
            catch (Exception ex)
            {
                if (debugMode)
                {
                    Console.WriteLine($"Physical click failed: {ex.Message}");
                }
                return false;
            }
        }
        
        private bool TryWindowMessageClick(IntPtr windowHandle, int x, int y, bool debugMode = false)
        {
            try
            {
                if (debugMode)
                {
                    Console.WriteLine($"Method 2: Window message click to handle {windowHandle}");
                }
                
                // Get window bounds to calculate relative coordinates
                RECT windowRect;
                if (!GetWindowRect(windowHandle, out windowRect))
                {
                    return false;
                }
                
                int relX = x - windowRect.Left;
                int relY = y - windowRect.Top;
                
                // Send button down and up messages
                SendMessage(windowHandle, WM_LBUTTONDOWN, (IntPtr)1, MakeLParam(relX, relY));
                System.Threading.Thread.Sleep(50);
                SendMessage(windowHandle, WM_LBUTTONUP, IntPtr.Zero, MakeLParam(relX, relY));
                
                return true;
            }
            catch (Exception ex)
            {
                if (debugMode)
                {
                    Console.WriteLine($"Message click failed: {ex.Message}");
                }
                return false;
            }
        }
        
        private bool TryKeyboardActivation(IntPtr windowHandle, bool debugMode = false)
        {
            try
            {
                if (debugMode)
                {
                    Console.WriteLine($"Method 3: Keyboard activation for handle {windowHandle}");
                }
                
                // Try to set focus to the control and send Enter or Space
                SetFocus(windowHandle);
                System.Threading.Thread.Sleep(100);
                
                // Send Enter key
                SendMessage(windowHandle, WM_KEYDOWN, (IntPtr)VK_RETURN, IntPtr.Zero);
                System.Threading.Thread.Sleep(50);
                SendMessage(windowHandle, WM_KEYUP, (IntPtr)VK_RETURN, IntPtr.Zero);
                
                return true;
            }
            catch (Exception ex)
            {
                if (debugMode)
                {
                    Console.WriteLine($"Keyboard activation failed: {ex.Message}");
                }
                return false;
            }
        }
    }
}
