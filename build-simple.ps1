#!/usr/bin/env powershell
# Build script pro SimpleStarter - bez konfliktu s ostatnimi Main metodami

Write-Host "=== Build SimpleStarter ===" -ForegroundColor Green
Write-Host ""

# Kontrola .NET SDK
Write-Host "Kontrola .NET SDK..." -ForegroundColor Yellow
try {
    $dotnetVersion = dotnet --version
    Write-Host ".NET SDK nalezeno: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host "CHYBA: .NET SDK neni nainstalovany nebo neni v PATH!" -ForegroundColor Red
    Write-Host "Stahni a nainstaluj .NET SDK z: https://dotnet.microsoft.com/download" -ForegroundColor Yellow
    Read-Host "Stiskni Enter pro ukonceni"
    exit 1
}

# Clean starych buildu
Write-Host ""
Write-Host "Mazani starych buildu..." -ForegroundColor Yellow
if (Test-Path "Starter.exe") { Remove-Item "Starter.exe" }
if (Test-Path "SimpleStarter.exe") { Remove-Item "SimpleStarter.exe" }
if (Test-Path "bin") { Remove-Item "bin" -Recurse -Force }
if (Test-Path "obj") { Remove-Item "obj" -Recurse -Force }

# Build pouze SimpleStarter.cs (bez ostatnich cs souboru)
Write-Host ""
Write-Host "Building SimpleStarter.exe..." -ForegroundColor Yellow

$buildCommand = "dotnet build SimpleStarter.cs -c Release -o . --verbosity quiet"
Write-Host "Prikaz: $buildCommand" -ForegroundColor Gray

try {
    Invoke-Expression $buildCommand
    
    if ($LASTEXITCODE -eq 0) {
        # Prejmenovani pokud je potreba
        if (Test-Path "SimpleStarter.exe") {
            if (Test-Path "Starter.exe") { Remove-Item "Starter.exe" }
            Move-Item "SimpleStarter.exe" "Starter.exe"
            Write-Host ""
            Write-Host "=== BUILD USPESNY! ===" -ForegroundColor Green
            Write-Host "Vytvoren soubor: Starter.exe" -ForegroundColor Green
        } else {
            Write-Host "VAROVANI: SimpleStarter.exe nebyl nalezen po buildu" -ForegroundColor Yellow
            if (Test-Path "Starter.exe") {
                Write-Host "Ale Starter.exe existuje - build byl pravdepodobne uspesny" -ForegroundColor Green
            }
        }
        
        Write-Host ""
        Write-Host "Testovani:" -ForegroundColor Cyan
        Write-Host "  .\Starter.exe --help" -ForegroundColor White
        Write-Host "  .\Starter.exe --dry-run --verbose" -ForegroundColor White
        Write-Host "  .\Starter.exe" -ForegroundColor White
        
    } else {
        Write-Host ""
        Write-Host "CHYBA pri buildovani! Exit code: $LASTEXITCODE" -ForegroundColor Red
    }
} catch {
    Write-Host ""
    Write-Host "CHYBA pri buildovani: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Read-Host "Stiskni Enter pro ukonceni"