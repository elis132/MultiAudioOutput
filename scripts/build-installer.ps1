# Multi Audio Output - Build Installer Only
# Builds just the Inno Setup installer

Write-Host "========================================" -ForegroundColor Cyan
Write-Host " Build Installer" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $PSScriptRoot

# Check for Inno Setup
$innoSetup = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
if (-not (Test-Path $innoSetup)) {
    Write-Host "ERROR: Inno Setup not found!" -ForegroundColor Red
    Write-Host "Download from: https://jrsoftware.org/isdl.php" -ForegroundColor Yellow
    exit 1
}

# Check for published files
$publishDir = Join-Path $projectRoot "artifacts\publish"
if (-not (Test-Path $publishDir)) {
    Write-Host "ERROR: Published files not found!" -ForegroundColor Red
    Write-Host "Run .\scripts\build.ps1 first" -ForegroundColor Yellow
    exit 1
}

Write-Host "Building installer..." -ForegroundColor Yellow
$setupIss = Join-Path $projectRoot "installer\setup.iss"

& $innoSetup $setupIss

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host " Installer Build Complete!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""

    $outputDir = Join-Path $projectRoot "installer\Output"
    $installer = Get-ChildItem $outputDir -Filter "*.exe" | Select-Object -First 1

    if ($installer) {
        $size = "{0:N2} MB" -f ($installer.Length / 1MB)
        Write-Host "Installer: $($installer.FullName)" -ForegroundColor White
        Write-Host "Size: $size" -ForegroundColor White
    }
}
else {
    Write-Host "ERROR: Installer build failed!" -ForegroundColor Red
    exit 1
}

Write-Host ""
