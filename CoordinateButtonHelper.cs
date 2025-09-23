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
                    Console.WriteLine("Creating coordinate-based buttons for SimHub...");
                }
                
                // Get window bounds
                RECT windowRect;
                if (!GetWindowRect(windowHandle, out windowRect))
                {
                    if (debugMode) Console.WriteLine("Could not get window rectangle");
                    return buttons;
                }
                
                RECT clientRect;
                if (!GetClientRect(windowHandle, out clientRect))
                {
                    if (debugMode) Console.WriteLine("Could not get client rectangle");
                    return buttons;
                }
                
                if (debugMode)
                {
                    Console.WriteLine($"Window rect: ({windowRect.Left}, {windowRect.Top}) - ({windowRect.Right}, {windowRect.Bottom})");
                    Console.WriteLine($"Client rect: ({clientRect.Left}, {clientRect.Top}) - ({clientRect.Right}, {clientRect.Bottom})");
                    Console.WriteLine($"Window size: {windowRect.Width}x{windowRect.Height}");
                }
                
                // Based on the SimHub screenshot, create buttons at typical positions
                // These coordinates are estimates based on common SimHub layouts
                
                // Top section buttons (Activate, Launch Game, Game config.)
                buttons.Add(CreateCoordinateButton(windowHandle, windowRect, 0.35f, 0.12f, 0.08f, 0.04f, "Activate", "ActivateBtn", 1));
                buttons.Add(CreateCoordinateButton(windowHandle, windowRect, 0.55f, 0.12f, 0.10f, 0.04f, "Launch Game", "LaunchGameBtn", 2));
                buttons.Add(CreateCoordinateButton(windowHandle, windowRect, 0.75f, 0.12f, 0.08f, 0.04f, "Game config.", "GameConfigBtn", 3));
                
                // Device section buttons
                buttons.Add(CreateCoordinateButton(windowHandle, windowRect, 0.65f, 0.28f, 0.08f, 0.04f, "Add device", "AddDeviceBtn", 4));
                buttons.Add(CreateCoordinateButton(windowHandle, windowRect, 0.82f, 0.28f, 0.15f, 0.04f, "Import device from file", "ImportDeviceBtn", 5));
                
                // License section buttons  
                buttons.Add(CreateCoordinateButton(windowHandle, windowRect, 0.65f, 0.82f, 0.20f, 0.04f, "TEST LICENSED FULL SPEED", "TestLicenseBtn", 6));
                buttons.Add(CreateCoordinateButton(windowHandle, windowRect, 0.65f, 0.88f, 0.15f, 0.04f, "GET LICENSED EDITION", "GetLicenseBtn", 7));
                buttons.Add(CreateCoordinateButton(windowHandle, windowRect, 0.65f, 0.94f, 0.15f, 0.04f, "LOAD MY LICENSE FILE", "LoadLicenseBtn", 8));
                
                // Left sidebar menu items (these act like buttons too)
                buttons.Add(CreateCoordinateButton(windowHandle, windowRect, 0.12f, 0.15f, 0.10f, 0.04f, "Home", "HomeBtn", 9));
                buttons.Add(CreateCoordinateButton(windowHandle, windowRect, 0.12f, 0.20f, 0.10f, 0.04f, "Car settings", "CarSettingsBtn", 10));
                buttons.Add(CreateCoordinateButton(windowHandle, windowRect, 0.12f, 0.25f, 0.10f, 0.04f, "Devices", "DevicesBtn", 11));
                buttons.Add(CreateCoordinateButton(windowHandle, windowRect, 0.12f, 0.30f, 0.10f, 0.04f, "Dash Studio", "DashStudioBtn", 12));
                buttons.Add(CreateCoordinateButton(windowHandle, windowRect, 0.12f, 0.35f, 0.10f, 0.04f, "Arduino", "ArduinoBtn", 13));
                buttons.Add(CreateCoordinateButton(windowHandle, windowRect, 0.12f, 0.77f, 0.10f, 0.04f, "Settings", "SettingsBtn", 14));
                
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
        
        public List<ButtonInfo> CreateAITrackButtons(IntPtr windowHandle, bool debugMode = false)
        {
            List<ButtonInfo> buttons = new List<ButtonInfo>();
            
            try
            {
                if (debugMode)
                {
                    Console.WriteLine("Creating coordinate-based buttons for AITrack...");
                }
                
                // Get window bounds
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
                
                // AITrack typically has buttons in the bottom area
                // Based on typical AITrack layout:
                
                // Main tracking button (usually center-bottom)
                buttons.Add(CreateCoordinateButton(windowHandle, windowRect, 0.5f, 0.8f, 0.2f, 0.08f, "Start Tracking", "StartTrackingBtn", 1));
                buttons.Add(CreateCoordinateButton(windowHandle, windowRect, 0.5f, 0.8f, 0.2f, 0.08f, "Start Tracking (Estimated)", "StartTrackingEstBtn", 2));
                
                // Stop button (usually appears after start)
                buttons.Add(CreateCoordinateButton(windowHandle, windowRect, 0.5f, 0.8f, 0.2f, 0.08f, "Stop Tracking", "StopTrackingBtn", 3));
                
                // Configuration/Settings buttons (usually top-right or bottom)
                buttons.Add(CreateCoordinateButton(windowHandle, windowRect, 0.8f, 0.1f, 0.15f, 0.06f, "Settings", "SettingsBtn", 4));
                buttons.Add(CreateCoordinateButton(windowHandle, windowRect, 0.1f, 0.9f, 0.15f, 0.06f, "Config", "ConfigBtn", 5));
                
                // Camera selection (usually top area)
                buttons.Add(CreateCoordinateButton(windowHandle, windowRect, 0.3f, 0.1f, 0.15f, 0.06f, "Camera", "CameraBtn", 6));
                
                if (debugMode)
                {
                    Console.WriteLine($"Created {buttons.Count} coordinate-based buttons for AITrack");
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
                    Console.WriteLine($"Error creating AITrack buttons: {ex.Message}");
                }
            }
            
            return buttons;
        }
        
        private ButtonInfo CreateCoordinateButton(IntPtr windowHandle, RECT windowRect, float relX, float relY, float relW, float relH, string text, string id, int controlId)
        {
            // Convert relative coordinates to absolute screen coordinates
            int absoluteX = windowRect.Left + (int)(windowRect.Width * relX);
            int absoluteY = windowRect.Top + (int)(windowRect.Height * relY);
            int width = (int)(windowRect.Width * relW);
            int height = (int)(windowRect.Height * relH);
            
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
                }
                
                // Move mouse to button and click
                WindowsAPIHelper.SetCursorPos(centerX, centerY);
                System.Threading.Thread.Sleep(100); // Small delay
                
                WindowsAPIHelper.mouse_event(WindowsAPIHelper.MOUSEEVENTF_LEFTDOWN, (uint)centerX, (uint)centerY, 0, IntPtr.Zero);
                System.Threading.Thread.Sleep(50);
                WindowsAPIHelper.mouse_event(WindowsAPIHelper.MOUSEEVENTF_LEFTUP, (uint)centerX, (uint)centerY, 0, IntPtr.Zero);
                
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
    }
}