using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ButtonRecognitionTool
{
    public class ButtonInfo
    {
        public IntPtr Handle { get; set; }
        public string Text { get; set; }
        public string ClassName { get; set; }
        public bool IsEnabled { get; set; }
        public bool IsVisible { get; set; }
        public WindowsAPIHelper.RECT Bounds { get; set; }
        public int ControlId { get; set; }

        public override string ToString()
        {
            return $"Button: '{Text}' (Class: {ClassName}, Enabled: {IsEnabled}, Visible: {IsVisible}, Handle: {Handle})";
        }
    }

    public class ApplicationInfo
    {
        public IntPtr MainWindowHandle { get; set; }
        public string ProcessName { get; set; }
        public string WindowTitle { get; set; }
        public int ProcessId { get; set; }
        public List<ButtonInfo> Buttons { get; set; } = new List<ButtonInfo>();

        public override string ToString()
        {
            return $"Application: {ProcessName} - '{WindowTitle}' (PID: {ProcessId}, Buttons: {Buttons.Count})";
        }
    }

    public class ButtonRecognizer
    {
        private List<string> buttonClassNames = new List<string>
        {
            "Button",
            "button",
            "BUTTON", 
            "ToolbarButton32",
            "TButton",
            "WindowsForms10.Button",
            "msctls_toolbarbutton32",
            // WPF Controls
            "Button",
            "System.Windows.Controls.Button",
            "WPF.Button",
            "ButtonBase",
            // Windows Forms variations
            "WindowsForms10.BUTTON",
            "WindowsForms10.button",
            // Qt Controls
            "QPushButton",
            "QToolButton",
            // Other common button classes
            "HwndHost",
            "Static",
            "STATIC",
            // Custom controls that might be buttons
            "Control",
            "UserControl"
        };

        public ApplicationInfo FindApplicationByName(string processName)
        {
            try
            {
                Process[] processes = Process.GetProcessesByName(processName);
                if (processes.Length == 0)
                {
                    Console.WriteLine($"No process found with name: {processName}");
                    return null;
                }

                Process targetProcess = processes[0]; // Take the first one if multiple
                IntPtr mainWindow = targetProcess.MainWindowHandle;

                if (mainWindow == IntPtr.Zero)
                {
                    Console.WriteLine($"Process {processName} has no main window");
                    return null;
                }

                ApplicationInfo appInfo = new ApplicationInfo
                {
                    MainWindowHandle = mainWindow,
                    ProcessName = targetProcess.ProcessName,
                    WindowTitle = WindowsAPIHelper.GetWindowText(mainWindow),
                    ProcessId = targetProcess.Id
                };

                Console.WriteLine($"Found application: {appInfo}");
                return appInfo;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error finding application: {ex.Message}");
                return null;
            }
        }

        public ApplicationInfo FindApplicationByWindowTitle(string windowTitle)
        {
            try
            {
                IntPtr mainWindow = WindowsAPIHelper.FindWindow(null, windowTitle);
                if (mainWindow == IntPtr.Zero)
                {
                    Console.WriteLine($"No window found with title: {windowTitle}");
                    return null;
                }

                // Try to get process information
                uint processId;
                GetWindowThreadProcessId(mainWindow, out processId);
                
                Process targetProcess = null;
                try
                {
                    targetProcess = Process.GetProcessById((int)processId);
                }
                catch
                {
                    // If we can't get process info, continue with limited information
                }

                ApplicationInfo appInfo = new ApplicationInfo
                {
                    MainWindowHandle = mainWindow,
                    ProcessName = targetProcess?.ProcessName ?? "Unknown",
                    WindowTitle = WindowsAPIHelper.GetWindowText(mainWindow),
                    ProcessId = (int)processId
                };

                Console.WriteLine($"Found application by window title: {appInfo}");
                return appInfo;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error finding application by window title: {ex.Message}");
                return null;
            }
        }

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        public void DiscoverButtons(ApplicationInfo appInfo, bool debugMode = false)
        {
            if (appInfo?.MainWindowHandle == IntPtr.Zero)
            {
                Console.WriteLine("Invalid application info");
                return;
            }

            appInfo.Buttons.Clear();
            Console.WriteLine($"Discovering buttons in {appInfo.WindowTitle}...");
            
            if (debugMode)
            {
                Console.WriteLine($"Main window handle: {appInfo.MainWindowHandle}");
                Console.WriteLine($"Main window class: {WindowsAPIHelper.GetClassName(appInfo.MainWindowHandle)}");
                Console.WriteLine($"Main window visible: {WindowsAPIHelper.IsWindowVisible(appInfo.MainWindowHandle)}");
            }

            try
            {
                List<string> allControls = new List<string>();
                List<IntPtr> allHandles = new List<IntPtr>();
                
                // First try to find direct child windows
                EnumerateChildWindows(appInfo.MainWindowHandle, appInfo.Buttons, debugMode ? allControls : null, debugMode ? allHandles : null);
                
                // If no child windows found, try alternative methods
                if (allControls.Count == 0 && debugMode)
                {
                    Console.WriteLine("No direct child windows found. Trying alternative enumeration...");
                    EnumerateProcessWindows(appInfo.MainWindowHandle, allControls, allHandles);
                }
                
                // If still no buttons found, try UI Automation for modern frameworks
                if (appInfo.Buttons.Count == 0)
                {
                    Console.WriteLine("Trying UI Automation for modern UI frameworks...");
                    TryUIAutomation(appInfo, debugMode);
                }
                
                Console.WriteLine($"Found {appInfo.Buttons.Count} buttons");

                if (debugMode)
                {
                    Console.WriteLine($"Total controls examined: {allControls.Count}");
                    Console.WriteLine($"Total window handles: {allHandles.Count}");
                    
                    if (allControls.Count > 0)
                    {
                        Console.WriteLine($"\n=== DEBUG: All found controls ({allControls.Count}) ===");
                        var uniqueClasses = allControls.Distinct().OrderBy(c => c);
                        foreach (var className in uniqueClasses)
                        {
                            int count = allControls.Count(c => c == className);
                            Console.WriteLine($"  {className} ({count})");
                        }
                    }
                    else
                    {
                        Console.WriteLine("\n=== DEBUG: No child controls found ===");
                        Console.WriteLine("This application might be using:");
                        Console.WriteLine("- Modern UI framework (WPF, UWP, WinUI)");
                        Console.WriteLine("- Custom rendering (DirectX, OpenGL)");
                        Console.WriteLine("- Web-based UI (Electron, CEF)");
                    }
                    Console.WriteLine();
                }

                foreach (var button in appInfo.Buttons)
                {
                    Console.WriteLine($"  - {button}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error discovering buttons: {ex.Message}");
            }
        }

        private void EnumerateChildWindows(IntPtr parentHandle, List<ButtonInfo> buttons, List<string> allControls = null, List<IntPtr> allHandles = null)
        {
            IntPtr childHandle = WindowsAPIHelper.GetWindow(parentHandle, WindowsAPIHelper.GW_CHILD);
            
            while (childHandle != IntPtr.Zero)
            {
                try
                {
                    string className = WindowsAPIHelper.GetClassName(childHandle);
                    
                    // For debug mode, collect all control class names and handles
                    if (allControls != null && !string.IsNullOrEmpty(className))
                    {
                        allControls.Add(className);
                    }
                    if (allHandles != null)
                    {
                        allHandles.Add(childHandle);
                    }
                    
                    if (IsButtonClass(className))
                    {
                        ButtonInfo buttonInfo = CreateButtonInfo(childHandle, className);
                        if (buttonInfo != null)
                        {
                            buttons.Add(buttonInfo);
                        }
                    }

                    // Recursively search child windows
                    EnumerateChildWindows(childHandle, buttons, allControls, allHandles);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing child window: {ex.Message}");
                }

                childHandle = WindowsAPIHelper.GetWindow(childHandle, WindowsAPIHelper.GW_HWNDNEXT);
            }
        }

        private bool IsButtonClass(string className)
        {
            if (string.IsNullOrEmpty(className)) return false;
            
            // Check explicit button class names first
            bool isExplicitButton = buttonClassNames.Exists(c => 
                string.Equals(c, className, StringComparison.OrdinalIgnoreCase));
                
            if (isExplicitButton) return true;
            
            // Check for common button patterns
            string lowerClassName = className.ToLower();
            return lowerClassName.Contains("button") || 
                   lowerClassName.Contains("btn") ||
                   // Qt specific patterns
                   lowerClassName.Contains("qpushbutton") ||
                   lowerClassName.Contains("qtoolbutton") ||
                   // Windows Forms patterns  
                   lowerClassName.Contains("windowsforms") && lowerClassName.Contains("button") ||
                   // WPF patterns
                   lowerClassName.Contains("system.windows.controls.button");
        }

        private ButtonInfo CreateButtonInfo(IntPtr handle, string className)
        {
            try
            {
                WindowsAPIHelper.RECT bounds;
                WindowsAPIHelper.GetWindowRect(handle, out bounds);
                
                ButtonInfo button = new ButtonInfo
                {
                    Handle = handle,
                    Text = WindowsAPIHelper.GetWindowText(handle),
                    ClassName = className,
                    IsEnabled = WindowsAPIHelper.IsWindowEnabled(handle),
                    IsVisible = WindowsAPIHelper.IsWindowVisible(handle),
                    ControlId = WindowsAPIHelper.GetDlgCtrlID(handle),
                    Bounds = bounds
                };
                return button;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating button info: {ex.Message}");
                return null;
            }
        }

        public ButtonInfo FindButtonByText(ApplicationInfo appInfo, string buttonText)
        {
            if (appInfo?.Buttons == null) return null;

            foreach (var button in appInfo.Buttons)
            {
                if (string.Equals(button.Text, buttonText, StringComparison.OrdinalIgnoreCase))
                {
                    return button;
                }
            }
            return null;
        }

        public List<ButtonInfo> FindButtonsContainingText(ApplicationInfo appInfo, string partialText)
        {
            List<ButtonInfo> matchingButtons = new List<ButtonInfo>();
            
            if (appInfo?.Buttons == null) return matchingButtons;

            foreach (var button in appInfo.Buttons)
            {
                if (!string.IsNullOrEmpty(button.Text) && 
                    button.Text.ToLower().Contains(partialText.ToLower()))
                {
                    matchingButtons.Add(button);
                }
            }
            return matchingButtons;
        }

        public bool ClickButton(ButtonInfo button)
        {
            if (button?.Handle == IntPtr.Zero)
            {
                Console.WriteLine("Invalid button handle");
                return false;
            }

            if (!button.IsEnabled)
            {
                Console.WriteLine($"Button '{button.Text}' is not enabled");
                return false;
            }

            try
            {
                Console.WriteLine($"Clicking button: '{button.Text}'");
                
                // Try multiple click methods for better compatibility
                WindowsAPIHelper.ClickButton(button.Handle);
                
                // Alternative method: physical mouse click
                // WindowsAPIHelper.ClickButtonAtPosition(button.Handle);
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error clicking button: {ex.Message}");
                return false;
            }
        }

        public void RefreshButtonStates(ApplicationInfo appInfo)
        {
            if (appInfo?.Buttons == null) return;

            Console.WriteLine("Refreshing button states...");
            foreach (var button in appInfo.Buttons)
            {
                try
                {
                    button.IsEnabled = WindowsAPIHelper.IsWindowEnabled(button.Handle);
                    button.IsVisible = WindowsAPIHelper.IsWindowVisible(button.Handle);
                    button.Text = WindowsAPIHelper.GetWindowText(button.Handle);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error refreshing button state: {ex.Message}");
                }
            }
        }
        
        private void EnumerateWindowsAlternative(IntPtr parentHandle, List<string> allControls, List<IntPtr> allHandles)
        {
            try
            {
                Console.WriteLine("Trying EnumChildWindows API...");
                
                // Try EnumChildWindows API as alternative
                EnumChildWindowsProc childProc = (hWnd, lParam) =>
                {
                    try
                    {
                        string className = WindowsAPIHelper.GetClassName(hWnd);
                        if (!string.IsNullOrEmpty(className))
                        {
                            allControls.Add(className);
                            allHandles.Add(hWnd);
                            Console.WriteLine($"Found child: {className} (Handle: {hWnd})");
                        }
                    }
                    catch { }
                    return true; // Continue enumeration
                };
                
                EnumChildWindows(parentHandle, childProc, IntPtr.Zero);
                
                // If still no children, try to enumerate all windows from this process
                if (allControls.Count == 0)
                {
                    Console.WriteLine("No child windows found, trying process-wide window enumeration...");
                    EnumerateProcessWindows(parentHandle, allControls, allHandles);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Alternative enumeration failed: {ex.Message}");
            }
        }
        
        private void EnumerateProcessWindows(IntPtr mainWindowHandle, List<string> allControls, List<IntPtr> allHandles)
        {
            try
            {
                // Get the process ID from main window
                uint processId;
                GetWindowThreadProcessId(mainWindowHandle, out processId);
                
                Console.WriteLine($"Enumerating all windows for process ID: {processId}");
                
                // Enumerate all windows and filter by process ID
                EnumWindowsProc windowProc = (hWnd, lParam) =>
                {
                    try
                    {
                        uint windowProcessId;
                        GetWindowThreadProcessId(hWnd, out windowProcessId);
                        
                        // Only process windows belonging to our target process
                        if (windowProcessId == processId)
                        {
                            string className = WindowsAPIHelper.GetClassName(hWnd);
                            string windowText = WindowsAPIHelper.GetWindowText(hWnd);
                            bool isVisible = WindowsAPIHelper.IsWindowVisible(hWnd);
                            
                            if (!string.IsNullOrEmpty(className))
                            {
                                allControls.Add(className);
                                allHandles.Add(hWnd);
                                Console.WriteLine($"Process window: {className} | Text: '{windowText}' | Visible: {isVisible} | Handle: {hWnd}");
                                
                                // Also try to find children of this window
                                EnumChildWindows(hWnd, (childHWnd, childLParam) =>
                                {
                                    try
                                    {
                                        string childClassName = WindowsAPIHelper.GetClassName(childHWnd);
                                        string childText = WindowsAPIHelper.GetWindowText(childHWnd);
                                        if (!string.IsNullOrEmpty(childClassName))
                                        {
                                            allControls.Add(childClassName);
                                            allHandles.Add(childHWnd);
                                            Console.WriteLine($"  Child: {childClassName} | Text: '{childText}' | Handle: {childHWnd}");
                                        }
                                    }
                                    catch { }
                                    return true;
                                }, IntPtr.Zero);
                            }
                        }
                    }
                    catch { }
                    return true; // Continue enumeration
                };
                
                EnumWindows(windowProc, IntPtr.Zero);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Process window enumeration failed: {ex.Message}");
            }
        }
        
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool EnumChildWindows(IntPtr hWndParent, EnumChildWindowsProc lpEnumFunc, IntPtr lParam);
        
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
        
        public delegate bool EnumChildWindowsProc(IntPtr hWnd, IntPtr lParam);
        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
        
        private void TryUIAutomation(ApplicationInfo appInfo, bool debugMode)
        {
            try
            {
                // Try modern UI pattern detection
                var modernUIHelper = new ModernUIHelper();
                var modernButtons = modernUIHelper.TryModernUIDetection(appInfo.MainWindowHandle, debugMode);
                
                if (modernButtons.Count > 0)
                {
                    appInfo.Buttons.AddRange(modernButtons);
                    if (debugMode)
                    {
                        Console.WriteLine($"Modern UI detection found {modernButtons.Count} button(s)");
                    }
                }
                
                // Show helpful tips for this type of application
                string windowClass = WindowsAPIHelper.GetClassName(appInfo.MainWindowHandle);
                modernUIHelper.ShowDetectionTips(windowClass, debugMode);
            }
            catch (Exception ex)
            {
                if (debugMode)
                {
                    Console.WriteLine($"Modern UI framework detection failed: {ex.Message}");
                }
            }
        }
    }
}
