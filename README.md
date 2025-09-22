# Startér - Windows Aplikační Launcher

Startér je kompaktní Windows launcher napsaný v C#, který automatizuje spouštění aplikací podle konfigurace a stavu internetového připojení.

## Funkce

- **Automatické spouštění základních aplikací** (App1-App3)
- **Detekce internetového připojení** pomocí ping testů
- **Podmíněné spouštění aplikací** podle online/offline stavu
- **UI Automation** pro automatické klikání na tlačítka
- **Fallback mechanismus** s relativními souřadnicemi
- **Správa procesů** - ukončení již běžících instancí před novým spuštěním
- **Logování** do konzole a volitelně do souboru
- **Dry-run režim** pro testování bez skutečného spouštění

## Sestavení

### Pomocí PowerShell skriptu (doporučeno)

```powershell
# Standardní sestavení
.\build-launcher.ps1

# S verbose výstupem
.\build-launcher.ps1 -Verbose

# Vlastní název výstupního souboru
.\build-launcher.ps1 -OutputName "MujLauncher.exe"
```

### Ruční sestavení

```batch
# Najděte csc.exe ve vašem .NET Framework
# Typicky: C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe

csc.exe /target:exe /out:Starter.exe /platform:anycpu /optimize+ ^
  /reference:System.dll ^
  /reference:System.Core.dll ^
  /reference:UIAutomationClient.dll ^
  /reference:UIAutomationTypes.dll ^
  LauncherAll.cs
```

## Použití

### Základní použití

```batch
# Spuštění s konfiguračním souborem
.\Starter.exe --config ".\config.json"

# S logováním do souboru
.\Starter.exe --config ".\config.json" --log ".\launcher.log"

# Testovací režim (nespouští aplikace)
.\Starter.exe --config ".\config.json" --dry-run

# Vlastní timeout pro čekání na okna
.\Starter.exe --config ".\config.json" --timeoutWindow 60

# Kombinace parametrů
.\Starter.exe --config ".\config.json" --timeoutWindow 60 --log ".\launcher.log"
```

### Parametry příkazové řádky

| Parametr | Popis | Povinný |
|----------|-------|---------|
| `--config "cesta"` | Cesta k konfiguračnímu JSON souboru | ✓ |
| `--log "cesta"` | Cesta k log souboru (append mód) | ✗ |
| `--dry-run` | Testovací režim - nekliká ani nespouští | ✗ |
| `--timeoutWindow N` | Timeout pro čekání na okna v sekundách (default: 45) | ✗ |
| `--help`, `-h` | Zobrazí nápovědu | ✗ |

### Návratové kódy

- **0** - Úspěch
- **1** - Chyba (kontrola logů pro podrobnosti)

## Konfigurace

Launcher používá JSON konfigurační soubor. Viz `config.sample.json` pro příklad.

### Struktura konfigurace

#### Sekce "apps" - základní aplikace (App1-App3)

```json
{
  "apps": [
    {
      "path": "C:\\Program Files\\SimRacingStudio 2.0\\simracingstudio.exe",
      "windowTitle": "Sim Racing Studio",
      "click": null
    },
    {
      "path": "C:\\Desktop\\SimHub.exe", 
      "windowTitle": "SimHub",
      "click": {
        "type": "uia",
        "buttonName": "Activate",
        "automationId": null
      }
    },
    {
      "path": "C:\\Desktop\\AiTrack.exe",
      "windowTitle": "AiTrack",
      "click": {
        "type": "uia",
        "buttonName": "Start tracking",
        "automationId": null
      }
    }
  ]
}
```

#### Sekce "conditional" - podmíněné aplikace

```json
{
  "conditional": {
    "online": {
      "path": "C:\\Desktop\\Drivetech Launcher.exe",
      "windowTitle": "Drivetech Launcher - Hlavní okno",
      "click": {
        "type": "uia",
        "buttonName": "Connect",
        "automationId": null
      },
      "fallbackClick": {
        "x": 200,
        "y": 120
      }
    },
    "offline": {
      "path": "C:\\Desktop\\Drivetech Offline.exe",
      "windowTitle": "Drivetech Offline - Hlavní okno",
      "click": {
        "type": "uia",
        "buttonName": "Offline Mode",
        "automationId": null
      }
    },
    "pingHosts": ["1.1.1.1", "9.9.9.9"],
    "pingTimeoutMs": 1500,
    "startAfterAllBaseApps": true
  }
}
```

