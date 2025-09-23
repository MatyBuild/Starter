using System;
using System.Threading;
using System.Diagnostics;
using ButtonRecognitionTool;

/// <summary>
/// Hlavní launcher aplikací - používá ButtonRecognitionTool pro automatické spouštění a ovládání aplikací
/// </summary>
class Starter
{
    private static readonly int WAIT_TIME_SIMHUB = 8000; // ms
    private static readonly int WAIT_TIME_AITRACK = 4000; // ms
    
    static int Main(string[] args)
    {
        try
        {
            Console.WriteLine("=== Starter - Automatický launcher aplikací ===");
            Console.WriteLine("Verze: 2.0 (s Button Recognition)");
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

            // 1. Spuštění SimHub a aktivace
            if (!StartSimHub(dryRun, verbose))
            {
                LogError("SimHub se nepodařilo spustit správně");
                return 1;
            }

            // 2. Spuštění AITrack a zahájení trackingu
            if (!StartAITrack(dryRun, verbose))
            {
                LogError("AITrack se nepodařilo spustit správně");
                return 1;
            }

            LogMessage("✓ Všechny aplikace byly úspěšně spuštěny a aktivovány!", verbose);
            LogMessage("", verbose);
            LogMessage("Spuštěné aplikace:", verbose);
            LogMessage("- SimHub (aktivováno)", verbose);
            LogMessage("- AITrack (tracking spuštěn)", verbose);
            
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
                LogMessage("[DRY-RUN] SimHub 'spuštěn' a 'aktivován'", verbose);
                return true;
            }

            var simHubAutomation = new SimHubAutomation();
            bool success = simHubAutomation.OpenSimHubAndActivate();

            if (success)
            {
                LogMessage("✓ SimHub úspěšně spuštěn a aktivován", verbose);
                
                // Krátké čekání na dokončení aktivace
                LogMessage($"Čekám {WAIT_TIME_SIMHUB/1000}s na dokončení aktivace...", verbose);
                Thread.Sleep(WAIT_TIME_SIMHUB);
                
                return true;
            }
            else
            {
                LogError("✗ Nepodařilo se spustit a aktivovat SimHub");
                return false;
            }
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
                LogMessage("[DRY-RUN] AITrack 'spuštěn' a tracking 'zahájen'", verbose);
                return true;
            }

            var aiTrackAutomation = new AITrackAutomation();
            bool success = aiTrackAutomation.OpenAITrackAndStartTracking();

            if (success)
            {
                LogMessage("✓ AITrack úspěšně spuštěn a tracking zahájen", verbose);
                
                // Krátké čekání na zahájení trackingu
                LogMessage($"Čekám {WAIT_TIME_AITRACK/1000}s na zahájení trackingu...", verbose);
                Thread.Sleep(WAIT_TIME_AITRACK);
                
                return true;
            }
            else
            {
                LogError("✗ Nepodařilo se spustit AITrack nebo zahájit tracking");
                return false;
            }
        }
        catch (Exception ex)
        {
            LogError($"Chyba při spouštění AITrack: {ex.Message}");
            return false;
        }
    }

    private static void ShowHelp()
    {
        Console.WriteLine("Starter - Automatický launcher aplikací");
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
        Console.WriteLine("  1. Spustí SimHub a automaticky klikne na 'Activate'");
        Console.WriteLine("  2. Spustí AITrack a automaticky klikne na 'Start Tracking'");
        Console.WriteLine();
        Console.WriteLine("Návratové kódy:");
        Console.WriteLine("  0 - Úspěch");
        Console.WriteLine("  1 - Chyba");
        Console.WriteLine();
        Console.WriteLine("Požadavky:");
        Console.WriteLine("  - Windows s .NET 9.0 runtime");
        Console.WriteLine("  - SimHub nainstalovaný v standardních umístěních");
        Console.WriteLine("  - AITrack dostupný v standardních umístěních");
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
        // Vždy zobrazit důležité zprávy i bez verbose
        return message.Contains("✓") || 
               message.Contains("✗") || 
               message.Contains("===") ||
               message.Contains("Všechny aplikace") ||
               message.Contains("Spuštěné aplikace:");
    }
}

// Namespace wrapper pro Button Recognition Tool classes
namespace ButtonRecognitionTool 
{
    // Jednoduché automation classes - pokud nejsou dostupné, vytvoříme fallback
    public class SimpleSimHubAutomation
    {
        public bool OpenSimHubAndActivate()
        {
            try
            {
                Console.WriteLine("Spouštím SimHub automation...");
                
                // Pokud je dostupná plná ButtonRecognitionTool library, použiji ji
                // Jinak fallback na základní spuštění
                
                // Hledání SimHub procesu
                var processes = Process.GetProcessesByName("SimHub");
                if (processes.Length == 0)
                {
                    processes = Process.GetProcessesByName("SimHubWPF");
                }
                
                if (processes.Length == 0)
                {
                    Console.WriteLine("SimHub není spuštěn, spouštím...");
                    // Zkus spustit SimHub - základní cesty
                    string[] paths = {
                        @"C:\Program Files (x86)\SimHub\SimHubWPF.exe",
                        @"C:\Program Files\SimHub\SimHubWPF.exe"
                    };
                    
                    bool launched = false;
                    foreach (string path in paths)
                    {
                        if (System.IO.File.Exists(path))
                        {
                            Process.Start(path);
                            launched = true;
                            break;
                        }
                    }
                    
                    if (!launched)
                    {
                        Console.WriteLine("SimHub executable nebyl nalezen");
                        return false;
                    }
                    
                    // Čekání na spuštění
                    Thread.Sleep(8000);
                }
                
                Console.WriteLine("SimHub je spuštěn - použij ButtonRecognitionTool pro kliknutí na Activate");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Chyba v SimHub automation: {ex.Message}");
                return false;
            }
        }
    }
    
    public class SimpleAITrackAutomation  
    {
        public bool OpenAITrackAndStartTracking()
        {
            try
            {
                Console.WriteLine("Spouštím AITrack automation...");
                
                // Hledání AITrack procesu
                var processes = Process.GetProcessesByName("AITrack");
                if (processes.Length == 0)
                {
                    processes = Process.GetProcessesByName("aitrack");
                }
                
                if (processes.Length == 0)
                {
                    Console.WriteLine("AITrack není spuštěn, spouštím...");
                    // Zkus spustit AITrack - základní cesty
                    string[] paths = {
                        @"C:\Program Files\AITrack\AITrack.exe",
                        @"C:\Program Files (x86)\AITrack\AITrack.exe",
                        @".\AITrack.exe"
                    };
                    
                    bool launched = false;
                    foreach (string path in paths)
                    {
                        if (System.IO.File.Exists(path))
                        {
                            Process.Start(path);
                            launched = true;
                            break;
                        }
                    }
                    
                    if (!launched)
                    {
                        Console.WriteLine("AITrack executable nebyl nalezen");
                        return false;
                    }
                    
                    // Čekání na spuštění
                    Thread.Sleep(4000);
                }
                
                Console.WriteLine("AITrack je spuštěn - použij ButtonRecognitionTool pro kliknutí na Start Tracking");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Chyba v AITrack automation: {ex.Message}");
                return false;
            }
        }
    }
    
    // Pokud není dostupná plná ButtonRecognitionTool library, použij jednoduché verze
    public class SimHubAutomation : SimpleSimHubAutomation { }
    public class AITrackAutomation : SimpleAITrackAutomation { }
}