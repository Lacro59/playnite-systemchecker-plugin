# Project README Generator Prompt (Raw)

Role: You are an expert Technical Writer and GitHub Documentation Specialist.

Task: Create a professional, release-ready `README.md` for a **Lacro59 Playnite extension** that matches the visual hierarchy and documentation style of the reference README: [playnite-gameactivity-plugin/README.md](https://github.com/Lacro59/playnite-gameactivity-plugin/blob/master/README.md).

## 0. File Preamble (required)

The README must start with this HTML comment (before badges):

```markdown
<!-- markdownlint-disable MD033 MD041 -->
```

This allows badge-only top lines and HTML screenshot blocks without markdownlint noise.

## 1. Mandatory Output Structure

Your generated README must follow this section order:

1. File preamble (`<!-- markdownlint-disable MD033 MD041 -->`)
2. Badges block (no heading)
3. `# {PluginDisplayName} for Playnite`
4. One-sentence tagline/description (end with link to [Playnite](https://playnite.link) when natural)
5. `## ✨ Features`
6. `## 📸 Screenshots`
7. `## 🔍 Global Search` (always present; see §3)
8. `## 🔍 QuickSearch` (only if QuickSearch integration exists; see §3)
9. `## ⚙️ Configuration`
10. `## 📥 Installation`
11. `## 🤝 Contributing & Feedback`
12. `## 💝 Support`
13. `## 📄 License`

Do not add extra top-level sections unless explicitly requested.

## 2. Badge Requirements

Generate Shields.io badges in this order (omit Crowdin only if the project has no translation program):

1. Crowdin localization badge
2. Latest Release (`color=8A2BE2`)
3. Release Date
4. Total Downloads (all releases, not “latest only”)
5. Monthly Commit Activity (default branch: `devel` unless specified)
6. Contributors
7. License

Badge/link patterns (replace placeholders):

```markdown
[![Crowdin](https://badges.crowdin.net/<crowdin-project>/localized.svg)](https://crowdin.com/project/<crowdin-project>)
[![GitHub release](https://img.shields.io/github/v/release/<user>/<repo>?logo=github&color=8A2BE2)](https://github.com/<user>/<repo>/releases/latest)
[![GitHub Release Date](https://img.shields.io/github/release-date/<user>/<repo>?logo=github)](https://github.com/<user>/<repo>/releases/latest)
[![GitHub downloads](https://img.shields.io/github/downloads/<user>/<repo>/total?logo=github)](https://github.com/<user>/<repo>/releases)
[![GitHub commit activity](https://img.shields.io/github/commit-activity/m/<user>/<repo>/<activity-branch>?logo=github)](https://github.com/<user>/<repo>/graphs/commit-activity)
[![GitHub contributors](https://img.shields.io/github/contributors/<user>/<repo>?logo=github)](https://github.com/<user>/<repo>/graphs/contributors)
[![GitHub license](https://img.shields.io/github/license/<user>/<repo>?logo=github)](https://github.com/<user>/<repo>/blob/<default-branch>/LICENSE)
```

- `<activity-branch>`: branch for commit-activity badge (default `devel`).
- `<default-branch>`: repository default branch for LICENSE link (`main`, `master`, etc.).

## 3. Content Requirements by Section

### Title and tagline

- H1 format: `# {PluginDisplayName} for Playnite` (e.g. `# CheckDlc for Playnite`, `# GameActivity for Playnite`).
- Tagline: one sentence, product-oriented; prefer closing with “directly inside [Playnite](https://playnite.link)” when it fits.

### `## ✨ Features`

- Provide 4 to 7 bullets.
- Format: `- **Feature name**: concise benefit` (colon after bold name; description starts lowercase unless a proper noun).
- Use indented sub-bullets for store lists, integration controls, or nested capabilities.
- Mention supported stores/libraries explicitly when the plugin is store- or library-scoped.

### `## 📸 Screenshots`

- Group with `###` titles. Preferred set when images exist:
  - `### Main interface`
  - `### In-view controls` (optional)
  - `### Settings panel`
- Image paths: `https://github.com/<user>/<repo>/blob/<default-branch>/forum/<file>.jpg?raw=true`
- Every image wrapped as:

```html
<a href="FULL_IMAGE_URL">
  <picture>
    <img alt="Meaningful alternative text" src="FULL_IMAGE_URL" height="200px">
  </picture>
</a>
```

- Height: always `200px`.
- Descriptive `alt` text only (no `image1`, `main_01`, etc.).

### `## 🔍 Global Search` (always include)

**If Playnite Global Search filters are not implemented**, the entire section is only:

```markdown
## 🔍 Global Search

Not implemented
```

**If implemented**, document:

- How name search and filters combine.
- At least one example: ``keyword -flag``.
- A Markdown table of parameters.
- Notes on combinable filters and case sensitivity.

### `## 🔍 QuickSearch` (conditional)

Include **only** when the plugin registers QuickSearch commands (e.g. GameActivity).

- State the command key/prefix (e.g. `ga`).
- Provide 2–4 example queries.
- Parameter table (`| Parameter | Purpose | Syntax | Example |`).
- Notes: combinable or not, case rules, date format if applicable.

Place this section **immediately after** `## 🔍 Global Search`.

### `## ⚙️ Configuration`

- Opening line when relevant: `Open **Settings → Extensions → {PluginDisplayName}**.`
- Subsections with `###` (adapt to plugin), e.g.:
  - `### General behavior`
  - `### Store / library settings` (if applicable)
  - `### Hardware monitoring` / `### Integration` / etc.
- Bullets: short, user-facing; bold lead terms optional inside bullets.
- End with a blockquote tip when auto-detection exists but manual setup improves results:

```markdown
> Short note: automatic behavior works in many setups, but manual configuration often improves accuracy/stability.
```

### `## 📥 Installation`

Always two subsections:

#### `### Install from Playnite Add-ons Browser (recommended)`

```markdown
1. Open Playnite.
2. Go to **Add-ons → Browse → Generic**.
3. Search for `{AddOnSearchName}` and install it.
4. Restart Playnite if requested.

Official Playnite guide: [Installing Extensions](https://api.playnite.link/docs/manual/features/extensionsSupport/installingExtensions.html)
```

#### `### Manual installation (\`.pext\`)`

```markdown
1. Download the latest `.pext` file from [Releases](https://github.com/<user>/<repo>/releases/latest).
2. In Playnite, open **Add-ons → Install from file**.
3. Select the downloaded `.pext`.
4. Restart Playnite[, then optional post-install step].
```

### `## 🤝 Contributing & Feedback`

Use this bullet pattern (fix templates: bugs ≠ feature template):

```markdown
- **Bug reports**: [Open an issue](https://github.com/<user>/<repo>/issues/new?template=bug_report.md)
- **Feature requests**: [Request an enhancement](https://github.com/<user>/<repo>/issues/new?template=feature_request.md)
- **Pull requests**: [Submit a PR](https://github.com/<user>/<repo>/pulls) targeting the `devel` branch
- **Translations**: [Contribute on Crowdin](https://crowdin.com/project/<crowdin-project>) (omit line if N/A)
- **Wiki & troubleshooting**: [Project wiki](https://github.com/<user>/<repo>/wiki) (omit line if no wiki)
```

### `## 💝 Support`

```markdown
[![Ko-fi](https://img.shields.io/badge/Ko--fi-Support-FF5E5B?logo=ko-fi&logoColor=white)](https://ko-fi.com/<kofi-username>)

If this plugin helps you, you can also support:

- [Playnite](https://www.patreon.com/playnite)
- [<optional third-party>](https://...)
```

- Do not use the legacy `ko-fi.com/img/githubbutton` SVG unless explicitly requested.
- Optional third-party links: SteamDB, Freepik, etc.—only list real dependencies cited by the project.

### `## 📄 License`

```markdown
This project is licensed under the [<License Name> License](https://github.com/<user>/<repo>/blob/<default-branch>/LICENSE).
```

## 4. Project Metadata Inputs

Populate the README using:

| Input | Description |
| --- | --- |
| Plugin display name | H1 name (e.g. `CheckDlc`, `GameActivity`) |
| Add-on search name | String for Add-ons Browser step 3 |
| Tagline | One sentence |
| GitHub path | `Username/repository` |
| Default branch | For LICENSE, screenshots (`main` / `master`) |
| Activity badge branch | Usually `devel` |
| Key features | 4–7 items; include stores if relevant |
| Global Search | `not implemented` **or** filter docs |
| QuickSearch | `not applicable` **or** command key + parameters |
| Configuration areas | Subsection titles + bullet topics |
| Screenshots | Group title + path under `forum/` + alt text |
| Ko-fi username | e.g. `lacro59` |
| Crowdin project | e.g. `playnite-extensions` (optional) |
| License | e.g. `MIT` |
| Ecosystem support links | Playnite + optional (SteamDB, …) |

## 5. Writing Style Constraints

- Language: English.
- Tone: Professional, welcoming, developer-friendly.
- Feature bullets: `**Name**: description` (not em dashes `—`).
- Prefer short paragraphs and scan-friendly bullets.
- Use emojis in section titles exactly as listed when the section is included.
- Playnite menu paths: `**Add-ons → …**` with arrow separators.

## 6. Robustness Rules

- Always include `## 🔍 Global Search` (`Not implemented` or full docs).
- Include `## 🔍 QuickSearch` only when the plugin actually integrates QuickSearch.
- If an optional input is missing, omit the related badge or Contributing line—never invent URLs, screenshots, or features.
- Screenshot URLs must use the repo’s real default branch and existing `forum/` assets.
- Keep all links absolute (`https://...`).
- Ensure Markdown is valid and copy-paste ready.

## 7. Output Format (Strict)

Return only one fenced Markdown code block containing the full README content (including the markdownlint preamble).

No explanations before or after the code block.

## 8. Reference Repositories

Use as style anchors (do not copy unrelated feature text):

- [playnite-gameactivity-plugin](https://github.com/Lacro59/playnite-gameactivity-plugin/blob/master/README.md) — full template with QuickSearch
- [playnite-checkdlc-plugin](https://github.com/Lacro59/playnite-checkdlc-plugin/blob/main/README.md) — Global Search only (`Not implemented`), store list in Features

---

**Last Updated:** 2026-06-03  
**Version:** 1.2
