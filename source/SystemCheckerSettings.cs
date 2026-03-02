using CommonPluginsShared.Interfaces;
using CommonPluginsShared.Plugins;
using CommonPluginsShared.UI;
using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace SystemChecker
{
    public class SystemCheckerSettings : PluginSettings
    {
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
            // Injecting the plugin instance is required for Save/Load because Playnite
            // saves data to a location determined by the requesting plugin.
            Plugin = plugin;

            // Load previously saved settings, or use defaults if none exist.
            SystemCheckerSettings savedSettings = plugin.LoadPluginSettings<SystemCheckerSettings>();
            Settings = savedSettings ?? new SystemCheckerSettings();
        }

        // Called when the settings dialog is opened and the user begins editing.
        public void BeginEdit()
        {
            EditingClone = Serialization.GetClone(Settings);
            InitializeCommands(SystemChecker.PluginName, SystemChecker.PluginDatabase);
        }

        // Called when the user cancels: restores the pre-edit snapshot.
        public void CancelEdit()
        {
            Settings = EditingClone;
		}

        // Called when the user confirms: persists the new values.
        public void EndEdit()
        {
            Plugin.SavePluginSettings(Settings);
            SystemChecker.PluginDatabase.PluginSettings = Settings;
            this.OnPropertyChanged();
        }

        // Called before EndEdit; returning false with errors prevents saving.
        public bool VerifySettings(out List<string> errors)
        {
            errors = new List<string>();
            return true;
        }
    }
}