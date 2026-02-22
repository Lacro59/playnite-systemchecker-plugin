[![Crowdin](https://badges.crowdin.net/playnite-extensions/localized.svg)](https://crowdin.com/project/playnite-extensions)
[![GitHub release](https://img.shields.io/github/v/release/Lacro59/playnite-systemchecker-plugin?logo=github&color=8A2BE2)](https://github.com/Lacro59/playnite-systemchecker-plugin/releases/latest)
[![GitHub Release Date](https://img.shields.io/github/release-date/Lacro59/playnite-systemchecker-plugin?logo=github)](https://github.com/Lacro59/playnite-systemchecker-plugin/releases/latest)
[![GitHub downloads](https://img.shields.io/github/downloads/Lacro59/playnite-systemchecker-plugin/total?logo=github)](https://github.com/Lacro59/playnite-systemchecker-plugin/releases)
[![GitHub commit activity](https://img.shields.io/github/commit-activity/m/Lacro59/playnite-systemchecker-plugin/devel?logo=github)](https://github.com/Lacro59/playnite-systemchecker-plugin/graphs/commit-activity)
[![GitHub contributors](https://img.shields.io/github/contributors/Lacro59/playnite-systemchecker-plugin?logo=github)](https://github.com/Lacro59/playnite-systemchecker-plugin/graphs/contributors)
[![GitHub license](https://img.shields.io/github/license/Lacro59/playnite-systemchecker-plugin?logo=github)](https://github.com/Lacro59/playnite-systemchecker-plugin/blob/master/LICENSE)

# System Checker for Playnite

A Playnite extension that automatically checks game system requirements against your PC configuration.

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

## 📥 Installation

### From Playnite Add-ons Browser (Recommended)

Please refer to the [Official Playnite Add-on Installation Guide](https://api.playnite.link/docs/manual/features/extensionsSupport/installingExtensions.html).

### Manual Installation

1. Download the latest `.pext` file from the [releases page](https://github.com/Lacro59/playnite-systemchecker-plugin/releases/latest)
2. Drag and drop the file into Playnite
3. Restart Playnite

## 🤝 Contributing & Feedback
Contributions are welcome! Please follow the templates provided below:

* 🐛 **[Reporting Bugs](https://github.com/Lacro59/playnite-systemchecker-plugin/issues/new?template=bug_report.md)**: Check existing issues first.
* ✨ **[Feature Requests](https://github.com/Lacro59/playnite-systemchecker-plugin/issues/new?template=feature_request.md)**: Suggest new ideas.
* 💻 **[Pull Requests](https://github.com/Lacro59/playnite-systemchecker-plugin/pulls)**: Submit PRs to the `devel` branch only.
* 🌍 **[Translation](https://crowdin.com/project/playnite-extensions)**: Help us localize the plugin via Crowdin.

## 💝 Support
If you find this plugin useful, feel free to support its development:

[![Support me on Ko-fi](https://img.shields.io/badge/Support%20me-Ko--fi-F16061?style=for-the-badge&logo=ko-fi&logoColor=white)](https://ko-fi.com/lacro59)

*You can also support the ecosystem:*
- **Playnite**: [Patreon](https://www.patreon.com/playnite).
- **PCGamingWiki**: [Donate](https://www.pcgamingwiki.com/wiki/PCGamingWiki:Donate).

## 📄 License
This project is licensed under the MIT License.