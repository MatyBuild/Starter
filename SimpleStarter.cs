using System;
using System.Threading;
using System.Diagnostics;

/// <summary>
/// Jednoduchý launcher aplikací - základní verze bez ButtonRecognitionTool závislostí
/// </summary>
class SimpleStarter
{
    private static readonly int WAIT_TIME_SIMHUB = 8000; // ms
    private static readonly int WAIT_TIME_AITRACK = 4000; // ms
    
    static int Main(string[] args)
    {
        try
        {
            Console.WriteLine("=== Starter - Launcher aplikací ===");
            Console.WriteLine("Verze: 1.0 (Simple)");
            Console.WriteLine();

            // Kontrola parametrů
            bool dryRun = Array.Exists(args, arg => arg == "--dry-run");
            bool verbose = Array.Exists(args, arg => arg == "--verbose" || arg == "-v");
            
            if (Array.Exists(args, arg => arg == "--help" || arg == "-h"))
            {
                ShowHelp();
                return 0;
            }

            LogMessage("Zahajuji spouštění aplikací...", verbose);

            // 1. Spuštění SimHub
            if (!StartSimHub(dryRun, verbose))
            {
                LogError("SimHub se nepodařilo spustit správně");
                return 1;
            }

            // 2. Spuštění AITrack
            if (!StartAITrack(dryRun, verbose))
            {
                LogError("AITrack se nepodařilo spustit správně");
                return 1;
            }

            LogMessage("✓ Všechny aplikace byly úspěšně spuštěny!", verbose);
            LogMessage("", verbose);
            LogMessage("Spuštěné aplikace:", verbose);
            LogMessage("- SimHub", verbose);
            LogMessage("- AITrack", verbose);
            LogMessage("", verbose);
            LogMessage("POZNÁMKA: Musíte manuálně kliknout na 'Activate' v SimHub a 'Start Tracking' v AITrack", verbose);
            
            return 0;
        }
        catch (Exception ex)
        {
            LogError($"Neočekávaná chyba: {ex.Message}");
            return 1;
        }
    }

    private static bool StartSimHub(bool dryRun, bool verbose)
    {
        try
        {
            LogMessage("=== Spouštím SimHub ===", verbose);
            
            if (dryRun)
            {
                LogMessage("[DRY-RUN] Simuluji spuštění SimHub...", verbose);
                Thread.Sleep(1000);
                LogMessage("[DRY-RUN] SimHub 'spuštěn'", verbose);
                return true;
            }

            // Kontrola, jestli už SimHub neběží
            var existingProcesses = Process.GetProcessesByName("SimHub");
            if (existingProcesses.Length == 0)
            {
                existingProcesses = Process.GetProcessesByName("SimHubWPF");
            }

            if (existingProcesses.Length > 0)
            {
                LogMessage("SimHub už běží", verbose);
                return true;
            }

            // Pokus o spuštění SimHub
            string[] possiblePaths = {
                @"C:\Program Files (x86)\SimHub\SimHubWPF.exe",
                @"C:\Program Files\SimHub\SimHubWPF.exe",
                @"C:\SimHub\SimHubWPF.exe",
                @"D:\SimHub\SimHubWPF.exe"
            };

            bool launched = false;
            foreach (string path in possiblePaths)
            {
                if (System.IO.File.Exists(path))
                {
                    LogMessage($"Spouštím SimHub z: {path}", verbose);
                    Process.Start(path);
                    launched = true;
                    break;
                }
            }

            if (!launched)
            {
                LogError("SimHub executable nebyl nalezen v těchto umístěních:");
                foreach (string path in possiblePaths)
                {
                    LogError($"  - {path}");
                }
                return false;
            }

            LogMessage($"Čekám {WAIT_TIME_SIMHUB/1000}s na spuštění SimHub...", verbose);
            Thread.Sleep(WAIT_TIME_SIMHUB);
            
            LogMessage("✓ SimHub spuštěn (nezapomeňte kliknout na 'Activate')", verbose);
            return true;
        }
        catch (Exception ex)
        {
            LogError($"Chyba při spouštění SimHub: {ex.Message}");
            return false;
        }
    }

