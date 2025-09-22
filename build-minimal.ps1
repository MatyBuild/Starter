# Build skript pro Startér Launcher (minimální verze bez UI Automation)
# Kompiluje LauncherAll.cs do spustitelného Starter.exe pouze s WinAPI

param(
    [string]$OutputName = "Starter.exe",
    [string]$SourceFile = "LauncherAll.cs",
    [switch]$Verbose
)

Write-Host "=== Build Startér Launcher (Minimální verze) ===" -ForegroundColor Green
Write-Host "Zdrojový soubor: $SourceFile" -ForegroundColor Cyan
Write-Host "Výstupní soubor: $OutputName" -ForegroundColor Cyan

# Kontrola existence zdrojového souboru
if (-not (Test-Path $SourceFile)) {
    Write-Host "CHYBA: Zdrojový soubor '$SourceFile' nenalezen!" -ForegroundColor Red
    exit 1
}

# Nalezení .NET Framework csc.exe
$dotNetVersions = @(
    "v4.0.30319",
    "v3.5",
    "v2.0.50727"
)

$cscPath = $null
foreach ($version in $dotNetVersions) {
    $testPath = "$env:WINDIR\Microsoft.NET\Framework64\$version\csc.exe"
    if (Test-Path $testPath) {
        $cscPath = $testPath
        Write-Host "Nalezen C# kompilátor: $testPath" -ForegroundColor Green
        break
    }
    
    $testPath32 = "$env:WINDIR\Microsoft.NET\Framework\$version\csc.exe"
    if (Test-Path $testPath32) {
        $cscPath = $testPath32
        Write-Host "Nalezen C# kompilátor (32-bit): $testPath32" -ForegroundColor Green
        break
    }
}

if (-not $cscPath) {
    Write-Host "CHYBA: C# kompilátor (csc.exe) nenalezen!" -ForegroundColor Red
    Write-Host "Zkontrolujte, zda máte nainstalovaný .NET Framework" -ForegroundColor Yellow
    exit 1
}

Write-Host "VAROVÁNÍ: Stavba bez UI Automation - použije se pouze WinAPI fallback!" -ForegroundColor Yellow

# Smazání existujícího výstupního souboru
if (Test-Path $OutputName) {
    Remove-Item $OutputName -Force
    Write-Host "Smazán existující soubor: $OutputName" -ForegroundColor Yellow
}

# Sestavení příkazu pro kompilaci (pouze základní .NET references)
$compileArgs = @(
    "/target:exe"
    "/out:`"$OutputName`""
    "/platform:anycpu"
    "/optimize+"
    "/warn:0"
    "/define:NO_UI_AUTOMATION"
    "/reference:System.Web.Extensions.dll"
    "`"$SourceFile`""
)

if ($Verbose) {
    Write-Host "Příkaz kompilace:" -ForegroundColor Cyan
    Write-Host "$cscPath $($compileArgs -join ' ')" -ForegroundColor Gray
}

Write-Host "Kompiluje se..." -ForegroundColor Yellow

# Spustit kompilátor
try {
    # Vytvoř dočasné soubory pro output
    $tempOut = [System.IO.Path]::GetTempFileName()
    $tempErr = [System.IO.Path]::GetTempFileName()
    
    $process = Start-Process -FilePath $cscPath -ArgumentList $compileArgs -Wait -PassThru -NoNewWindow -RedirectStandardOutput $tempOut -RedirectStandardError $tempErr
    
    $stdout = if (Test-Path $tempOut) { Get-Content $tempOut -Raw } else { "" }
    $stderr = if (Test-Path $tempErr) { Get-Content $tempErr -Raw } else { "" }
    
    if ($process.ExitCode -eq 0) {
        Write-Host "=== KOMPILACE ÚSPĚŠNÁ ===" -ForegroundColor Green
        
        if (Test-Path $OutputName) {
            $fileInfo = Get-Item $OutputName
            Write-Host "Vytvořen soubor: $OutputName ($([math]::Round($fileInfo.Length / 1KB, 2)) KB)" -ForegroundColor Green
            Write-Host ""
            Write-Host "POZNÁMKA: Tato verze NEOBSAHUJE UI Automation!" -ForegroundColor Yellow
            Write-Host "  - Automatické klikání na tlačítka nebude fungovat" -ForegroundColor Yellow
            Write-Host "  - Použijte fallbackClick souřadnice v konfiguraci" -ForegroundColor Yellow
            Write-Host "  - Nebo nainstalujte Windows SDK pro plnou funkčnost" -ForegroundColor Yellow
        } else {
            Write-Host "CHYBA: Výstupní soubor nebyl vytvořen!" -ForegroundColor Red
        }
    } else {
        Write-Host "=== KOMPILACE SELHALA ===" -ForegroundColor Red
        Write-Host "Exit kód: $($process.ExitCode)" -ForegroundColor Red
        
        if ($stdout) {
            Write-Host "STDOUT:" -ForegroundColor Yellow
            Write-Host $stdout
        }
        
        if ($stderr) {
            Write-Host "STDERR:" -ForegroundColor Red
            Write-Host $stderr
        }
    }
    
    # Vyčisti dočasné soubory
    if (Test-Path $tempOut) { Remove-Item $tempOut -Force -ErrorAction SilentlyContinue }
    if (Test-Path $tempErr) { Remove-Item $tempErr -Force -ErrorAction SilentlyContinue }
    
    exit $process.ExitCode
    
} catch {
    Write-Host "CHYBA při spouštění kompilátoru: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}