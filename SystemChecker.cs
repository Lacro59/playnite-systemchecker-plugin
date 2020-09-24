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
using System.Management;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SystemChecker.Clients;
using SystemChecker.Models;
using SystemChecker.Views;

namespace SystemChecker
{
    public class SystemChecker : Plugin
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private static IResourceProvider resources = new ResourceProvider();

        private SystemCheckerSettings settings { get; set; }

        public override Guid Id { get; } = Guid.Parse("e248b230-6edf-41ea-a3c3-7861fa267263");

        private Game GameSelected { get; set; }
        private readonly IntegrationUI ui = new IntegrationUI();

        private CheckSystem CheckMinimum { get; set; }
        private CheckSystem CheckRecommanded { get; set; }

        public SystemChecker(IPlayniteAPI api) : base(api)
        {
            settings = new SystemCheckerSettings(this);


            // Get plugin's location 
            string pluginFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            // Add plugin localization in application ressource.
            PluginCommon.Localization.SetPluginLanguage(pluginFolder, api.Paths.ConfigurationPath);
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
        }

        public override IEnumerable<ExtensionFunction> GetFunctions()
        {
            return new List<ExtensionFunction>
            {
                new ExtensionFunction(
                    "SystemChecker",
                    () =>
                    {
                        // Add code to be execute when user invokes this menu entry.

                        new SystemCheckerGameView(this.GetPluginUserDataPath(), GameSelected, PlayniteApi).ShowDialog();
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
                // Delete
                logger.Info("SystemChecker - Delete");
                ui.RemoveButtonInGameSelectedActionBarButtonOrToggleButton("PART_ScheckButton");


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
                    var taskSystem = Task.Run(() =>
                    {
                        SystemApi systemApi = new SystemApi(this.GetPluginUserDataPath(), PlayniteApi);
                        SystemConfiguration systemConfiguration = systemApi.GetInfo();
                        GameRequierements gameRequierements = systemApi.GetGameRequierements(GameSelected);


                        if (gameRequierements.Minimum != null)
                        {
                            foreach (var item in gameRequierements.Minimum.Gpu)
                            {
                                Gpu gpu = new Gpu(systemConfiguration, item);
                            }
                        }
                        if (gameRequierements.Recommanded != null)
                        {
                            foreach (var item in gameRequierements.Recommanded.Gpu)
                            {
                                Gpu gpu = new Gpu(systemConfiguration, item);
                            }
                        }


                        CheckMinimum = new CheckSystem();
                        CheckRecommanded = new CheckSystem();
                        if (gameRequierements.Minimum != null && gameRequierements.Minimum.Os.Count != 0)
                        {
                            CheckMinimum = SystemApi.CheckConfig(gameRequierements.Minimum, systemConfiguration);
                        }
                        if (gameRequierements.Recommanded != null && gameRequierements.Recommanded.Os.Count != 0)
                        {
                            CheckRecommanded = SystemApi.CheckConfig(gameRequierements.Recommanded, systemConfiguration);
                        }
                    })
                    .ContinueWith(antecedent =>
                    {
                        Application.Current.Dispatcher.Invoke(new Action(() => {
                            // Auto adding button
                            if (settings.EnableIntegrationButton || settings.EnableIntegrationButtonDetails)
                            {
                                Button bt = new Button();
                                bt.Content = "";
                                bt.FontFamily = new FontFamily("Wingdings");

                                if (settings.EnableIntegrationButtonDetails)
                                {

                                    if (CheckMinimum.AllOk != null)
                                    {
                                        if (!(bool)CheckMinimum.AllOk)
                                        {
                                            bt.Foreground = Brushes.Red;
                                        }

                                        if ((bool)CheckMinimum.AllOk)
                                        {
                                            bt.Foreground = Brushes.Orange;
                                            if (CheckRecommanded.AllOk == null)
                                            {
                                                bt.Foreground = Brushes.Green;
                                            }
                                        }
                                    }
                                    if (CheckRecommanded.AllOk != null)
                                    {
                                        if ((bool)CheckRecommanded.AllOk)
                                        {
                                            bt.Foreground = Brushes.Green;
                                        }
                                    }
                                }

                                bt.Name = "PART_ScheckButton";
                                bt.HorizontalAlignment = HorizontalAlignment.Right;
                                bt.VerticalAlignment = VerticalAlignment.Stretch;
                                bt.Margin = new Thickness(10, 0, 0, 0);
                                bt.Click += OnBtGameSelectedActionBarClick;

                                ui.AddButtonInGameSelectedActionBarButtonOrToggleButton(bt);
                            }
                        }));
                    });
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "SystemChecker", $"Impossible integration");
            }
        }

        private void OnBtGameSelectedActionBarClick(object sender, RoutedEventArgs e)
        {
            // Show SystemChecker
            new SystemCheckerGameView(this.GetPluginUserDataPath(), GameSelected, PlayniteApi).ShowDialog();
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