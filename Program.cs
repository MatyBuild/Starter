using System;
using System.Collections.Generic;
using System.Threading;

namespace ButtonRecognitionTool
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== Button Recognition Tool ===");
            Console.WriteLine("This tool can find and interact with buttons in any Windows application.\n");

            ButtonRecognizer recognizer = new ButtonRecognizer();
            ApplicationInfo currentApp = null;

            while (true)
            {
                try
                {
                    ShowMenu();
                    string choice = Console.ReadLine()?.Trim();

                    switch (choice?.ToLower())
                    {
                        case "1":
                            currentApp = FindApplicationByProcess(recognizer);
                            break;
                        case "2":
                            currentApp = FindApplicationByWindowTitle(recognizer);
                            break;
                        case "3":
                            DiscoverButtons(recognizer, currentApp);
                            break;
                        case "4":
                            RefreshButtons(recognizer, currentApp);
                            break;
                        case "5":
                            ClickButtonByText(recognizer, currentApp);
                            break;
                        case "6":
                            SearchButtons(recognizer, currentApp);
                            break;
                        case "7":
                            ListAllButtons(currentApp);
                            break;
                        case "8":
                            MonitorButtonStates(recognizer, currentApp);
                            break;
                        case "9":
                        case "q":
                        case "quit":
                        case "exit":
                            Console.WriteLine("Goodbye!");
                            return;
                        default:
                            Console.WriteLine("Invalid choice. Please try again.");
                            break;
                    }

                    if (choice != "9" && choice?.ToLower() != "q" && 
                        choice?.ToLower() != "quit" && choice?.ToLower() != "exit")
                    {
                        Console.WriteLine("\nPress any key to continue...");
                        Console.ReadKey();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred: {ex.Message}");
                    Console.WriteLine("Press any key to continue...");
                    Console.ReadKey();
                }
            }
        }

        static void ShowMenu()
        {
            Console.Clear();
            Console.WriteLine("=== Button Recognition Tool - Main Menu ===");
            Console.WriteLine();
            Console.WriteLine("1. Find Application by Process Name");
            Console.WriteLine("2. Find Application by Window Title");
            Console.WriteLine("3. Discover Buttons in Current Application");
            Console.WriteLine("4. Refresh Button States");
            Console.WriteLine("5. Click Button by Text");
            Console.WriteLine("6. Search Buttons");
            Console.WriteLine("7. List All Buttons");
            Console.WriteLine("8. Monitor Button States");
            Console.WriteLine("9. Exit");
            Console.WriteLine();
            Console.Write("Enter your choice: ");
        }

        static ApplicationInfo FindApplicationByProcess(ButtonRecognizer recognizer)
        {
            Console.WriteLine("\n=== Find Application by Process Name ===");
            Console.Write("Enter the process name (without .exe): ");
            string processName = Console.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(processName))
            {
                Console.WriteLine("Process name cannot be empty.");
                return null;
            }

            ApplicationInfo app = recognizer.FindApplicationByName(processName);
            if (app != null)
            {
                Console.WriteLine($"\n✓ Successfully found application: {app}");
            }
            else
            {
                Console.WriteLine($"\n✗ Could not find process: {processName}");
                Console.WriteLine("Make sure the application is running and has a visible window.");
            }

            return app;
        }

        static ApplicationInfo FindApplicationByWindowTitle(ButtonRecognizer recognizer)
        {
            Console.WriteLine("\n=== Find Application by Window Title ===");
            Console.Write("Enter the window title (exact match): ");
            string windowTitle = Console.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(windowTitle))
            {
                Console.WriteLine("Window title cannot be empty.");
                return null;
            }

            ApplicationInfo app = recognizer.FindApplicationByWindowTitle(windowTitle);
            if (app != null)
            {
                Console.WriteLine($"\n✓ Successfully found application: {app}");
            }
            else
            {
                Console.WriteLine($"\n✗ Could not find window with title: {windowTitle}");
                Console.WriteLine("Make sure the window title is exact and the application is visible.");
            }

            return app;
        }

        static void DiscoverButtons(ButtonRecognizer recognizer, ApplicationInfo currentApp)
        {
            if (currentApp == null)
            {
                Console.WriteLine("\n✗ No application selected. Please find an application first.");
                return;
            }

            Console.WriteLine($"\n=== Discovering Buttons in {currentApp.WindowTitle} ===");
            recognizer.DiscoverButtons(currentApp);
            
            if (currentApp.Buttons.Count == 0)
            {
                Console.WriteLine("No buttons found. The application might use custom controls or different button classes.");
            }
            else
            {
                Console.WriteLine($"\n✓ Discovery complete! Found {currentApp.Buttons.Count} buttons.");
            }
        }

        static void RefreshButtons(ButtonRecognizer recognizer, ApplicationInfo currentApp)
        {
            if (currentApp == null)
            {
                Console.WriteLine("\n✗ No application selected. Please find an application first.");
                return;
            }

            if (currentApp.Buttons.Count == 0)
            {
                Console.WriteLine("\n✗ No buttons to refresh. Please discover buttons first.");
                return;
            }

            Console.WriteLine("\n=== Refreshing Button States ===");
            recognizer.RefreshButtonStates(currentApp);
            Console.WriteLine("✓ Button states refreshed.");
        }

        static void ClickButtonByText(ButtonRecognizer recognizer, ApplicationInfo currentApp)
        {
            if (currentApp == null)
            {
                Console.WriteLine("\n✗ No application selected. Please find an application first.");
                return;
            }

            if (currentApp.Buttons.Count == 0)
            {
                Console.WriteLine("\n✗ No buttons available. Please discover buttons first.");
                return;
            }

            Console.WriteLine("\n=== Click Button by Text ===");
            Console.Write("Enter the button text to click: ");
            string buttonText = Console.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(buttonText))
            {
                Console.WriteLine("Button text cannot be empty.");
                return;
            }

            ButtonInfo button = recognizer.FindButtonByText(currentApp, buttonText);
            if (button == null)
            {
                Console.WriteLine($"\n✗ Button with text '{buttonText}' not found.");
                Console.WriteLine("Available buttons:");
                foreach (var btn in currentApp.Buttons)
                {
                    Console.WriteLine($"  - '{btn.Text}' ({btn.ClassName})");
                }
                return;
            }

            Console.WriteLine($"\nFound button: {button}");
            Console.Write("Do you want to click this button? (y/n): ");
            string confirm = Console.ReadLine()?.Trim().ToLower();

            if (confirm == "y" || confirm == "yes")
            {
                bool success = recognizer.ClickButton(button);
                if (success)
                {
                    Console.WriteLine("✓ Button clicked successfully!");
                }
                else
                {
                    Console.WriteLine("✗ Failed to click button.");
                }
            }
            else
            {
                Console.WriteLine("Button click cancelled.");
            }
        }

        static void SearchButtons(ButtonRecognizer recognizer, ApplicationInfo currentApp)
        {
            if (currentApp == null)
            {
                Console.WriteLine("\n✗ No application selected. Please find an application first.");
                return;
            }

            if (currentApp.Buttons.Count == 0)
            {
                Console.WriteLine("\n✗ No buttons available. Please discover buttons first.");
                return;
            }

            Console.WriteLine("\n=== Search Buttons ===");
            Console.Write("Enter text to search for in button labels: ");
            string searchText = Console.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(searchText))
            {
                Console.WriteLine("Search text cannot be empty.");
                return;
            }

            var matchingButtons = recognizer.FindButtonsContainingText(currentApp, searchText);
            if (matchingButtons.Count == 0)
            {
                Console.WriteLine($"\n✗ No buttons found containing '{searchText}'.");
                return;
            }

            Console.WriteLine($"\n✓ Found {matchingButtons.Count} button(s) containing '{searchText}':");
            for (int i = 0; i < matchingButtons.Count; i++)
            {
                Console.WriteLine($"  {i + 1}. {matchingButtons[i]}");
            }

            Console.Write($"\nEnter button number to click (1-{matchingButtons.Count}), or 0 to cancel: ");
            if (int.TryParse(Console.ReadLine()?.Trim(), out int choice) && 
                choice > 0 && choice <= matchingButtons.Count)
            {
                ButtonInfo selectedButton = matchingButtons[choice - 1];
                bool success = recognizer.ClickButton(selectedButton);
                if (success)
                {
                    Console.WriteLine("✓ Button clicked successfully!");
                }
                else
                {
                    Console.WriteLine("✗ Failed to click button.");
                }
            }
            else
            {
                Console.WriteLine("Operation cancelled.");
            }
        }

        static void ListAllButtons(ApplicationInfo currentApp)
        {
            if (currentApp == null)
            {
                Console.WriteLine("\n✗ No application selected. Please find an application first.");
                return;
            }

            Console.WriteLine($"\n=== All Buttons in {currentApp.WindowTitle} ===");
            
            if (currentApp.Buttons.Count == 0)
            {
                Console.WriteLine("No buttons discovered yet. Please discover buttons first.");
                return;
            }

            Console.WriteLine($"Total buttons: {currentApp.Buttons.Count}\n");
            
            for (int i = 0; i < currentApp.Buttons.Count; i++)
            {
                var button = currentApp.Buttons[i];
                Console.WriteLine($"{i + 1:D2}. {button}");
                Console.WriteLine($"     Position: ({button.Bounds.Left}, {button.Bounds.Top}) - ({button.Bounds.Right}, {button.Bounds.Bottom})");
                Console.WriteLine($"     Control ID: {button.ControlId}");
                Console.WriteLine();
            }
        }

        static void MonitorButtonStates(ButtonRecognizer recognizer, ApplicationInfo currentApp)
        {
            if (currentApp == null)
            {
                Console.WriteLine("\n✗ No application selected. Please find an application first.");
                return;
            }

            if (currentApp.Buttons.Count == 0)
            {
                Console.WriteLine("\n✗ No buttons available. Please discover buttons first.");
                return;
            }

            Console.WriteLine("\n=== Monitoring Button States ===");
            Console.WriteLine("Press any key to stop monitoring...\n");

            while (!Console.KeyAvailable)
            {
                Console.Clear();
                Console.WriteLine("=== Real-time Button States ===");
                Console.WriteLine($"Application: {currentApp.WindowTitle}");
                Console.WriteLine($"Monitoring {currentApp.Buttons.Count} buttons...\n");

                recognizer.RefreshButtonStates(currentApp);

                foreach (var button in currentApp.Buttons)
                {
                    string status = button.IsEnabled ? "ENABLED" : "DISABLED";
                    string visibility = button.IsVisible ? "VISIBLE" : "HIDDEN";
                    Console.WriteLine($"[{status,-8}] [{visibility,-7}] {button.Text} ({button.ClassName})");
                }

                Console.WriteLine("\nPress any key to stop monitoring...");
                Thread.Sleep(1000); // Update every second
            }

            Console.ReadKey(); // Consume the key press
            Console.WriteLine("\nMonitoring stopped.");
        }
    }
}