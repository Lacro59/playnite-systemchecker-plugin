using Playnite.SDK;
using Playnite.SDK.Models;
using PluginCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SystemChecker.Clients;
using SystemChecker.Models;
using SystemChecker.Views;

namespace SystemChecker.Services
{
    public class SystemCheckerUI : PlayniteUiHelper
    {
        public readonly SystemCheckerSettings _Settings;

        private const string BtActionBarName = "PART_ScheckButton";
        private Button PART_ScheckButton;
        private Brush DefaultBtForeground;


        public SystemCheckerUI(IPlayniteAPI PlayniteApi, SystemCheckerSettings Settings, string PluginUserDataPath) : base(PlayniteApi, PluginUserDataPath)
        {
            _Settings = Settings; 
        }


        #region BtActionBar
        public override void AddBtActionBar()
        {
            // Check type view
            // TODO Don't work when you change view
            if (PART_ScheckButton != null)
            {
                FrameworkElement BtActionBarParent = (FrameworkElement)PART_ScheckButton.Parent;
                if (BtActionBarParent is StackPanel && BtActionBarParentType != ParentTypeView.Details)
                {
#if DEBUG
                    logger.Debug($"SystemChecker - PART_ScheckButton removed from DetailsView");
#endif
                    RemoveBtActionBar();
                    BtActionBarParentType = ParentTypeView.Details;
                }
                if (BtActionBarParent is Grid && BtActionBarParentType != ParentTypeView.Grid)
                {
#if DEBUG
                    logger.Debug($"SystemChecker - PART_ScheckButton removed from GridView");
#endif
                    RemoveBtActionBar();
                    BtActionBarParentType = ParentTypeView.Grid;
                }
            }

            if (PART_ScheckButton != null)
            { 
#if DEBUG
                logger.Debug($"SystemChecker - PART_ScheckButton allready insert - {BtActionBarParentType}");
#endif
                return;
            }

            if (_Settings.EnableIntegrationButton || _Settings.EnableIntegrationButtonDetails) { 
                Button BtActionBar = new Button
                {
                    Name = BtActionBarName,
                    Content = "",
                    FontFamily = new FontFamily("Wingdings"),
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    Margin = new Thickness(10, 0, 0, 0)
                };
                BtActionBar.Click += OnBtActionBarClick;

                DefaultBtForeground = BtActionBar.Foreground;

                try
                {
                    ui.AddButtonInGameSelectedActionBarButtonOrToggleButton(BtActionBar);
                    PART_ScheckButton = (Button)ui.SearchElementByName(BtActionBarName);
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, "SystemChecker", "Error on AddBtActionBar()");
                }
            }
        }

        public override void RefreshBtActionBar(Game GameSelected)
        {
            taskHelper.Check();
            CancellationTokenSource tokenSource = new CancellationTokenSource();
            CancellationToken ct = tokenSource.Token;

            Task TaskRefreshBtActionBar = Task.Run(() => {
                // Load data
                SystemApi systemApi = new SystemApi(_PluginUserDataPath, _PlayniteApi);
                SystemConfiguration systemConfiguration = systemApi.GetInfo();
                GameRequierements gameRequierements = systemApi.GetGameRequierements(GameSelected);

                CheckSystem CheckMinimum = new CheckSystem();
                CheckSystem CheckRecommanded = new CheckSystem();
                if (gameRequierements.Minimum != null && gameRequierements.Minimum.Os.Count != 0)
                {
                    CheckMinimum = SystemApi.CheckConfig(gameRequierements.Minimum, systemConfiguration);
                }
                if (gameRequierements.Recommanded != null && gameRequierements.Recommanded.Os.Count != 0)
                {
                    CheckRecommanded = SystemApi.CheckConfig(gameRequierements.Recommanded, systemConfiguration);
                }

                // If not cancel, show
                if (!ct.IsCancellationRequested)
                {
                    Application.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        if (PART_ScheckButton != null)
                        {
                            PART_ScheckButton.Visibility = Visibility.Collapsed;

                            if (_Settings.EnableIntegrationButton)
                            {
                                PART_ScheckButton.Visibility = Visibility.Visible;
                                PART_ScheckButton.Foreground = DefaultBtForeground;
                            }

                            if (_Settings.EnableIntegrationButtonDetails)
                            {
                                PART_ScheckButton.Visibility = Visibility.Visible;

                                if (CheckMinimum.AllOk != null)
                                {
                                    if (!(bool)CheckMinimum.AllOk)
                                    {
                                        PART_ScheckButton.Foreground = Brushes.Red;
                                    }

                                    if ((bool)CheckMinimum.AllOk)
                                    {
                                        PART_ScheckButton.Foreground = Brushes.Orange;
                                        if (CheckRecommanded.AllOk == null)
                                        {
                                            PART_ScheckButton.Foreground = Brushes.Green;
                                        }
                                    }
                                }
                                if (CheckRecommanded.AllOk != null)
                                {
                                    if ((bool)CheckRecommanded.AllOk)
                                    {
                                        PART_ScheckButton.Foreground = Brushes.Green;
                                    }
                                }
                            }
                        }
                        else
                        {
                            logger.Warn($"SystemChecker - PART_ScheckButton is not defined");
                        }
                    }));
                }
            });

            taskHelper.Add(TaskRefreshBtActionBar, tokenSource);
        }

        public override void RemoveBtActionBar()
        {
            ui.RemoveButtonInGameSelectedActionBarButtonOrToggleButton(BtActionBarName);
            PART_ScheckButton = null;
            BtActionBarParentType = ParentTypeView.Unknown;
        }


        private void OnBtActionBarClick(object sender, RoutedEventArgs e)
        {
            if (SystemChecker.GameSelected != null)
            {
                var ViewExtension = new SystemCheckerGameView(_PluginUserDataPath, SystemChecker.GameSelected, _PlayniteApi);
                Window windowExtension = CreateExtensionWindow(_PlayniteApi, "SystemChecker", ViewExtension);
                windowExtension.ShowDialog();
            }
            else
            {
                _PlayniteApi.Dialogs.ShowErrorMessage("No game selected for show extension view.", "SystemChecker");
            }
        }
        #endregion
    }
}
