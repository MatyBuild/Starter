@echo off
REM Build skript pro SimpleStarter - jednoduchá verze
echo === Build SimpleStarter ===
echo.

REM Kontrola .NET SDK
echo Kontrola .NET SDK...
dotnet --version >nul 2>&1
if %ERRORLEVEL% neq 0 (
    echo CHYBA: .NET SDK neni nainstalovany nebo neni v PATH!
    echo Stahni a nainstaluj .NET SDK z: https://dotnet.microsoft.com/download
    pause
    exit /b 1
)

echo .NET SDK nalezeno:
dotnet --version

REM Clean
echo.
echo Mazani starych buildu...
if exist Starter.exe del Starter.exe
if exist SimpleStarter.exe del SimpleStarter.exe

REM Build pouze SimpleStarter.cs jako exe
echo.
echo Kompiluje SimpleStarter.cs...
dotnet publish -f net9.0 -c Release --self-contained false -o . SimpleStarter.cs

if %ERRORLEVEL% neq 0 (
    echo.
    echo CHYBA pri buildovani!
    pause
    exit /b 1
)

REM Rename output
if exist SimpleStarter.exe (
    echo.
    echo Prejmenovavam SimpleStarter.exe na Starter.exe...
    move SimpleStarter.exe Starter.exe
)

REM Success
echo.
echo === BUILD USPESNY! ===
echo Vytvoren soubor: Starter.exe
echo.
echo Testovani:
echo   Starter.exe --help
echo   Starter.exe --dry-run --verbose
echo   Starter.exe
echo.
pause