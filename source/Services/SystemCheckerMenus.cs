using System;
using System.Collections.Generic;
using System.Linq;
using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using CommonPluginsShared;
using CommonPluginsShared.Collections;
using CommonPluginsShared.Commands;
using CommonPluginsShared.Interfaces;
using CommonPluginsShared.Plugins;

namespace SystemChecker.Services
{
	/// <summary>
	/// Manages the context menus for the SystemChecker plugin.
	/// Handles the creation and logic of both Game Menu items and Main Menu items.
	/// </summary>
	public class SystemCheckerMenus : PluginMenus
	{
		public SystemCheckerMenus(IPluginSettings settings, IPluginDatabase database) : base(settings, database)
		{
		}

		/// <inheritdoc />
		public override IEnumerable<GameMenuItem> GetGameMenuItems(GetGameMenuItemsArgs args)
		{
			List<Game> includedGames = args.Games
				.Where(game => PlayniteTools.ShouldIncludeLibraryGame(game, _settings))
				.ToList();

			if (includedGames.Count == 0)
			{
				Common.LogDebug(true, string.Format(
					"[LibraryFilter] SystemCheckerMenus: game menu hidden — no eligible game in selection ({0} selected, IncludeEmulatedGames={1}, SourceFilter={2})",
					args.Games.Count,
					_settings.IncludeEmulatedGames,
					PlayniteTools.FormatSourceFilterForLog(_settings)));

				return Enumerable.Empty<GameMenuItem>();
			}

			int excludedCount = args.Games.Count - includedGames.Count;
			if (excludedCount > 0)
			{
				Common.LogDebug(true, string.Format(
					"[LibraryFilter] SystemCheckerMenus: {0}/{1} selected game(s) excluded from menu actions (IncludeEmulatedGames={2}, SourceFilter={3})",
					excludedCount,
					args.Games.Count,
					_settings.IncludeEmulatedGames,
					PlayniteTools.FormatSourceFilterForLog(_settings)));
			}

			Game gameMenu = includedGames.First();
			List<Guid> ids = includedGames.Select(x => x.Id).ToList();

			// Retrieve cached requirements data for the selected game
			PluginGameEntry pluginGameRequirements = _database.Get(gameMenu, true);

			List<GameMenuItem> gameMenuItems = new List<GameMenuItem>();

			// Add Check Configuration option if data exists
			if (pluginGameRequirements.HasData)
			{
				gameMenuItems.Add(new GameMenuItem
				{
					MenuSection = ResourceProvider.GetString("LOCSystemChecker"),
					Description = ResourceProvider.GetString("LOCSystemCheckerCheckConfig"),
					Action = gameMenuItem =>
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

			gameMenuItems.Add(new GameMenuItem
			{
				MenuSection = ResourceProvider.GetString("LOCSystemChecker"),
				Description = ResourceProvider.GetString("LOCCommonRefreshGameData"),
				Action = gameMenuItem =>
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
					Action = mainMenuItem =>
					{
						if (ids.Count == 1)
						{
							_database.Remove(gameMenu.Id);
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
				Action = mainMenuItem => { }
			});
#endif

			return gameMenuItems;
		}

		/// <inheritdoc />
		public override IEnumerable<MainMenuItem> GetMainMenuItems(GetMainMenuItemsArgs args)
		{
			string menuInExtensions = string.Empty;
			if (_settings.MenuInExtensions)
			{
				menuInExtensions = "@";
			}

			List<MainMenuItem> mainMenuItems = new List<MainMenuItem>
			{
                // Download missing data for all games in the database
                new MainMenuItem
				{
					MenuSection = menuInExtensions + ResourceProvider.GetString("LOCSystemChecker"),
					Description = ResourceProvider.GetString("LOCCommonDownloadPluginData"),
					Action = mainMenuItem => _database.GetSelectData()
				}
			};

			// Tag management options if enabled in settings
			if (_settings.EnableTag)
			{
				mainMenuItems.Add(new MainMenuItem
				{
					MenuSection = menuInExtensions + ResourceProvider.GetString("LOCSystemChecker"),
					Description = "-"
				});

				// Add tag for selected games in database (games with existing data only)
				mainMenuItems.Add(new MainMenuItem
				{
					MenuSection = menuInExtensions + ResourceProvider.GetString("LOCSystemChecker"),
					Description = ResourceProvider.GetString("LOCCommonAddTPlugin"),
					Action = mainMenuItem => _database.AddTagSelectData()
				});

				// Add tag for ALL games — delegates to CommandsPlugin for consistent error handling
				mainMenuItems.Add(new MainMenuItem
				{
					MenuSection = menuInExtensions + ResourceProvider.GetString("LOCSystemChecker"),
					Description = ResourceProvider.GetString("LOCCommonAddAllTags"),
					Action = mainMenuItem => _commands.CmdAddTag.Execute(null)
				});

				// Remove tag for ALL games — delegates to CommandsPlugin for consistent error handling
				mainMenuItems.Add(new MainMenuItem
				{
					MenuSection = menuInExtensions + ResourceProvider.GetString("LOCSystemChecker"),
					Description = ResourceProvider.GetString("LOCCommonRemoveAllTags"),
					Action = mainMenuItem => _commands.CmdRemoveTag.Execute(null)
				});
			}

			mainMenuItems.Add(new MainMenuItem
			{
				MenuSection = menuInExtensions + ResourceProvider.GetString("LOCSystemChecker"),
				Description = "-"
			});

			mainMenuItems.Add(new MainMenuItem
			{
				MenuSection = menuInExtensions + ResourceProvider.GetString("LOCSystemChecker"),
				Description = ResourceProvider.GetString("LOCCommonViewNoData"),
				Action = mainMenuItem => _database.PluginWindows.ShowPluginGameNoDataWindow()
			});

			mainMenuItems.Add(new MainMenuItem
			{
				MenuSection = menuInExtensions + ResourceProvider.GetString("LOCSystemChecker"),
				Description = "-"
			});

			// Purge the entire database — delegates to CommandsPlugin (includes confirmation dialog)
			mainMenuItems.Add(new MainMenuItem
			{
				MenuSection = menuInExtensions + ResourceProvider.GetString("LOCSystemChecker"),
				Description = ResourceProvider.GetString("LOCCommonDeletePluginData"),
				Action = mainMenuItem => _commands.CmdClearAll.Execute(null)
			});

#if DEBUG
			mainMenuItems.Add(new MainMenuItem
			{
				MenuSection = menuInExtensions + ResourceProvider.GetString("LOCSystemChecker"),
				Description = "-"
			});

			mainMenuItems.Add(new MainMenuItem
			{
				MenuSection = menuInExtensions + ResourceProvider.GetString("LOCSystemChecker"),
				Description = "Test",
				Action = mainMenuItem => { }
			});
#endif

			return mainMenuItems;
		}
	}
}