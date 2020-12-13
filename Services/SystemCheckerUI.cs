using Newtonsoft.Json;
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
using System.Windows.Threading;
using SystemChecker.Clients;
using SystemChecker.Models;
using SystemChecker.Views;
using SystemChecker.Views.Interfaces;

namespace SystemChecker.Services
{
    public class SystemCheckerUI : PlayniteUiHelper
    {
        private SystemCheckerDatabase PluginDatabase = SystemChecker.PluginDatabase;

        public override string _PluginUserDataPath { get; set; } = string.Empty;

        public override bool IsFirstLoad { get; set; } = true;

        public override string BtActionBarName { get; set; } = string.Empty;
        public override FrameworkElement PART_BtActionBar { get; set; }

        public override string SpDescriptionName { get; set; } = string.Empty;
        public override FrameworkElement PART_SpDescription { get; set; }


        public override string SpInfoBarFSName { get; set; } = string.Empty;
        public override FrameworkElement PART_SpInfoBarFS { get; set; }

        public override string BtActionBarFSName { get; set; } = string.Empty;
        public override FrameworkElement PART_BtActionBarFS { get; set; }


        public override List<CustomElement> ListCustomElements { get; set; } = new List<CustomElement>();


        public static Brush DefaultBtForeground;

        public static CheckSystem CheckMinimum = new CheckSystem();
        public static CheckSystem CheckRecommanded = new CheckSystem();


        public SystemCheckerUI(IPlayniteAPI PlayniteApi, SystemCheckerSettings Settings, string PluginUserDataPath) : base(PlayniteApi, PluginUserDataPath)
        {
            _PluginUserDataPath = PluginUserDataPath;

            BtActionBarName = "PART_BtActionBar";
        }


        public override void Initial()
        {


        }

        public override DispatcherOperation AddElements()
        {
            if (_PlayniteApi.ApplicationInfo.Mode == ApplicationMode.Desktop)
            {
                if (IsFirstLoad)
                {
#if DEBUG
                    logger.Debug($"SystemChecker - IsFirstLoad");
#endif
                    Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new ThreadStart(delegate
                    {
                        System.Threading.SpinWait.SpinUntil(() => IntegrationUI.SearchElementByName("PART_HtmlDescription") != null, 5000);
                    })).Wait();
                    IsFirstLoad = false;
                }


                return Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new ThreadStart(delegate
                {
                    CheckTypeView();

                    if (PluginDatabase.PluginSettings.EnableIntegrationButton || PluginDatabase.PluginSettings.EnableIntegrationButtonDetails)
                    {
#if DEBUG
                        logger.Debug($"SystemChecker - AddBtActionBar()");
#endif
                        AddBtActionBar();
                    }

                    if (PluginDatabase.PluginSettings.EnableIntegrationInCustomTheme)
                    {
#if DEBUG
                        logger.Debug($"SystemChecker - AddCustomElements()");
#endif
                        AddCustomElements();
                    }
                }));
            }

