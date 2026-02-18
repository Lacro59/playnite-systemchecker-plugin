using CommonPluginsShared.Plugins;
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
        #region Settings variables

        private bool _enableIntegrationViewItem = true;
        public bool EnableIntegrationViewItem
        {
            get => _enableIntegrationViewItem;
            set => SetValue(ref _enableIntegrationViewItem, value);
        }

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

        #region Variables exposed (not serialized)

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


    public class SystemCheckerSettingsViewModel : ObservableObject, ISettings
    {
        private static readonly ILogger Logger = LogManager.GetLogger();

        private readonly SystemChecker Plugin;
        private SystemCheckerSettings EditingClone { get; set; }

        private SystemCheckerSettings _settings;
        public SystemCheckerSettings Settings
        {
            get => _settings;
            set => SetValue(ref _settings, value);
        }

        #region Commands

        /// <summary>
        /// Adds a tag to all games in the library based on their system check result.
        /// Replaces the former ButtonAddTag_Click code-behind handler.
        /// </summary>
        public RelayCommand CmdAddTag { get; private set; }

        /// <summary>
        /// Removes the system checker tag from all games in the library.
        /// Replaces the former ButtonRemoveTag_Click code-behind handler.
        /// </summary>
        public RelayCommand CmdRemoveTag { get; private set; }

        /// <summary>
        /// Clears all plugin data from the database.
        /// Replaces the former Button_Click code-behind handler.
        /// </summary>
        public RelayCommand CmdClearAll { get; private set; }

        #endregion

        public SystemCheckerSettingsViewModel(SystemChecker plugin)
        {
            // Injecting the plugin instance is required for Save/Load because Playnite
            // saves data to a location determined by the requesting plugin.
            Plugin = plugin;

            // Load previously saved settings, or use defaults if none exist.
            SystemCheckerSettings savedSettings = plugin.LoadPluginSettings<SystemCheckerSettings>();
            Settings = savedSettings ?? new SystemCheckerSettings();

            InitializeCommands();
        }

        /// <summary>
        /// Initializes all RelayCommands for the settings view.
        /// Keeping command wiring in a dedicated method avoids constructor bloat.
        /// </summary>
        private void InitializeCommands()
        {
            // Add tag command: delegates to the plugin database helper.
            // Wrapped in try/catch to surface errors via Playnite notifications
            // rather than crashing the settings dialog.
            CmdAddTag = new RelayCommand(() =>
            {
                try
                {
                    SystemChecker.PluginDatabase.AddTagAllGames();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "CmdAddTag failed");
                    API.Instance.Notifications.Add(
                        "SystemChecker_AddTag_Error",
                        ResourceProvider.GetString("LOCSystemCheckerAddTagError"),
                        NotificationType.Error);
                }
            });

            // Remove tag command: delegates to the plugin database helper.
            CmdRemoveTag = new RelayCommand(() =>
            {
                try
                {
                    SystemChecker.PluginDatabase.RemoveTagAllGames();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "CmdRemoveTag failed");
                    API.Instance.Notifications.Add(
                        "SystemChecker_RemoveTag_Error",
                        ResourceProvider.GetString("LOCSystemCheckerRemoveTagError"),
                        NotificationType.Error);
                }
            });

            // Clear all data command: asks for confirmation before wiping data.
            // Uses Playnite's built-in dialog API to stay consistent with the host UI.
            CmdClearAll = new RelayCommand(() =>
            {
                try
                {
                    MessageBoxResult result = API.Instance.Dialogs.ShowMessage(
                        ResourceProvider.GetString("LOCSystemCheckerClearAllConfirm"),
                        ResourceProvider.GetString("LOCSystemChecker"),
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (result == MessageBoxResult.Yes)
                    {
                        SystemChecker.PluginDatabase.ClearDatabase();
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "CmdClearAll failed");
                    API.Instance.Notifications.Add(
                        "SystemChecker_ClearAll_Error",
                        ResourceProvider.GetString("LOCSystemCheckerClearAllError"),
                        NotificationType.Error);
                }
            });
        }

        // Called when the settings dialog is opened and the user begins editing.
        public void BeginEdit()
        {
            EditingClone = Serialization.GetClone(Settings);
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
            SystemChecker.PluginDatabase.PluginSettings = this;
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