# Multi Audio Output - Release Script
# Builds portable ZIP and installer

param(
    [Parameter(Mandatory=$true)]
    [string]$Version,
    [switch]$SkipBuild = $false
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host " Multi Audio Output - Release Script" -ForegroundColor Cyan
Write-Host " Version: $Version" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $PSScriptRoot
$artifactsDir = Join-Path $projectRoot "artifacts"
$publishDir = Join-Path $artifactsDir "publish"

# Build application
if (-not $SkipBuild) {
    Write-Host "[1/4] Building application..." -ForegroundColor Yellow
    & (Join-Path $PSScriptRoot "build.ps1") -Configuration Release
    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERROR: Build failed!" -ForegroundColor Red
        exit 1
    }
}
else {
    Write-Host "[1/4] Skipping build (using existing)..." -ForegroundColor Yellow
}

# Create portable ZIP
Write-Host "[2/4] Creating portable ZIP..." -ForegroundColor Yellow
$portableZip = Join-Path $artifactsDir "MultiAudioOutput-Portable-v$Version-x64.zip"

if (Test-Path $portableZip) {
    Remove-Item $portableZip -Force
}

Compress-Archive -Path "$publishDir\*" -DestinationPath $portableZip
Write-Host "  Created: $portableZip" -ForegroundColor Green

# Build installer
Write-Host "[3/4] Building installer..." -ForegroundColor Yellow

$innoSetup = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
if (-not (Test-Path $innoSetup)) {
    Write-Host "ERROR: Inno Setup not found at: $innoSetup" -ForegroundColor Red
    Write-Host "Download from: https://jrsoftware.org/isdl.php" -ForegroundColor Yellow
    exit 1
}

# Update version in setup.iss
$setupIss = Join-Path $projectRoot "installer\setup.iss"
$setupTemp = Join-Path $projectRoot "installer\setup-temp.iss"

$setupContent = Get-Content $setupIss -Raw
$setupContent = $setupContent -replace '#define MyAppVersion ".*"', "#define MyAppVersion ""$Version"""
$setupContent | Set-Content $setupTemp

# Build installer
& $innoSetup $setupTemp

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Installer build failed!" -ForegroundColor Red
    Remove-Item $setupTemp -Force -ErrorAction SilentlyContinue
    exit 1
}

# Clean up temp file
Remove-Item $setupTemp -Force -ErrorAction SilentlyContinue

# Move installer to artifacts
$installerSource = Join-Path $projectRoot "installer\Output\MultiAudioOutput-Setup-$Version.exe"
$installerDest = Join-Path $artifactsDir "MultiAudioOutput-Setup-v$Version-x64.exe"

if (Test-Path $installerSource) {
    Move-Item $installerSource $installerDest -Force
    Write-Host "  Created: $installerDest" -ForegroundColor Green
}
else {
    Write-Host "ERROR: Installer not found at expected location" -ForegroundColor Red
    exit 1
}

# Generate checksums
Write-Host "[4/4] Generating SHA256 checksums..." -ForegroundColor Yellow
$checksumFile = Join-Path $artifactsDir "SHA256SUMS.txt"

$files = Get-ChildItem $artifactsDir -Filter "MultiAudioOutput-*" | Where-Object { $_.Extension -in ".zip", ".exe" }
$checksums = @()

foreach ($file in $files) {
    $hash = (Get-FileHash $file.FullName -Algorithm SHA256).Hash
    $checksums += "$hash  $($file.Name)"
}

$checksums | Out-File -FilePath $checksumFile -Encoding utf8
Write-Host "  Created: $checksumFile" -ForegroundColor Green

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host " Release Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Release artifacts:" -ForegroundColor Yellow
foreach ($file in $files) {
    $size = "{0:N2} MB" -f ($file.Length / 1MB)
    Write-Host "  $($file.Name) ($size)" -ForegroundColor White
}

Write-Host ""
Write-Host "Checksums:" -ForegroundColor Yellow
Get-Content $checksumFile | ForEach-Object {
    Write-Host "  $_" -ForegroundColor White
}

Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "  1. Test the installer and portable version" -ForegroundColor White
Write-Host "  2. Create a git tag: git tag v$Version" -ForegroundColor White
Write-Host "  3. Push the tag: git push origin v$Version" -ForegroundColor White
Write-Host "  4. GitHub Actions will create the release automatically" -ForegroundColor White
Write-Host ""
