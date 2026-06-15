# Stores Settings View — Usage Guide

## Purpose

`StoresSettingsView` is a reusable **master-detail** layout for the **Libraries** settings tab (`LOCLibraries`). It replaces the legacy vertical stack of store `PanelView` expanders with:

- a **navigation list** on the left (store icon, name, optional subtitle, colored auth status dot),
- a **detail panel** on the right (account card, connection section, and store configuration for the selected store).

Each plugin registers **only the stores it supports**. CheckDlc registers Steam, Epic, and GOG; SuccessStory can register four stores; another plugin may register only one.

---

## Location in `playnite-plugincommon`

| Component             | Path                                                                                                 |
| --------------------- | ---------------------------------------------------------------------------------------------------- |
| Master-detail control | `source/playnite-plugincommon/CommonPluginsControls/Stores/StoresSettingsView.xaml`                  |
| Code-behind           | `source/playnite-plugincommon/CommonPluginsControls/Stores/StoresSettingsView.xaml.cs`               |
| View model            | `source/playnite-plugincommon/CommonPluginsControls/Stores/StoresSettingsViewModel.cs`               |
| Store entry model     | `source/playnite-plugincommon/CommonPluginsControls/Stores/Models/StoreSettingsEntry.cs`             |
| Auth panel contract   | `source/playnite-plugincommon/CommonPluginsControls/Stores/IStorePanelViewModel.cs`                  |
| Connection section    | `source/playnite-plugincommon/CommonPluginsControls/Stores/StoreConnectionSection.xaml`              |
| Shared panel styles   | `source/playnite-plugincommon/CommonPluginsControls/Stores/StorePanelResources.xaml`                 |
| Settings logging      | `source/playnite-plugincommon/CommonPluginsControls/Stores/StoreSettingsLog.cs`                      |
| Embedded panel helper | `source/playnite-plugincommon/CommonPluginsControls/Stores/StorePanelAttachedProperties.cs`          |
| Per-store panels      | `source/playnite-plugincommon/CommonPluginsControls/Stores/{Steam,Epic,Gog,GameJolt}/PanelView.xaml` |

Shared expander helpers (`HideExpanderHeader`, `HideExpanderArrow`) live in:

`source/playnite-plugincommon/CommonPluginsShared/Controls/AttachedProperties.cs`

---

## When to Use

| Layout                                    | Use when                                                                             |
| ----------------------------------------- | ------------------------------------------------------------------------------------ |
| **`StoresSettingsView`**                  | New or modernized settings UI; multiple stores; master-detail navigation is desired. |
| **Legacy stack of `PanelView` expanders** | Existing plugins not yet migrated; still fully supported.                            |

Both layouts use the same `PanelView` controls and the same `StoreSettings` / `IStoreApi` wiring. Migration is limited to XAML layout and registration code; store logic does not change.

---

## Architecture Overview

```text
┌─────────────────────────────────────────────────────────────┐
│  StoresSettingsView                                         │
│  ┌──────────────┐   ┌─────────────────────────────────────┐ │
│  │ Navigation   │   │ Detail area (embedded PanelView)    │ │
│  │ (ListBox)    │   │  - Account card (avatar + pseudo)   │ │
│  │              │   │  - Connection (badge + Sign in/out) │ │
│  │  [icon] Steam│●  │  - Store-specific settings          │ │
│  │  [icon] Epic │●  │                                     │ │
│  │  [icon] GOG  │●  │                                     │ │
│  └──────────────┘   └─────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
         ● = auth status dot (green / amber / red)
```

1. The plugin declares `PanelView` instances in XAML (with bindings and `x:Name`).
2. After `InitializeComponent()`, the plugin calls `RegisterStore()` for each supported store.
3. `StoresSettingsView` moves each panel into its detail host and toggles visibility when the user selects a store in the sidebar.
4. `StorePanelAttachedProperties.NavigationEmbedded` hides the expander header/chrome so the panel fits the detail area.
5. `StoreSettingsEntry.BindAuthStatus()` wires the sidebar auth dot to each panel's `IStorePanelViewModel`.

---

## Integration Steps

### 1. Add project references (`.csproj`)

Include the new shared files in the plugin `.csproj` (same pattern as other `CommonPluginsControls` views):

