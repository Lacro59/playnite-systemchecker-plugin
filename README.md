[![Crowdin](https://badges.crowdin.net/playnite-extensions/localized.svg)](https://crowdin.com/project/playnite-extensions)
[![GitHub release (latest by date)](https://img.shields.io/github/v/release/Lacro59/playnite-systemchecker-plugin?cacheSeconds=5000&logo=github)](https://github.com/Lacro59/playnite-systemchecker-plugin/releases/latest)
[![GitHub Release Date](https://img.shields.io/github/release-date/Lacro59/playnite-systemchecker-plugin?cacheSeconds=5000)](https://github.com/Lacro59/playnite-systemchecker-plugin/releases/latest)
[![Github Lastest Releases](https://img.shields.io/github/downloads/Lacro59/playnite-systemchecker-plugin/latest/total.svg)]()
[![GitHub commit activity](https://img.shields.io/github/commit-activity/m/Lacro59/playnite-systemchecker-plugin)](https://github.com/Lacro59/playnite-systemchecker-plugin/graphs/commit-activity)
[![GitHub contributors](https://img.shields.io/github/contributors/Lacro59/playnite-systemchecker-plugin?cacheSeconds=5000)](https://github.com/Lacro59/playnite-systemchecker-plugin/graphs/contributors)
[![GitHub](https://img.shields.io/github/license/Lacro59/playnite-systemchecker-plugin?cacheSeconds=50000)](https://github.com/Lacro59/playnite-systemchecker-plugin/blob/master/LICENSE)

# System Checker for Playnite

A powerful Playnite extension that automatically checks game system requirements against your PC configuration, helping you determine if your system can run your games.

## 📋 Table of Contents

- [Features](#-features)
- [Screenshots](#-screenshots)
- [Installation](#-installation)
- [Usage](#-usage)
- [Configuration](#-configuration)
- [Theme Integration](#-theme-integration)
- [Contributing](#-contributing)
- [Translation](#-translation)
- [Support](#-support)
- [License](#-license)

## ✨ Features

- **Automatic Requirements Detection**: Retrieves game system requirements from [PCGamingWiki](https://www.pcgamingwiki.com/wiki/Home) and Steam
- **Real-time Compatibility Check**: Compares your PC configuration against game requirements
- **Visual Indicators**: See at a glance which games your system can run
- **Auto-tagging**: Automatically tag games based on compatibility
- **Theme Integration**: Seamlessly integrate system checker data into custom Playnite themes
  - Display compatibility in Details View
  - Display compatibility in Grid View
  - Show detailed requirements in game details page

## 📸 Screenshots

### Main Interface
<a href="https://github.com/Lacro59/playnite-systemchecker-plugin/blob/master/wiki/main_01.jpg?raw=true">
  <picture>
    <img alt="Main interface showing system compatibility" src="https://github.com/Lacro59/playnite-systemchecker-plugin/blob/master/wiki/main_01.jpg?raw=true" height="200px">
  </picture>
</a>

### Settings Panel
<a href="https://github.com/Lacro59/playnite-systemchecker-plugin/blob/master/wiki/settings_01.jpg?raw=true">
  <picture>
    <img alt="Plugin settings configuration" src="https://github.com/Lacro59/playnite-systemchecker-plugin/blob/master/wiki/settings_01.jpg?raw=true" height="200px">
  </picture>
</a>
<a href="https://github.com/Lacro59/playnite-systemchecker-plugin/blob/master/wiki/settings_02.jpg?raw=true">
  <picture>
    <img alt="Advanced settings options" src="https://github.com/Lacro59/playnite-systemchecker-plugin/blob/master/wiki/settings_02.jpg?raw=true" height="200px">
  </picture>
</a>

### Game Details Integration
<a href="https://github.com/Lacro59/playnite-systemchecker-plugin/blob/master/wiki/control_01.jpg?raw=true">
  <picture>
    <img alt="System requirements in game details" src="https://github.com/Lacro59/playnite-systemchecker-plugin/blob/master/wiki/control_01.jpg?raw=true" height="200px">
  </picture>
</a>

## 📥 Installation

### From Playnite Add-ons Browser (Recommended)

1. Open Playnite
2. Press `F9` or go to `Add-ons...` from the main menu
3. Go to `Browse` tab
4. Search for "System Checker"
5. Click `Install`

### Manual Installation

1. Download the latest `.pext` file from the [releases page](https://github.com/Lacro59/playnite-systemchecker-plugin/releases/latest)
2. Drag and drop the file into Playnite
3. Restart Playnite

## 🚀 Usage

### Initial Setup

1. After installation, open Playnite settings (`F4`)
2. Navigate to `Extensions` → `System Checker`
3. Configure your PC specifications (if not auto-detected)
4. Choose your preferred data sources (PCGamingWiki, Steam, or both)

### Checking Games

The plugin will automatically:
- Fetch system requirements when you add new games
- Compare requirements against your PC configuration
- Display compatibility status in supported views

### Manual Check

To manually check a specific game:
1. Right-click on a game in your library
2. Select `Extensions` → `System Checker` → 

## 🔍 Global Search

System Checker integrates with Playnite's global search feature (accessible via `Ctrl+F` or the search bar), allowing you to quickly find games based on system requirements compatibility.

**Important**: Search parameters are **filters that complement the name search**. When you use parameters, the plugin first filters by game name, then applies the additional filters.

For example:
- `rpg -min` → Finds games with "rpg" in their name **AND** that meet minimum requirements
- `-min` → Finds all games that meet minimum requirements (no name filter)

You can use the following parameters to filter games:

| Parameter | Description |
|-----------|-------------|
| `-min` | Games that meet minimum requirements |
| `-rec` | Games that meet recommended requirements |
| `-any` | Games that meet either minimum or recommended requirements |
| `-np` | Games with no playtime (never played) |
| `-fav` | Filter by favorite games only |
| `-stores=` | Filter by specific stores (comma-separated) |
| `-status=` | Filter by completion status (comma-separated) |

> **Note**: All search filters can be combined. The game name search is case-insensitive.

## ⚙️ Configuration

### General Settings

- **Enable Auto-Check**: Automatically check system requirements for new games
- **Auto-Tagging**: Automatically add tags based on compatibility

### PC Configuration

Configure your system specifications:
- CPU
- GPU
- RAM
- Storage
- Operating System

> **Note**: The plugin can auto-detect most specifications, but manual configuration ensures accuracy.

### Display Options

- **Show in Game List**: Display compatibility indicators in list views
- **Show in Game Details**: Display detailed requirements in game details page
- **Indicator Style**: Choose how compatibility is displayed

## 🎨 Theme Integration

The extension provides custom controls that theme developers can integrate into their themes.

For theme developers, detailed integration instructions are available in the [wiki](https://github.com/Lacro59/playnite-systemchecker-plugin/wiki/Addition-in-a-custom-theme).

## 🤝 Contributing

Contributions are welcome! Here's how you can help:

### Reporting Bugs

1. Check if the issue already exists in the [Issues](https://github.com/Lacro59/playnite-systemchecker-plugin/issues) section
2. If not, create a new issue with:
   - A clear description of the problem
   - Steps to reproduce
   - Expected vs actual behavior
   - Your Playnite and plugin version

### Feature Requests

Open an issue with the `enhancement` label and describe:
- The feature you'd like to see
- Why it would be useful
- Any implementation ideas you might have

### Pull Requests

> **Important**: The `master` branch represents the current released version. All contributions should be made to the `devel` branch.

1. Fork the repository
2. Create a feature branch from `devel` (`git checkout devel && git checkout -b feature/amazing-feature`)
3. Make your changes and commit them (`git commit -m 'Add amazing feature'`)
4. Push to your fork (`git push origin feature/amazing-feature`)
5. Open a Pull Request **targeting the `devel` branch**

#### Branch Structure
- `master` - Current released version (stable)
- `devel` - Development branch for upcoming releases (all PRs go here)

## 🌍 Translation

Help translate System Checker into your language! 

Translations are managed through [Crowdin](https://crowdin.com/project/playnite-extensions). Simply:
1. Visit the project page
2. Select your language
3. Start translating

Your contributions help make this plugin accessible to users worldwide!

## 💝 Support

### Getting Help

- **Documentation**: Check the [Wiki](https://github.com/Lacro59/playnite-systemchecker-plugin/wiki) for detailed guides
- **Issues**: Report bugs or request features on [GitHub Issues](https://github.com/Lacro59/playnite-systemchecker-plugin/issues)
- **Discussions**: Ask questions on the [Playnite Forum](https://playnite.link/wiki/)

### Support the Project

If you find this plugin useful, consider supporting:
- **Playnite**: [Patreon](https://www.patreon.com/playnite) - Support the main application
- **PCGamingWiki**: [Donate](https://www.pcgamingwiki.com/wiki/PCGamingWiki:Donate) - Support the data source
- **This Plugin**: <a href='https://ko-fi.com/lacro59'><img height='22' src='https://storage.ko-fi.com/cdn/brandasset/v2/support_me_on_kofi_dark.png' alt='Buy Me a Coffee at ko-fi.com' /></a>

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/Lacro59/playnite-systemchecker-plugin/blob/master/LICENSE) file for details.

---

**Acknowledgments**
- Icons by [Freepik](https://www.flaticon.com/authors/freepik)
- Game data from [PCGamingWiki](https://www.pcgamingwiki.com/wiki/Home) and Steam
- Built for [Playnite](https://playnite.link)
