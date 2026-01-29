# Build Scripts

This directory contains PowerShell scripts for building and releasing Multi Audio Output.

## Scripts

### build.ps1

Builds the application for release.

**Usage:**
```powershell
.\scripts\build.ps1 [-Configuration Release] [-Runtime win-x64] [-SelfContained] [-SkipRestore]
```

**Parameters:**
- `-Configuration` - Build configuration (default: Release)
- `-Runtime` - Target runtime (default: win-x64)
- `-SelfContained` - Create self-contained build (default: false)
- `-SkipRestore` - Skip dependency restore (default: false)

**Examples:**
```powershell
# Standard release build
.\scripts\build.ps1

# Debug build
.\scripts\build.ps1 -Configuration Debug

# Self-contained build
.\scripts\build.ps1 -SelfContained
```

**Output:**
- `artifacts/publish/MultiAudioOutput.exe`
- `artifacts/publish/Resources/`
- `artifacts/publish/LICENSE`
- `artifacts/publish/README.md`
- `artifacts/publish/CHANGELOG.md`

---

### release.ps1

Creates a complete release with portable ZIP and installer.

**Usage:**
```powershell
.\scripts\release.ps1 -Version <version> [-SkipBuild]
```

**Parameters:**
- `-Version` - **Required.** Release version (e.g., "1.1.0")
- `-SkipBuild` - Skip build step, use existing artifacts

**Examples:**
```powershell
# Full release build
.\scripts\release.ps1 -Version 1.1.0

# Use existing build
.\scripts\release.ps1 -Version 1.1.0 -SkipBuild
```

**Output:**
- `artifacts/MultiAudioOutput-Portable-v1.1.0-x64.zip`
- `artifacts/MultiAudioOutput-Setup-v1.1.0-x64.exe`
- `artifacts/SHA256SUMS.txt`

**Requirements:**
- Inno Setup 6.x installed at: `C:\Program Files (x86)\Inno Setup 6\ISCC.exe`

---

### build-installer.ps1

Builds only the Inno Setup installer using existing published files.

**Usage:**
```powershell
.\scripts\build-installer.ps1
```

**Requirements:**
- Application already built in `artifacts/publish/`
- Inno Setup 6.x installed

**Output:**
- `installer/Output/MultiAudioOutput-Setup-{version}.exe`

---

## Workflow

### Development Build

```powershell
# Quick build for testing
.\scripts\build.ps1 -Configuration Debug
```

### Release Build

```powershell
# Create full release
.\scripts\release.ps1 -Version 1.1.0
```

This will:
1. Build the application
2. Create portable ZIP
3. Build installer
4. Generate checksums

### CI/CD

GitHub Actions automatically:
- Builds on every push/PR
- Creates releases when tags are pushed

To trigger a release:
```powershell
git tag v1.1.0
git push origin v1.1.0
```

---

## Requirements

### All Scripts
- Windows 10/11
- PowerShell 5.1 or later
- .NET 8.0 SDK

### release.ps1 and build-installer.ps1
- Inno Setup 6.x

Download Inno Setup: https://jrsoftware.org/isdl.php

---

## Troubleshooting

### "Inno Setup not found"

Install Inno Setup 6.x to default location:
```
C:\Program Files (x86)\Inno Setup 6\
```

Or update the path in the scripts.

### "Published files not found"

Run build first:
```powershell
.\scripts\build.ps1
```

### "Permission denied"

Run PowerShell as Administrator or adjust execution policy:
```powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

### Build fails

1. Ensure .NET 8.0 SDK is installed:
   ```powershell
   dotnet --version
   ```

2. Restore dependencies:
   ```powershell
   dotnet restore
   ```

3. Clean and rebuild:
   ```powershell
   dotnet clean
   .\scripts\build.ps1
   ```

---

## Manual Build

If scripts don't work, build manually:

```powershell
# Restore
dotnet restore

# Build
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o artifacts/publish

# Copy resources
New-Item -ItemType Directory -Force -Path "artifacts/publish/Resources"
Copy-Item "Resources/icon.ico" "artifacts/publish/Resources/"
Copy-Item "Resources/background.png" "artifacts/publish/Resources/" -ErrorAction SilentlyContinue
Copy-Item "LICENSE" "artifacts/publish/"
Copy-Item "README.md" "artifacts/publish/"
Copy-Item "CHANGELOG.md" "artifacts/publish/"

# Build installer (Inno Setup)
& "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" "installer\setup.iss"
```
