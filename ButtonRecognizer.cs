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

            try
            {
                List<string> allControls = new List<string>();
                EnumerateChildWindows(appInfo.MainWindowHandle, appInfo.Buttons, debugMode ? allControls : null);
                
                Console.WriteLine($"Found {appInfo.Buttons.Count} buttons");

                if (debugMode && allControls.Count > 0)
                {
                    Console.WriteLine($"\n=== DEBUG: All found controls ({allControls.Count}) ===\n");
                    var uniqueClasses = allControls.Distinct().OrderBy(c => c);
                    foreach (var className in uniqueClasses)
                    {
                        int count = allControls.Count(c => c == className);
                        Console.WriteLine($"  {className} ({count})");
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

        private void EnumerateChildWindows(IntPtr parentHandle, List<ButtonInfo> buttons, List<string> allControls = null)
        {
            IntPtr childHandle = WindowsAPIHelper.GetWindow(parentHandle, WindowsAPIHelper.GW_CHILD);
            
            while (childHandle != IntPtr.Zero)
            {
                try
                {
                    string className = WindowsAPIHelper.GetClassName(childHandle);
                    
                    // For debug mode, collect all control class names
                    if (allControls != null && !string.IsNullOrEmpty(className))
                    {
                        allControls.Add(className);
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
                    EnumerateChildWindows(childHandle, buttons, allControls);
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
            
            return buttonClassNames.Exists(c => 
                string.Equals(c, className, StringComparison.OrdinalIgnoreCase) ||
                className.ToLower().Contains("button"));
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
    }
}