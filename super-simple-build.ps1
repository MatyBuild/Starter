#!/usr/bin/env powershell
# Super jednoduchý build pouze pro SimpleStarter.cs

Write-Host "=== Super Simple Build ===" -ForegroundColor Green

# Kontrola .NET SDK
try {
    $version = dotnet --version
    Write-Host ".NET SDK: $version" -ForegroundColor Green
} catch {
    Write-Host "CHYBA: .NET SDK nenalezeno!" -ForegroundColor Red
    exit 1
}

# Clean
if (Test-Path "Starter.exe") { Remove-Item "Starter.exe" }
if (Test-Path "build_temp") { Remove-Item "build_temp" -Recurse -Force }

# Vytvoření izolované build složky
Write-Host "Vytvarim build... " -ForegroundColor Yellow
New-Item -ItemType Directory -Name "build_temp" | Out-Null
Set-Location "build_temp"

# Projekt s unikátním názvem
@'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net9.0</TargetFramework>
    <AssemblyName>Starter</AssemblyName>
    <StartupObject>SimpleStarter</StartupObject>
  </PropertyGroup>
</Project>
'@ | Out-File -FilePath "Build.csproj" -Encoding UTF8

# Kopírování POUZE SimpleStarter.cs (nic víc!)
Copy-Item "..\SimpleStarter.cs" "."

# Build s explicitním main class
Write-Host "Building..." -ForegroundColor Yellow
dotnet build Build.csproj -c Release -o ".." -p:StartupObject=SimpleStarter

if ($LASTEXITCODE -eq 0) {
    Set-Location ".."
    Remove-Item "build_temp" -Recurse -Force
    
    Write-Host ""
    Write-Host "=== BUILD USPESNY! ===" -ForegroundColor Green
    Write-Host "Vytvoren: Starter.exe" -ForegroundColor Green
    
    Write-Host ""
    Write-Host "Test:" -ForegroundColor Cyan
    Write-Host "  .\Starter.exe --help" -ForegroundColor White
} else {
    Set-Location ".."
    Remove-Item "build_temp" -Recurse -Force
    Write-Host "BUILD SELHAL!" -ForegroundColor Red
}

Read-Host "Stiskni Enter"