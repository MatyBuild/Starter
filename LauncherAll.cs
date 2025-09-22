using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Automation;
using System.Text.Json;

namespace StarterLauncher
{
    [STAThread]
    class Program
    {
        private static string configPath = "";
        private static string logPath = "";
        private static bool dryRun = false;
        private static int timeoutWindowSeconds = 45;
        private static LauncherConfig config;
        private static StreamWriter logWriter;

        // WinAPI pro manipulaci s okny
        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int X, int Y);

        [DllImport("user32.dll")]
        private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;
        private const int SW_MINIMIZE = 6;
        private const int SW_RESTORE = 9;

        static int Main(string[] args)
        {
            try
            {
                if (!ParseArguments(args))
                {
                    ShowHelp();
                    return 1;
                }

                InitializeLogging();
                LogMessage("=== Startér Launcher spuštěn ===");

                // Načtení konfigurace
                config = LoadConfiguration();
                if (config == null)
                {
                    LogError("Chyba při načítání konfigurace");
                    return 1;
                }

                // Krok 1-3: Spuštění základních aplikací (App1-App3)
                LogMessage("Spouštím základní aplikace (App1-App3)...");
                if (!StartBaseApps())
                {
                    LogError("Chyba při spouštění základních aplikací");
                    return 1;
                }

                // Krok 4: Kontrola konektivity
                LogMessage("Kontrolujem konektivitu...");
                bool isOnline = CheckConnectivity();
                LogMessage($"Režim: {(isOnline ? "ONLINE" : "OFFLINE")}");

                // Krok 5: Spuštění podmíněné aplikace
                if (!StartConditionalApp(isOnline))
                {
                    LogError("Chyba při spouštění podmíněné aplikace");
                    return 1;
                }

                LogMessage("=== Launcher úspešně dokončen ===");
                return 0;
            }
            catch (Exception ex)
            {
                LogError($"Neočekávaná chyba: {ex.Message}");
                LogError($"Stack trace: {ex.StackTrace}");
                return 1;
            }
            finally
            {
                logWriter?.Close();
            }
        }

