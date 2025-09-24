# WARP - Windows Application Launcher System

**WARP** (Windows Application Rapid Processor) je pokročilý launcher systém navržený pro automatické spouštění a správu aplikací na Windows systémech s podporou UI Automation a pokročilého řízení procesů.

## 🎯 Přehled systému

WARP je komplexní řešení pro automatizaci spouštění aplikací, které kombinuje:
- **Inteligentní detekci procesů** - Automatické vyhledávání aplikací na ploše
- **UI Automation** - Pokročilé ovládání tlačítek a oken
- **Síťová konektivita** - Rozhodování na základě online/offline stavu
- **Robustní error handling** - Zpracování chyb a fallback mechanismy
- **Flexibilní konfiguraci** - JSON-based konfigurace pro různé scénáře

## 🏗️ Architektura systému

### Komponenty

1. **LauncherAll.cs** - Hlavní aplikace
2. **config.json / config.sample.json** - Konfigurace aplikací
3. **build-launcher.ps1** - Build script
4. **Button Recognition Tools** - Podpůrné nástroje pro detekci UI elementů

### Workflow procesu

```
1. Načtení konfigurace
     ↓
2. Spuštění základních aplikací (App1-App3)
   - App1: Sim Racing Studio (volitelné)
   - App2: SimHub (aktivace + minimalizace)
   - App3: AiTrack (start tracking)
     ↓
3. Test konektivity (ping 1.1.1.1 a 9.9.9.9)
     ↓
4. Podmíněné spuštění
   - ONLINE → Drivetech Launcher (App4)
   - OFFLINE → Drivetech Offline (App5)
     ↓
5. UI Automation (klikání na tlačítka)
     ↓
6. Dokončení s exit kódem
```

## ⚙️ Klíčové funkce

### 1. Proces Management
- **Detekce běžících procesů**: Kontrola, zda aplikace již běží
- **Automatické ukončování**: Ukončení duplicitních procesů
- **Restart mechanismus**: Bezpečné restartování aplikací
- **Desktop search**: Vyhledávání aplikací na ploše podle názvu

### 2. UI Automation
- **Window detection**: Čekání na okna s timeout
- **Button recognition**: Vyhledávání tlačítek podle názvu nebo AutomationId
- **Click simulation**: UI Automation + fallback na souřadnice
- **Window manipulation**: Minimalizace, zavírání oken

### 3. Connectivity Testing
- **Dual ping strategy**: Test na 1.1.1.1 a 9.9.9.9
- **Smart decision making**: Online/Offline rozhodování
- **Configurable timeouts**: Nastavitelný timeout pro ping testy
- **Fallback support**: Graceful degradation při síťových problémech

### 4. Error Handling & Logging
- **Comprehensive logging**: Konzole + soubor
- **Exception handling**: Try/catch bloky pro všechny kritické operace
- **Exit codes**: 0 = úspěch, 1 = chyba
- **Verbose output**: Detailní informace o průběhu

## 📝 Konfigurace

### Struktura config.json

```json
{
  "apps": [
    {
      "path": "C:\\Path\\App.exe",
      "windowTitle": "Window Title",
      "click": {
        "type": "uia",
        "buttonName": "Button Text",
        "automationId": "AutomationId"
      }
    }
  ],
  "conditional": {
    "online": {
      "path": "C:\\Path\\OnlineApp.exe",
      "windowTitle": "Online App",
      "click": { ... },
      "fallbackClick": { "x": 200, "y": 120 }
    },
    "offline": {
      "path": "C:\\Path\\OfflineApp.exe",
      "windowTitle": "Offline App",
      "click": { ... }
    },
    "pingHosts": ["1.1.1.1", "9.9.9.9"],
    "pingTimeoutMs": 1500,
    "startAfterAllBaseApps": true
  }
}
```

### Aplikační profily

#### App1 - Sim Racing Studio
- **Chování**: Volitelné spuštění, detekce aktualizací, minimalizace
- **Speciální funkce**: Automatické zavření update dialogu
- **Error tolerance**: Může selhat bez ovlivnění ostatních

#### App2 - SimHub  
- **Chování**: Spuštění → čekání na okno → klik "Activate" → minimalizace
- **Timeout**: 30 sekund (dlouhé spouštění)
- **Critical**: Musí uspět pro pokračování

#### App3 - AiTrack
- **Chování**: Spuštění → klik "Start tracking" → zůstat otevřené
- **UI Detection**: Vyhledávání specifického tlačítka
- **State**: Aktivní po celou dobu

#### App4/App5 - Drivetech (Conditional)
- **Online Mode**: Drivetech Launcher s "Connect" tlačítkem
- **Offline Mode**: Drivetech Offline s "Offline Mode"
- **Fallback**: Souřadnicový klik pokud UI Automation selže

## 🛠️ Použití

### Základní spuštění
```cmd
LauncherAll.exe --config "config.json"
```

