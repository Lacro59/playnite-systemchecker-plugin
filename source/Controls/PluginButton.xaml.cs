using CommonPluginsShared;
using CommonPluginsShared.Collections;
using CommonPluginsShared.Controls;
using CommonPluginsShared.Interfaces;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using SystemChecker.Models;
using SystemChecker.Services;
using SystemChecker.Views;

namespace SystemChecker.Controls
{
    /// <summary>
    /// Logique d'interaction pour PluginButton.xaml
    /// </summary>
    public partial class PluginButton : PluginUserControlExtend
    {
        private SystemCheckerDatabase PluginDatabase = SystemChecker.PluginDatabase;
        internal override IPluginDatabase _PluginDatabase
        {
            get
            {
                return PluginDatabase;
            }
            set
            {
                PluginDatabase = (SystemCheckerDatabase)_PluginDatabase;
            }
        }

        public PluginButtonDataContext ControlDataContext = new PluginButtonDataContext();
        internal override IDataContext _ControlDataContext
        {
            get
            {
                return ControlDataContext;
            }
            set
            {
                ControlDataContext = (PluginButtonDataContext)_ControlDataContext;
            }
        }


        private readonly string IconOk = "\uea50";
        private readonly string IconKo = "\uea52";
        private readonly string IconMinimum = "\uea51";
        private readonly string IconEmpty = "\uea53";


        public PluginButton()
        {
            InitializeComponent();
            this.DataContext = ControlDataContext;

            Task.Run(() =>
            {
                // Wait extension database are loaded
                System.Threading.SpinWait.SpinUntil(() => PluginDatabase.IsLoaded, -1);

                this.Dispatcher.BeginInvoke((Action)delegate
                {
                    PluginDatabase.PluginSettings.PropertyChanged += PluginSettings_PropertyChanged;
                    PluginDatabase.Database.ItemUpdated += Database_ItemUpdated;
                    PluginDatabase.Database.ItemCollectionChanged += Database_ItemCollectionChanged;
                    PluginDatabase.PlayniteApi.Database.Games.ItemUpdated += Games_ItemUpdated;

                    // Apply settings
                    PluginSettings_PropertyChanged(null, null);
                });
            });
        }


        public override void SetDefaultDataContext()
        {
            ControlDataContext.IsActivated = PluginDatabase.PluginSettings.Settings.EnableIntegrationButton;
            ControlDataContext.DisplayDetails = PluginDatabase.PluginSettings.Settings.EnableIntegrationButtonDetails;

            ControlDataContext.Text = IconEmpty;
        }


        public override void SetData(Game newContext, PluginDataBaseGameBase PluginGameData)
        {
            GameRequierements gameRequierements = (GameRequierements)PluginGameData;

            if (ControlDataContext.DisplayDetails)
            {
                SystemConfiguration systemConfiguration = PluginDatabase.Database.PC;
                Requirement systemMinimum = gameRequierements.GetMinimum();
                Requirement systemRecommanded = gameRequierements.GetRecommanded();

                CheckSystem CheckMinimum = CheckMinimum = SystemApi.CheckConfig(newContext, systemMinimum, systemConfiguration, newContext.IsInstalled);
                CheckSystem CheckRecommanded = SystemApi.CheckConfig(newContext, systemRecommanded, systemConfiguration, newContext.IsInstalled);

                if (systemMinimum.HasData)
                {
                    if (!(bool)CheckMinimum.AllOk)
                    {
                        ControlDataContext.Text = IconKo;
                    }
                    else if ((bool)CheckMinimum.AllOk)
                    {
                        ControlDataContext.Text = IconMinimum;

                        if (!systemRecommanded.HasData)
                        {
                            ControlDataContext.Text = IconOk;
                        }
                    }
                }

                if (systemRecommanded.HasData && (bool)CheckRecommanded.AllOk)
                {
                    ControlDataContext.Text = IconOk;
                }
            }
        }

        #region Events
        private void PART_PluginButton_Click(object sender, RoutedEventArgs e)
        {
            var ViewExtension = new SystemCheckerGameView(PluginDatabase.PlayniteApi, PluginDatabase.Paths.PluginUserDataPath, PluginDatabase.GameContext);
            Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(PluginDatabase.PlayniteApi, "SystemChecker", ViewExtension);
            windowExtension.ShowDialog();
        }
        #endregion
    }


    public class PluginButtonDataContext : ObservableObject, IDataContext
    {
        private bool _IsActivated;
        public bool IsActivated { get => _IsActivated; set => SetValue(ref _IsActivated, value); }

        public bool _DisplayDetails;
        public bool DisplayDetails { get => _DisplayDetails; set => SetValue(ref _DisplayDetails, value); }

        public string _Text;
        public string Text { get => _Text; set => SetValue(ref _Text, value); }
    }
}
