# Security Policy

## Supported Versions

We release patches for security vulnerabilities in the following versions:

| Version | Supported          |
| ------- | ------------------ |
| 1.1.x   | :white_check_mark: |
| 1.0.x   | :white_check_mark: |
| < 1.0   | :x:                |

## Reporting a Vulnerability

**Please do not report security vulnerabilities through public GitHub issues.**

Instead, please report them via one of the following methods:

### Option 1: GitHub Security Advisory (Preferred)

1. Go to the [Security tab](https://github.com/elis132/MultiAudioOutput/security)
2. Click "Report a vulnerability"
3. Fill in the details

### Option 2: Email

Send an email to: **elis132@protonmail.com**

Include the following information:

- Type of vulnerability
- Full paths of source file(s) related to the vulnerability
- Location of the affected source code (tag/branch/commit or direct URL)
- Step-by-step instructions to reproduce the issue
- Proof-of-concept or exploit code (if possible)
- Impact of the issue and how an attacker might exploit it

### What to Expect

- **Acknowledgment**: Within 48 hours
- **Initial Assessment**: Within 5 business days
- **Status Updates**: Every 7 days until resolution
- **Resolution Timeline**: Depends on severity
  - **Critical**: 7-14 days
  - **High**: 14-30 days
  - **Medium**: 30-60 days
  - **Low**: 60-90 days

## Security Best Practices

When using Multi Audio Output:

1. **Download Only from Official Sources**
   - GitHub Releases only
   - Verify checksums (SHA256SUMS.txt)

2. **Keep Software Updated**
   - Enable automatic updates (when available)
   - Check for updates regularly

3. **Review Permissions**
   - The app only requires audio device access
   - No network access required
   - No administrator rights needed

4. **Settings File**
   - Settings are stored in `%APPDATA%\MultiAudioOutput\settings.json`
   - Contains only device preferences
   - No sensitive data stored

## Known Security Considerations

### Audio Device Access

Multi Audio Output requires access to:
- WASAPI loopback capture (read audio from devices)
- WASAPI render (write audio to devices)

This is standard for audio routing applications and poses no security risk.

### Registry Access (Optional)

When "Start with Windows" is enabled:
- Writes to: `HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run`
- Only stores application path
- Removed on uninstall or when option disabled

### No Network Access

Multi Audio Output:
- Does NOT connect to the internet
- Does NOT collect telemetry
- Does NOT send any data
- Operates entirely locally

## Disclosure Policy

When a security vulnerability is reported and confirmed:

1. **Patch Development**: Develop and test fix
2. **Security Advisory**: Create GitHub Security Advisory
3. **Release**: Publish patched version
4. **Announcement**: Notify users via GitHub Release notes
5. **Credit**: Reporter credited in CHANGELOG (if desired)

## Security Hall of Fame

We thank the following individuals for responsibly disclosing security issues:

*No vulnerabilities reported yet*

---

**Last Updated**: January 29, 2026
