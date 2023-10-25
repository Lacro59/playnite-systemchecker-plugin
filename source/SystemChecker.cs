using CommonPluginsControls.Views;
using CommonPluginsShared;
using CommonPluginsShared.PlayniteExtended;
using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using SystemChecker.Controls;
using SystemChecker.Models;
using SystemChecker.Services;
using SystemChecker.Views;

namespace SystemChecker
{
    public class SystemChecker : PluginExtended<SystemCheckerSettingsViewModel, SystemCheckerDatabase>
    {
        public override Guid Id { get; } = Guid.Parse("e248b230-6edf-41ea-a3c3-7861fa267263");

        private bool preventLibraryUpdatedOnStart { get; set; } = true;

        public SystemChecker(IPlayniteAPI api) : base(api)
        {
            // Custom theme button
            EventManager.RegisterClassHandler(typeof(Button), Button.ClickEvent, new RoutedEventHandler(OnCustomThemeButtonClick));

            // Custom elements integration
            AddCustomElementSupport(new AddCustomElementSupportArgs
            {
                ElementList = new List<string> { "PluginButton", "PluginViewItem" },
                SourceName = "SystemChecker"
            });

            // Settings integration
            AddSettingsSupport(new AddSettingsSupportArgs
            {
                SourceName = "SystemChecker",
                SettingsRoot = $"{nameof(PluginSettings)}.{nameof(PluginSettings.Settings)}"
            });

            //Playnite search integration
            Searches = new List<SearchSupport>
            {
                new SearchSupport("sc", "SystemChecker", new SystemCheckerSearch())
            };
        }


