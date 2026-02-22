using System;
using System.Collections.Generic;
using System.Linq;
using Playnite.SDK;
using Playnite.SDK.Plugins;
using Playnite.SDK.Models;
using CommonPluginsShared;
using CommonPluginsShared.Collections;
using CommonPluginsShared.Plugins;
using CommonPluginsShared.Interfaces;

namespace SystemChecker.Services
{
    /// <summary>
    /// Manages the context menus for the SystemChecker plugin.
    /// Handles the creation and logic of both Game Menu items and Main Menu items.
    /// </summary>
    public class SystemCheckerMenus : PluginMenus
    {
        public SystemCheckerMenus(PluginSettings settings, IPluginDatabase database) : base(settings, database)
        {
        }

        /// <summary>
        /// Gets the list of menu items for the Game Menu (accessible via right-click on a game).
        /// </summary>
        /// <param name="args">Arguments containing information about the selected games.</param>
        /// <returns>A list of <see cref="GameMenuItem"/>.</returns>
        public override IEnumerable<GameMenuItem> GetGameMenuItems(GetGameMenuItemsArgs args)
        {
            // Only support single game selection for most features currently
            Game gameMenu = args.Games.First();
            List<Guid> ids = args.Games.Select(x => x.Id).ToList();

            // Retrieve cached requirements data for the selected game
            PluginDataBaseGameBase pluginGameRequirements = _database.Get(gameMenu, true);

            List<GameMenuItem> gameMenuItems = new List<GameMenuItem>();

            // Add "Check Configuration" option if data exists
            if (pluginGameRequirements.HasData)
            {
                gameMenuItems.Add(new GameMenuItem
                {
                    MenuSection = ResourceProvider.GetString("LOCSystemChecker"),
                    Description = ResourceProvider.GetString("LOCSystemCheckerCheckConfig"),
                    Action = (gameMenuItem) =>
                    {
                        try
                        {
                            _database.PluginWindows.ShowPluginGameDataWindow(gameMenu);
                        }
                        catch (Exception ex)
                        {
                            Common.LogError(ex, false, true, _database.PluginName);
                        }
                    }
                });

                gameMenuItems.Add(new GameMenuItem
                {
                    MenuSection = ResourceProvider.GetString("LOCSystemChecker"),
                    Description = "-"
                });
            }


            // Option to refresh (delete & download) requirements data
            gameMenuItems.Add(new GameMenuItem
            {
                MenuSection = ResourceProvider.GetString("LOCSystemChecker"),
                Description = ResourceProvider.GetString("LOCCommonRefreshGameData"),
                Action = (gameMenuItem) =>
                {
                    if (ids.Count == 1)
                    {
                        _database.Refresh(gameMenu.Id);
                    }
                    else
                    {
                        // Bulk refresh for multiple selected games
                        _database.Refresh(ids);
                    }
                }
            });


            // Option to delete data, visible only if data exists
            if (pluginGameRequirements.HasData)
            {
                gameMenuItems.Add(new GameMenuItem
                {
                    MenuSection = ResourceProvider.GetString("LOCSystemChecker"),
                    Description = ResourceProvider.GetString("LOCCommonDeleteGameData"),
                    Action = (mainMenuItem) =>
                    {
                        if (ids.Count == 1)
                        {
                            _database.Remove(gameMenu);
                        }
                        else
                        {
                            // Bulk remove for multiple selected games
                            _database.Remove(ids);
                        }
                    }
                });
            }

#if DEBUG
            gameMenuItems.Add(new GameMenuItem
            {
                MenuSection = ResourceProvider.GetString("LOCSystemChecker"),
                Description = "-"
            });
            gameMenuItems.Add(new GameMenuItem
            {
                MenuSection = ResourceProvider.GetString("LOCSystemChecker"),
                Description = "Test",
                Action = (mainMenuItem) => { }
            });
#endif

            return gameMenuItems;
        }

