#!/usr/bin/env powershell
# Final build pro StarterProgram.cs

Write-Host "=== Final Build Starter ===" -ForegroundColor Green

# Clean
if (Test-Path "Starter.exe") { Remove-Item "Starter.exe" }
if (Test-Path "final_build") { Remove-Item "final_build" -Recurse -Force }

# Build složka
Write-Host "Building StarterProgram..." -ForegroundColor Yellow
New-Item -ItemType Directory -Name "final_build" | Out-Null
Set-Location "final_build"

# Projekt s klasickou Program třídou
@'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net9.0</TargetFramework>
    <AssemblyName>Starter</AssemblyName>
  </PropertyGroup>
</Project>
'@ | Out-File -FilePath "Starter.csproj" -Encoding UTF8

# Kopírování StarterProgram.cs
Copy-Item "..\StarterProgram.cs" "Program.cs"

# Build
dotnet build -c Release -o ".."

if ($LASTEXITCODE -eq 0) {
    Set-Location ".."
    Remove-Item "final_build" -Recurse -Force
    
    Write-Host ""
    Write-Host "=== USPECH! ===" -ForegroundColor Green
    Write-Host "Vytvoren: Starter.exe" -ForegroundColor Green
    
    # Test
    Write-Host ""
    Write-Host "Test:" -ForegroundColor Cyan
    Write-Host ".\Starter.exe --help" -ForegroundColor White
    Write-Host ""
    .\Starter.exe --help
    
} else {
    Set-Location ".."
    Remove-Item "final_build" -Recurse -Force
    Write-Host "BUILD SELHAL!" -ForegroundColor Red
}

Read-Host "Stiskni Enter"