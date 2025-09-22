# Button Recognition Tool

A powerful C# tool for recognizing and interacting with buttons in any Windows application. This tool can discover buttons, monitor their states, and simulate clicks programmatically.

## Features

- **Application Discovery**: Find applications by process name or window title
- **Button Detection**: Automatically discover all buttons in an application
- **Real-time Monitoring**: Monitor button states (enabled/disabled, visible/hidden)
- **Smart Interaction**: Click buttons by text or search criteria
- **Multiple Click Methods**: Support for different click mechanisms for maximum compatibility
- **Comprehensive Information**: Get detailed button information including position, class, and control ID

## Requirements

- .NET 6.0 or later
- Windows operating system
- Visual Studio 2022 or Visual Studio Code (for development)

## Usage

### Building the Application

```bash
# Build the application
dotnet build

# Run the application
dotnet run
```

### Interactive Menu

The tool provides an interactive console menu with the following options:

1. **Find Application by Process Name** - Locate an app by its process name (e.g., "notepad")
2. **Find Application by Window Title** - Find an app by its exact window title
3. **Discover Buttons** - Scan the selected application for all buttons
4. **Refresh Button States** - Update the enabled/disabled status of all buttons
5. **Click Button by Text** - Click a button by its exact text label
6. **Search Buttons** - Find buttons containing specific text
7. **List All Buttons** - Display detailed information about all discovered buttons
8. **Monitor Button States** - Real-time monitoring of button states

### Example Usage

Here's a step-by-step example of using the tool:

```bash
# 1. Start the tool
dotnet run

# 2. Find an application (e.g., Calculator)
# Choose option 1: Find Application by Process Name
# Enter: calc

# 3. Discover buttons in the application
# Choose option 3: Discover Buttons in Current Application

# 4. Click a button
# Choose option 5: Click Button by Text
# Enter the button text, e.g., "1" or "+"

# 5. Monitor button states in real-time
# Choose option 8: Monitor Button States
```

### Programmatic Usage

You can also use the tool programmatically in your own C# applications:

```csharp
using ButtonRecognitionTool;

// Create a button recognizer
ButtonRecognizer recognizer = new ButtonRecognizer();

// Find an application
ApplicationInfo app = recognizer.FindApplicationByName("calc");

if (app != null)
{
    // Discover buttons
    recognizer.DiscoverButtons(app);
    
    // Find a specific button
    ButtonInfo button = recognizer.FindButtonByText(app, "1");
    
    // Click the button
    if (button != null)
    {
        recognizer.ClickButton(button);
    }
    
    // Monitor button states
    recognizer.RefreshButtonStates(app);
    foreach (var btn in app.Buttons)
    {
        Console.WriteLine($"Button: {btn.Text}, Enabled: {btn.IsEnabled}");
    }
}
```

## Supported Button Types

The tool recognizes various types of button controls:

- Standard Windows buttons (`Button`, `BUTTON`)
- Toolbar buttons (`ToolbarButton32`, `msctls_toolbarbutton32`)
- Windows Forms buttons (`WindowsForms10.Button`)
- Custom button implementations (contains "button" in class name)

## Technical Details

### Windows API Integration

The tool uses several Windows APIs through P/Invoke:

- `FindWindow` / `FindWindowEx` - For finding windows and controls
- `GetWindowText` - For retrieving button text
- `GetClassName` - For identifying control types
- `IsWindowEnabled` / `IsWindowVisible` - For checking button states
- `SendMessage` / `PostMessage` - For simulating clicks
- `GetWindowRect` - For getting button positions

### Click Methods

The tool implements multiple click methods for maximum compatibility:

1. **Message-based clicking**: Sends `WM_LBUTTONDOWN` and `WM_LBUTTONUP` messages
2. **Command-based clicking**: Sends `WM_COMMAND` messages to the parent window
3. **Physical mouse clicking**: Moves the mouse cursor and performs actual clicks

## Troubleshooting

### Common Issues

1. **No buttons found**: Some applications use custom controls that aren't recognized as standard buttons. The tool includes common button class names but may need customization for specific apps.

2. **Button clicks don't work**: Different applications respond to different click methods. The tool tries multiple approaches, but some apps may require specific techniques.

3. **Access denied**: Some applications with elevated privileges may not be accessible. Run the tool as administrator if needed.

4. **Application not found**: Ensure the target application is running and has a visible window.

### Debugging Tips

- Use option 7 (List All Buttons) to see detailed information about discovered buttons
- Use option 8 (Monitor Button States) to watch how button states change in real-time
- Check the console output for error messages and diagnostic information

## Limitations

- **Windows Only**: This tool only works on Windows due to its reliance on Windows APIs
- **UI Automation**: Some modern applications use UI frameworks that may not be fully compatible
- **Security**: Elevated applications may require elevated privileges to interact with
- **Custom Controls**: Applications with heavily customized UI controls may not be fully supported

## Safety Considerations

- Always test on non-critical applications first
- Be cautious when automating interactions with important applications
- Some button clicks may have irreversible effects
- The tool includes confirmation prompts for potentially destructive actions

## License

This tool is provided as-is for educational and development purposes. Use responsibly and in accordance with the terms of service of the applications you're automating.

## Contributing

Feel free to extend the tool with additional features:

- Support for more control types (checkboxes, radio buttons, etc.)
- Image-based button recognition
- Configuration files for application-specific settings
- Logging and audit trail functionality
- GUI interface instead of console-based interaction
