param(
    [string]$OutputName = "ButtonRecognitionTool.exe",
    [switch]$Verbose
)

Write-Host "=== Build Button Recognition Tool ===" -ForegroundColor Green
Write-Host ""

# Check if we're in the right directory
if (!(Test-Path "ButtonRecognitionTool.csproj")) {
    Write-Host "CHYBA: ButtonRecognitionTool.csproj nenalezen!" -ForegroundColor Red
    Write-Host "Ujistěte se, že jste ve správné složce projektu." -ForegroundColor Yellow
    exit 1
}

# Check required source files
$requiredFiles = @(
    "Program.cs",
    "WindowsAPIHelper.cs", 
    "ButtonRecognizer.cs",
    "ModernUIHelper.cs",
    "CoordinateButtonHelper.cs",
    "SimHubAutomation.cs",
    "AITrackAutomation.cs"
)

$missingFiles = @()
foreach ($file in $requiredFiles) {
    if (!(Test-Path $file)) {
        $missingFiles += $file
    }
}

if ($missingFiles.Count -gt 0) {
    Write-Host "CHYBA: Chybějící zdrojové soubory:" -ForegroundColor Red
    foreach ($file in $missingFiles) {
        Write-Host "  - $file" -ForegroundColor Red
    }
    exit 1
}

Write-Host "Zdrojové soubory: OK" -ForegroundColor Green
Write-Host "Výstupní soubor: $OutputName" -ForegroundColor Cyan
Write-Host ""

try {
    Write-Host "Čištění předchozích buildů..." -ForegroundColor Yellow
    if ($Verbose) {
        dotnet clean --verbosity normal
    } else {
        dotnet clean --verbosity quiet
    }

    Write-Host "Sestavování projektu..." -ForegroundColor Yellow
    
    if ($Verbose) {
        $buildResult = dotnet build -c Release --verbosity normal
    } else {
        $buildResult = dotnet build -c Release --verbosity quiet
    }
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "CHYBA: Sestavení selhalo!" -ForegroundColor Red
        exit $LASTEXITCODE
    }

    Write-Host "Publikování single-file executable..." -ForegroundColor Yellow
    
    $publishArgs = @(
        "publish"
        "-c", "Release"
        "-r", "win-x64"
        "--self-contained", "false"
        "-p:PublishSingleFile=true"
        "-p:AssemblyName=$($OutputName.Replace('.exe', ''))"
        "-o", "./dist"
    )
    
    if ($Verbose) {
        $publishArgs += "--verbosity", "normal"
    } else {
        $publishArgs += "--verbosity", "quiet"
    }
    
    $publishResult = & dotnet $publishArgs
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "CHYBA: Publikování selhalo!" -ForegroundColor Red
        exit $LASTEXITCODE
    }

    # Copy the output file to current directory with desired name
    $sourceFile = "./dist/$($OutputName.Replace('.exe', '')).exe"
    $targetFile = "./$OutputName"
    
    if (Test-Path $sourceFile) {
        Copy-Item $sourceFile $targetFile -Force
        Write-Host ""
        Write-Host "✓ ÚSPĚCH!" -ForegroundColor Green
        Write-Host "Výstupní soubor: $targetFile" -ForegroundColor Cyan
        
        $fileInfo = Get-Item $targetFile
        Write-Host "Velikost: $([math]::Round($fileInfo.Length / 1MB, 2)) MB" -ForegroundColor Gray
        Write-Host "Vytvořeno: $($fileInfo.CreationTime)" -ForegroundColor Gray
        
        Write-Host ""
        Write-Host "Spuštění:" -ForegroundColor Yellow
        Write-Host "  $targetFile" -ForegroundColor White
        
    } else {
        Write-Host "CHYBA: Výstupní soubor nebyl vytvořen: $sourceFile" -ForegroundColor Red
        exit 1
    }

} catch {
    Write-Host "CHYBA: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Hotovo!" -ForegroundColor Green