```xml
<Compile Include="playnite-plugincommon\CommonPluginsControls\Stores\Models\StoreSettingsEntry.cs" />
<Compile Include="playnite-plugincommon\CommonPluginsControls\Stores\IStorePanelViewModel.cs" />
<Compile Include="playnite-plugincommon\CommonPluginsControls\Stores\StoreConnectionSection.xaml.cs">
  <DependentUpon>StoreConnectionSection.xaml</DependentUpon>
</Compile>
<Compile Include="playnite-plugincommon\CommonPluginsControls\Stores\StorePanelAttachedProperties.cs" />
<Compile Include="playnite-plugincommon\CommonPluginsControls\Stores\StoreSettingsLog.cs" />
<Compile Include="playnite-plugincommon\CommonPluginsControls\Stores\StoresSettingsView.xaml.cs">
  <DependentUpon>StoresSettingsView.xaml</DependentUpon>
</Compile>
<Compile Include="playnite-plugincommon\CommonPluginsControls\Stores\StoresSettingsViewModel.cs" />

<Page Include="playnite-plugincommon\CommonPluginsControls\Stores\StoreConnectionSection.xaml">
  <Generator>MSBuild:Compile</Generator>
  <SubType>Designer</SubType>
</Page>
<Page Include="playnite-plugincommon\CommonPluginsControls\Stores\StorePanelResources.xaml">
  <Generator>MSBuild:Compile</Generator>
  <SubType>Designer</SubType>
</Page>
<Page Include="playnite-plugincommon\CommonPluginsControls\Stores\StoresSettingsView.xaml">
  <Generator>MSBuild:Compile</Generator>
  <SubType>Designer</SubType>
</Page>
```

Also include the `PanelView` files for each store the plugin uses (Steam, Epic, GOG, GameJolt, etc.).

### 2. XAML — Libraries tab

Add the `stores` namespace and replace the legacy `StackPanel` with `StoresSettingsView`.

Keep `PanelView` instances in a **collapsed host** so bindings are resolved at load time; panels are reparented into `StoresSettingsView` at runtime.

```xml
xmlns:stores="clr-namespace:CommonPluginsControls.Stores"
xmlns:Steam="clr-namespace:CommonPluginsControls.Stores.Steam"
xmlns:Epic="clr-namespace:CommonPluginsControls.Stores.Epic"
xmlns:Gog="clr-namespace:CommonPluginsControls.Stores.Gog"

<!-- Inside the settings TabControl -->
<TabItem Header="{DynamicResource LOCLibraries}">
    <stores:StoresSettingsView x:Name="StoresSettings" />
</TabItem>

<!-- Outside TabControl, same root Grid -->
<StackPanel x:Name="StorePanelsHost" Visibility="Collapsed">
    <Steam:PanelView x:Name="SteamPanel" ForceAuth="True"
                     UseApi="{Binding DataContext.Settings.SteamStoreSettings.UseApi, Mode=TwoWay, RelativeSource={RelativeSource FindAncestor, AncestorType={x:Type UserControl}}}"
                     UseAuth="{Binding DataContext.Settings.SteamStoreSettings.UseAuth, Mode=TwoWay, RelativeSource={RelativeSource FindAncestor, AncestorType={x:Type UserControl}}}" />

    <Epic:PanelView x:Name="EpicPanel" ForceAuth="True"
                    UseAuth="{Binding DataContext.Settings.EpicStoreSettings.UseAuth, Mode=TwoWay, RelativeSource={RelativeSource FindAncestor, AncestorType={x:Type UserControl}}}" />

    <Gog:PanelView x:Name="GogPanel" ForceAuth="True"
                   UseAuth="{Binding DataContext.Settings.GogStoreSettings.UseAuth, Mode=TwoWay, RelativeSource={RelativeSource FindAncestor, AncestorType={x:Type UserControl}}}" />
</StackPanel>
```

**Important:** Do not place `PanelView` controls both in the collapsed host and inside `StoresSettingsView` in XAML. Registration reparents them automatically.

### 3. Code-behind — wire APIs and register stores

