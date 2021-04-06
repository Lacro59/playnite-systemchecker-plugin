using CommonPluginsShared.Collections;
using CommonPluginsShared.Controls;
using CommonPluginsShared.Interfaces;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
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

        private PluginViewItemDataContext ControlDataContext;
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
            ControlDataContext = new PluginViewItemDataContext
            {
                IsActivated = PluginDatabase.PluginSettings.Settings.EnableIntegrationViewItem,

                Text = IconEmpty,
                Foreground = (SolidColorBrush)resources.GetResource("GlyphBrush")
            };
        }


        public override Task<bool> SetData(Game newContext, PluginDataBaseGameBase PluginGameData)
        {
            return Task.Run(() =>
            {
                GameRequierements gameRequierements = (GameRequierements)PluginGameData;

                SystemConfiguration systemConfiguration = PluginDatabase.Database.PC;
                Requirement systemMinimum = gameRequierements.GetMinimum();
                Requirement systemRecommanded = gameRequierements.GetRecommanded();

                CheckSystem CheckMinimum = CheckMinimum = SystemApi.CheckConfig(systemMinimum, systemConfiguration);
                CheckSystem CheckRecommanded = SystemApi.CheckConfig(systemRecommanded, systemConfiguration);

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

                this.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new ThreadStart(delegate
                {
                    this.DataContext = ControlDataContext;
                }));

                return true;
            });
        }
    }


    public class PluginViewItemDataContext : IDataContext
    {
        public bool IsActivated { get; set; }

        public string Text { get; set; }
        public SolidColorBrush Foreground { get; set; }
    }
}