        /// <summary>
        /// Gets the list of menu items for the Main Menu (accessible via the main Playnite menu).
        /// </summary>
        /// <param name="args">Arguments for main menu items.</param>
        /// <returns>A list of <see cref="MainMenuItem"/>.</returns>
        public override IEnumerable<MainMenuItem> GetMainMenuItems(GetMainMenuItemsArgs args)
        {
            string MenuInExtensions = string.Empty;
            if (_settings.MenuInExtensions)
            {
                MenuInExtensions = "@";
            }

            List<MainMenuItem> mainMenuItems = new List<MainMenuItem>
            {
                // Download missing data for all games in the database
                new MainMenuItem
                {
                    MenuSection = MenuInExtensions + ResourceProvider.GetString("LOCSystemChecker"),
                    Description = ResourceProvider.GetString("LOCCommonDownloadPluginData"),
                    Action = (mainMenuItem) =>
                    {
                        _database.GetSelectData();
                    }
                }
            };

            // Tag management options (if enabled in settings)
            if (_settings.EnableTag)
            {
                mainMenuItems.Add(new MainMenuItem
                {
                    MenuSection = MenuInExtensions + ResourceProvider.GetString("LOCSystemChecker"),
                    Description = "-"
                });

                // Add tag for selected games in database (games with existing data)
                mainMenuItems.Add(new MainMenuItem
                {
                    MenuSection = MenuInExtensions + ResourceProvider.GetString("LOCSystemChecker"),
                    Description = ResourceProvider.GetString("LOCCommonAddTPlugin"),
                    Action = (mainMenuItem) =>
                    {
                        _database.AddTagSelectData();
                    }
                });
                // Add tag for ALL games
                mainMenuItems.Add(new MainMenuItem
                {
                    MenuSection = MenuInExtensions + ResourceProvider.GetString("LOCSystemChecker"),
                    Description = ResourceProvider.GetString("LOCCommonAddAllTags"),
                    Action = (mainMenuItem) =>
                    {
                        _database.AddTagAllGames();
                    }
                });
                // Remove tag for ALL games
                mainMenuItems.Add(new MainMenuItem
                {
                    MenuSection = MenuInExtensions + ResourceProvider.GetString("LOCSystemChecker"),
                    Description = ResourceProvider.GetString("LOCCommonRemoveAllTags"),
                    Action = (mainMenuItem) =>
                    {
                        _database.RemoveTagAllGames();
                    }
                });
            }

            mainMenuItems.Add(new MainMenuItem
            {
                MenuSection = MenuInExtensions + ResourceProvider.GetString("LOCSystemChecker"),
                Description = "-"
            });

            // View list of games with no data
            mainMenuItems.Add(new MainMenuItem
            {
                MenuSection = MenuInExtensions + ResourceProvider.GetString("LOCSystemChecker"),
                Description = ResourceProvider.GetString("LOCCommonViewNoData"),
                Action = (mainMenuItem) =>
                {
                    _database.PluginWindows.ShowPluginGameNoDataWindow();
                }
            });

            mainMenuItems.Add(new MainMenuItem
            {
                MenuSection = MenuInExtensions + ResourceProvider.GetString("LOCSystemChecker"),
                Description = "-"
            });


            // Option to purge the entire database
            mainMenuItems.Add(new MainMenuItem
            {
                MenuSection = MenuInExtensions + ResourceProvider.GetString("LOCSystemChecker"),
                Description = ResourceProvider.GetString("LOCCommonDeletePluginData"),
                Action = (mainMenuItem) =>
                {
                    _database.ClearDatabase();
                }
            });

#if DEBUG
            mainMenuItems.Add(new MainMenuItem
            {
                MenuSection = MenuInExtensions + ResourceProvider.GetString("LOCSystemChecker"),
                Description = "-"
            });
            mainMenuItems.Add(new MainMenuItem
            {
                MenuSection = MenuInExtensions + ResourceProvider.GetString("LOCSystemChecker"),
                Description = "Test",
                Action = (mainMenuItem) => { }
            });
#endif

            return mainMenuItems;
        }
    }
}