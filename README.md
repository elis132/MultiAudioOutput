# Multi Audio Output

<div align="center">

![Multi Audio Output](Resources/icon.ico)

**Stream your audio to multiple devices simultaneously with full surround sound support**

[![Build Status](https://github.com/elis132/MultiAudioOutput/workflows/CI/badge.svg)](https://github.com/elis132/MultiAudioOutput/actions)
[![Release](https://img.shields.io/github/v/release/elis132/MultiAudioOutput)](https://github.com/elis132/MultiAudioOutput/releases/latest)
[![License](https://img.shields.io/github/license/elis132/MultiAudioOutput)](LICENSE)
[![Downloads](https://img.shields.io/github/downloads/elis132/MultiAudioOutput/total)](https://github.com/elis132/MultiAudioOutput/releases)

[Download](#-download) • [Features](#-features) • [Documentation](#-documentation) • [Contributing](#-contributing)

</div>

---

## 🎯 What Problem Does This Solve?

Windows only allows audio output to **one device at a time**. Want to hear your game through headphones **AND** speakers? Want your music on your desktop speakers **AND** Bluetooth speaker? Multi Audio Output makes this possible.

Unlike other solutions that require virtual audio cables or complex routing, Multi Audio Output provides a **clean, simple interface** to duplicate your system audio to any combination of output devices with **zero latency** and **full surround sound support**.

---

## ✨ Features

### Core Functionality
- 🔊 **Multi-Device Output** - Stream to unlimited audio devices simultaneously
- 🎵 **Zero Latency** - Direct WASAPI loopback for real-time audio
- 🎭 **Channel Mapping** - Full control over each device's audio channels
  - Stereo, Left, Right, Center
  - Front Left/Right, Back Left/Right
  - Subwoofer (LFE)
  - Full surround sound support (5.1, 7.1)
- 🎚️ **Per-Device Control** - Enable/disable devices on the fly
- 🏷️ **Custom Device Names** - Rename devices for easy identification

### User Experience
- 🌙 **Modern Dark UI** - Premium dark theme with green accents
- 🌍 **Multilingual** - 23 languages supported
  - English, Swedish, Norwegian, Danish, Finnish
  - German, French, Spanish, Portuguese, Italian
  - Dutch, Polish, Russian, Ukrainian, Serbian
  - Turkish, Arabic, Hindi, Bengali, Urdu
  - Chinese, Japanese, Korean
- 💾 **Persistent Settings** - Remembers your configuration
- 🚀 **System Tray** - Minimize to tray, start with Windows
- ⚡ **Lightweight** - Minimal CPU and memory usage

---

## 📥 Download

### Windows Installer (Recommended)

Download the latest `MultiAudioOutput-Setup-x64.exe` from [Releases](https://github.com/elis132/MultiAudioOutput/releases/latest)

**Features:**
- One-click installation
- Desktop shortcut option
- Start menu integration
- Start with Windows option
- Automatic uninstaller

### Portable Version

Download `MultiAudioOutput-Portable-x64.zip` from [Releases](https://github.com/elis132/MultiAudioOutput/releases/latest)

**Features:**
- No installation required
- Run from USB drive
- Portable settings

---

## 🚀 Installation

### Using the Installer

1. Download `MultiAudioOutput-Setup-x64.exe`
2. Run the installer
3. Select your language
4. Accept the license agreement
5. Choose installation options:
   - ✅ Desktop shortcut
   - ✅ Start menu shortcut
   - ✅ Start with Windows (optional)
6. Click Install

### Portable Installation

1. Download `MultiAudioOutput-Portable-x64.zip`
2. Extract to any folder
3. Run `MultiAudioOutput.exe`

### Silent Installation

For IT deployments:

```powershell
MultiAudioOutput-Setup-x64.exe /VERYSILENT /SUPPRESSMSGBOXES /NORESTART /TASKS="desktopicon,startupicon"
```

See [docs/installer.md](docs/installer.md) for all installation options.

---

## 💻 System Requirements

- **OS**: Windows 10 (64-bit) or later
- **Framework**: .NET 8.0 Runtime (included in installer)
- **Audio**: WASAPI-compatible audio devices (all modern devices)
- **RAM**: 50 MB
- **Disk**: 10 MB

---

## 🎮 Usage

### Quick Start

1. **Select Source Device** - Choose which audio device to capture from
2. **Enable Output Devices** - Check the devices you want to play audio on
3. **Configure Channels** - Select channel mode for each device
4. **Press Start** - Audio now plays on all selected devices

### Channel Modes Explained

| Mode | Description |
|------|-------------|
| **Stereo** | Full stereo output (default) |
| **Left** | Left channel only |
| **Right** | Right channel only |
| **Center** | Mix of both channels (mono) |
| **Front Left/Right** | Specific surround channels |
| **Back Left/Right** | Rear surround channels |
| **Subwoofer (LFE)** | Low frequency channel only |

### Start with Windows

Enable in settings or during installation to automatically start audio duplication when Windows boots.

---

## 🛠️ Build from Source

### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Inno Setup 6.x](https://jrsoftware.org/isdl.php) (for installer)
- Windows 10 SDK (included with Visual Studio)

### Build Steps

```powershell
# Clone the repository
git clone https://github.com/elis132/MultiAudioOutput.git
cd MultiAudioOutput

# Restore dependencies
dotnet restore

# Build (Debug)
dotnet build

# Build (Release)
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o publish

# Build installer
.\scripts\build-installer.ps1
```

### Development

```powershell
# Run in development mode
dotnet run

# Build and run
.\build.ps1
```

---

## 📚 Documentation

- [Installation Guide](docs/installer.md) - Detailed installation instructions
- [Contributing Guide](CONTRIBUTING.md) - How to contribute
- [Changelog](CHANGELOG.md) - Version history
- [Security Policy](SECURITY.md) - Security and vulnerability reporting

---

## 🤝 Contributing

We welcome contributions! Please see our [Contributing Guide](CONTRIBUTING.md) for details.

### Ways to Contribute

- 🐛 Report bugs
- 💡 Suggest features
- 🌍 Add translations
- 📝 Improve documentation
- 💻 Submit pull requests

---

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

## 🙏 Acknowledgments

- **NAudio** - Audio library for .NET
- **Inno Setup** - Installer creation tool
- **Community** - All contributors and users

---

## 💬 Support

- **Issues**: [GitHub Issues](https://github.com/elis132/MultiAudioOutput/issues)
- **Discussions**: [GitHub Discussions](https://github.com/elis132/MultiAudioOutput/discussions)

---

## 🌟 Star History

If you find this project useful, please consider giving it a star! ⭐

---

<div align="center">

**Made with ❤️ by [elis132](https://github.com/elis132)**

[Report Bug](https://github.com/elis132/MultiAudioOutput/issues/new?assignees=&labels=bug&template=bug_report.md) • [Request Feature](https://github.com/elis132/MultiAudioOutput/issues/new?assignees=&labels=enhancement&template=feature_request.md)

</div>
