# Installation Guide

This guide covers all installation methods for Multi Audio Output.

## Table of Contents

- [System Requirements](#system-requirements)
- [Installer Installation](#installer-installation)
- [Portable Installation](#portable-installation)
- [Silent Installation](#silent-installation)
- [Uninstallation](#uninstallation)
- [Settings Location](#settings-location)

---

## System Requirements

### Minimum Requirements

- **Operating System**: Windows 10 (64-bit) or later
- **Framework**: .NET 8.0 Runtime (Desktop)
- **Processor**: Any 64-bit processor
- **RAM**: 50 MB
- **Disk Space**: 10 MB
- **Audio**: WASAPI-compatible audio devices

### Recommended

- **Operating System**: Windows 11 (64-bit)
- **RAM**: 100 MB free
- **Audio**: Multiple WASAPI audio output devices

---

## Installer Installation

### Download

1. Go to [Releases](https://github.com/elis132/MultiAudioOutput/releases/latest)
2. Download `MultiAudioOutput-Setup-v{version}-x64.exe`

### Installation Steps

1. **Run the Installer**
   - Double-click the downloaded `.exe` file
   - Windows SmartScreen may appear - click "More info" then "Run anyway"

2. **Select Language**
   - Choose your preferred language from the dropdown
   - Click "OK"

3. **Welcome Screen**
   - Read the welcome message
   - Click "Next"

4. **License Agreement**
   - Read the End User License Agreement
   - Check "I accept the agreement"
   - Click "Next"

5. **Select Destination**
   - Default: `C:\Program Files\Multi Audio Output`
   - Click "Browse" to change location
   - Click "Next"

6. **Select Components** (Optional)
   - All components are selected by default
   - Click "Next"

7. **Select Additional Tasks**
   - ☐ **Create a desktop icon** - Creates shortcut on desktop
   - ☐ **Create a Quick Launch icon** - Creates shortcut in Quick Launch
   - ☐ **Start with Windows** - Automatically starts when Windows boots
   - Click "Next"

8. **Ready to Install**
   - Review your choices
   - Click "Install"

9. **Installation Complete**
   - ☐ **Launch Multi Audio Output** - Start the app immediately
   - Click "Finish"

### First Launch

On first launch:
1. The app will appear in the system tray
2. Click the tray icon to open the main window
3. Select your audio devices and configuration
4. Click "Start Audio" to begin streaming

---

## Portable Installation

### Download

1. Go to [Releases](https://github.com/elis132/MultiAudioOutput/releases/latest)
2. Download `MultiAudioOutput-Portable-v{version}-x64.zip`

### Installation Steps

1. **Extract Files**
   - Extract the ZIP to any folder
   - Example: `C:\Tools\MultiAudioOutput\`

2. **Run Application**
   - Double-click `MultiAudioOutput.exe`
   - No installation required

### Portable Mode Features

- **No Registry Changes** - Doesn't write to Windows Registry (except when "Start with Windows" is enabled)
- **Portable Settings** - Settings saved in same folder
- **USB Drive** - Can run from external drives
- **No Admin Required** - Runs with user privileges

### Making it Truly Portable

By default, settings are saved to `%APPDATA%`. To make settings portable:

1. Create a file named `portable.txt` next to `MultiAudioOutput.exe`
2. Settings will now be saved in `.\settings\` folder

---

## Silent Installation

For IT deployments, system administrators, or automated installations.

### Basic Silent Install

```powershell
MultiAudioOutput-Setup-v1.1.0-x64.exe /VERYSILENT /SUPPRESSMSGBOXES /NORESTART
```

### With Desktop Icon

```powershell
MultiAudioOutput-Setup-v1.1.0-x64.exe /VERYSILENT /SUPPRESSMSGBOXES /NORESTART /TASKS="desktopicon"
```

### With Start with Windows

```powershell
MultiAudioOutput-Setup-v1.1.0-x64.exe /VERYSILENT /SUPPRESSMSGBOXES /NORESTART /TASKS="desktopicon,startupicon"
```

### Custom Install Location

```powershell
MultiAudioOutput-Setup-v1.1.0-x64.exe /VERYSILENT /SUPPRESSMSGBOXES /NORESTART /DIR="C:\MyApps\MultiAudioOutput"
```

### All Options

| Parameter | Description |
|-----------|-------------|
| `/VERYSILENT` | No UI, completely silent |
| `/SILENT` | UI with progress only |
| `/SUPPRESSMSGBOXES` | Suppress message boxes |
| `/NORESTART` | Don't restart computer |
| `/DIR="path"` | Custom installation directory |
| `/TASKS="task1,task2"` | Select tasks (comma-separated) |
| `/LOG="path"` | Create installation log |

### Available Tasks

- `desktopicon` - Create desktop shortcut
- `quicklaunchicon` - Create Quick Launch shortcut
- `startupicon` - Start with Windows

### Silent Uninstall

```powershell
"C:\Program Files\Multi Audio Output\uninstall\unins000.exe" /VERYSILENT /SUPPRESSMSGBOXES /NORESTART
```

---

## Uninstallation

### Using Windows Settings

1. Open **Settings** > **Apps** > **Installed apps**
2. Search for "Multi Audio Output"
3. Click the three dots (...) > **Uninstall**
4. Confirm uninstallation

### Using Uninstaller

1. Go to installation folder (default: `C:\Program Files\Multi Audio Output`)
2. Navigate to `uninstall` folder
3. Run `unins000.exe`
4. Follow the uninstallation wizard

### What Gets Removed

- Application files
- Desktop shortcut (if created)
- Start menu shortcuts
- "Start with Windows" registry entry

### What Gets Kept

By default, your settings are preserved. During uninstall, you'll be asked:

> "Do you want to remove your settings and preferences?"

- **Yes** - Removes settings from `%APPDATA%\MultiAudioOutput`
- **No** - Keeps settings for future reinstallation

---

## Settings Location

### Installer Version

Settings are stored in:
```
C:\Users\{YourUsername}\AppData\Roaming\MultiAudioOutput\settings.json
```

### Portable Version

#### Default Mode
```
C:\Users\{YourUsername}\AppData\Roaming\MultiAudioOutput\settings.json
```

#### Portable Mode (with portable.txt)
```
{ApplicationFolder}\settings\settings.json
```

### What's Stored

- Selected audio devices
- Device channel modes
- Custom device names
- Language preference
- Window position
- Startup preferences

### Settings File Format

```json
{
  "Language": "en",
  "StartWithWindows": false,
  "AutoStart": false,
  "SourceDeviceId": "{device-id}",
  "Devices": [
    {
      "DeviceId": "{device-id}",
      "CustomName": "My Speakers",
      "ChannelMode": 0,
      "IsSelected": true
    }
  ]
}
```

---

## Troubleshooting

### Installer Won't Run

- **Windows SmartScreen**: Click "More info" > "Run anyway"
- **Antivirus**: Add exception for the installer
- **Permissions**: Run as administrator

### Application Won't Start

1. **Check .NET Runtime**:
   ```powershell
   dotnet --list-runtimes
   ```
   Should show `Microsoft.WindowsDesktop.App 8.0.x`

2. **Install .NET 8.0 Runtime**:
   - Download from: https://dotnet.microsoft.com/download/dotnet/8.0/runtime
   - Choose "Desktop Runtime" for Windows

### Settings Not Saving

- Check folder permissions for `%APPDATA%\MultiAudioOutput`
- For portable mode, ensure `portable.txt` exists
- Run as administrator if needed

### Start with Windows Not Working

- Check registry entry:
  ```
  HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run\MultiAudioOutput
  ```
- Reinstall and select "Start with Windows" option

---

## Getting Help

- **GitHub Issues**: https://github.com/elis132/MultiAudioOutput/issues
- **Documentation**: https://github.com/elis132/MultiAudioOutput/tree/main/docs
- **Discussions**: https://github.com/elis132/MultiAudioOutput/discussions
