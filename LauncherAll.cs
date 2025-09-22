using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
#if !NO_UI_AUTOMATION
using System.Windows.Automation;
#endif
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
                LogMessage(string.Format("Režim: {0}", (isOnline ? "ONLINE" : "OFFLINE")));

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
                LogError(string.Format("Neočekávaná chyba: {0}", ex.Message));
                LogError(string.Format("Stack trace: {0}", ex.StackTrace));
                return 1;
            }
            finally
            {
                if (logWriter != null) logWriter.Close();
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
                    Console.WriteLine(string.Format("Varování: Nepodařilo se otevřít log soubor: {0}", ex.Message));
                }
            }
        }

        private static void LogMessage(string message)
        {
            string logLine = string.Format("[{0:yyyy-MM-dd HH:mm:ss}] {1}", DateTime.Now, message);
            Console.WriteLine(logLine);
            if (logWriter != null) logWriter.WriteLine(logLine);
            if (logWriter != null) logWriter.Flush();
        }

        private static void LogError(string message)
        {
            string logLine = string.Format("[{0:yyyy-MM-dd HH:mm:ss}] CHYBA: {1}", DateTime.Now, message);
            Console.WriteLine(logLine);
            if (logWriter != null) logWriter.WriteLine(logLine);
            if (logWriter != null) logWriter.Flush();
        }

        private static LauncherConfig LoadConfiguration()
        {
            try
            {
                if (!File.Exists(configPath))
                {
                    LogError(string.Format("Konfigurační soubor nenalezen: {0}", configPath));
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
                LogError(string.Format("Chyba při načítání konfigurace: {0}", ex.Message));
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
                LogMessage(string.Format("Spouštím App{0}: {1}", i + 1, app.Path));

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
                        LogError(string.Format("Aplikace nenalezena: {0}", app.Path));
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
                            LogError(string.Format("Nepodařilo se spustit: {0}", appPath));
                            return false;
                        }

                        LogMessage(string.Format("App{0} spuštěna (PID: {1})", i + 1, process.Id));

                        // Specifické chování podle aplikace
                        if (!HandleAppSpecificBehavior(i + 1, app))
                        {
                            LogError(string.Format("Chyba při zpracování App{0}", i + 1));
                            return false;
                        }
                    }
                    else
                    {
                        LogMessage(string.Format("[DRY-RUN] Spustil bych: {0}", appPath));
                    }
                }
                catch (Exception ex)
                {
                    LogError(string.Format("Chyba při spouštění App{0}: {1}", i + 1, ex.Message));
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
                            LogMessage(string.Format("Nalezena aplikace na ploše: {0}", path));
                            return path;
                        }

                        // Zkus i malá písmena
                        path = Path.Combine(desktop, name.ToLower() + ext);
                        if (File.Exists(path))
                        {
                            LogMessage(string.Format("Nalezena aplikace na ploše: {0}", path));
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
                        LogMessage(string.Format("Ukončuji běžící proces: {0} (PID: {1})", process.ProcessName, process.Id));
                        process.Kill();
                        process.WaitForExit(5000);
                    }
                    catch (Exception ex)
                    {
                        LogMessage(string.Format("Varování: Nepodařilo se ukončit proces {0}: {1}", process.ProcessName, ex.Message));
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage(string.Format("Varování při ukončování procesů: {0}", ex.Message));
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
                LogError(string.Format("Chyba při zpracování App1: {0}", ex.Message));
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
                    LogError(string.Format("App2 okno nenalezeno: {0}", app.WindowTitle));
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
                LogError(string.Format("Chyba při zpracování App2: {0}", ex.Message));
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
                    LogError(string.Format("App3 okno nenalezeno: {0}", app.WindowTitle));
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
                LogError(string.Format("Chyba při zpracování App3: {0}", ex.Message));
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
                        LogMessage(string.Format("[DRY-RUN] Pingoval bych: {0}", host));
                        continue;
                    }

                    using (Ping ping = new Ping())
                    {
                        PingReply reply = ping.Send(host, timeout);
                        LogMessage(string.Format("Ping {0}: {1}", host, reply.Status));

                        if (reply.Status == IPStatus.Success)
                        {
                            LogMessage(string.Format("Ping na {0} úspěšný - režim ONLINE", host));
                            return true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogMessage(string.Format("Ping {0} selhal: {1}", host, ex.Message));
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
                LogMessage(string.Format("Žádná aplikace pro režim {0}", (isOnline ? "ONLINE" : "OFFLINE")));
                return true;
            }

            try
            {
                LogMessage(string.Format("Spouštím {0} aplikace: {1}", (isOnline ? "ONLINE" : "OFFLINE"), app.Path));

                // Ukonči případné běžící procesy
                TerminateExistingProcesses(app.Path);

                string appPath = FindApplicationPath(app.Path, GetAppSearchNames(isOnline ? 4 : 5));
                if (string.IsNullOrEmpty(appPath))
                {
                    LogError(string.Format("Podmíněná aplikace nenalezena: {0}", app.Path));
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
                        LogError(string.Format("Nepodařilo se spustit: {0}", appPath));
                        return false;
                    }

                    LogMessage(string.Format("Podmíněná aplikace spuštěna (PID: {0})", process.Id));

                    // Pokud je definován klik, proveď ho
                    if (app.Click != null && !string.IsNullOrEmpty(app.WindowTitle))
                    {
                        return HandleConditionalAppClick(app);
                    }
                }
                else
                {
                    LogMessage(string.Format("[DRY-RUN] Spustil bych: {0}", appPath));
                }

                return true;
            }
            catch (Exception ex)
            {
                LogError(string.Format("Chyba při spouštění podmíněné aplikace: {0}", ex.Message));
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
                    LogError(string.Format("Okno podmíněné aplikace nenalezeno: {0}", app.WindowTitle));
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
                LogError(string.Format("Chyba při kliku v podmíněné aplikaci: {0}", ex.Message));
                return true; // Aplikace je spuštěna, jen klik selhal
            }
        }

        private static AutomationElement WaitForWindow(string windowTitle, int timeoutSeconds)
        {
#if NO_UI_AUTOMATION
            LogMessage(string.Format("UI Automation není dostupné - přeskakuji hledání okna: {0}", windowTitle));
            return null;
#else
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
                        LogMessage(string.Format("Okno nalezeno: {0}", windowTitle));
                        return window;
                    }
                }
                catch (Exception ex)
                {
                    LogMessage(string.Format("Chyba při hledání okna {0}: {1}", windowTitle, ex.Message));
                }

                Thread.Sleep(1000);
            }

            LogMessage(string.Format("Okno nenalezeno po {0}s: {1}", timeoutSeconds, windowTitle));
            return null;
