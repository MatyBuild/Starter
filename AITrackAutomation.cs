using System;
using System.Diagnostics;
using System.Threading;

namespace ButtonRecognitionTool
{
    public class AITrackAutomation
    {
        private ButtonRecognizer recognizer;
        private ApplicationInfo aiTrackApp;
        
        public AITrackAutomation()
        {
            recognizer = new ButtonRecognizer();
        }
        
        public bool OpenAITrackAndStartTracking()
        {
            try
            {
                Console.WriteLine("=== AITrack Automation Script ===");
                
                // 1. Launch AITrack if not running
                if (!LaunchAITrack())
                {
                    Console.WriteLine("Failed to launch AITrack");
                    return false;
                }
                
                // 2. Wait for AITrack to fully load
                Console.WriteLine("Waiting for AITrack to load...");
                Thread.Sleep(3000); // AITrack usually loads faster than SimHub
                
                // 3. Find AITrack application
                aiTrackApp = recognizer.FindApplicationByName("aitrack");
                if (aiTrackApp == null)
                {
                    // Try alternative names
                    aiTrackApp = recognizer.FindApplicationByName("AITrack");
                }
                
                if (aiTrackApp == null)
                {
                    Console.WriteLine("AITrack application not found");
                    return false;
                }
                
                Console.WriteLine($"Found AITrack: {aiTrackApp}");
                
                // 4. Discover buttons (this will create coordinate-based buttons for Qt)
                recognizer.DiscoverButtons(aiTrackApp, false); // Set to true for debug
                
                if (aiTrackApp.Buttons.Count == 0)
                {
                    Console.WriteLine("No buttons found in AITrack");
                    return false;
                }
                
                Console.WriteLine($"Found {aiTrackApp.Buttons.Count} buttons in AITrack");
                
                // 5. Click the Start Tracking button
                return ClickStartTrackingButton();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in AITrack automation: {ex.Message}");
                return false;
            }
        }
        
        private bool LaunchAITrack()
        {
            try
            {
                // Check if AITrack is already running
                Process[] existingProcesses = Process.GetProcessesByName("aitrack");
                if (existingProcesses.Length == 0)
                {
                    existingProcesses = Process.GetProcessesByName("AITrack");
                }
                
                if (existingProcesses.Length > 0)
                {
                    Console.WriteLine("AITrack is already running");
                    return true;
                }
                
                // Launch AITrack - common paths
                string[] possiblePaths = {
                    @"C:\Program Files\AITrack\AITrack.exe",
                    @"C:\Program Files (x86)\AITrack\AITrack.exe",
                    @"C:\Users\" + Environment.UserName + @"\Desktop\AITrack.exe",
                    @"C:\Users\" + Environment.UserName + @"\Downloads\AITrack\AITrack.exe",
                    @"D:\AITrack\AITrack.exe",
                    @".\AITrack.exe" // Current directory
                };
                
                string aiTrackPath = null;
                foreach (string path in possiblePaths)
                {
                    if (System.IO.File.Exists(path))
                    {
                        aiTrackPath = path;
                        break;
                    }
                }
                
                if (aiTrackPath == null)
                {
                    Console.WriteLine("AITrack executable not found in common locations:");
                    foreach (string path in possiblePaths)
                    {
                        Console.WriteLine($"  - {path}");
                    }
                    Console.WriteLine("Please update the path in the script or place AITrack.exe in the current directory");
                    return false;
                }
                
                Console.WriteLine($"Launching AITrack from {aiTrackPath}");
                Process.Start(aiTrackPath);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error launching AITrack: {ex.Message}");
                return false;
            }
        }
        
        private bool ClickStartTrackingButton()
        {
            try
            {
                // Find the Start Tracking button (might have different variations)
                ButtonInfo trackingButton = null;
                
                // Try different possible button texts
                string[] buttonTexts = { 
                    "Start Tracking", 
                    "Start Tracking (Estimated)", 
                    "Start", 
                    "Track",
                    "Begin Tracking"
                };
                
                foreach (string buttonText in buttonTexts)
                {
                    trackingButton = recognizer.FindButtonByText(aiTrackApp, buttonText);
                    if (trackingButton != null)
                    {
                        Console.WriteLine($"Found tracking button with text: '{buttonText}'");
                        break;
                    }
                }
                
                if (trackingButton == null)
                {
                    Console.WriteLine("Start Tracking button not found");
                    
                    // List available buttons for debugging
                    Console.WriteLine("Available buttons:");
                    foreach (var btn in aiTrackApp.Buttons)
                    {
                        Console.WriteLine($"  - {btn.Text}");
                    }
                    return false;
                }
                
                Console.WriteLine($"Found Start Tracking button: {trackingButton}");
                
                // Click the button
                bool success = recognizer.ClickButton(trackingButton);
                if (success)
                {
                    Console.WriteLine("Successfully clicked Start Tracking button!");
                    
                    // Wait a moment for the action to complete
                    Thread.Sleep(1000);
                    return true;
                }
                else
                {
                    Console.WriteLine("Failed to click Start Tracking button");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error clicking Start Tracking button: {ex.Message}");
                return false;
            }
        }
        
        // Additional helper methods for other AITrack operations
        public bool StopTracking()
        {
            if (aiTrackApp == null)
            {
                Console.WriteLine("AITrack not initialized. Call OpenAITrackAndStartTracking() first.");
                return false;
            }
            
            // Refresh buttons to get current state
            recognizer.DiscoverButtons(aiTrackApp, false);
            
            var stopButton = recognizer.FindButtonByText(aiTrackApp, "Stop Tracking");
            if (stopButton == null)
            {
                stopButton = recognizer.FindButtonByText(aiTrackApp, "Stop");
            }
            
            if (stopButton == null)
            {
                Console.WriteLine("Stop Tracking button not found");
                return false;
            }
            
            return recognizer.ClickButton(stopButton);
        }
        
        public bool ClickButton(string buttonText)
        {
            if (aiTrackApp == null)
            {
                Console.WriteLine("AITrack not initialized. Call OpenAITrackAndStartTracking() first.");
                return false;
            }
            
            var button = recognizer.FindButtonByText(aiTrackApp, buttonText);
            if (button == null)
            {
                Console.WriteLine($"Button '{buttonText}' not found");
                return false;
            }
            
            return recognizer.ClickButton(button);
        }
        
        public void ListAllButtons()
        {
            if (aiTrackApp == null)
            {
                Console.WriteLine("AITrack not initialized");
                return;
            }
            
            Console.WriteLine("Available AITrack buttons:");
            foreach (var button in aiTrackApp.Buttons)
            {
                Console.WriteLine($"  - {button.Text}");
            }
        }
        
        public bool IsTrackingActive()
        {
            if (aiTrackApp == null)
                return false;
            
            // Refresh buttons to check current state
            recognizer.DiscoverButtons(aiTrackApp, false);
            
            // If we find "Stop Tracking" button, tracking is probably active
            var stopButton = recognizer.FindButtonByText(aiTrackApp, "Stop Tracking");
            return stopButton != null;
        }
    }
}