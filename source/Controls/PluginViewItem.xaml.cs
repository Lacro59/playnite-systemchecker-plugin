using CommonPluginsShared;
using CommonPluginsShared.Collections;
using CommonPluginsShared.Controls;
using CommonPluginsShared.Interfaces;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
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
        internal override IPluginDatabase pluginDatabase => PluginDatabase;

        private PluginViewItemDataContext ControlDataContext = new PluginViewItemDataContext();
        internal override IDataContext controlDataContext
        {
            get => ControlDataContext;           
            set => ControlDataContext = (PluginViewItemDataContext)controlDataContext;
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
                _ = System.Threading.SpinWait.SpinUntil(() => PluginDatabase.IsLoaded, -1);

                this.Dispatcher.BeginInvoke((Action)delegate
                {
                    PluginDatabase.PluginSettings.PropertyChanged += PluginSettings_PropertyChanged;
                    PluginDatabase.Database.ItemUpdated += Database_ItemUpdated;
                    PluginDatabase.Database.ItemCollectionChanged += Database_ItemCollectionChanged;
                    API.Instance.Database.Games.ItemUpdated += Games_ItemUpdated;

                    // Apply settings
                    PluginSettings_PropertyChanged(null, null);
                });
            });
        }


        public override void SetDefaultDataContext()
        {
            ControlDataContext.IsActivated = PluginDatabase.PluginSettings.Settings.EnableIntegrationViewItem;

            ControlDataContext.Text = IconEmpty;
            ControlDataContext.Foreground = (SolidColorBrush)ResourceProvider.GetResource("GlyphBrush");
        }


        public override void SetData(Game newContext, PluginDataBaseGameBase PluginGameData)
        {
            GameRequierements gameRequierements = (GameRequierements)PluginGameData;

            SystemConfiguration systemConfiguration = PluginDatabase.Database.PC;
            Requirement systemMinimum = gameRequierements.GetMinimum();
            Requirement systemRecommanded = gameRequierements.GetRecommanded();

            CheckSystem CheckMinimum = SystemApi.CheckConfig(newContext, systemMinimum, systemConfiguration, newContext.IsInstalled);
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

            if (!systemMinimum.HasData && systemRecommanded.HasData && !(bool)CheckRecommanded?.AllOk)
            {
                ControlDataContext.Text = IconKo;
            }
        }
    }


    public class PluginViewItemDataContext : ObservableObject, IDataContext
    {
        private bool _isActivated;
        public bool IsActivated { get => _isActivated; set => SetValue(ref _isActivated, value); }

        public string _text;
        public string Text { get => _text; set => SetValue(ref _text, value); }

        public SolidColorBrush _foreground;
        public SolidColorBrush Foreground { get => _foreground; set => SetValue(ref _foreground, value); }
    }
}
