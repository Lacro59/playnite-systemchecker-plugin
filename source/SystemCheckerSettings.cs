using CommonPluginsShared;
using CommonPluginsShared.Interfaces;
using CommonPluginsShared.Plugins;
using CommonPluginsShared.SystemInfo;
using CommonPluginsShared.Commands;
using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using SystemChecker.Services;

namespace SystemChecker
{
    public class SystemCheckerSettings : PluginSettings
    {
        /// <summary>
        /// Normalized source names excluded from SystemChecker operations (see <see cref="PlayniteTools.GetSourceName"/>).
        /// Fixed plugin policy — not exposed in the settings UI.
        /// </summary>
        private static readonly IReadOnlyList<string> FixedExcludedSources = new List<string>
        {
            "Android",
            "Playstation",
            "Nintendo",
            "EmuLibrary",
            "RetroAchievements",
            "Rpcs3"
        };

        public SystemCheckerSettings()
        {
            ApplyFixedLibraryFilterPolicy();
        }

        /// <summary>
        /// Applies fixed library filter values for this plugin (not user-configurable).
        /// </summary>
        public void ApplyFixedLibraryFilterPolicy()
        {
            IncludeEmulatedGames = false;
            LibrarySourceFilterMode = SourceFilterMode.Blacklist;
            EnabledSources = new List<string>();
            ExcludedSources = new List<string>(FixedExcludedSources);
        }

        #region PluginViewItem

        private bool _enableIntegrationViewItem = true;
        public bool EnableIntegrationViewItem
        {
            get => _enableIntegrationViewItem;
            set => SetValue(ref _enableIntegrationViewItem, value);
        }

		#endregion

		#region PluginButton

		private bool _enableIntegrationButton = true;
        public bool EnableIntegrationButton
        {
            get => _enableIntegrationButton;
            set => SetValue(ref _enableIntegrationButton, value);
        }

        private bool _enableIntegrationButtonDetails = false;
        public bool EnableIntegrationButtonDetails
        {
            get => _enableIntegrationButtonDetails;
            set => SetValue(ref _enableIntegrationButtonDetails, value);
        }

        #endregion

        #region Manual system configuration

        private string _manualCpu = string.Empty;

        /// <summary>
        /// Gets or sets a user-defined CPU name used for requirement comparisons when non-empty.
        /// When empty, the WMI-detected CPU from <see cref="Services.SystemCheckerDatabase.PC"/> is used.
        /// </summary>
        public string ManualCpu
        {
            get => _manualCpu;
            set => SetValue(ref _manualCpu, value ?? string.Empty);
        }

        private string _manualGpu = string.Empty;

        /// <summary>
        /// Gets or sets a user-defined GPU name used for requirement comparisons when non-empty.
        /// When empty, the WMI-detected GPU from <see cref="Services.SystemCheckerDatabase.PC"/> is used.
        /// </summary>
        public string ManualGpu
        {
            get => _manualGpu;
            set => SetValue(ref _manualGpu, value ?? string.Empty);
        }

        private bool _retagOnManualConfigChange = true;

        /// <summary>
        /// When <c>true</c> and <see cref="PluginSettings.EnableTag"/> is enabled, compatibility tags are
        /// refreshed for all library games after manual CPU or GPU overrides are saved.
        /// </summary>
        public bool RetagOnManualConfigChange
        {
            get => _retagOnManualConfigChange;
            set => SetValue(ref _retagOnManualConfigChange, value);
        }

        #endregion

        #region Theme variables

        private bool _isMinimumOK = false;
        [DontSerialize]
        public bool IsMinimumOK { get => _isMinimumOK; set => SetValue(ref _isMinimumOK, value); }

        private bool _isRecommendedOK = false;
        [DontSerialize]
        public bool IsRecommendedOK { get => _isRecommendedOK; set => SetValue(ref _isRecommendedOK, value); }

        private bool _isAllOK = false;
        [DontSerialize]
        public bool IsAllOK { get => _isAllOK; set => SetValue(ref _isAllOK, value); }

        private string _recommendedStorage = string.Empty;
        [DontSerialize]
        public string RecommendedStorage { get => _recommendedStorage; set => SetValue(ref _recommendedStorage, value); }

        #endregion
    }


    public class SystemCheckerSettingsViewModel : PluginSettingsViewModel, IPluginSettingsViewModel
	{
        private readonly SystemChecker Plugin;
        private SystemCheckerSettings EditingClone { get; set; }

		IPluginSettings IPluginSettingsViewModel.Settings => Settings;

		private SystemCheckerSettings _settings;
        public SystemCheckerSettings Settings
        {
            get => _settings;
            set => SetValue(ref _settings, value);
        }

        public SystemCheckerSettingsViewModel(SystemChecker plugin)
		{
            Plugin = plugin;

            SystemCheckerSettings savedSettings = plugin.LoadPluginSettings<SystemCheckerSettings>();
            Settings = savedSettings ?? new SystemCheckerSettings();
            Settings.ApplyFixedLibraryFilterPolicy();
        }

