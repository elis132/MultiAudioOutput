# Changelog

All notable changes to Multi Audio Output will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.3.0] - 2026-07-03

### Added
- 🔉 Per-device volume control - individual slider on every output card, adjustable live while audio is running, persisted per device
- 🪟 Resizable window - drag any edge, double-click the title bar to maximize
- 📝 Error logging to `%AppData%\MultiAudioOutput\app.log` for easier troubleshooting

### Changed
- 🌍 Language changes now apply instantly - no restart needed
- ⚡ Removed a hidden timer that repainted the whole window 20x per second while running
- 📦 Replaced Newtonsoft.Json with the built-in System.Text.Json (one less dependency)

### Fixed
- 🔊 Channel modes (Center/Mono, Back Left/Right, Surround, Subwoofer) now actually work - they previously did nothing on the 32-bit float audio that Windows loopback capture delivers
- 🎚️ Subwoofer bass boost no longer distorts on loud passages (samples are clamped)
- 🧵 Fixed a race between the audio thread and stopping playback
- 🚫 Launching the app twice no longer creates duplicate tray icons - the second instance shows a notice and exits
- 🏷️ Tray menu and error dialogs showed raw translation key names (e.g. "ShowWindow") - now properly localized in all 23 languages

## [1.2.0] - 2026-01-31

### Changed
- ⚡ Improved audio latency
- 📦 Installer now supports in-place upgrades

## [1.1.0] - 2026-01-29

### Added
- 🌍 Added 6 new languages: Ukrainian, Italian, Dutch, Polish, Turkish, Korean (total: 23 languages)
- 🎨 Custom high-performance dropdown controls for better UI responsiveness
- 📱 All audio devices now shown in source dropdown (both input and output)

### Changed
- ✨ Improved UI theme with premium dark design and green accents
- ⚡ Optimized dropdown rendering for smooth hover effects (no lag)
- 🖱️ Enhanced device card hover behavior - now works on all child elements
- 🎯 Better click handling for dropdowns with toggle behavior

### Fixed
- 🐛 Fixed dropdown performance issues with large device lists
- 🐛 Fixed rounded corner rendering artifacts on device cards
- 🐛 Fixed dropdown arrow flickering when starting audio
- 🐛 Fixed hover state propagation from child controls
- 🐛 Removed unwanted tooltips from UI elements
- 🪟 Fixed window close button to minimize to tray instead of closing
- 🎨 Fixed tray icon to use custom icon instead of generic system icon

### Technical
- Replaced owner-drawn ComboBox with custom ToolStripDropDown implementation
- Added debounce mechanism for dropdown toggle behavior
- Improved Paint event handling with proper double buffering
- Enhanced background rendering with rounded corner support

## [1.0.0] - 2026-01-29

### Added
- ✨ Initial release
- 🔊 Multi-device audio output support
- 🎛️ 10 channel modes (Stereo, Left, Right, Center, Front L/R, Back L/R, Surround, Subwoofer)
- 🌍 17 language support (English, Swedish, German, Spanish, French, Portuguese, Russian, Japanese, Chinese, Korean, Norwegian, Danish, Finnish, Serbian, Arabic, Hindi, Bengali, Urdu)
- 💾 Settings persistence with JSON storage
- ✏️ Device renaming with right-click menu
- 🚀 Auto-start with Windows option
- 📌 System tray integration
- 🎨 Modern dark UI with custom title bar
- ⚡ Real-time audio routing with NAudio WASAPI
- 🔄 Device refresh functionality
- ⚙️ Settings dialog (language, startup options)

### Technical
- Built with .NET 8.0 Windows Forms
- NAudio 2.2.1 for WASAPI audio handling
- Newtonsoft.Json for settings serialization
- Inno Setup installer support
- Single-file publish configuration

---

## Future Plans

### Planned Features
- 🎚️ Individual volume control per device
- 🎼 Audio equalizer
- 📊 Audio level visualization
- 🔌 Plugin system
- 🌈 Custom themes
- 📱 Remote control via mobile app

### Under Consideration
- Virtual audio cable creation
- Audio effects (reverb, delay, etc.)
- Profile system for different setups
- Hotkey support

---

[1.1.0]: https://github.com/elis132/MultiAudioOutput/releases/tag/v1.1.0
[1.0.0]: https://github.com/elis132/MultiAudioOutput/releases/tag/v1.0.0
