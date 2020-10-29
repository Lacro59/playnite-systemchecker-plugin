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
using System.Threading.Tasks;
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
        public static SystemCheckerUI systemCheckerUI;
        public static string pluginFolder;


        public SystemChecker(IPlayniteAPI api) : base(api)
        {
            settings = new SystemCheckerSettings(this);


            // Get plugin's location 
            pluginFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

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
            systemCheckerUI = new SystemCheckerUI(api, settings, this.GetPluginUserDataPath());

            // Custom theme button
            EventManager.RegisterClassHandler(typeof(Button), Button.ClickEvent, new RoutedEventHandler(systemCheckerUI.OnCustomThemeButtonClick));
        }

        // To add new game menu items override GetGameMenuItems
        public override List<GameMenuItem> GetGameMenuItems(GetGameMenuItemsArgs args)
        {
            List<GameMenuItem> gameMenuItems = new List<GameMenuItem>
            {
                new GameMenuItem {
                    MenuSection = resources.GetString("LOCSystemChecker"),
                    Description = resources.GetString("LOCSystemCheckerCheckConfig"),
                    Action = (gameMenuItem) =>
                    {
                        var ViewExtension = new SystemCheckerGameView(this.GetPluginUserDataPath(), args.Games.First(), PlayniteApi);
                        Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(PlayniteApi, "SystemChecker", ViewExtension);
                        windowExtension.ShowDialog();
                    }
                },
                new GameMenuItem {
                    MenuSection = resources.GetString("LOCSystemChecker"),
                    Description = resources.GetString("LOCCommonRefreshGameData"),
                    Action = (gameMenuItem) =>
                    {
                        var TaskIntegrationUI = Task.Run(() =>
                        {
                            systemCheckerUI.Initial();
                            systemCheckerUI.RefreshElements(args.Games.First(), true);
                        });
                    }
                }
            };

#if DEBUG
            gameMenuItems.Add(new GameMenuItem
            {
                MenuSection = resources.GetString("LOCSystemChecker"),
                Description = "Test",
                Action = (mainMenuItem) => { }
            });
#endif

            return gameMenuItems;
        }

        // To add new main menu items override GetMainMenuItems
        public override List<MainMenuItem> GetMainMenuItems(GetMainMenuItemsArgs args)
        {
            string MenuInExtensions = string.Empty;
            if (settings.MenuInExtensions)
            {
                MenuInExtensions = "@";
            }

            List<MainMenuItem> mainMenuItems = new List<MainMenuItem>
            {
                new MainMenuItem
                {
                    MenuSection = MenuInExtensions + resources.GetString("LOCSystemChecker"),
                    Description = resources.GetString("LOCCommonGettingAllDatas"),
                    Action = (mainMenuItem) => 
                    {
                        SystemApi systemApi = new SystemApi(this.GetPluginUserDataPath(), PlayniteApi);
                        systemApi.GetDataGetAll();
                    }
                },
                new MainMenuItem
                {
                    MenuSection = MenuInExtensions + resources.GetString("LOCSystemChecker"),
                    Description = resources.GetString("LOCCommonClearAllDatas"),
                    Action = (mainMenuItem) => 
                    {
                        SystemApi.DataDeleteAll(PlayniteApi, this.GetPluginUserDataPath());
                    }
                }
            };

#if DEBUG
            mainMenuItems.Add(new MainMenuItem
            {
                MenuSection = MenuInExtensions + resources.GetString("LOCSystemChecker"),
                Description = "Test",
                Action = (mainMenuItem) => { }
            });
#endif

            return mainMenuItems;
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

                    var TaskIntegrationUI = Task.Run(() =>
                    {
                        systemCheckerUI.AddElements();
                        systemCheckerUI.RefreshElements(GameSelected);
                    });
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "CheckLocalizations", $"OnGameSelected()");
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