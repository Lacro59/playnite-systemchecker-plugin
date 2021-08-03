using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SystemChecker
{
    public class SystemCheckerSettings : ObservableObject
    {
        #region Settings variables
        public bool MenuInExtensions { get; set; } = true;

        public bool EnableTag { get; set; } = false;


        private bool _EnableIntegrationViewItem { get; set; } = false;
        public bool EnableIntegrationViewItem
        {
            get => _EnableIntegrationViewItem;
            set
            {
                _EnableIntegrationViewItem = value;
                OnPropertyChanged();
            }
        }

        private bool _EnableIntegrationButton { get; set; } = false;
        public bool EnableIntegrationButton
        {
            get => _EnableIntegrationButton;
            set
            {
                _EnableIntegrationButton = value;
                OnPropertyChanged();
            }
        }

        private bool _EnableIntegrationButtonDetails { get; set; } = false;
        public bool EnableIntegrationButtonDetails
        {
            get => _EnableIntegrationButtonDetails;
            set
            {
                _EnableIntegrationButtonDetails = value;
                OnPropertyChanged();
            }
        }
        #endregion

        // Playnite serializes settings object to a JSON object and saves it as text file.
        // If you want to exclude some property from being saved then use `JsonDontSerialize` ignore attribute.
        #region Variables exposed
        private bool _HasData { get; set; } = false;
        [DontSerialize]
        public bool HasData
        {
            get => _HasData;
            set
            {
                _HasData = value;
                OnPropertyChanged();
            }
        }

        private bool _IsMinimumOK { get; set; } = false;
        [DontSerialize]
        public bool IsMinimumOK
        {
            get => _IsMinimumOK;
            set
            {
                _IsMinimumOK = value;
                OnPropertyChanged();
            }
        }

        private bool _IsRecommandedOK { get; set; } = false;
        [DontSerialize]
        public bool IsRecommandedOK
        {
            get => _IsRecommandedOK;
            set
            {
                _IsRecommandedOK = value;
                OnPropertyChanged();
            }
        }

        private bool _IsAllOK { get; set; } = false;
        [DontSerialize]
        public bool IsAllOK
        {
            get => _IsAllOK;
            set
            {
                _IsAllOK = value;
                OnPropertyChanged();
            }
        }
        #endregion  
    }


    public class SystemCheckerSettingsViewModel : ObservableObject, ISettings
    {
        private readonly SystemChecker Plugin;
        private SystemCheckerSettings EditingClone { get; set; }

        private SystemCheckerSettings _Settings;
        public SystemCheckerSettings Settings
        {
            get => _Settings;
            set
            {
                _Settings = value;
                OnPropertyChanged();
            }
        }


        public SystemCheckerSettingsViewModel(SystemChecker plugin)
        {
            // Injecting your plugin instance is required for Save/Load method because Playnite saves data to a location based on what plugin requested the operation.
            Plugin = plugin;

            // Load saved settings.
            var savedSettings = plugin.LoadPluginSettings<SystemCheckerSettings>();

            // LoadPluginSettings returns null if not saved data is available.
            if (savedSettings != null)
            {
                Settings = savedSettings;
            }
            else
            {
                Settings = new SystemCheckerSettings();
            }
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