# Build skript pro Startér Launcher
# Kompiluje LauncherAll.cs do spustitelného Startér.exe

param(
    [string]$OutputName = "Startér.exe",
    [string]$SourceFile = "LauncherAll.cs",
    [switch]$Verbose
)

Write-Host "=== Build Startér Launcher ===" -ForegroundColor Green
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

# Nalezení potřebných DLL knihoven
$windowsDir = [Environment]::GetFolderPath([Environment+SpecialFolder]::Windows)
$referencesDir = "$windowsDir\Microsoft.NET\Framework64\v4.0.30319"
if (-not (Test-Path $referencesDir)) {
    $referencesDir = "$windowsDir\Microsoft.NET\Framework\v4.0.30319"
}

# Možné lokace pro UI Automation DLL
$possibleUiAutomationPaths = @(
    "$referencesDir",
    "$windowsDir\System32",
    "$windowsDir\assembly\GAC_MSIL\UIAutomationClient\3.0.0.0__31bf3856ad364e35",
    "$windowsDir\assembly\GAC_MSIL\UIAutomationTypes\3.0.0.0__31bf3856ad364e35",
    "C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.0",
    "C:\Program Files\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.0",
    "C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2",
    "C:\Program Files\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8"
)

$requiredReferences = @(
    "System.dll",
    "System.Core.dll",
    "System.Web.Extensions.dll"
)

$uiAutomationReferences = @(
    "UIAutomationClient.dll",
    "UIAutomationTypes.dll"
)

$references = @()

# Přidej základní references
foreach ($ref in $requiredReferences) {
    $refPath = Join-Path $referencesDir $ref
    if (Test-Path $refPath) {
        $references += "/reference:`"$refPath`""
        if ($Verbose) {
            Write-Host "  Reference: $refPath" -ForegroundColor Gray
        }
    } else {
        Write-Host "VAROVÁNÍ: Reference '$ref' nenalezena" -ForegroundColor Yellow
    }
}

# Hledej UI Automation DLL
foreach ($uiRef in $uiAutomationReferences) {
    $found = $false
    foreach ($searchPath in $possibleUiAutomationPaths) {
        $refPath = Join-Path $searchPath $uiRef
        if (Test-Path $refPath) {
            $references += "/reference:`"$refPath`""
            if ($Verbose) {
                Write-Host "  Reference: $refPath" -ForegroundColor Gray
            }
            $found = $true
            break
        }
    }
    if (-not $found) {
        Write-Host "VAROVÁNÍ: UI Automation reference '$uiRef' nenalezena" -ForegroundColor Yellow
        Write-Host "  Zkuste nainstalovat Windows SDK nebo použít .NET Framework 4.5+" -ForegroundColor Yellow
    }
}

# Smazání existujícího výstupního souboru
if (Test-Path $OutputName) {
    Remove-Item $OutputName -Force
    Write-Host "Smazán existující soubor: $OutputName" -ForegroundColor Yellow
}

# Sestavení příkazu pro kompilaci
$compileArgs = @(
    "/target:exe"
    "/out:`"$OutputName`""
    "/platform:anycpu"
    "/optimize+"
    "/warn:0"
    "`"$SourceFile`""
) + $references

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
            
            # Test spuštění s --help
            Write-Host "Testuje se spuštění..." -ForegroundColor Cyan
            try {
                $testProcess = Start-Process -FilePath ".\$OutputName" -ArgumentList "--help" -Wait -PassThru -NoNewWindow -RedirectStandardOutput
                if ($testProcess.ExitCode -eq 1) {  # --help vrací 1, což je normální
                    Write-Host "Test spuštění: OK" -ForegroundColor Green
                } else {
                    Write-Host "Test spuštění: Neočekávaný exit kód $($testProcess.ExitCode)" -ForegroundColor Yellow
                }
            } catch {
                Write-Host "Varování: Test spuštění selhal: $($_.Exception.Message)" -ForegroundColor Yellow
            }
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