### Chování aplikací podle zadání

#### App1 - Sim Racing Studio
- **Cesta**: `C:\\Program Files\\SimRacingStudio 2.0\\simracingstudio.exe`
- **Fallback**: Hledá na ploše podle názvů: "Sim Racing Studio", "SMS", "Motion System"
- **Chování**: Spustí se, čeká na aktualizační dialog a zavře ho ("Nekontrolovat"), pak se minimalizuje
- **Poznámka**: Může chybět - launcher pokračuje dále

#### App2 - SimHub
- **Název**: SimHub
- **Fallback**: Hledá na ploše podle názvů: "SimHub", "simhub", "SIMHUB"
- **Chování**: Spustí se, čeká na hlavní okno, klikne na "Activate", minimalizuje se

#### App3 - AiTrack
- **Název**: AiTrack
- **Fallback**: Hledá na ploše podle názvů: "AiTrack", "AI Track", "aitrack"
- **Chování**: Spustí se, čeká na hlavní okno, klikne na "Start tracking", zůstává otevřené

#### App4/App5 (podmíněné)
- **App4 (Online)**: Drivetech Launcher (může být i "Drivetech Louncher", "Drivetech Online")
- **App5 (Offline)**: Drivetech Offline (může být i jen "Drivetech")
- **Hledání**: Hledají se na ploše ve všech variantách velikosti písmen

## Sekvence spouštění

1. **Načtení konfigurace** ze zadaného JSON souboru
2. **Spuštění základních aplikací** (App1-App3) - všechny najednou
3. **Kontrola konektivity** - ping na 1.1.1.1 a 9.9.9.9
4. **Rozhodnutí ONLINE/OFFLINE** podle výsledku pingu
5. **Spuštění podmíněné aplikace** (App4 nebo App5)
6. **UI Automation** - klikání na definované tlačítka

## Správa procesů

- Před spuštěním každé aplikace se **ukončí již běžící instance**
- Používá se `Process.GetProcessesByName()` a `Process.Kill()`
- Bezpečné ukončování s timeout 5 sekund

## UI Automation

### Podporované metody klikání

1. **UIA podle názvu tlačítka** (`buttonName`)
2. **UIA podle AutomationId** (`automationId`) 
3. **Fallback relativní klik** (`fallbackClick` - souřadnice x,y)

### Hledání oken

- Hledá podle `windowTitle` v `AutomationElement.Name`
- Timeout 45 sekund (konfigurovatelný přes `--timeoutWindow`)
- Polling každou sekundu

## Detekce připojení

- **Ping hosty**: 1.1.1.1, 9.9.9.9 (konfigurovatelné)
- **Timeout**: 1500ms na host (konfigurovatelný)
- **ONLINE**: Alespoň jeden ping úspěšný (`IPStatus.Success`)
- **OFFLINE**: Všechny pingy selhaly

## Logování

- **Konzole**: Vždy zapnuté s časovými značkami
- **Soubor**: Volitelný přes `--log` parametr (append mód)
- **Formát**: `[yyyy-MM-dd HH:mm:ss] zpráva`
- **Chyby**: Prefix "CHYBA: "

## Požadavky

- **Windows** s .NET Framework 4.0+
- **UI Automation** knihovny (obvykle součást Windows)
- **Administrátorská práva** - mohou být potřeba pro některé aplikace

## Troubleshooting

### Aplikace se nenašla
- Zkontrolujte cestu v konfiguraci
- Pro App1, App2, App3, App4, App5 se hledají i na ploše podle definovaných názvů (bez ohledu na velikost písmen)
- Zkontrolujte, zda soubory existují a jsou spustitelné

### UI Automation selhala
- Zkontrolujte názvy tlačítek v aplikaci (používejte Inspect.exe)
- Definujte `fallbackClick` souřadnice jako záložní řešení
- Zvyšte timeout přes `--timeoutWindow`

### Ping selhává
- Zkontrolujte síťové připojení
- Můžete změnit ping hosty v konfiguraci
- Zvyšte `pingTimeoutMs` v konfiguraci

### Aplikace se nespustí
- Zkontrolujte oprávnění
- Spusťte launcher jako administrátor
- Zkontrolujte logy pro konkrétní chyby

## Vytvořeno podle specifikace

Tento launcher byl vytvořen podle podrobné specifikace pro automatizaci spouštění sim racing a gaming aplikací s podporou online/offline režimů a inteligentním UI automation systémem.