        #region Custom event
        public void OnCustomThemeButtonClick(object sender, RoutedEventArgs e)
        {
            try
            {
                string ButtonName = ((Button)sender).Name;
                if (ButtonName == "PART_CustomSysCheckerButton")
                {
                    PluginDatabase.IsViewOpen = true;
                    var ViewExtension = new SystemCheckerGameView(PlayniteApi, this.GetPluginUserDataPath(), PluginDatabase.GameContext);
                    Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(PlayniteApi, PluginDatabase.PluginName, ViewExtension);
                    windowExtension.ShowDialog();
                    PluginDatabase.IsViewOpen = false;
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }
        }
        #endregion


        #region Theme integration
        public override IEnumerable<TopPanelItem> GetTopPanelItems()
        {
            yield break;
        }

        // List custom controls
        public override Control GetGameViewControl(GetGameViewControlArgs args)
        {
            if (args.Name == "PluginButton")
            {
                return new PluginButton();
            }

            if (args.Name == "PluginViewItem")
            {
                return new PluginViewItem();
            }

            return null;
        }
        #endregion


        #region Menus
        // To add new game menu items override GetGameMenuItems
        public override IEnumerable<GameMenuItem> GetGameMenuItems(GetGameMenuItemsArgs args)
        {
            Game GameMenu = args.Games.First();
            List<Guid> Ids = args.Games.Select(x => x.Id).ToList();
            GameRequierements gameRequierements = PluginDatabase.Get(GameMenu, true);

            List<GameMenuItem> gameMenuItems = new List<GameMenuItem>();

            if (gameRequierements.HasData)
            {
                // Show requierements for the selected game
                gameMenuItems.Add(new GameMenuItem
                {
                    MenuSection = resources.GetString("LOCSystemChecker"),
                    Description = resources.GetString("LOCSystemCheckerCheckConfig"),
                    Action = (gameMenuItem) =>
                    {
                        PluginDatabase.IsViewOpen = true;
                        var ViewExtension = new SystemCheckerGameView(PlayniteApi, this.GetPluginUserDataPath(), GameMenu);
                        Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(PlayniteApi, PluginDatabase.PluginName, ViewExtension);
                        windowExtension.ShowDialog();
                        PluginDatabase.IsViewOpen = false;
                    }
                });

                gameMenuItems.Add(new GameMenuItem
                {
                    MenuSection = resources.GetString("LOCSystemChecker"),
                    Description = "-"
                });
            }


            // Delete & download requierements data for the selected game
            gameMenuItems.Add(new GameMenuItem
            {
                MenuSection = resources.GetString("LOCSystemChecker"),
                Description = resources.GetString("LOCCommonRefreshGameData"),
                Action = (gameMenuItem) =>
                {
                    if (Ids.Count == 1)
                    {
                        PluginDatabase.Refresh(GameMenu.Id);
                    }
                    else
                    {
                        PluginDatabase.Refresh(Ids);
                    }
                }
            });


            if (gameRequierements.HasData)
            {
                gameMenuItems.Add(new GameMenuItem
                {
                    MenuSection = resources.GetString("LOCSystemChecker"),
                    Description = resources.GetString("LOCCommonDeleteGameData"),
                    Action = (mainMenuItem) =>
                    {
                        if (Ids.Count == 1)
                        {
                            PluginDatabase.Remove(GameMenu);
                        }
                        else
                        {
                            PluginDatabase.Remove(Ids);
                        }
                    }
                });
            }

#if DEBUG
            gameMenuItems.Add(new GameMenuItem
            {
                MenuSection = resources.GetString("LOCSystemChecker"),
                Description = "-"
            });
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
        public override IEnumerable<MainMenuItem> GetMainMenuItems(GetMainMenuItemsArgs args)
        {
            string MenuInExtensions = string.Empty;
            if (PluginSettings.Settings.MenuInExtensions)
            {
                MenuInExtensions = "@";
            }

            List<MainMenuItem> mainMenuItems = new List<MainMenuItem>
            {
                // Download missing data for all game in database
                new MainMenuItem
                {
                    MenuSection = MenuInExtensions + resources.GetString("LOCSystemChecker"),
                    Description = resources.GetString("LOCCommonDownloadPluginData"),
                    Action = (mainMenuItem) =>
                    {
                        PluginDatabase.GetSelectData();
                    }
                }
            };

            if (PluginDatabase.PluginSettings.Settings.EnableTag)
            {
                mainMenuItems.Add(new MainMenuItem
                {
                    MenuSection = MenuInExtensions + resources.GetString("LOCSystemChecker"),
                    Description = "-"
                });

                // Add tag for selected game in database if data exists
                mainMenuItems.Add(new MainMenuItem
                {
                    MenuSection = MenuInExtensions + resources.GetString("LOCSystemChecker"),
                    Description = resources.GetString("LOCCommonAddTPlugin"),
                    Action = (mainMenuItem) =>
                    {
                        PluginDatabase.AddTagSelectData();
                    }
                });
                // Add tag for all games
                mainMenuItems.Add(new MainMenuItem
                {
                    MenuSection = MenuInExtensions + resources.GetString("LOCSystemChecker"),
                    Description = resources.GetString("LOCCommonAddAllTags"),
                    Action = (mainMenuItem) =>
                    {
                        PluginDatabase.AddTagAllGame();
                    }
                });
                // Remove tag for all game in database
                mainMenuItems.Add(new MainMenuItem
                {
                    MenuSection = MenuInExtensions + resources.GetString("LOCSystemChecker"),
                    Description = resources.GetString("LOCCommonRemoveAllTags"),
                    Action = (mainMenuItem) =>
                    {
                        PluginDatabase.RemoveTagAllGame();
                    }
                });
            }

            mainMenuItems.Add(new MainMenuItem
            {
                MenuSection = MenuInExtensions + resources.GetString("LOCSystemChecker"),
                Description = "-"
            });
            mainMenuItems.Add(new MainMenuItem
            {
                MenuSection = MenuInExtensions + resources.GetString("LOCSystemChecker"),
                Description = resources.GetString("LOCCommonViewNoData"),
                Action = (mainMenuItem) =>
                {
                    var windowOptions = new WindowOptions
                    {
                        ShowMinimizeButton = false,
                        ShowMaximizeButton = false,
                        ShowCloseButton = true
                    };

                    var ViewExtension = new ListWithNoData(PlayniteApi, PluginDatabase);
                    Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(PlayniteApi, resources.GetString("LOCSystemChecker"), ViewExtension, windowOptions);
                    windowExtension.ShowDialog();
                }
            });

            mainMenuItems.Add(new MainMenuItem
            {
                MenuSection = MenuInExtensions + resources.GetString("LOCSystemChecker"),
                Description = "-"
            });


            // Delete database
            mainMenuItems.Add(new MainMenuItem
            {
                MenuSection = MenuInExtensions + resources.GetString("LOCSystemChecker"),
                Description = resources.GetString("LOCCommonDeletePluginData"),
                Action = (mainMenuItem) =>
                {
                    PluginDatabase.ClearDatabase();
                }
            });

#if DEBUG
            mainMenuItems.Add(new MainMenuItem
            {
                MenuSection = MenuInExtensions + resources.GetString("LOCSystemChecker"),
                Description = "-"
            });
            mainMenuItems.Add(new MainMenuItem
            {
                MenuSection = MenuInExtensions + resources.GetString("LOCSystemChecker"),
                Description = "Test",
                Action = (mainMenuItem) => { }
            });
#endif

            return mainMenuItems;
        }
        #endregion


        #region Game event
        public override void OnGameSelected(OnGameSelectedEventArgs args)
        {
            try
            {
                if (args.NewValue?.Count == 1 && PluginDatabase.IsLoaded)
                {
                    PluginDatabase.GameContext = args.NewValue[0];
                    PluginDatabase.SetThemesResources(PluginDatabase.GameContext);
                }
                else
                {
                    Task.Run(() =>
                    {
                        System.Threading.SpinWait.SpinUntil(() => PluginDatabase.IsLoaded, -1);

                        Application.Current.Dispatcher.BeginInvoke((Action)delegate
                        {
                            if (args.NewValue?.Count == 1)
                            {
                                PluginDatabase.GameContext = args.NewValue[0];
                                PluginDatabase.SetThemesResources(PluginDatabase.GameContext);
                            }
                        });
                    });
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }
        }

        // Add code to be executed when game is finished installing.
        public override void OnGameInstalled(OnGameInstalledEventArgs args)
        {

        }

        // Add code to be executed when game is uninstalled.
        public override void OnGameUninstalled(OnGameUninstalledEventArgs args)
        {

        }

        // Add code to be executed when game is preparing to be started.
        public override void OnGameStarting(OnGameStartingEventArgs args)
        {

        }

        // Add code to be executed when game is started running.
        public override void OnGameStarted(OnGameStartedEventArgs args)
        {

        }

        // Add code to be executed when game is preparing to be started.
        public override void OnGameStopped(OnGameStoppedEventArgs args)
        {

        }
        #endregion


        #region Application event
        // Add code to be executed when Playnite is initialized.
        public override void OnApplicationStarted(OnApplicationStartedEventArgs args)
        {
            Task.Run(() => 
            {
                Thread.Sleep(30000);
                preventLibraryUpdatedOnStart = false;
            });

            // TODO TMP
            if (!PluginSettings.Settings.IsPurged)
            {
                GlobalProgressOptions globalProgressOptions = new GlobalProgressOptions(
                    $"SystemChecker - {resources.GetString("LOCSettingsUpdating")}",
                    true
                );
                globalProgressOptions.IsIndeterminate = true;

                PlayniteApi.Dialogs.ActivateGlobalProgress((activateGlobalProgress) =>
                {
                    try
                    {
                        // Wait extension database are loaded
                        System.Threading.SpinWait.SpinUntil(() => PluginDatabase.IsLoaded, -1);

                        foreach (GameRequierements values in PluginDatabase.Database.Items.Values)
                        {
                            GameRequierements gameRequierements = PluginDatabase.PurgeGraphicsCardData(values);
                            PluginDatabase.Update(gameRequierements);
                        }
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, false, true, PluginDatabase.PluginName);
                    }
                }, globalProgressOptions);

                PluginSettings.Settings.IsPurged = true;
                SavePluginSettings(PluginSettings.Settings);
            }
        }

        // Add code to be executed when Playnite is shutting down.
        public override void OnApplicationStopped(OnApplicationStoppedEventArgs args)
        {

        }
        #endregion


        // Add code to be executed when library is updated.
        public override void OnLibraryUpdated(OnLibraryUpdatedEventArgs args)
        {
            if (PluginSettings.Settings.AutoImport && !preventLibraryUpdatedOnStart)
            {
                var PlayniteDb = PlayniteApi.Database.Games
                        .Where(x => x.Added != null && x.Added > PluginSettings.Settings.LastAutoLibUpdateAssetsDownload)
                        .ToList();

                GlobalProgressOptions globalProgressOptions = new GlobalProgressOptions(
                    $"SystemChecker - {resources.GetString("LOCCommonGettingData")}",
                    true
                );
                globalProgressOptions.IsIndeterminate = false;

                PlayniteApi.Dialogs.ActivateGlobalProgress((activateGlobalProgress) =>
                {
                    try
                    {
                        Stopwatch stopWatch = new Stopwatch();
                        stopWatch.Start();

                        activateGlobalProgress.ProgressMaxValue = (double)PlayniteDb.Count();

                        string CancelText = string.Empty;

                        foreach (Game game in PlayniteDb)
                        {
                            if (activateGlobalProgress.CancelToken.IsCancellationRequested)
                            {
                                CancelText = " canceled";
                                break;
                            }

                            Thread.Sleep(10);
                            PluginDatabase.RefreshNoLoader(game.Id);

                            activateGlobalProgress.CurrentProgressValue++;
                        }

                        stopWatch.Stop();
                        TimeSpan ts = stopWatch.Elapsed;
                        logger.Info($"Task OnLibraryUpdated(){CancelText} - {string.Format("{0:00}:{1:00}.{2:00}", ts.Minutes, ts.Seconds, ts.Milliseconds / 10)} for {activateGlobalProgress.CurrentProgressValue}/{(double)PlayniteDb.Count()} items");
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, false, true, PluginDatabase.PluginName);
                    }
                }, globalProgressOptions);

                PluginSettings.Settings.LastAutoLibUpdateAssetsDownload = DateTime.Now;
                SavePluginSettings(PluginSettings.Settings);
            }
        }


        #region Settings
        public override ISettings GetSettings(bool firstRunSettings)
        {
            return PluginSettings;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new SystemCheckerSettingsView();
        }
        #endregion
    }
}