```csharp
using CommonPluginsControls.Stores;
using CommonPluginsControls.Stores.Models;

public MyPluginSettingsView()
{
    InitializeComponent();

    SteamPanel.StoreApi = MyPlugin.SteamApi;
    EpicPanel.StoreApi = MyPlugin.EpicApi;
    GogPanel.StoreApi = MyPlugin.GogApi;

    RegisterStorePanels();
}

private void RegisterStorePanels()
{
    StoresSettings.RegisterStore(new StoreSettingsEntry
    {
        Id = "Steam",
        NameResourceKey = "LOCCommonStoreSteam",
        CategoryResourceKey = "LOCCommonStoresLaunchers",
        Panel = SteamPanel,
        IsVisible = PluginDatabase.PluginSettings.PluginState.SteamIsEnabled,
        SortOrder = 0
    });

    StoresSettings.RegisterStore(new StoreSettingsEntry
    {
        Id = "Epic",
        NameResourceKey = "LOCCommonStoreEpic",
        CategoryResourceKey = "LOCCommonStoresLaunchers",
        Panel = EpicPanel,
        IsVisible = PluginDatabase.PluginSettings.PluginState.EpicIsEnabled,
        SortOrder = 1
    });

    StoresSettings.RegisterStore(new StoreSettingsEntry
    {
        Id = "Gog",
        NameResourceKey = "LOCCommonStoreGog",
        CategoryResourceKey = "LOCCommonStoresLaunchers",
        Panel = GogPanel,
        IsVisible = PluginDatabase.PluginSettings.PluginState.GogIsEnabled,
        SortOrder = 2
    });
}
```

Reference implementation: `source/Views/CheckDlcSettingsView.xaml` and `source/Views/CheckDlcSettingsView.xaml.cs`.

### 4. Settings model

Each store still uses `StoreSettings` on the plugin settings class:

```csharp
public StoreSettings SteamStoreSettings { get; set; } = new StoreSettings { ForceAuth = true, UseAuth = true, UseApi = false };
public StoreSettings EpicStoreSettings { get; set; } = new StoreSettings { ForceAuth = true, UseAuth = true };
public StoreSettings GogStoreSettings { get; set; } = new StoreSettings { ForceAuth = true, UseAuth = true };
```

Apply settings on save in `PluginSettingsViewModel.EndEdit()` as before (assign `StoreSettings` to each `IStoreApi` instance).

---

## `StoreSettingsEntry` Properties

| Property              | Type               | Description                                                                                                                                  |
| --------------------- | ------------------ | -------------------------------------------------------------------------------------------------------------------------------------------- |
| `Id`                  | `string`           | Unique identifier (for example, `"Steam"`). Fallback display name if `NameResourceKey` is empty. Also used as default icon key (lowercased). |
| `NameResourceKey`     | `string`           | Localization key for the store name in the sidebar and detail header.                                                                        |
| `CategoryResourceKey` | `string`           | Optional localization key for grouping (reserved for future grouped navigation).                                                             |
| `SubtitleResourceKey` | `string`           | Optional subtitle under the store name (for example, `"Uses Exophase"`).                                                                     |
| `IconName`            | `string`           | Optional icon key for `TransformIcon.Get()`. When empty, `Id` (lowercased) is used.                                                          |
| `Panel`               | `FrameworkElement` | The store `PanelView` instance. Required.                                                                                                    |
| `IsVisible`           | `bool`             | When `false`, the store is hidden from navigation.                                                                                           |
| `SortOrder`           | `int`              | Sort order within a category (lower values first).                                                                                           |

Read-only computed properties (for bindings inside the control):

- `DisplayName` — resolved from `NameResourceKey`
- `CategoryName` — resolved from `CategoryResourceKey`
- `Subtitle` — resolved from `SubtitleResourceKey`
- `StoreIcon` / `HasStoreIcon` — glyph from `TransformIcon`
- `AuthStatus` / `ShowAuthStatusIndicator` — from the panel's `IStorePanelViewModel`

Call `BindAuthStatus(Panel)` is done automatically by `RegisterStore()`.

---

## Categories and Multiple Store Groups

Use different `CategoryResourceKey` values to split stores into sidebar sections:

| Key                             | Default English text |
| ------------------------------- | -------------------- |
| `LOCCommonStoresLaunchers`      | Stores & Launchers   |
| `LOCCommonStoresConsolesMobile` | Console & Mobile     |

