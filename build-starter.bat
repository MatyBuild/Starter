@echo off
echo === Build Starter Launcher ===
echo.

if not exist "Starter.cs" (
    echo CHYBA: Starter.cs nenalezen!
    pause
    exit /b 1
)

echo Zdrojovy soubor: Starter.cs
echo Vystupni soubor: Starter.exe
echo.

echo Vytvarim docasny projekt...
echo ^<Project Sdk="Microsoft.NET.Sdk"^> > temp_starter.csproj
echo   ^<PropertyGroup^> >> temp_starter.csproj
echo     ^<OutputType^>Exe^</OutputType^> >> temp_starter.csproj
echo     ^<TargetFramework^>net9.0-windows^</TargetFramework^> >> temp_starter.csproj
echo     ^<AssemblyName^>Starter^</AssemblyName^> >> temp_starter.csproj
echo   ^</PropertyGroup^> >> temp_starter.csproj
echo ^</Project^> >> temp_starter.csproj

echo Sestavuji projekt...
dotnet build temp_starter.csproj -c Release --verbosity quiet

if %errorlevel% neq 0 (
    echo CHYBA: Sestaveni selhalo!
    goto cleanup
)

if exist "bin\Release\net9.0-windows\Starter.exe" (
    copy "bin\Release\net9.0-windows\Starter.exe" "Starter.exe" >nul
    echo.
    echo ✓ USPECH!
    echo Vystupni soubor: Starter.exe
    
    for %%A in ("Starter.exe") do (
        echo Velikost: %%~zA bytes
    )
    
    echo.
    echo Spusteni:
    echo   Starter.exe
    echo   Starter.exe --help
    echo   Starter.exe --dry-run --verbose
) else (
    echo CHYBA: Vystupni soubor nebyl vytvoren!
    goto cleanup
)

:cleanup
echo.
echo Uklizim docasne soubory...
if exist temp_starter.csproj del temp_starter.csproj
if exist bin rmdir /s /q bin
if exist obj rmdir /s /q obj

echo.
echo Hotovo!
pause