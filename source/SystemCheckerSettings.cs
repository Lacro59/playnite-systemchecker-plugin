using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SystemChecker
{
    public class SystemCheckerSettings : ObservableObject
    {
        #region Settings variables
        public bool MenuInExtensions { get; set; } = true;
        public DateTime LastAutoLibUpdateAssetsDownload { get; set; } = DateTime.Now;

        public bool EnableTag { get; set; } = false;
        public bool AutoImport { get; set; } = true;

        private bool _EnableIntegrationViewItem = true;
        public bool EnableIntegrationViewItem { get => _EnableIntegrationViewItem; set => SetValue(ref _EnableIntegrationViewItem, value); }

        private bool _EnableIntegrationButton = true;
        public bool EnableIntegrationButton { get => _EnableIntegrationButton; set => SetValue(ref _EnableIntegrationButton, value); }

        private bool _EnableIntegrationButtonDetails = false;
        public bool EnableIntegrationButtonDetails { get => _EnableIntegrationButtonDetails; set => SetValue(ref _EnableIntegrationButtonDetails, value); }
        #endregion

        // Playnite serializes settings object to a JSON object and saves it as text file.
        // If you want to exclude some property from being saved then use `JsonDontSerialize` ignore attribute.
        #region Variables exposed
        private bool _HasData = false;
        [DontSerialize]
        public bool HasData { get => _HasData; set => SetValue(ref _HasData, value); }

        private bool _IsMinimumOK = false;
        [DontSerialize]
        public bool IsMinimumOK { get => _IsMinimumOK; set => SetValue(ref _IsMinimumOK, value); }

        private bool _IsRecommandedOK = false;
        [DontSerialize]
        public bool IsRecommandedOK { get => _IsRecommandedOK; set => SetValue(ref _IsRecommandedOK, value); }

        private bool _IsAllOK = false;
        [DontSerialize]
        public bool IsAllOK { get => _IsAllOK; set => SetValue(ref _IsAllOK, value); }

        private string _RecommandedStorage = string.Empty;
        [DontSerialize]
        public string RecommandedStorage { get => _RecommandedStorage; set => SetValue(ref _RecommandedStorage, value); }
        #endregion  
    }


    public class SystemCheckerSettingsViewModel : ObservableObject, ISettings
    {
        private readonly SystemChecker Plugin;
        private SystemCheckerSettings EditingClone { get; set; }

        private SystemCheckerSettings _Settings;
        public SystemCheckerSettings Settings { get => _Settings; set => SetValue(ref _Settings, value); }


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