        private static bool ParseArguments(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i].ToLower())
                {
                    case "--config":
                        if (i + 1 < args.Length)
                        {
                            configPath = args[++i];
                        }
                        break;
                    case "--log":
                        if (i + 1 < args.Length)
                        {
                            logPath = args[++i];
                        }
                        break;
                    case "--dry-run":
                        dryRun = true;
                        break;
                    case "--timeoutwindow":
                        if (i + 1 < args.Length && int.TryParse(args[++i], out int timeout))
                        {
                            timeoutWindowSeconds = timeout;
                        }
                        break;
                    case "--help":
                    case "-h":
                        return false;
                }
            }

            return !string.IsNullOrEmpty(configPath);
        }

        private static void ShowHelp()
        {
            Console.WriteLine("Startér Launcher - Windows aplikační spouštěč");
            Console.WriteLine("Použití:");
            Console.WriteLine("  LauncherAll.exe --config \"cesta\\config.json\" [další parametry]");
            Console.WriteLine("");
            Console.WriteLine("Parametry:");
            Console.WriteLine("  --config \"cesta\"     Povinný: cesta k konfiguračnímu JSON souboru");
            Console.WriteLine("  --log \"cesta\"        Volitelný: cesta k log souboru");
            Console.WriteLine("  --dry-run            Volitelný: testovací režim (nespouští aplikace)");
            Console.WriteLine("  --timeoutWindow N    Volitelný: timeout pro čekání na okna (sekundy, default: 45)");
            Console.WriteLine("  --help, -h           Zobrazí tuto nápovědu");
        }

        private static void InitializeLogging()
        {
            if (!string.IsNullOrEmpty(logPath))
            {
                try
                {
                    logWriter = new StreamWriter(logPath, true);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Varování: Nepodařilo se otevřít log soubor: {ex.Message}");
                }
            }
        }

        private static void LogMessage(string message)
        {
            string logLine = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
            Console.WriteLine(logLine);
            logWriter?.WriteLine(logLine);
            logWriter?.Flush();
        }

        private static void LogError(string message)
        {
            string logLine = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] CHYBA: {message}";
            Console.WriteLine(logLine);
            logWriter?.WriteLine(logLine);
            logWriter?.Flush();
        }

        private static LauncherConfig LoadConfiguration()
        {
            try
            {
                if (!File.Exists(configPath))
                {
                    LogError($"Konfigurační soubor nenalezen: {configPath}");
                    return null;
                }

                string jsonContent = File.ReadAllText(configPath);
                return JsonSerializer.Deserialize<LauncherConfig>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception ex)
            {
                LogError($"Chyba při načítání konfigurace: {ex.Message}");
                return null;
            }
        }

        private static bool StartBaseApps()
        {
            if (config.Apps == null || config.Apps.Length == 0)
            {
                LogMessage("Žádné základní aplikace k spuštění");
                return true;
            }

            for (int i = 0; i < config.Apps.Length; i++)
            {
                var app = config.Apps[i];
                LogMessage($"Spouštím App{i + 1}: {app.Path}");

                try
                {
                    // Najdi a ukonči již běžící procesy této aplikace
                    TerminateExistingProcesses(app.Path);

                    string appPath = FindApplicationPath(app.Path, GetAppSearchNames(i + 1));
                    if (string.IsNullOrEmpty(appPath))
                    {
                        if (i == 0) // App1 může chybět
                        {
                            LogMessage("App1 není dostupná - pokračujem");
                            continue;
                        }
                        LogError($"Aplikace nenalezena: {app.Path}");
                        return false;
                    }

                    if (!dryRun)
                    {
                        Process process = Process.Start(new ProcessStartInfo
                        {
                            FileName = appPath,
                            UseShellExecute = true
                        });

                        if (process == null)
                        {
                            LogError($"Nepodařilo se spustit: {appPath}");
                            return false;
                        }

                        LogMessage($"App{i + 1} spuštěna (PID: {process.Id})");

                        // Specifické chování podle aplikace
                        if (!HandleAppSpecificBehavior(i + 1, app))
                        {
                            LogError($"Chyba při zpracování App{i + 1}");
                            return false;
                        }
                    }
                    else
                    {
                        LogMessage($"[DRY-RUN] Spustil bych: {appPath}");
                    }
                }
                catch (Exception ex)
                {
                    LogError($"Chyba při spouštění App{i + 1}: {ex.Message}");
                    if (i != 0) // App1 může chybět
                        return false;
                }
            }

            return true;
        }

        private static string[] GetAppSearchNames(int appNumber)
        {
            switch (appNumber)
            {
                case 1:
                    return new[] { "Sim Racing Studio", "SMS", "Motion System" };
                case 3:
                    return new[] { "AiTrack", "AI Track", "aitrack" };
                case 4:
                    return new[] { "Drivetech Launcher", "Drivetech Louncher", "Drivetech Online" };
                case 5:
                    return new[] { "Drivetech Offline", "Drivetech" };
                default:
                    return new string[0];
            }
        }

        private static string FindApplicationPath(string primaryPath, string[] searchNames)
        {
            // Zkus primární cestu
            if (File.Exists(primaryPath))
                return primaryPath;

            // Zkus hledat na ploše
            if (searchNames.Length > 0)
            {
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                foreach (string name in searchNames)
                {
                    string[] extensions = { ".exe", ".lnk" };
                    foreach (string ext in extensions)
                    {
                        string path = Path.Combine(desktop, name + ext);
                        if (File.Exists(path))
                        {
                            LogMessage($"Nalezena aplikace na ploše: {path}");
                            return path;
                        }

                        // Zkus i malá písmena
                        path = Path.Combine(desktop, name.ToLower() + ext);
                        if (File.Exists(path))
                        {
                            LogMessage($"Nalezena aplikace na ploše: {path}");
                            return path;
                        }
                    }
                }
            }

            return null;
        }

        private static void TerminateExistingProcesses(string appPath)
        {
            try
            {
                string processName = Path.GetFileNameWithoutExtension(appPath);
                Process[] processes = Process.GetProcessesByName(processName);

                foreach (Process process in processes)
                {
                    try
                    {
                        LogMessage($"Ukončuji běžící proces: {process.ProcessName} (PID: {process.Id})");
                        process.Kill();
                        process.WaitForExit(5000);
                    }
                    catch (Exception ex)
                    {
                        LogMessage($"Varování: Nepodařilo se ukončit proces {process.ProcessName}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Varování při ukončování procesů: {ex.Message}");
            }
        }

        private static bool HandleAppSpecificBehavior(int appNumber, AppConfig app)
        {
            switch (appNumber)
            {
                case 1: // App1 - Sim Racing Studio
                    return HandleApp1();
                case 2: // App2 - klik na Activate a minimalizace
                    return HandleApp2(app);
                case 3: // App3 - klik na Start tracking, zůstat otevřené
                    return HandleApp3(app);
                default:
                    return true;
            }
        }

        private static bool HandleApp1()
        {
            try
            {
                // Čekej na okno a zkontroluj aktualizaci
                Thread.Sleep(3000); // Krátké čekání na spuštění

                // Hledej okno s aktualizací
                AutomationElement updateWindow = WaitForWindow("Aktualizace programu", 10);
                if (updateWindow != null)
                {
                    LogMessage("Nalezeno okno aktualizace, klikám na 'Nekontrolovat'");
                    if (!ClickButtonByName(updateWindow, "Nekontrolovat"))
                    {
                        // Zkus zavřít okno
                        CloseWindow(updateWindow);
                    }
                }

                // Najdi hlavní okno aplikace a minimalizuj
                AutomationElement mainWindow = WaitForWindow("Sim Racing Studio", 10);
                if (mainWindow != null)
                {
                    MinimizeWindow(mainWindow);
                    LogMessage("App1 minimalizována");
                }

                return true;
            }
            catch (Exception ex)
            {
                LogError($"Chyba při zpracování App1: {ex.Message}");
                return true; // App1 může selhat
            }
        }

        private static bool HandleApp2(AppConfig app)
        {
            try
            {
                if (string.IsNullOrEmpty(app.WindowTitle))
                    return true;

                AutomationElement window = WaitForWindow(app.WindowTitle, timeoutWindowSeconds);
                if (window == null)
                {
                    LogError($"App2 okno nenalezeno: {app.WindowTitle}");
                    return false;
                }

                // Klikni na Activate
                if (!ClickButtonByName(window, "Activate"))
                {
                    LogError("App2: Tlačítko 'Activate' nenalezeno");
                    return false;
                }

                // Minimalizuj okno
                Thread.Sleep(1000);
                MinimizeWindow(window);
                LogMessage("App2: kliknuto na Activate a minimalizováno");
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Chyba při zpracování App2: {ex.Message}");
                return false;
            }
        }

        private static bool HandleApp3(AppConfig app)
        {
            try
            {
                if (string.IsNullOrEmpty(app.WindowTitle))
                    return true;

                AutomationElement window = WaitForWindow(app.WindowTitle, timeoutWindowSeconds);
                if (window == null)
                {
                    LogError($"App3 okno nenalezeno: {app.WindowTitle}");
                    return false;
                }

                // Klikni na Start tracking
                if (!ClickButtonByName(window, "Start tracking"))
                {
                    LogError("App3: Tlačítko 'Start tracking' nenalezeno");
                    return false;
                }

                LogMessage("App3: kliknuto na 'Start tracking', okno zůstává otevřené");
                return true;
            }
            catch (Exception ex)
            {
                LogError($"Chyba při zpracování App3: {ex.Message}");
                return false;
            }
        }

        private static bool CheckConnectivity()
        {
            string[] hosts = config.Conditional?.PingHosts ?? new[] { "1.1.1.1", "9.9.9.9" };
            int timeout = config.Conditional?.PingTimeoutMs ?? 1500;

            foreach (string host in hosts)
            {
                try
                {
                    if (dryRun)
                    {
                        LogMessage($"[DRY-RUN] Pingoval bych: {host}");
                        continue;
                    }

                    using (Ping ping = new Ping())
                    {
                        PingReply reply = ping.Send(host, timeout);
                        LogMessage($"Ping {host}: {reply.Status}");

                        if (reply.Status == IPStatus.Success)
                        {
                            LogMessage($"Ping na {host} úspěšný - režim ONLINE");
                            return true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogMessage($"Ping {host} selhal: {ex.Message}");
                }
            }

            LogMessage("Všechny pingy selhaly - režim OFFLINE");
            return false;
        }

        private static bool StartConditionalApp(bool isOnline)
        {
            if (config.Conditional == null)
            {
                LogMessage("Žádné podmíněné aplikace konfigurovány");
                return true;
            }

            AppConfig app = isOnline ? config.Conditional.Online : config.Conditional.Offline;
            if (app == null)
            {
                LogMessage($"Žádná aplikace pro režim {(isOnline ? "ONLINE" : "OFFLINE")}");
                return true;
            }

            try
            {
                LogMessage($"Spouštím {(isOnline ? "ONLINE" : "OFFLINE")} aplikace: {app.Path}");

                // Ukonči případné běžící procesy
                TerminateExistingProcesses(app.Path);

                string appPath = FindApplicationPath(app.Path, GetAppSearchNames(isOnline ? 4 : 5));
                if (string.IsNullOrEmpty(appPath))
                {
                    LogError($"Podmíněná aplikace nenalezena: {app.Path}");
                    return false;
                }

                if (!dryRun)
                {
                    Process process = Process.Start(new ProcessStartInfo
                    {
                        FileName = appPath,
                        UseShellExecute = true
                    });

                    if (process == null)
                    {
                        LogError($"Nepodařilo se spustit: {appPath}");
                        return false;
                    }

                    LogMessage($"Podmíněná aplikace spuštěna (PID: {process.Id})");

                    // Pokud je definován klik, proveď ho
                    if (app.Click != null && !string.IsNullOrEmpty(app.WindowTitle))
                    {
                        return HandleConditionalAppClick(app);
                    }
                }
                else
                {
                    LogMessage($"[DRY-RUN] Spustil bych: {appPath}");
                }

                return true;
            }
            catch (Exception ex)
            {
                LogError($"Chyba při spouštění podmíněné aplikace: {ex.Message}");
                return false;
            }
        }

        private static bool HandleConditionalAppClick(AppConfig app)
        {
            try
            {
                AutomationElement window = WaitForWindow(app.WindowTitle, timeoutWindowSeconds);
                if (window == null)
                {
                    LogError($"Okno podmíněné aplikace nenalezeno: {app.WindowTitle}");
                    return false;
                }

                bool clickSuccessful = false;

                if (app.Click.Type.ToLower() == "uia")
                {
                    string buttonName = app.Click.ButtonName ?? "";
                    string automationId = app.Click.AutomationId ?? "";

                    if (!string.IsNullOrEmpty(buttonName))
                    {
                        clickSuccessful = ClickButtonByName(window, buttonName);
                    }
                    else if (!string.IsNullOrEmpty(automationId))
                    {
                        clickSuccessful = ClickButtonByAutomationId(window, automationId);
                    }
                }

                // Fallback klik
                if (!clickSuccessful && app.FallbackClick != null)
                {
                    LogMessage("UIA klik selhal, používám fallback klik");
                    clickSuccessful = ClickRelative(window, app.FallbackClick.X, app.FallbackClick.Y);
                }

                if (clickSuccessful)
                {
                    LogMessage("Klik v podmíněné aplikaci úspěšný");
                }
                else
                {
                    LogMessage("Varování: Klik v podmíněné aplikaci se nezdařil");
                }

                return true; // I když klik selže, aplikace je spuštěna
            }
            catch (Exception ex)
            {
                LogError($"Chyba při kliku v podmíněné aplikaci: {ex.Message}");
                return true; // Aplikace je spuštěna, jen klik selhal
            }
        }

        private static AutomationElement WaitForWindow(string windowTitle, int timeoutSeconds)
        {
            DateTime timeout = DateTime.Now.AddSeconds(timeoutSeconds);
            
            while (DateTime.Now < timeout)
            {
                try
                {
                    AutomationElement desktop = AutomationElement.RootElement;
                    PropertyCondition condition = new PropertyCondition(AutomationElement.NameProperty, windowTitle);
                    AutomationElement window = desktop.FindFirst(TreeScope.Children, condition);

                    if (window != null)
                    {
                        LogMessage($"Okno nalezeno: {windowTitle}");
                        return window;
                    }
                }
                catch (Exception ex)
                {
                    LogMessage($"Chyba při hledání okna {windowTitle}: {ex.Message}");
                }

                Thread.Sleep(1000);
            }

            LogMessage($"Okno nenalezeno po {timeoutSeconds}s: {windowTitle}");
            return null;
        }

        private static bool ClickButtonByName(AutomationElement window, string buttonName)
        {
            try
            {
                PropertyCondition buttonCondition = new PropertyCondition(AutomationElement.NameProperty, buttonName);
                AutomationElement button = window.FindFirst(TreeScope.Descendants, buttonCondition);

                if (button != null)
                {
                    LogMessage($"Tlačítko nalezeno: {buttonName}");
                    
                    if (button.TryGetCurrentPattern(InvokePattern.Pattern, out object pattern))
                    {
                        ((InvokePattern)pattern).Invoke();
                        LogMessage($"Kliknuto na tlačítko: {buttonName}");
                        return true;
                    }
                }

                LogMessage($"Tlačítko nenalezeno nebo nejde kliknout: {buttonName}");
                return false;
            }
            catch (Exception ex)
            {
                LogMessage($"Chyba při kliku na tlačítko {buttonName}: {ex.Message}");
                return false;
            }
        }

        private static bool ClickButtonByAutomationId(AutomationElement window, string automationId)
        {
            try
            {
                PropertyCondition buttonCondition = new PropertyCondition(AutomationElement.AutomationIdProperty, automationId);
                AutomationElement button = window.FindFirst(TreeScope.Descendants, buttonCondition);

                if (button != null)
                {
                    LogMessage($"Tlačítko nalezeno (AutomationId): {automationId}");
                    
                    if (button.TryGetCurrentPattern(InvokePattern.Pattern, out object pattern))
                    {
                        ((InvokePattern)pattern).Invoke();
                        LogMessage($"Kliknuto na tlačítko (AutomationId): {automationId}");
                        return true;
                    }
                }

                LogMessage($"Tlačítko nenalezeno (AutomationId): {automationId}");
                return false;
            }
            catch (Exception ex)
            {
                LogMessage($"Chyba při kliku na tlačítko (AutomationId) {automationId}: {ex.Message}");
                return false;
            }
        }

        private static bool ClickRelative(AutomationElement window, int x, int y)
        {
            try
            {
                IntPtr hwnd = new IntPtr(window.Current.NativeWindowHandle);
                RECT rect;
                
                if (!GetWindowRect(hwnd, out rect))
                {
                    LogMessage("Nepodařilo se získat rozměry okna pro relativní klik");
                    return false;
                }

                int clickX = rect.Left + x;
                int clickY = rect.Top + y;

                LogMessage($"Relativní klik na souřadnice: {x}, {y} (absolutní: {clickX}, {clickY})");

                SetForegroundWindow(hwnd);
                Thread.Sleep(100);
                
                SetCursorPos(clickX, clickY);
                Thread.Sleep(50);
                
                mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
                Thread.Sleep(50);
                mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);

                LogMessage("Relativní klik proveden");
                return true;
            }
            catch (Exception ex)
            {
                LogMessage($"Chyba při relativním kliku: {ex.Message}");
                return false;
            }
        }

        private static void MinimizeWindow(AutomationElement window)
        {
            try
            {
                IntPtr hwnd = new IntPtr(window.Current.NativeWindowHandle);
                ShowWindow(hwnd, SW_MINIMIZE);
            }
            catch (Exception ex)
            {
                LogMessage($"Chyba při minimalizaci okna: {ex.Message}");
            }
        }

        private static void CloseWindow(AutomationElement window)
        {
            try
            {
                if (window.TryGetCurrentPattern(WindowPattern.Pattern, out object pattern))
                {
                    ((WindowPattern)pattern).Close();
                    LogMessage("Okno zavřeno");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Chyba při zavírání okna: {ex.Message}");
            }
        }
    }

    // Konfigurační třídy
    public class LauncherConfig
    {
        public AppConfig[] Apps { get; set; }
        public ConditionalConfig Conditional { get; set; }
    }

    public class AppConfig
    {
        public string Path { get; set; }
        public string WindowTitle { get; set; }
        public ClickConfig Click { get; set; }
        public FallbackClickConfig FallbackClick { get; set; }
    }

    public class ConditionalConfig
    {
        public AppConfig Online { get; set; }
        public AppConfig Offline { get; set; }
        public string[] PingHosts { get; set; }
        public int PingTimeoutMs { get; set; }
        public bool StartAfterAllBaseApps { get; set; }
    }

    public class ClickConfig
    {
        public string Type { get; set; }
        public string ButtonName { get; set; }
        public string AutomationId { get; set; }
    }

    public class FallbackClickConfig
    {
        public int X { get; set; }
        public int Y { get; set; }
    }
}