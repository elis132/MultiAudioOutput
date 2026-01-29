# Contributing to Multi Audio Output

Thank you for your interest in contributing to Multi Audio Output! 🎉

## How to Contribute

### Reporting Bugs

If you find a bug, please create an issue with:
- Clear description of the problem
- Steps to reproduce
- Expected vs actual behavior
- System information (Windows version, audio devices)
- Error messages or screenshots if applicable

### Suggesting Features

Feature requests are welcome! Please:
- Check existing issues first to avoid duplicates
- Describe the feature and its use case
- Explain why it would be useful to users

### Pull Requests

1. **Fork the repository**
   ```bash
   git clone https://github.com/elis132/MultiAudioOutput.git
   cd MultiAudioOutput
   ```

2. **Create a feature branch**
   ```bash
   git checkout -b feature/your-feature-name
   ```

3. **Make your changes**
   - Follow existing code style
   - Test your changes thoroughly
   - Update documentation if needed

4. **Commit your changes**
   ```bash
   git add .
   git commit -m "Add: brief description of changes"
   ```

5. **Push and create PR**
   ```bash
   git push origin feature/your-feature-name
   ```
   Then open a Pull Request on GitHub

## Code Style Guidelines

- Use C# naming conventions (PascalCase for classes/methods, camelCase for variables)
- Add comments for complex logic
- Keep methods focused and concise
- Follow existing patterns in the codebase

## Adding New Languages

To add a new language to `Localization.cs`:

1. Add the language code to the dictionary
2. Translate all strings in the translations dictionary
3. Test the UI with your translation
4. Update README.md with the new language

## Development Setup

### Prerequisites
- .NET 8.0 SDK
- Windows 10/11
- Visual Studio 2022 or VS Code (optional)

### Building
```bash
dotnet restore
dotnet build
dotnet run
```

### Testing
Test with different audio devices and configurations:
- Multiple output devices
- Different channel modes
- Surround sound setups
- Auto-start functionality
- Language switching

## Questions?

Feel free to open an issue for any questions or discussions!

---

Thank you for contributing! 💙
