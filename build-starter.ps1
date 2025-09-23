param(
    [string]$OutputName = "Starter.exe",
    [switch]$Verbose
)

Write-Host "=== Build Starter Launcher ===" -ForegroundColor Green
Write-Host ""

# Check if source file exists
if (!(Test-Path "Starter.cs")) {
    Write-Host "CHYBA: Starter.cs nenalezen!" -ForegroundColor Red
    exit 1
}

Write-Host "Zdrojovy soubor: Starter.cs" -ForegroundColor Cyan
Write-Host "Vystupni soubor: $OutputName" -ForegroundColor Cyan
Write-Host ""

try {
    # Find CSC compiler
    $cscPath = ""
    $possiblePaths = @(
        "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe",
        "C:\Program Files\dotnet\sdk\9.0.305\Roslyn\bincore\csc.dll"
    )
    
    foreach ($path in $possiblePaths) {
        if (Test-Path $path) {
            $cscPath = $path
            break
        }
    }
    
    if ($cscPath -eq "") {
        Write-Host "Zkousim dotnet approach..." -ForegroundColor Yellow
        
        # Create temporary project file
        $tempProject = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net9.0-windows</TargetFramework>
    <AssemblyName>$($OutputName.Replace('.exe', ''))</AssemblyName>
  </PropertyGroup>
</Project>
"@
        
        Set-Content -Path "temp_starter.csproj" -Value $tempProject
        
        # Build with dotnet
        if ($Verbose) {
            dotnet build temp_starter.csproj -c Release -v normal
        } else {
            dotnet build temp_starter.csproj -c Release -v quiet
        }
        
        if ($LASTEXITCODE -eq 0) {
            $builtFile = "bin\Release\net9.0-windows\$($OutputName.Replace('.exe', '')).exe"
            if (Test-Path $builtFile) {
                Copy-Item $builtFile $OutputName -Force
                Write-Host ""
                Write-Host "✓ USPECH!" -ForegroundColor Green
                Write-Host "Vystupni soubor: $OutputName" -ForegroundColor Cyan
            }
        }
        
        # Clean up
        Remove-Item "temp_starter.csproj" -ErrorAction SilentlyContinue
        Remove-Item "bin" -Recurse -ErrorAction SilentlyContinue
        Remove-Item "obj" -Recurse -ErrorAction SilentlyContinue
        
    } else {
        Write-Host "Pouzivam CSC: $cscPath" -ForegroundColor Yellow
        
        # Compile with CSC
        $compileArgs = @(
            "/target:exe"
            "/out:$OutputName"
            "/platform:anycpu"
            "/optimize+"
            "Starter.cs"
        )
        
        if ($cscPath.EndsWith("csc.exe")) {
            & $cscPath $compileArgs
        } else {
            & dotnet $cscPath $compileArgs
        }
        
        if ($LASTEXITCODE -eq 0 -and (Test-Path $OutputName)) {
            Write-Host ""
            Write-Host "✓ USPECH!" -ForegroundColor Green
            Write-Host "Vystupni soubor: $OutputName" -ForegroundColor Cyan
        } else {
            Write-Host "CHYBA: Kompilace selhala!" -ForegroundColor Red
            exit 1
        }
    }
    
    # Show file info
    if (Test-Path $OutputName) {
        $fileInfo = Get-Item $OutputName
        Write-Host "Velikost: $([math]::Round($fileInfo.Length / 1KB, 2)) KB" -ForegroundColor Gray
        Write-Host "Vytvoren: $($fileInfo.CreationTime)" -ForegroundColor Gray
        
        Write-Host ""
        Write-Host "Spusteni:" -ForegroundColor Yellow
        Write-Host "  $OutputName" -ForegroundColor White
        Write-Host "  $OutputName --help" -ForegroundColor White
        Write-Host "  $OutputName --dry-run --verbose" -ForegroundColor White
    }

} catch {
    Write-Host "CHYBA: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Hotovo!" -ForegroundColor Green