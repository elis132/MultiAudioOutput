# Multi Audio Output Build Script
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host " Multi Audio Output Build Script" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

# Build release
Write-Host "[1/3] Building application..." -ForegroundColor Yellow
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o bin\Release\publish
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Build failed!" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}
Write-Host ""

# Copy resources
Write-Host "[2/3] Copying resources..." -ForegroundColor Yellow
New-Item -ItemType Directory -Force -Path "bin\Release\publish\Resources" | Out-Null
Copy-Item "Resources\icon.ico" "bin\Release\publish\Resources\" -Force
Copy-Item "Resources\background.png" "bin\Release\publish\Resources\" -Force -ErrorAction SilentlyContinue
Write-Host ""

# Build installer
Write-Host "[3/3] Building installer..." -ForegroundColor Yellow
$innoSetup = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
if (Test-Path $innoSetup) {
    & $innoSetup "Installer\setup.iss"
    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "=====================================" -ForegroundColor Green
        Write-Host " Build Complete!" -ForegroundColor Green
        Write-Host "=====================================" -ForegroundColor Green
        Write-Host ""
        Write-Host "Application: bin\Release\publish\MultiAudioOutput.exe" -ForegroundColor White
        Write-Host "Installer: Installer\Output\MultiAudioOutput-Setup-1.0.0.exe" -ForegroundColor White
    } else {
        Write-Host "WARNING: Installer build failed!" -ForegroundColor Yellow
    }
} else {
    Write-Host "WARNING: Inno Setup not found. Skipping installer build." -ForegroundColor Yellow
    Write-Host "Download from: https://jrsoftware.org/isdl.php" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "=====================================" -ForegroundColor Green
    Write-Host " Build Complete (no installer)" -ForegroundColor Green
    Write-Host "=====================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "Application: bin\Release\publish\MultiAudioOutput.exe" -ForegroundColor White
}

Write-Host ""
Read-Host "Press Enter to exit"