Example for a console platform with an Exophase subtitle (add the subtitle key to Common `LocSource.xaml` first):

```csharp
StoresSettings.RegisterStore(new StoreSettingsEntry
{
    Id = "PlayStation",
    NameResourceKey = "LOCCommonStorePlayStation",
    CategoryResourceKey = "LOCCommonStoresConsolesMobile",
    SubtitleResourceKey = "LOCCommonUsesExophase",
    Panel = PlayStationPanel,
    IsVisible = settings.PluginState.PsnIsEnabled,
    SortOrder = 0
});
```

---

## Filtering Stores by Playnite Library Plugin

Use `PluginSettings.PluginState` to show only stores whose Playnite library integration is installed and enabled:

| Property             | Store                          |
| -------------------- | ------------------------------ |
| `SteamIsEnabled`     | Steam                          |
| `EpicIsEnabled`      | Epic Games (Epic or Legendary) |
| `GogIsEnabled`       | GOG (GOG or GOG OSS)           |
| `OriginIsEnabled`    | EA / Origin                    |
| `XboxIsEnabled`      | Xbox                           |
| `PsnIsEnabled`       | PlayStation                    |
| `NintendoIsEnabled`  | Nintendo                       |
| `BattleNetIsEnabled` | Battle.net                     |
| `GameJoltIsEnabled`  | Game Jolt                      |

If library availability changes while the settings view is open, update `IsVisible` on the relevant entries and call:

```csharp
StoresSettings.RefreshStores();
```

---

## Available Store Panels

| Store     | Control              | Namespace                               |
| --------- | -------------------- | --------------------------------------- |
| Steam     | `Steam:PanelView`    | `CommonPluginsControls.Stores.Steam`    |
| Epic      | `Epic:PanelView`     | `CommonPluginsControls.Stores.Epic`     |
| GOG       | `Gog:PanelView`      | `CommonPluginsControls.Stores.Gog`      |
| Game Jolt | `GameJolt:PanelView` | `CommonPluginsControls.Stores.GameJolt` |

### Common `PanelView` dependency properties

| Property    | Description                                                                   |
| ----------- | ----------------------------------------------------------------------------- |
| `StoreApi`  | Set in code-behind to the plugin's `IStoreApi` instance.                      |
| `ForceAuth` | When `true`, forces authentication mode (hides manual configuration toggles). |
| `UseAuth`   | Two-way binding to `StoreSettings.UseAuth`.                                   |
| `UseApi`    | Two-way binding to `StoreSettings.UseApi` (Steam only).                       |

Each store `PanelViewModel` implements `IStorePanelViewModel` and exposes:

| Member                                 | Description                                                      |
| -------------------------------------- | ---------------------------------------------------------------- |
| `AuthStatus`                           | Current auth state (`Ok`, `AuthRequired`, `Checking`, …).        |
| `ShowConnectionSection`                | When `true`, shows `StoreConnectionSection` (Sign in / Log out). |
| `CanLogin`                             | Sign in enabled when not connected and not checking.             |
| `CanLogout`                            | Log out enabled only when connected (`AuthStatus.Ok`).           |
| `LoginCommand` / `ClearSessionCommand` | Wired to `StoreConnectionSection` buttons.                       |
| `RefreshAuthCommandStates()`           | Re-evaluates auth bindings (called on view load).                |

`StoreConnectionSection` displays a colored status badge and action buttons. **Log out** calls `IStoreApi.ClearSession()` (cookies, token, avatar/pseudo cleared; UserId and ApiKey kept).

---

## Localization

### Shared keys (Common `LocSource.xaml`)

Already defined in `source/playnite-plugincommon/CommonPluginsResources/Localization/Common/LocSource.xaml`:

