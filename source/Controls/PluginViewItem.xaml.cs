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
using System.Windows.Media;
using SystemChecker.Models;
using SystemChecker.Services;

namespace SystemChecker.Controls
{
    /// <summary>
    /// Logique d'interaction pour PluginViewItem.xaml
    /// </summary>
    public partial class PluginViewItem : PluginUserControlExtend
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

        private PluginViewItemDataContext ControlDataContext = new PluginViewItemDataContext();
        internal override IDataContext _ControlDataContext
        {
            get
            {
                return ControlDataContext;
            }
            set
            {
                ControlDataContext = (PluginViewItemDataContext)_ControlDataContext;
            }
        }

        private readonly string IconOk = "\uea50";
        private readonly string IconKo = "\uea52";
        private readonly string IconMinimum = "\uea51";
        private readonly string IconEmpty = "\uea53";


        public PluginViewItem()
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
            ControlDataContext.IsActivated = PluginDatabase.PluginSettings.Settings.EnableIntegrationViewItem;

            ControlDataContext.Text = IconEmpty;
            ControlDataContext.Foreground = (SolidColorBrush)resources.GetResource("GlyphBrush");
        }


        public override void SetData(Game newContext, PluginDataBaseGameBase PluginGameData)
        {
            GameRequierements gameRequierements = (GameRequierements)PluginGameData;

            SystemConfiguration systemConfiguration = PluginDatabase.Database.PC;
            Requirement systemMinimum = gameRequierements.GetMinimum();
            Requirement systemRecommanded = gameRequierements.GetRecommanded();

            CheckSystem CheckMinimum = CheckMinimum = SystemApi.CheckConfig(newContext, systemMinimum, systemConfiguration, newContext.IsInstalled);
            CheckSystem CheckRecommanded = SystemApi.CheckConfig(newContext, systemRecommanded, systemConfiguration, newContext.IsInstalled);

            if (systemMinimum.HasData)
            {
                if (!(bool)CheckMinimum.AllOk)
                {
                    //ControlDataContext.Foreground = Brushes.Red;
                    ControlDataContext.Text = IconKo;
                }
                else if ((bool)CheckMinimum.AllOk)
                {
                    //ControlDataContext.Foreground = Brushes.Orange;
                    ControlDataContext.Text = IconMinimum;

                    if (!systemRecommanded.HasData)
                    {
                        //ControlDataContext.Foreground = Brushes.Green;
                        ControlDataContext.Text = IconOk;
                    }
                }
            }

            if (systemRecommanded.HasData && (bool)CheckRecommanded.AllOk)
            {
                //ControlDataContext.Foreground = Brushes.Green;
                ControlDataContext.Text = IconOk;
            }
        }
    }


    public class PluginViewItemDataContext : ObservableObject, IDataContext
    {
        private bool _IsActivated;
        public bool IsActivated { get => _IsActivated; set => SetValue(ref _IsActivated, value); }

        public string _Text;
        public string Text { get => _Text; set => SetValue(ref _Text, value); }

        public SolidColorBrush _Foreground;
        public SolidColorBrush Foreground { get => _Foreground; set => SetValue(ref _Foreground, value); }
    }
}
