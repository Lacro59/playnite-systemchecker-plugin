using CommonPluginsShared.Plugins;
using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SystemChecker
{
    public class SystemCheckerSettings : PluginSettings
    {
        #region Settings variables
        private bool _enableIntegrationViewItem = true;
        public bool EnableIntegrationViewItem { get => _enableIntegrationViewItem; set => SetValue(ref _enableIntegrationViewItem, value); }

        private bool _enableIntegrationButton = true;
        public bool EnableIntegrationButton { get => _enableIntegrationButton; set => SetValue(ref _enableIntegrationButton, value); }

        private bool _enableIntegrationButtonDetails = false;
        public bool EnableIntegrationButtonDetails { get => _enableIntegrationButtonDetails; set => SetValue(ref _enableIntegrationButtonDetails, value); }
        #endregion

        // Playnite serializes settings object to a JSON object and saves it as text file.
        // If you want to exclude some property from being saved then use `JsonDontSerialize` ignore attribute.
        #region Variables exposed
        private bool _isMinimumOK = false;
        [DontSerialize]
        public bool IsMinimumOK { get => _isMinimumOK; set => SetValue(ref _isMinimumOK, value); }

        private bool _isRecommandedOK = false;
        [DontSerialize]
        public bool IsRecommandedOK { get => _isRecommandedOK; set => SetValue(ref _isRecommandedOK, value); }

        private bool _isAllOK = false;
        [DontSerialize]
        public bool IsAllOK { get => _isAllOK; set => SetValue(ref _isAllOK, value); }

        private string _recommandedStorage = string.Empty;
        [DontSerialize]
        public string RecommandedStorage { get => _recommandedStorage; set => SetValue(ref _recommandedStorage, value); }
        #endregion  


        // TODO TMP
        public bool IsPurged { get; set; } = false;
    }


    public class SystemCheckerSettingsViewModel : ObservableObject, ISettings
    {
        private readonly SystemChecker Plugin;
        private SystemCheckerSettings EditingClone { get; set; }

        private SystemCheckerSettings _settings;
        public SystemCheckerSettings Settings { get => _settings; set => SetValue(ref _settings, value); }


        public SystemCheckerSettingsViewModel(SystemChecker plugin)
        {
            // Injecting your plugin instance is required for Save/Load method because Playnite saves data to a location based on what plugin requested the operation.
            Plugin = plugin;

            // Load saved settings.
            SystemCheckerSettings savedSettings = plugin.LoadPluginSettings<SystemCheckerSettings>();

            // LoadPluginSettings returns null if not saved data is available.
            Settings = savedSettings ?? new SystemCheckerSettings();
        }

        // Code executed when settings view is opened and user starts editing values.
        public void BeginEdit()
        {
            EditingClone = Serialization.GetClone(Settings);
        }

        // Code executed when user decides to cancel any changes made since BeginEdit was called.
        // This method should revert any changes made to Option1 and Option2.
        public void CancelEdit()
        {
            Settings = EditingClone;
        }

        // Code executed when user decides to confirm changes made since BeginEdit was called.
        // This method should save settings made to Option1 and Option2.
        public void EndEdit()
        {
            Plugin.SavePluginSettings(Settings);
            SystemChecker.PluginDatabase.PluginSettings = this;
            this.OnPropertyChanged();
        }

        // Code execute when user decides to confirm changes made since BeginEdit was called.
        // Executed before EndEdit is called and EndEdit is not called if false is returned.
        // List of errors is presented to user if verification fails.
        public bool VerifySettings(out List<string> errors)
        {
            errors = new List<string>();
            return true;
        }
    }
}
