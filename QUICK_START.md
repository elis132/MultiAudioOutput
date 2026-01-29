# Quick Start Guide

**Fast reference for building and releasing Multi Audio Output.**

---

## 🚀 Development

### Build and Run
```powershell
# Debug build
dotnet build

# Run
dotnet run

# Release build
.\scripts\build.ps1
```

---

## 📦 Create Release

### Full Release Process
```powershell
# 1. Create release artifacts
.\scripts\release.ps1 -Version 1.1.0

# 2. Test installer
.\artifacts\MultiAudioOutput-Setup-v1.1.0-x64.exe

# 3. Test portable
Expand-Archive .\artifacts\MultiAudioOutput-Portable-v1.1.0-x64.zip -DestinationPath test
.\test\MultiAudioOutput.exe

# 4. Commit and tag
git add .
git commit -m "chore: release v1.1.0"
git push origin main

git tag v1.1.0 -m "Release version 1.1.0"
git push origin v1.1.0

# 5. GitHub Actions automatically creates the release!
```

---

## 🔧 Build Commands

| Command | Purpose |
|---------|---------|
| `dotnet restore` | Restore dependencies |
| `dotnet build` | Build debug version |
| `.\scripts\build.ps1` | Build release version |
| `.\scripts\release.ps1 -Version X.Y.Z` | Create full release |
| `.\scripts\build-installer.ps1` | Build installer only |

---

## 📋 Before Release

### Update Version
1. `MultiAudioOutput.csproj` - Line 10
2. `installer/setup.iss` - Line 5
3. `CHANGELOG.md` - Add new version section

### Test Checklist
- [ ] Application builds
- [ ] Application runs
- [ ] All features work
- [ ] Installer works
- [ ] Portable version works

---

## 🌐 GitHub Actions

### Trigger CI (on every push)
```powershell
git push origin main
```

### Trigger Release (on tag)
```powershell
git tag v1.1.0
git push origin v1.1.0
```

**Automatic Release Includes:**
- Portable ZIP
- Installer EXE
- SHA256 checksums
- GitHub Release page

---

## 🐛 Troubleshooting

### Build Fails
```powershell
dotnet clean
dotnet restore
.\scripts\build.ps1
```

### Inno Setup Not Found
Download: https://jrsoftware.org/isdl.php
Install to: `C:\Program Files (x86)\Inno Setup 6\`

### .NET SDK Not Found
Download: https://dotnet.microsoft.com/download/dotnet/8.0
Install: .NET 8.0 SDK

---

## 📁 Important Directories

| Directory | Purpose |
|-----------|---------|
| `artifacts/` | Build outputs (gitignored) |
| `bin/obj/` | Temp build files (gitignored) |
| `installer/Output/` | Installer output (gitignored) |
| `docs/` | Documentation |
| `scripts/` | Build scripts |
| `Resources/` | Icons and images |

---

## 🔗 Quick Links

- **Repository**: https://github.com/elis132/MultiAudioOutput
- **Releases**: https://github.com/elis132/MultiAudioOutput/releases
- **Issues**: https://github.com/elis132/MultiAudioOutput/issues

---

## 📖 Full Documentation

- [README.md](README.md) - Main documentation
- [CONTRIBUTING.md](CONTRIBUTING.md) - How to contribute
- [docs/installer.md](docs/installer.md) - Installation guide
- [docs/RELEASE_CHECKLIST.md](docs/RELEASE_CHECKLIST.md) - Release process
- [PRODUCTION_READY.md](PRODUCTION_READY.md) - Production checklist
- [TRANSFORMATION_COMPLETE.md](TRANSFORMATION_COMPLETE.md) - What was done

---

**Need help? Check [PRODUCTION_READY.md](PRODUCTION_READY.md) for detailed testing commands.**