### Pokročilé parametry
```cmd
# S logováním
LauncherAll.exe --config "config.json" --log "launcher.log"

# Dry-run režim (testování bez spouštění)
LauncherAll.exe --config "config.json" --dry-run

# Vlastní timeout pro okna
LauncherAll.exe --config "config.json" --timeoutWindow 60
```

### Build proces
```powershell
# Standardní build
.\build-launcher.ps1

# S vlastním názvem
.\build-launcher.ps1 -OutputName "MyLauncher.exe"

# Verbose output
.\build-launcher.ps1 -Verbose
```

## 🔧 Technické detaily

### Windows API Integration
- **User32.dll**: Window management, mouse simulation
- **UIAutomationClient.dll**: Modern UI element detection
- **UIAutomationTypes.dll**: Type definitions pro UI Automation
- **System.Net.NetworkInformation**: Ping functionality

### Click Mechanisms
1. **UI Automation (Primary)**: `InvokePattern` pro standardní tlačítka
2. **Coordinate Fallback**: Absolutní souřadnice + mouse events
3. **Window Activation**: `SetForegroundWindow` před kliknutím

### Process Detection Strategies
1. **Exact Path Match**: Přesná shoda s nakonfigurovanou cestou
2. **Desktop Search**: Hledání podle názvu na ploše (.exe, .lnk)
3. **Fuzzy Matching**: Různé varianty názvů (velká/malá písmena)
4. **Process Name Matching**: Detekce běžících procesů podle názvu

## 🚨 Troubleshooting

### Běžné problémy

#### 1. "Aplikace nenalezena"
- **Příčina**: Nesprávná cesta nebo aplikace není na ploše
- **Řešení**: Zkontrolovat cestu v config.json nebo umístit na plochu

#### 2. "Okno nenalezeno po Xs"
- **Příčina**: Aplikace se spouští pomalu nebo má jiný název okna
- **Řešení**: Zvýšit timeout nebo upravit windowTitle v konfiguraci

#### 3. "Tlačítko nenalezeno"
- **Příčina**: UI Automation nemůže najít element
- **Řešení**: Použít fallbackClick souřadnice nebo změnit buttonName

#### 4. "Ping selhaly - režim OFFLINE"
- **Příčina**: Síťové problémy nebo firewall
- **Řešení**: Zkontrolovat připojení nebo upravit pingHosts

### Debug postupy

1. **Použít --dry-run**: Test konfigurace bez spouštění
2. **Zkontrolovat logy**: Detailní informace v log souboru
3. **Použít Button Recognition Tool**: Analýza UI elementů
4. **Process Monitor**: Sledování file system aktivit

## 📊 Monitoring & Metrics

### Úspěšnost metrik
- **App Launch Success Rate**: Poměr úspěšných spuštění
- **UI Automation Success**: Úspěšnost klikání na tlačítka
- **Network Connectivity**: Online/Offline poměr
- **Error Recovery**: Úspěšnost fallback mechanismů

### Logování
```
[2024-01-15 10:30:15] === Startér Launcher spuštěn ===
[2024-01-15 10:30:15] Spouštím základní aplikace (App1-App3)...
[2024-01-15 10:30:16] App1 není dostupná - pokračujem
[2024-01-15 10:30:17] App2 spuštěna (PID: 1234)
[2024-01-15 10:30:18] Okno nalezeno: SimHub
[2024-01-15 10:30:19] Kliknuto na tlačítko: Activate
```

## 🔮 Budoucí rozšíření

### Plánované funkce
- **GUI konfigurátor**: Grafické rozhraní pro editaci konfigurace
- **Task Scheduler integration**: Automatické spouštění při startu systému
- **Remote monitoring**: Web dashboard pro sledování stavu
- **Plugin system**: Rozšiřitelnost pro custom akce
- **Performance profiling**: Měření času spouštění aplikací

### Možná vylepšení
- **Image recognition**: Rozpoznávání tlačítek pomocí obrázků
- **AI-powered UI detection**: Machine learning pro lepší detekci elementů
- **Cloud configuration**: Synchronizace konfigurace mezi zařízeními
- **Multi-monitor support**: Podpora pro více monitorů
- **Voice commands**: Hlasové ovládání launcheru

## 📋 Bezpečnostní aspekty

### Práva a oprávnění
- **Administrative privileges**: Některé aplikace vyžadují admin práva
- **Process termination**: Oprávnění pro ukončování procesů
- **UI Automation access**: Práva pro přístup k UI elementům
- **Network access**: Oprávnění pro ping testy

### Bezpečnostní opatření
- **Path validation**: Kontrola cest k aplikacím
- **Process verification**: Ověření identity spouštěných procesů
- **Timeout limits**: Omezení času čekání
- **Error containment**: Izolace chyb jednotlivých aplikací

---

## 📄 License & Credits

**WARP** je vyvíjen pro účely automatizace desktopových aplikací ve Windows prostředí. 
Využívá standardní Windows API a .NET Framework komponenty.

*Vytvořeno pro efektivní správu racing a simulation aplikací.*