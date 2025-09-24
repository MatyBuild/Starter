@echo off
REM Jednoduchý build skript pro SimpleStarter
echo === Build SimpleStarter (Clean) ===
echo.

REM Kontrola .NET SDK
echo Kontrola .NET SDK...
dotnet --version >nul 2>&1
if %ERRORLEVEL% neq 0 (
    echo CHYBA: .NET SDK neni nainstalovany!
    pause
    exit /b 1
)

echo .NET SDK OK: 
dotnet --version

REM Vytvoření dočasného adresáře
echo.
echo Vytvarim docasny projekt...
if exist temp_build rmdir /s /q temp_build
mkdir temp_build
cd temp_build

REM Vytvoření .csproj
echo ^<Project Sdk="Microsoft.NET.Sdk"^> > SimpleStarter.csproj
echo   ^<PropertyGroup^> >> SimpleStarter.csproj
echo     ^<OutputType^>Exe^</OutputType^> >> SimpleStarter.csproj
echo     ^<TargetFramework^>net9.0^</TargetFramework^> >> SimpleStarter.csproj
echo     ^<AssemblyName^>Starter^</AssemblyName^> >> SimpleStarter.csproj
echo   ^</PropertyGroup^> >> SimpleStarter.csproj
echo ^</Project^> >> SimpleStarter.csproj

REM Kopírování zdrojového souboru
copy ..\SimpleStarter.cs Program.cs >nul

REM Build
echo.
echo Building...
dotnet build -c Release -o ..\

if %ERRORLEVEL% neq 0 (
    cd ..
    echo.
    echo CHYBA pri buildovani!
    rmdir /s /q temp_build
    pause
    exit /b 1
)

REM Cleanup
cd ..
rmdir /s /q temp_build

REM Success
echo.
echo === BUILD USPESNY! ===
if exist Starter.exe (
    echo Vytvoren soubor: Starter.exe
    echo.
    echo Testovani:
    echo   Starter.exe --help
    echo   Starter.exe --dry-run
) else (
    echo VAROVANI: Starter.exe nebyl nalezen
)

echo.
pause