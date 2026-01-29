# Multi Audio Output - Build Script
# Builds the application for release

param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [switch]$SelfContained = $false,
    [switch]$SkipRestore = $false
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host " Multi Audio Output - Build Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $PSScriptRoot
$artifactsDir = Join-Path $projectRoot "artifacts"
$publishDir = Join-Path $artifactsDir "publish"

# Clean artifacts directory
if (Test-Path $artifactsDir) {
    Write-Host "[1/4] Cleaning artifacts directory..." -ForegroundColor Yellow
    Remove-Item $artifactsDir -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $publishDir | Out-Null

# Restore dependencies
if (-not $SkipRestore) {
    Write-Host "[2/4] Restoring dependencies..." -ForegroundColor Yellow
    dotnet restore $projectRoot
    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERROR: Restore failed!" -ForegroundColor Red
        exit 1
    }
}
else {
    Write-Host "[2/4] Skipping restore..." -ForegroundColor Yellow
}

# Build
Write-Host "[3/4] Building application..." -ForegroundColor Yellow
$buildArgs = @(
    "publish"
    "-c", $Configuration
    "-r", $Runtime
    "--self-contained", $SelfContained.ToString().ToLower()
    "-p:PublishSingleFile=true"
    "-o", $publishDir
)

dotnet @buildArgs

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Build failed!" -ForegroundColor Red
    exit 1
}

# Copy resources
Write-Host "[4/4] Copying resources..." -ForegroundColor Yellow
$resourcesDir = Join-Path $publishDir "Resources"
New-Item -ItemType Directory -Force -Path $resourcesDir | Out-Null

$sourceResources = Join-Path $projectRoot "Resources"
Copy-Item (Join-Path $sourceResources "icon.ico") $resourcesDir -Force
Copy-Item (Join-Path $sourceResources "background.png") $resourcesDir -Force -ErrorAction SilentlyContinue

# Copy documentation
Copy-Item (Join-Path $projectRoot "LICENSE") $publishDir -Force
Copy-Item (Join-Path $projectRoot "README.md") $publishDir -Force
Copy-Item (Join-Path $projectRoot "CHANGELOG.md") $publishDir -Force

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host " Build Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Output: $publishDir" -ForegroundColor White
Write-Host ""

# List built files
Write-Host "Files:" -ForegroundColor Yellow
Get-ChildItem $publishDir -File | ForEach-Object {
    $size = "{0:N2} MB" -f ($_.Length / 1MB)
    Write-Host "  $($_.Name) ($size)" -ForegroundColor White
}

Write-Host ""