        #region Detected hardware (read-only, WMI)

        private string _detectedOs = string.Empty;

        /// <summary>Gets the WMI-detected operating system name shown in settings (read-only).</summary>
        public string DetectedOs
        {
            get => _detectedOs;
            private set => SetValue(ref _detectedOs, value);
        }

        private string _detectedCpu = string.Empty;

        /// <summary>Gets the WMI-detected CPU name shown in settings (read-only).</summary>
        public string DetectedCpu
        {
            get => _detectedCpu;
            private set => SetValue(ref _detectedCpu, value);
        }

        private string _detectedGpu = string.Empty;

        /// <summary>Gets the WMI-detected GPU name shown in settings (read-only).</summary>
        public string DetectedGpu
        {
            get => _detectedGpu;
            private set => SetValue(ref _detectedGpu, value);
        }

        private string _detectedRam = string.Empty;

        /// <summary>Gets the WMI-detected RAM summary shown in settings (read-only).</summary>
        public string DetectedRam
        {
            get => _detectedRam;
            private set => SetValue(ref _detectedRam, value);
        }

        /// <summary>Loads WMI-detected values from the plugin database for display in the settings UI.</summary>
        private void LoadDetectedConfiguration()
        {
            SystemConfiguration pc = SystemChecker.PluginDatabase?.PC;
            if (pc == null)
            {
                DetectedOs = string.Empty;
                DetectedCpu = string.Empty;
                DetectedGpu = string.Empty;
                DetectedRam = string.Empty;
                return;
            }

            DetectedOs = pc.Os ?? string.Empty;
            DetectedCpu = pc.Cpu ?? string.Empty;
            DetectedGpu = pc.GpuName ?? string.Empty;
            DetectedRam = pc.RamUsage ?? string.Empty;
        }

        #endregion

        #region Benchmark picker commands

        /// <summary>Opens the PassMark CPU benchmark picker for manual configuration.</summary>
        public RelayCommand CmdPickCpu { get; private set; }

        /// <summary>Opens the PassMark GPU benchmark picker for manual configuration.</summary>
        public RelayCommand CmdPickGpu { get; private set; }

        private void InitializeBenchmarkPickerCommands()
        {
            if (CmdPickCpu == null)
            {
                CmdPickCpu = new RelayCommand(() => PickBenchmark(isGpu: false));
                CmdPickGpu = new RelayCommand(() => PickBenchmark(isGpu: true));
            }
        }

        private void PickBenchmark(bool isGpu)
        {
            string detected = isGpu ? DetectedGpu : DetectedCpu;
            string current = isGpu ? Settings.ManualGpu : Settings.ManualCpu;
            string picked = BenchmarkPickerDialog.Pick(isGpu, detected, current);

            if (picked == null)
            {
                return;
            }

            if (isGpu)
            {
                Settings.ManualGpu = picked;
            }
            else
            {
                Settings.ManualCpu = picked;
            }
        }

        #endregion

        public void BeginEdit()
        {
            EditingClone = Serialization.GetClone(Settings);
            InitializeCommands(SystemChecker.PluginName, SystemChecker.PluginDatabase);
            InitializeBenchmarkPickerCommands();
            LoadDetectedConfiguration();
        }

        public void CancelEdit()
        {
            Settings = EditingClone;
		}

        public void EndEdit()
        {
            Settings.ApplyFixedLibraryFilterPolicy();

            bool manualConfigChanged = HasManualConfigurationChanged(EditingClone, Settings);

            Plugin.SavePluginSettings(Settings);
            SystemChecker.PluginDatabase.PluginSettings = Settings;

            if (manualConfigChanged)
            {
                SystemApi.InvalidateCpuAndGpuCache();

                if (Settings.EnableTag && Settings.RetagOnManualConfigChange)
                {
                    SystemChecker.PluginDatabase.AddTagAllGames();
                }
            }

            this.OnPropertyChanged();
        }

        /// <summary>
        /// Returns <c>true</c> when trimmed manual CPU or GPU overrides differ between two settings snapshots.
        /// </summary>
        private static bool HasManualConfigurationChanged(SystemCheckerSettings before, SystemCheckerSettings after)
        {
            if (before == null || after == null)
            {
                return false;
            }

            return !string.Equals(NormalizeManualOverride(before.ManualCpu), NormalizeManualOverride(after.ManualCpu), StringComparison.Ordinal)
                || !string.Equals(NormalizeManualOverride(before.ManualGpu), NormalizeManualOverride(after.ManualGpu), StringComparison.Ordinal);
        }

        private static string NormalizeManualOverride(string value)
        {
            return (value ?? string.Empty).Trim();
        }

        public bool VerifySettings(out List<string> errors)
        {
            errors = new List<string>();
            return true;
        }
    }
}
