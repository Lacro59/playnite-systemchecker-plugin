using CommonPluginsShared;
using CommonPluginsShared.PlayniteExtended;
using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using SystemChecker.Controls;
using SystemChecker.Services;
using SystemChecker.Views;

namespace SystemChecker
{
    public class SystemChecker : PluginExtended<SystemCheckerSettingsViewModel, SystemCheckerDatabase>
    {
        public override Guid Id { get; } = Guid.Parse("e248b230-6edf-41ea-a3c3-7861fa267263");

        public SystemChecker(IPlayniteAPI api) : base(api, "SystemChecker")
        {
            // Menus
            _menus = new SystemCheckerMenus(PluginSettings.Settings, PluginDatabase);

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
                string btName = ((Button)sender).Name;
                if (btName == "PART_CustomSysCheckerButton")
                {
                    PluginDatabase.PluginWindows.ShowPluginGameDataWindow(PluginDatabase.GameContext);
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }
        }

		#endregion

		#region Theme integration

		/// <inheritdoc />
		public override IEnumerable<TopPanelItem> GetTopPanelItems()
        {
            yield break;
        }

		/// <inheritdoc />
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

		/// <inheritdoc />
		public override IEnumerable<GameMenuItem> GetGameMenuItems(GetGameMenuItemsArgs args)
        {
            return _menus.GetGameMenuItems(args);
        }

		/// <inheritdoc />
		public override IEnumerable<MainMenuItem> GetMainMenuItems(GetMainMenuItemsArgs args)
        {
            return _menus.GetMainMenuItems(args);
        }

		#endregion

		#region Game event

		/// <inheritdoc />
		public override void OnGameSelected(OnGameSelectedEventArgs args)
		{
			try
			{
				if (args.NewValue?.Count != 1)
				{
					return;
				}

				Game selectedGame = args.NewValue[0];

				API.Instance.MainView.UIDispatcher.BeginInvoke((Action)delegate
				{
					if (!PluginDatabase.IsLoaded)
					{
						return;
					}

					PluginDatabase.GameContext = selectedGame;
					PluginDatabase.SetThemesResources(selectedGame);
				});
			}
			catch (Exception ex)
			{
				Common.LogError(ex, false, true, PluginDatabase.PluginName);
			}
		}

		/// <inheritdoc />
		public override void OnGameInstalled(OnGameInstalledEventArgs args)
        {
        }

		/// <inheritdoc />
		public override void OnGameUninstalled(OnGameUninstalledEventArgs args)
        {
        }

		/// <inheritdoc />
		public override void OnGameStarting(OnGameStartingEventArgs args)
        {
        }

		/// <inheritdoc />
		public override void OnGameStarted(OnGameStartedEventArgs args)
        {
        }

		/// <inheritdoc />
		public override void OnGameStopped(OnGameStoppedEventArgs args)
        {
        }

		#endregion

		#region Application event

		/// <inheritdoc />
		public override void OnApplicationStarted(OnApplicationStartedEventArgs args)
        {
            Task.Run(() =>
            {
                Thread.Sleep(30000);
                _preventLibraryUpdatedOnStart = false;
            });
        }

		/// <inheritdoc />
		public override void OnApplicationStopped(OnApplicationStoppedEventArgs args)
        {
        }

		#endregion

		/// <inheritdoc />
		public override void OnLibraryUpdated(OnLibraryUpdatedEventArgs args)
        {
            if (PluginSettings.Settings.AutoImport && !_preventLibraryUpdatedOnStart)
            {
                var playniteDb = PlayniteApi.Database.Games
                        .Where(x => x.Added != null && x.Added > PluginSettings.Settings.LastAutoLibUpdateAssetsDownload)
                        .ToList();

                GlobalProgressOptions globalProgressOptions = new GlobalProgressOptions($"SystemChecker - {ResourceProvider.GetString("LOCCommonGettingData")}")
                {
                    Cancelable = true,
                    IsIndeterminate = false
                };

                PlayniteApi.Dialogs.ActivateGlobalProgress((a) =>
                {
                    try
                    {
                        Stopwatch stopWatch = new Stopwatch();
                        stopWatch.Start();

                        a.ProgressMaxValue = (double)playniteDb.Count();

                        string cancelText = string.Empty;

                        foreach (Game game in playniteDb)
                        {
                            if (a.CancelToken.IsCancellationRequested)
                            {
								cancelText = " canceled";
                                break;
                            }

                            Thread.Sleep(10);
                            PluginDatabase.RefreshNoLoader(game.Id);

                            a.CurrentProgressValue++;
                        }

                        stopWatch.Stop();
                        TimeSpan ts = stopWatch.Elapsed;
                        Logger.Info($"Task OnLibraryUpdated(){cancelText} - {string.Format("{0:00}:{1:00}.{2:00}", ts.Minutes, ts.Seconds, ts.Milliseconds / 10)} for {a.CurrentProgressValue}/{(double)playniteDb.Count()} items");
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

		/// <inheritdoc />
		public override ISettings GetSettings(bool firstRunSettings)
        {
            return PluginSettings;
        }

		/// <inheritdoc />
		public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new SystemCheckerSettingsView();
        }

        #endregion
    }
}