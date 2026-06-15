using CommonPluginsShared;
using CommonPluginsShared.Interfaces;
using CommonPluginsShared.Plugins;
using CommonPluginsShared.UI;
using Playnite.SDK;
using Playnite.SDK.Data;
using System.Collections.Generic;

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

        public void BeginEdit()
        {
            EditingClone = Serialization.GetClone(Settings);
            InitializeCommands(SystemChecker.PluginName, SystemChecker.PluginDatabase);
        }

        public void CancelEdit()
        {
            Settings = EditingClone;
		}

        public void EndEdit()
        {
            Settings.ApplyFixedLibraryFilterPolicy();
            Plugin.SavePluginSettings(Settings);
            SystemChecker.PluginDatabase.PluginSettings = Settings;
            this.OnPropertyChanged();
        }

        public bool VerifySettings(out List<string> errors)
        {
            errors = new List<string>();
            return true;
        }
    }
}
