# Release Checklist

Use this checklist before creating a new release.

## Pre-Release

### Code Quality
- [ ] All features working as expected
- [ ] No debug code or console logs in production
- [ ] Code follows project conventions
- [ ] No hardcoded paths or secrets

### Version Management
- [ ] Update version in `MultiAudioOutput.csproj`
- [ ] Update version in `installer/setup.iss`
- [ ] Add release notes to `CHANGELOG.md`
- [ ] Update README.md if needed

### Documentation
- [ ] README.md is up to date
- [ ] CHANGELOG.md has all changes
- [ ] docs/ folder has current information
- [ ] Code comments are accurate

### Testing
- [ ] Clean build succeeds: `.\scripts\build.ps1`
- [ ] Application runs without errors
- [ ] All audio devices detected
- [ ] Multi-device output works
- [ ] Channel modes function correctly
- [ ] Settings save and load
- [ ] Tray icon works
- [ ] Start with Windows works
- [ ] Language switching works
- [ ] Device renaming works

### Installer Testing
- [ ] Build installer: `.\scripts\release.ps1 -Version X.Y.Z`
- [ ] Install on clean Windows VM
- [ ] Desktop shortcut created (if selected)
- [ ] Start menu shortcut created
- [ ] Start with Windows works (if selected)
- [ ] Application launches after install
- [ ] Uninstaller works correctly
- [ ] Settings removed if selected during uninstall

### Portable Testing
- [ ] Portable ZIP created
- [ ] Extract and run on clean system
- [ ] Application functions without install
- [ ] portable.txt mode works

## Release Process

### 1. Local Build and Test
```powershell
# Build release
.\scripts\build.ps1 -Configuration Release

# Create release artifacts
.\scripts\release.ps1 -Version 1.1.0

# Test installer
.\artifacts\MultiAudioOutput-Setup-v1.1.0-x64.exe

# Test portable
Expand-Archive .\artifacts\MultiAudioOutput-Portable-v1.1.0-x64.zip -DestinationPath test
.\test\MultiAudioOutput.exe
```

### 2. Commit Changes
```powershell
git add .
git commit -m "chore: prepare release v1.1.0"
git push origin main
```

### 3. Create and Push Tag
```powershell
git tag v1.1.0
git push origin v1.1.0
```

### 4. Verify GitHub Actions
- [ ] CI workflow passes
- [ ] Release workflow starts
- [ ] Artifacts uploaded
- [ ] GitHub Release created

### 5. Post-Release
- [ ] Test download links
- [ ] Verify checksums
- [ ] Test installer from GitHub
- [ ] Test portable from GitHub
- [ ] Announce release (if applicable)

## Post-Release Checks

### GitHub Release
- [ ] Release is published (not draft)
- [ ] Release notes are clear
- [ ] All assets attached:
  - [ ] MultiAudioOutput-Setup-vX.Y.Z-x64.exe
  - [ ] MultiAudioOutput-Portable-vX.Y.Z-x64.zip
  - [ ] SHA256SUMS.txt
- [ ] Checksums match local build

### Documentation
- [ ] README.md download links work
- [ ] Installation guide is accurate
- [ ] Changelog is complete

### User Communication
- [ ] Update website (if applicable)
- [ ] Post to social media (if applicable)
- [ ] Notify users (if applicable)

## Hotfix Process

If critical bug found after release:

1. Create hotfix branch
```powershell
git checkout -b hotfix/v1.1.1
```

2. Fix the bug and test thoroughly

3. Update version to X.Y.Z+1
   - MultiAudioOutput.csproj
   - installer/setup.iss
   - CHANGELOG.md

4. Commit and tag
```powershell
git commit -m "fix: critical bug description"
git tag v1.1.1
git push origin hotfix/v1.1.1
git push origin v1.1.1
```

5. Merge back to main
```powershell
git checkout main
git merge hotfix/v1.1.1
git push origin main
```

## Version Numbering

Follow Semantic Versioning (MAJOR.MINOR.PATCH):

- **MAJOR**: Breaking changes, major features
- **MINOR**: New features, backward compatible
- **PATCH**: Bug fixes, small improvements

Examples:
- `1.0.0` → `1.1.0` - Added new features
- `1.1.0` → `1.1.1` - Bug fixes only
- `1.1.1` → `2.0.0` - Breaking changes

## Support Channels

After release, monitor:
- GitHub Issues
- GitHub Discussions
- Email (if provided)

Respond to:
- Bug reports
- Feature requests
- Installation issues
- General questions
