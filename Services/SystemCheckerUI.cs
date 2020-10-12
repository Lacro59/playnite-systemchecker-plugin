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
using SystemChecker.Views.Interfaces;

namespace SystemChecker.Services
{
    public class SystemCheckerUI : PlayniteUiHelper
    {
        private readonly SystemCheckerSettings _Settings;

        private Brush DefaultBtForeground;

        CheckSystem CheckMinimum = new CheckSystem();
        CheckSystem CheckRecommanded = new CheckSystem();


        public SystemCheckerUI(IPlayniteAPI PlayniteApi, SystemCheckerSettings Settings, string PluginUserDataPath) : base(PlayniteApi, PluginUserDataPath)
        {
            _Settings = Settings;
            BtActionBarName = "PART_BtActionBar";
        }


        public override void Initial()
        {
            if (_Settings.EnableIntegrationButton)
            {
#if DEBUG
                logger.Debug($"SystemChecker - InitialBtActionBar()");
#endif
                InitialBtActionBar();
            }

            if (_Settings.EnableIntegrationInCustomTheme)
            {
#if DEBUG
                logger.Debug($"SystemChecker - InitialCustomElements()");
#endif
                InitialCustomElements();
            }
        }

        public override void AddElements()
        {
            if (IsFirstLoad)
            {
#if DEBUG
                logger.Debug($"SystemChecker - IsFirstLoad");
#endif
                Thread.Sleep(1000);
                IsFirstLoad = false;
            }

            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                if (_Settings.EnableIntegrationButton || _Settings.EnableIntegrationButtonDetails)
                {
#if DEBUG
                    logger.Debug($"SystemChecker - AddBtActionBar()");
#endif
                    AddBtActionBar();
                }

                if (_Settings.EnableIntegrationInCustomTheme)
                {
#if DEBUG
                    logger.Debug($"SystemChecker - AddCustomElements()");
#endif
                    AddCustomElements();
                }
            }));
        }

        public override void RefreshElements(Game GameSelected)
        {
            taskHelper.Check();
            CancellationTokenSource tokenSource = new CancellationTokenSource();
            CancellationToken ct = tokenSource.Token;

            Task TaskRefreshBtActionBar = Task.Run(() => {
                try
                {
                    Initial();

                    // Reset resources
                    List<ResourcesList> resourcesLists = new List<ResourcesList>();
                    resourcesLists.Add(new ResourcesList { Key = "Scheck_HasData", Value = false });
                    resourcesLists.Add(new ResourcesList { Key = "Scheck_IsMinimumOK", Value = false });
                    resourcesLists.Add(new ResourcesList { Key = "Scheck_IsRecommandedOK", Value = false });
                    resourcesLists.Add(new ResourcesList { Key = "Scheck_IsAllOK", Value = false });
                    ui.AddResources(resourcesLists);

                    if (!PlayniteTools.IsGameEmulated(_PlayniteApi, GameSelected))
                    {
                        // Load data
                        SystemApi systemApi = new SystemApi(_PluginUserDataPath, _PlayniteApi);
                        SystemConfiguration systemConfiguration = systemApi.GetInfo();
                        GameRequierements gameRequierements = systemApi.GetGameRequierements(GameSelected);

                        CheckMinimum = new CheckSystem();
                        CheckRecommanded = new CheckSystem();
                        if (gameRequierements.Minimum != null && gameRequierements.Minimum.Os.Count != 0)
                        {
                            CheckMinimum = SystemApi.CheckConfig(gameRequierements.Minimum, systemConfiguration);

                            resourcesLists.Add(new ResourcesList { Key = "Scheck_HasData", Value = true });
                            resourcesLists.Add(new ResourcesList { Key = "Scheck_IsMinimumOK", Value = CheckMinimum.AllOk });
                            resourcesLists.Add(new ResourcesList { Key = "Scheck_IsAllOK", Value = CheckMinimum.AllOk });
                        }
                        if (gameRequierements.Recommanded != null && gameRequierements.Recommanded.Os.Count != 0)
                        {
                            CheckRecommanded = SystemApi.CheckConfig(gameRequierements.Recommanded, systemConfiguration);

                            resourcesLists.Add(new ResourcesList { Key = "Scheck_HasData", Value = true });
                            resourcesLists.Add(new ResourcesList { Key = "Scheck_IsRecommandedOK", Value = CheckRecommanded.AllOk });
                            resourcesLists.Add(new ResourcesList { Key = "Scheck_IsAllOK", Value = CheckRecommanded.AllOk });
                        }

                        // If not cancel, show
                        if (!ct.IsCancellationRequested)
                        {
                            ui.AddResources(resourcesLists);

                            Application.Current.Dispatcher.Invoke(new Action(() =>
                            {
                                if (_Settings.EnableIntegrationButton || _Settings.EnableIntegrationButtonDetails)
                                {
#if DEBUG
                                    logger.Debug($"SystemChecker - RefreshBtActionBar()");
#endif
                                    RefreshBtActionBar();
                                }

                                if (_Settings.EnableIntegrationInCustomTheme)
                                {
#if DEBUG
                                    logger.Debug($"SystemChecker - RefreshCustomElements()");
#endif
                                    RefreshCustomElements();
                                }
                            }));
                        }
                    }
                    else
                    {
                        logger.Info($"SystemChecker - No treatment for emulated game");
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, "CheckLocalizations", $"Error on TaskRefreshBtActionBar()");
                }
            });

            taskHelper.Add(TaskRefreshBtActionBar, tokenSource);
        }


        #region BtActionBar
        public override void InitialBtActionBar()
        {
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                if (PART_BtActionBar != null)
                {
                    PART_BtActionBar.Visibility = Visibility.Collapsed;
                    ((Button)PART_BtActionBar).Foreground = DefaultBtForeground;
                }
            }));
        }

        public override void AddBtActionBar()
        {
            CheckTypeView();

            if (PART_BtActionBar != null)
            { 
#if DEBUG
                logger.Debug($"SystemChecker - PART_BtActionBar allready insert - {BtActionBarParentType}");
#endif
                return;
            }

            Button BtActionBar = new Button();

            if (_Settings.EnableIntegrationButton)
            {
                BtActionBar = new SystemCheckerButton();
            }

            if (_Settings.EnableIntegrationButtonDetails)
            {
                BtActionBar = new SystemCheckerButtonDetails();
            }

            BtActionBar.Click += OnBtActionBarClick;
            BtActionBar.Name = BtActionBarName;
            BtActionBar.Margin = new Thickness(10, 0, 0, 0);
            DefaultBtForeground = BtActionBar.Foreground;

            try
            {
                ui.AddButtonInGameSelectedActionBarButtonOrToggleButton(BtActionBar);
                PART_BtActionBar = IntegrationUI.SearchElementByName(BtActionBarName);
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "SystemChecker", "Error on AddBtActionBar()");
            }
        }

        public override void RefreshBtActionBar()
        {
            if (PART_BtActionBar != null)
            {
                PART_BtActionBar.Visibility = Visibility.Visible;

                if (PART_BtActionBar is SystemCheckerButtonDetails)
                {
                    ((SystemCheckerButtonDetails)PART_BtActionBar).SetData(CheckMinimum, CheckRecommanded, DefaultBtForeground);
                }
            }
            else
            {
                logger.Warn($"CheckLocalizations - PART_BtActionBar is not defined");
            }
        }


        public void OnBtActionBarClick(object sender, RoutedEventArgs e)
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

        public void OnCustomThemeButtonClick(object sender, RoutedEventArgs e)
        {
            if (_Settings.EnableIntegrationInCustomTheme)
            {
                string ButtonName = string.Empty;
                try
                {
                    ButtonName = ((Button)sender).Name;
                    if (ButtonName == "PART_ScheckCustomButton")
                    {
                        OnBtActionBarClick(sender, e);
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, "SystemChecker", "OnCustomThemeButtonClick() error");
                }
            }
        }
        #endregion


        #region CustomElements
        public override void InitialCustomElements()
        {
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                foreach (CustomElement customElement in ListCustomElements)
                {
                    customElement.Element.Visibility = Visibility.Collapsed;
                    if (customElement.Element is Button)
                    {
                        ((Button)customElement.Element).Foreground = DefaultBtForeground;
                    }
                }
            }));
        }

        public override void AddCustomElements()
        {
            if (ListCustomElements.Count > 0)
            {
#if DEBUG
                logger.Debug($"SystemChecker - CustomElements allready insert - {ListCustomElements.Count}");
#endif
                return;
            }

            FrameworkElement PART_ScheckButtonWithJustIcon = null;
            FrameworkElement PART_ScheckButtonWithJustIconAndDetails = null;
            try
            {
                PART_ScheckButtonWithJustIcon = IntegrationUI.SearchElementByName("PART_ScheckButtonWithJustIcon");
                PART_ScheckButtonWithJustIconAndDetails = IntegrationUI.SearchElementByName("PART_ScheckButtonWithJustIconAndDetails");
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "SystemChecker", $"Error on find custom element");
            }

            if (PART_ScheckButtonWithJustIcon != null)
            {
                PART_ScheckButtonWithJustIcon = new SystemCheckerButton();
                ((Button)PART_ScheckButtonWithJustIcon).Click += OnBtActionBarClick;
                try
                {
                    ui.AddElementInCustomTheme(PART_ScheckButtonWithJustIcon, "PART_ScheckButtonWithJustIcon");
                    ListCustomElements.Add(new CustomElement { ParentElementName = "PART_ScheckButtonWithJustIcon", Element = PART_ScheckButtonWithJustIcon });
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, "CheckLocalizations", "Error on AddCustomElements()");
                }
            }

            if (PART_ScheckButtonWithJustIconAndDetails != null)
            {
                PART_ScheckButtonWithJustIconAndDetails = new SystemCheckerButtonDetails();
                ((Button)PART_ScheckButtonWithJustIconAndDetails).Click += OnBtActionBarClick;
                try
                {
                    ui.AddElementInCustomTheme(PART_ScheckButtonWithJustIconAndDetails, "PART_ScheckButtonWithJustIconAndDetails");
                    ListCustomElements.Add(new CustomElement { ParentElementName = "PART_ScheckButtonWithJustIconAndDetails", Element = PART_ScheckButtonWithJustIconAndDetails });
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, "CheckLocalizations", "Error on AddCustomElements()");
                }
            }
        }

        public override void RefreshCustomElements()
        {
            foreach (CustomElement customElement in ListCustomElements)
            {
                customElement.Element.Visibility = Visibility.Visible;

                if (customElement.Element is SystemCheckerButtonDetails)
                {
                    ((SystemCheckerButtonDetails)customElement.Element).SetData(CheckMinimum, CheckRecommanded, DefaultBtForeground);
                }
            }
        }
        #endregion
    }
}
