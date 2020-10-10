using Newtonsoft.Json;
using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using PluginCommon;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SystemChecker.Clients;
using SystemChecker.Services;
using SystemChecker.Views;

namespace SystemChecker
{
    public class SystemChecker : Plugin
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private static IResourceProvider resources = new ResourceProvider();

        private SystemCheckerSettings settings { get; set; }

        public override Guid Id { get; } = Guid.Parse("e248b230-6edf-41ea-a3c3-7861fa267263");

        public static Game GameSelected { get; set; }
        //private readonly IntegrationUI ui = new IntegrationUI();
        public static SystemCheckerUI systemCheckerUI;
        //private readonly TaskHelper taskHelper = new TaskHelper();

        private CheckSystem CheckMinimum { get; set; }
        private CheckSystem CheckRecommanded { get; set; }

        public SystemChecker(IPlayniteAPI api) : base(api)
        {
            settings = new SystemCheckerSettings(this);


            // Get plugin's location 
            string pluginFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            // Add plugin localization in application ressource.
            PluginCommon.Localization.SetPluginLanguage(pluginFolder, api.ApplicationSettings.Language);
            // Add common in application ressource.
            PluginCommon.Common.Load(pluginFolder);

            // Check version
            if (settings.EnableCheckVersion)
            {
                CheckVersion cv = new CheckVersion();

                if (cv.Check("SystemChecker", pluginFolder))
                {
                    cv.ShowNotification(api, "SystemChecker - " + resources.GetString("LOCUpdaterWindowTitle"));
                }
            }

            // Init ui interagration
            systemCheckerUI = new SystemCheckerUI(PlayniteApi, settings, this.GetPluginUserDataPath());
        }

        public override IEnumerable<ExtensionFunction> GetFunctions()
        {
            return new List<ExtensionFunction>
            {
                new ExtensionFunction(
                    "SystemChecker",
                    () =>
                    {
                        var ViewExtension = new SystemCheckerGameView(this.GetPluginUserDataPath(), SystemChecker.GameSelected, PlayniteApi);
                        Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(PlayniteApi, "SystemChecker", ViewExtension);
                        windowExtension.ShowDialog();
                    })
            };
        }

        public override void OnGameInstalled(Game game)
        {
            // Add code to be executed when game is finished installing.
        }

        public override void OnGameStarted(Game game)
        {
            // Add code to be executed when game is started running.
        }

        public override void OnGameStarting(Game game)
        {
            // Add code to be executed when game is preparing to be started.
        }

        public override void OnGameStopped(Game game, long elapsedSeconds)
        {
            // Add code to be executed when game is preparing to be started.
        }

        public override void OnGameUninstalled(Game game)
        {
            // Add code to be executed when game is uninstalled.
        }

        public override void OnApplicationStarted()
        {
            // Add code to be executed when Playnite is initialized.
        }

        public override void OnApplicationStopped()
        {
            // Add code to be executed when Playnite is shutting down.
        }

        public override void OnLibraryUpdated()
        {
            // Add code to be executed when library is updated.
        }

        public override void OnGameSelected(GameSelectionEventArgs args)
        {
            try
            {
                if (args.NewValue != null && args.NewValue.Count == 1)
                {
                    GameSelected = args.NewValue[0];
                    Integration();
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "SystemChecker", $"OnGameSelected()");
            }
        }

        private void Integration()
        {
            try
            {
                systemCheckerUI.AddBtActionBar();

                List<Guid> ListEmulators = new List<Guid>();
                foreach (var item in PlayniteApi.Database.Emulators)
                {
                    ListEmulators.Add(item.Id);
                }
                
                if (GameSelected.PlayAction != null && GameSelected.PlayAction.EmulatorId != null && ListEmulators.Contains(GameSelected.PlayAction.EmulatorId))
                {
                    // Emulator
                }
                else
                {
                    systemCheckerUI.RefreshBtActionBar(GameSelected);
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "SystemChecker", $"Impossible integration");
            }
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return settings;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new SystemCheckerSettingsView(PlayniteApi, this.GetPluginUserDataPath());
        }
    }
}