#endif
        }

        private static bool ClickButtonByName(AutomationElement window, string buttonName)
        {
#if NO_UI_AUTOMATION
            LogMessage(string.Format("UI Automation není dostupné - nelze kliknout na tlačítko: {0}", buttonName));
            return false;
#else
            try
            {
                PropertyCondition buttonCondition = new PropertyCondition(AutomationElement.NameProperty, buttonName);
                AutomationElement button = window.FindFirst(TreeScope.Descendants, buttonCondition);

                if (button != null)
                {
                    LogMessage(string.Format("Tlačítko nalezeno: {0}", buttonName));
                    
                    if (button.TryGetCurrentPattern(InvokePattern.Pattern, out object pattern))
                    {
                        ((InvokePattern)pattern).Invoke();
                        LogMessage(string.Format("Kliknuto na tlačítko: {0}", buttonName));
                        return true;
                    }
                }

                LogMessage(string.Format("Tlačítko nenalezeno nebo nejde kliknout: {0}", buttonName));
                return false;
            }
            catch (Exception ex)
            {
                LogMessage(string.Format("Chyba při kliku na tlačítko {0}: {1}", buttonName, ex.Message));
                return false;
            }
#endif
        }

        private static bool ClickButtonByAutomationId(AutomationElement window, string automationId)
        {
#if NO_UI_AUTOMATION
            LogMessage(string.Format("UI Automation není dostupné - nelze kliknout na tlačítko (AutomationId): {0}", automationId));
            return false;
#else
            try
            {
                PropertyCondition buttonCondition = new PropertyCondition(AutomationElement.AutomationIdProperty, automationId);
                AutomationElement button = window.FindFirst(TreeScope.Descendants, buttonCondition);

                if (button != null)
                {
                    LogMessage(string.Format("Tlačítko nalezeno (AutomationId): {0}", automationId));
                    
                    if (button.TryGetCurrentPattern(InvokePattern.Pattern, out object pattern))
                    {
                        ((InvokePattern)pattern).Invoke();
                        LogMessage(string.Format("Kliknuto na tlačítko (AutomationId): {0}", automationId));
                        return true;
                    }
                }

                LogMessage(string.Format("Tlačítko nenalezeno (AutomationId): {0}", automationId));
                return false;
            }
            catch (Exception ex)
            {
                LogMessage(string.Format("Chyba při kliku na tlačítko (AutomationId) {0}: {1}", automationId, ex.Message));
                return false;
            }
#endif
        }

        private static bool ClickRelative(AutomationElement window, int x, int y)
        {
#if NO_UI_AUTOMATION
            LogMessage(string.Format("UI Automation není dostupné - nelze provést relativní klik na {0}, {1}", x, y));
            return false;
#else
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

                LogMessage(string.Format("Relativní klik na souřadnice: {0}, {1} (absolutní: {2}, {3})", x, y, clickX, clickY));

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
                LogMessage(string.Format("Chyba při relativním kliku: {0}", ex.Message));
                return false;
            }
#endif
        }

        private static void MinimizeWindow(AutomationElement window)
        {
#if NO_UI_AUTOMATION
            LogMessage("UI Automation není dostupné - nelze minimalizovat okno");
#else
            try
            {
                IntPtr hwnd = new IntPtr(window.Current.NativeWindowHandle);
                ShowWindow(hwnd, SW_MINIMIZE);
            }
            catch (Exception ex)
            {
                LogMessage(string.Format("Chyba při minimalizaci okna: {0}", ex.Message));
            }
#endif
        }

        private static void CloseWindow(AutomationElement window)
        {
#if NO_UI_AUTOMATION
            LogMessage("UI Automation není dostupné - nelze zavřít okno");
#else
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
                LogMessage(string.Format("Chyba při zavírání okna: {0}", ex.Message));
            }
#endif
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