| Key                                | Purpose                               |
| ---------------------------------- | ------------------------------------- |
| `LOCCommonStoresLaunchers`         | Sidebar category — stores & launchers |
| `LOCCommonStoresConsolesMobile`    | Sidebar category — console & mobile   |
| `LOCCommonStoreSteam`              | Store name — Steam                    |
| `LOCCommonStoreEpic`               | Store name — Epic Games               |
| `LOCCommonStoreGog`                | Store name — GOG                      |
| `LOCCommonStoresNoStoreSelected`   | Empty state when no store is selected |
| `LOCCommonConnection`              | Connection section header             |
| `LOCCommonAccountSection`          | Account section header                |
| `LOCCommonAuthenticateLabel`       | Sign in button                        |
| `LOCCommonClearSession`            | Log out button                        |
| `LOCCommonClearSessionDescription` | Log out tooltip                       |
| `LOCCommonLoggedIn`                | Connected badge                       |
| `LOCCommonNotLoggedIn`             | Sign in required badge                |

Add new store names or category labels to **Common** `LocSource.xaml` when they are reusable across plugins. Add plugin-specific strings to `source/Localization`.

Follow the rules in `.ai/Playnite UI Modernization, Localization & Common Styling.md`.

---

## API Reference

### `StoresSettingsView.RegisterStore(StoreSettingsEntry entry)`

Registers a store panel:

1. Sets `NavigationEmbedded` on the panel (hides expander chrome).
2. Detaches the panel from its XAML host (if any) and adds it to the detail host.
3. Adds the entry to the navigation collection.
4. Selects the first visible store if none is selected.

Returns silently if `entry` or `entry.Panel` is null.

### `StoresSettingsView.RefreshStores()`

Refreshes the filtered navigation list, panel visibility, and auth command states after `IsVisible` changes or external session updates. Also called on `Loaded`.

### Debugging

Filter Playnite logs with `[StoreSettings]` or `ClearSession` to trace registration, auth binding, and log out.

### `StoresSettingsView.Stores`

Read-only access to the underlying `ObservableCollection<StoreSettingsEntry>` (advanced scenarios).

---

## Backward Compatibility

Plugins that still use the legacy layout are unaffected:

```xml
<StackPanel Margin="10">
    <Steam:PanelView x:Name="SteamPanel" Margin="0,0,0,10" ... />
    <Epic:PanelView x:Name="EpicPanel" Margin="0,0,0,10" ... />
</StackPanel>
```

Do **not** set `NavigationEmbedded` on panels used in the legacy layout. It is applied automatically by `RegisterStore()` for the master-detail layout only.

---

## Styling Dependencies

`StoresSettingsView` and store panels use shared styles from `CommonPluginsResources`:

- `SectionBorder`, `SectionHeaderText`, `CardBorder`, `BaseTextBlockStyle` — from `Resources/Common.xaml` and `ResourcesPlaynite/Common.xaml`
- `StorePanelResources.xaml` — auth badge, sidebar status dot, tooltip text styles
- Theme brushes and corner radius — via `{DynamicResource ...}` from `ResourcesPlaynite/Constants.xaml`

Ensure the plugin loads shared resources the same way as other modernized views (see `Common.cs` and SystemChecker / CheckDlc settings views).

---

## Known Limitations and Future Work

- **Grouped navigation:** `CategoryResourceKey` is available on entries but the sidebar currently shows a flat list. Group headers can be added later.
- **Panel chrome:** `PanelView` content still uses the internal expander structure; only the header is hidden in embedded mode.
- **Dynamic registration:** Stores are expected to be registered once at view construction. Re-registering the same panel instance is not supported.
- **WPF reparenting:** Panels must exist in XAML (collapsed host) or be created before `RegisterStore()` so bindings and `StoreApi` assignment work correctly.

---

## Checklist for New Plugin Integration

- [ ] Add `StoresSettingsView` and related files to `.csproj`
- [ ] Add required `PanelView` files for supported stores
- [ ] Add `StoreSettings` properties to plugin settings model
- [ ] Replace Libraries tab content with `StoresSettingsView`
- [ ] Declare `PanelView` instances in collapsed `StorePanelsHost`
- [ ] Assign `StoreApi` in code-behind constructor
- [ ] Call `RegisterStore()` for each supported store with correct `IsVisible` / `SortOrder`
- [ ] Wire `EndEdit()` to push `StoreSettings` back to API instances
- [ ] Add missing localization keys to Common or plugin `LocSource.xaml`
- [ ] Verify settings save/load, Sign in, and Log out flows in Playnite runtime (filter logs: `[StoreSettings]`)

---

**Last Updated:** 2026-05-29  
**Version:** 1.1