    private static bool StartAITrack(bool dryRun, bool verbose)
    {
        try
        {
            LogMessage("=== Spouštím AITrack ===", verbose);
            
            if (dryRun)
            {
                LogMessage("[DRY-RUN] Simuluji spuštění AITrack...", verbose);
                Thread.Sleep(1000);
                LogMessage("[DRY-RUN] AITrack 'spuštěn'", verbose);
                return true;
            }

            // Kontrola, jestli už AITrack neběží
            var existingProcesses = Process.GetProcessesByName("AITrack");
            if (existingProcesses.Length == 0)
            {
                existingProcesses = Process.GetProcessesByName("aitrack");
            }

            if (existingProcesses.Length > 0)
            {
                LogMessage("AITrack už běží", verbose);
                return true;
            }

            // Pokus o spuštění AITrack
            string[] possiblePaths = {
                @"C:\Program Files\AITrack\AITrack.exe",
                @"C:\Program Files (x86)\AITrack\AITrack.exe",
                @"C:\AITrack\AITrack.exe",
                @"D:\AITrack\AITrack.exe",
                @".\AITrack.exe"
            };

            bool launched = false;
            foreach (string path in possiblePaths)
            {
                if (System.IO.File.Exists(path))
                {
                    LogMessage($"Spouštím AITrack z: {path}", verbose);
                    Process.Start(path);
                    launched = true;
                    break;
                }
            }

            if (!launched)
            {
                LogError("AITrack executable nebyl nalezen v těchto umístěních:");
                foreach (string path in possiblePaths)
                {
                    LogError($"  - {path}");
                }
                return false;
            }

            LogMessage($"Čekám {WAIT_TIME_AITRACK/1000}s na spuštění AITrack...", verbose);
            Thread.Sleep(WAIT_TIME_AITRACK);
            
            LogMessage("✓ AITrack spuštěn (nezapomeňte kliknout na 'Start Tracking')", verbose);
            return true;
        }
        catch (Exception ex)
        {
            LogError($"Chyba při spouštění AITrack: {ex.Message}");
            return false;
        }
    }

    private static void ShowHelp()
    {
        Console.WriteLine("SimpleStarter - Jednoduchý launcher aplikací");
        Console.WriteLine();
        Console.WriteLine("Použití:");
        Console.WriteLine("  Starter.exe [možnosti]");
        Console.WriteLine();
        Console.WriteLine("Možnosti:");
        Console.WriteLine("  --dry-run    Testovací režim (nespouští skutečné aplikace)");
        Console.WriteLine("  --verbose    Podrobný výstup");
        Console.WriteLine("  -v          Zkrácená forma --verbose");
        Console.WriteLine("  --help      Zobrazí tuto nápovědu");
        Console.WriteLine("  -h          Zkrácená forma --help");
        Console.WriteLine();
        Console.WriteLine("Funkce:");
        Console.WriteLine("  1. Spustí SimHub (musíte manuálně kliknout na 'Activate')");
        Console.WriteLine("  2. Spustí AITrack (musíte manuálně kliknout na 'Start Tracking')");
        Console.WriteLine();
        Console.WriteLine("Návratové kódy:");
        Console.WriteLine("  0 - Úspěch");
        Console.WriteLine("  1 - Chyba");
    }

    private static void LogMessage(string message, bool verbose = true)
    {
        if (verbose || IsImportantMessage(message))
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            Console.WriteLine($"[{timestamp}] {message}");
        }
    }

    private static void LogError(string message)
    {
        string timestamp = DateTime.Now.ToString("HH:mm:ss");
        Console.WriteLine($"[{timestamp}] CHYBA: {message}");
    }

    private static bool IsImportantMessage(string message)
    {
        return message.Contains("✓") || 
               message.Contains("✗") || 
               message.Contains("===") ||
               message.Contains("Všechny aplikace") ||
               message.Contains("Spuštěné aplikace:");
    }
}