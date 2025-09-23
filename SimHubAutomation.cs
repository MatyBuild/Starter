using System;
using System.Diagnostics;
using System.Threading;

namespace ButtonRecognitionTool
{
    public class SimHubAutomation
    {
        private ButtonRecognizer recognizer;
        private ApplicationInfo simHubApp;
        
        public SimHubAutomation()
        {
            recognizer = new ButtonRecognizer();
        }
        
        public bool OpenSimHubAndActivate()
        {
            try
            {
                Console.WriteLine("=== SimHub Automation Script ===");
                
                // 1. Launch SimHub if not running
                if (!LaunchSimHub())
                {
                    Console.WriteLine("Failed to launch SimHub");
                    return false;
                }
                
                // 2. Wait for SimHub to fully load
                Console.WriteLine("Waiting for SimHub to load...");
                Thread.Sleep(5000);
                
                // 3. Find SimHub application
                simHubApp = recognizer.FindApplicationByName("simhub");
                if (simHubApp == null)
                {
                    // Try alternative name
                    simHubApp = recognizer.FindApplicationByName("SimHubWPF");
                }
                
                if (simHubApp == null)
                {
                    Console.WriteLine("SimHub application not found");
                    return false;
                }
                
                Console.WriteLine($"Found SimHub: {simHubApp}");
                
                // 4. Discover buttons (this will create coordinate-based buttons)
                recognizer.DiscoverButtons(simHubApp, false); // Set to true for debug
                
                if (simHubApp.Buttons.Count == 0)
                {
                    Console.WriteLine("No buttons found in SimHub");
                    return false;
                }
                
                Console.WriteLine($"Found {simHubApp.Buttons.Count} buttons in SimHub");
                
                // 5. Click the Activate button
                return ClickActivateButton();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SimHub automation: {ex.Message}");
                return false;
            }
        }
        
        private bool LaunchSimHub()
        {
            try
            {
                // Check if SimHub is already running
                Process[] existingProcesses = Process.GetProcessesByName("simhub");
                if (existingProcesses.Length == 0)
                {
                    existingProcesses = Process.GetProcessesByName("SimHubWPF");
                }
                
                if (existingProcesses.Length > 0)
                {
                    Console.WriteLine("SimHub is already running");
                    return true;
                }
                
                // Launch SimHub - you'll need to adjust the path
                string simHubPath = @"C:\Program Files (x86)\SimHub\SimHubWPF.exe";
                
                // Alternative common paths:
                // string simHubPath = @"C:\Users\{username}\Desktop\SimHub\SimHubWPF.exe";
                // string simHubPath = @"D:\SimHub\SimHubWPF.exe";
                
                if (!System.IO.File.Exists(simHubPath))
                {
                    Console.WriteLine($"SimHub not found at {simHubPath}");
                    Console.WriteLine("Please update the path in the script");
                    return false;
                }
                
                Console.WriteLine($"Launching SimHub from {simHubPath}");
                Process.Start(simHubPath);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error launching SimHub: {ex.Message}");
                return false;
            }
        }
        
        private bool ClickActivateButton()
        {
            try
            {
                // Find the Activate button
                var activateButton = recognizer.FindButtonByText(simHubApp, "Activate");
                if (activateButton == null)
                {
                    Console.WriteLine("Activate button not found");
                    
                    // List available buttons for debugging
                    Console.WriteLine("Available buttons:");
                    foreach (var btn in simHubApp.Buttons)
                    {
                        Console.WriteLine($"  - {btn.Text}");
                    }
                    return false;
                }
                
                Console.WriteLine($"Found Activate button: {activateButton}");
                
                // Click the button
                bool success = recognizer.ClickButton(activateButton);
                if (success)
                {
                    Console.WriteLine("Successfully clicked Activate button!");
                    
                    // Wait a moment for the action to complete
                    Thread.Sleep(1000);
                    return true;
                }
                else
                {
                    Console.WriteLine("Failed to click Activate button");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error clicking Activate button: {ex.Message}");
                return false;
            }
        }
        
        // Additional helper methods for other SimHub operations
        public bool ClickButton(string buttonText)
        {
            if (simHubApp == null)
            {
                Console.WriteLine("SimHub not initialized. Call OpenSimHubAndActivate() first.");
                return false;
            }
            
            var button = recognizer.FindButtonByText(simHubApp, buttonText);
            if (button == null)
            {
                Console.WriteLine($"Button '{buttonText}' not found");
                return false;
            }
            
            return recognizer.ClickButton(button);
        }
        
        public void ListAllButtons()
        {
            if (simHubApp == null)
            {
                Console.WriteLine("SimHub not initialized");
                return;
            }
            
            Console.WriteLine("Available buttons:");
            foreach (var button in simHubApp.Buttons)
            {
                Console.WriteLine($"  - {button.Text}");
            }
        }
    }
    
    // Ukázka použití
    class SimHubScript
    {
        static void RunSimHubAutomation()
        {
            var automation = new SimHubAutomation();
            
            // Otevře SimHub a klikne na Activate
            bool success = automation.OpenSimHubAndActivate();
            
            if (success)
            {
                Console.WriteLine("SimHub automation completed successfully!");
                
                // Můžete kliknout na další tlačítka
                // automation.ClickButton("Launch Game");
                // automation.ClickButton("Settings");
            }
            else
            {
                Console.WriteLine("SimHub automation failed!");
            }
        }
    }
}