            return null;
        }

        public override void RefreshElements(Game GameSelected, bool force = false)
        {
            CancellationTokenSource tokenSource = new CancellationTokenSource();
            CancellationToken ct = tokenSource.Token;

            Task TaskRefresh = Task.Run(() =>
            {
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
                        if (!PluginDatabase.IsLoaded)
                        {
                            return;
                        }
                        GameRequierements gameRequierements = PluginDatabase.Get(GameSelected);

                        SystemConfiguration systemConfiguration = PluginDatabase.Database.PC;


                        CheckMinimum = new CheckSystem();
                        CheckRecommanded = new CheckSystem();
                        resourcesLists = new List<ResourcesList>();
                        if (gameRequierements.GetMinimum().HasData)
                        {
                            CheckMinimum = SystemApi.CheckConfig(gameRequierements.GetMinimum(), systemConfiguration);
#if DEBUG
                            logger.Debug($"SystemChecker - CheckMinimum: {JsonConvert.SerializeObject(CheckMinimum)}");
#endif
                            resourcesLists.Add(new ResourcesList { Key = "Scheck_HasData", Value = true });
                            resourcesLists.Add(new ResourcesList { Key = "Scheck_IsMinimumOK", Value = CheckMinimum.AllOk });
                            resourcesLists.Add(new ResourcesList { Key = "Scheck_IsAllOK", Value = CheckMinimum.AllOk });
                        }
                        if (gameRequierements.GetRecommanded().HasData)
                        {
                            CheckRecommanded = SystemApi.CheckConfig(gameRequierements.GetRecommanded(), systemConfiguration);
#if DEBUG
                            logger.Debug($"SystemChecker - CheckRecommanded: {JsonConvert.SerializeObject(CheckRecommanded)}");
#endif
                            resourcesLists.Add(new ResourcesList { Key = "Scheck_HasData", Value = true });
                            resourcesLists.Add(new ResourcesList { Key = "Scheck_IsRecommandedOK", Value = CheckRecommanded.AllOk });
                            resourcesLists.Add(new ResourcesList { Key = "Scheck_IsAllOK", Value = CheckRecommanded.AllOk });
                        }

                        // If not cancel, show
                        if (!ct.IsCancellationRequested && GameSelected.Id == SystemChecker.GameSelected.Id)
                        {
                            ui.AddResources(resourcesLists);

                            if (_PlayniteApi.ApplicationInfo.Mode == ApplicationMode.Desktop)
                            {
                                PluginDatabase.SetCurrent(gameRequierements);
                            }
                        }
                    }
                    else
                    {
                        logger.Info($"SystemChecker - No treatment for emulated game");
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, "SystemChecker", $"Error on TaskRefreshBtActionBar()");
                }
            }, ct);

            taskHelper.Add(TaskRefresh, tokenSource);
        }


        #region BtActionBar
        public override void InitialBtActionBar()
        {

        }

        public override void AddBtActionBar()
        {
            CheckTypeView();

            if (PART_BtActionBar != null)
            {
#if DEBUG
                logger.Debug($"SystemChecker - PART_BtActionBar allready insert");
#endif
                return;
            }

            Button BtActionBar = new Button();

            if (PluginDatabase.PluginSettings.EnableIntegrationButton)
            {
                BtActionBar = new SystemCheckerButton();
            }

            if (PluginDatabase.PluginSettings.EnableIntegrationButtonDetails)
            {
                BtActionBar = new SystemCheckerButtonDetails();
            }

            BtActionBar.Click += OnBtActionBarClick;
            BtActionBar.Name = BtActionBarName;
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

        }


        public void OnBtActionBarClick(object sender, RoutedEventArgs e)
        {
            if (SystemChecker.GameSelected != null)
            {
                PluginDatabase.IsViewOpen = true;
                var ViewExtension = new SystemCheckerGameView(_PlayniteApi, _PluginUserDataPath, SystemChecker.GameSelected);
                Window windowExtension = CreateExtensionWindow(_PlayniteApi, "SystemChecker", ViewExtension);
                windowExtension.ShowDialog();
                PluginDatabase.IsViewOpen = false;
            }
            else
            {
                _PlayniteApi.Dialogs.ShowErrorMessage("No game selected for show extension view.", "SystemChecker");
            }
        }

        public void OnCustomThemeButtonClick(object sender, RoutedEventArgs e)
        {
            if (PluginDatabase.PluginSettings.EnableIntegrationInCustomTheme)
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


        #region SpDescription
        public override void InitialSpDescription()
        {
        }

        public override void AddSpDescription()
        {
        }

        public override void RefreshSpDescription()
        {
        }
        #endregion  


        #region CustomElements
        public override void InitialCustomElements()
        {

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
                PART_ScheckButtonWithJustIcon = IntegrationUI.SearchElementByName("PART_ScheckButtonWithJustIcon", false, true);
                PART_ScheckButtonWithJustIconAndDetails = IntegrationUI.SearchElementByName("PART_ScheckButtonWithJustIconAndDetails", false, true);
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
                    Common.LogError(ex, "SystemChecker", "Error on AddCustomElements()");
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
                    Common.LogError(ex, "SystemChecker", "Error on AddCustomElements()");
                }
            }
        }

        public override void RefreshCustomElements()
        {

        }
        #endregion




        public override DispatcherOperation AddElementsFS()
        {
            if (_PlayniteApi.ApplicationInfo.Mode == ApplicationMode.Fullscreen)
            {
                if (IsFirstLoad)
                {
#if DEBUG
                    logger.Debug($"CheckLocalizations - IsFirstLoad");
#endif
                    Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new ThreadStart(delegate
                    {
                        System.Threading.SpinWait.SpinUntil(() => IntegrationUI.SearchElementByName("PART_ButtonContext") != null, 5000);
                    })).Wait();
                    IsFirstLoad = false;
                }

                return Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new ThreadStart(delegate
                {
                    if (PluginDatabase.PluginSettings.EnableIntegrationFS)
                    {
#if DEBUG
                        logger.Debug($"CheckLocalizations - AddBtInfoBarFS()");
#endif
                        AddSpInfoBarFS();
                        AddSpInfoBarFS();
                    }
                }));
            }

            return null;
        }


        #region SpInfoBarFS
        public override void InitialSpInfoBarFS()
        {

        }

        public override void AddSpInfoBarFS()
        {

        }

        public override void RefreshSpInfoBarFS()
        {

        }
        #endregion


        #region BtActionBarFS
        public override void InitialBtActionBarFS()
        {

        }

        public override void AddBtActionBarFS()
        {

        }

        public override void RefreshBtActionBarFS()
        {

        }
        #endregion
    }
}
