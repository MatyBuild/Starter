#!/usr/bin/env powershell
# Jednoduchý build skript pro SimpleStarter v PowerShell

Write-Host "=== Quick Build SimpleStarter ===" -ForegroundColor Green
Write-Host ""

# Kontrola .NET SDK
try {
    $version = dotnet --version
    Write-Host ".NET SDK: $version" -ForegroundColor Green
} catch {
    Write-Host "CHYBA: .NET SDK nenalezeno!" -ForegroundColor Red
    exit 1
}

# Clean
Write-Host "Mazani starych buildu..." -ForegroundColor Yellow
if (Test-Path "Starter.exe") { Remove-Item "Starter.exe" }
if (Test-Path "temp_build") { Remove-Item "temp_build" -Recurse -Force }

# Vytvoření temp adresáře
Write-Host "Vytvarim docasny projekt..." -ForegroundColor Yellow
New-Item -ItemType Directory -Name "temp_build" | Out-Null
Set-Location "temp_build"

# Vytvoření projekt souboru
@'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net9.0</TargetFramework>
    <AssemblyName>Starter</AssemblyName>
  </PropertyGroup>
</Project>
'@ | Out-File -FilePath "SimpleStarter.csproj" -Encoding UTF8

# Kopírování zdrojového kódu
Copy-Item "..\SimpleStarter.cs" "Program.cs"

# Build
Write-Host "Building..." -ForegroundColor Yellow
dotnet build -c Release -o ".."

if ($LASTEXITCODE -eq 0) {
    Set-Location ".."
    Remove-Item "temp_build" -Recurse -Force
    
    Write-Host ""
    Write-Host "=== BUILD USPESNY! ===" -ForegroundColor Green
    if (Test-Path "Starter.exe") {
        Write-Host "Vytvoren: Starter.exe" -ForegroundColor Green
        
        # Ukázka použití
        Write-Host ""
        Write-Host "Testovani:" -ForegroundColor Cyan
        Write-Host "  .\Starter.exe --help" -ForegroundColor White
        Write-Host "  .\Starter.exe --dry-run" -ForegroundColor White
        Write-Host "  .\Starter.exe" -ForegroundColor White
    } else {
        Write-Host "VAROVANI: Starter.exe nebyl nalezen" -ForegroundColor Yellow
    }
} else {
    Set-Location ".."
    Remove-Item "temp_build" -Recurse -Force
    Write-Host "BUILD SELHAL!" -ForegroundColor Red
    exit 1
}

Write-Host ""
Read-Host "Stiskni Enter"