using CommonPluginsShared;
using Newtonsoft.Json;
using Playnite.SDK;
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
using SystemChecker.Services;

namespace SystemChecker.Views.Interfaces
{
    /// <summary>
    /// Logique d'interaction pour SystemCheckerButtonDetails.xaml
    /// </summary>
    public partial class SystemCheckerButtonDetails : Button
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private SystemCheckerDatabase PluginDatabase = SystemChecker.PluginDatabase;


        public SystemCheckerButtonDetails()
        {
            InitializeComponent();

            PluginDatabase.PropertyChanged += OnPropertyChanged;
        }


        protected void OnPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            try
            {
                if (e.PropertyName == "GameSelectedData" || e.PropertyName == "PluginSettings")
                {
                    this.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new ThreadStart(delegate
                    {
                        //OnlyIcon.Foreground = SystemCheckerUI.DefaultBtForeground;
                        //
                        //if (PluginDatabase.GameSelectedData.HasData)
                        //{
                        //    this.Visibility = Visibility.Visible;
                        //}
                        //else
                        //{
                        //    this.Visibility = Visibility.Collapsed;
                        //    return;
                        //}
                    }));

                    this.Dispatcher.BeginInvoke(DispatcherPriority.Background, new ThreadStart(delegate
                    {
                        //CheckSystem CheckMinimum = SystemCheckerUI.CheckMinimum;
                        //CheckSystem CheckRecommanded = SystemCheckerUI.CheckRecommanded;
                        //
                        //if (CheckMinimum.AllOk != null)
                        //{
                        //    if (!(bool)CheckMinimum.AllOk)
                        //    {
                        //        OnlyIcon.Foreground = Brushes.Red;
                        //    }
                        //
                        //    if ((bool)CheckMinimum.AllOk)
                        //    {
                        //        OnlyIcon.Foreground = Brushes.Orange;
                        //        if (CheckRecommanded.AllOk == null)
                        //        {
                        //            OnlyIcon.Foreground = Brushes.Green;
                        //        }
                        //    }
                        //}
                        //if (CheckRecommanded.AllOk != null)
                        //{
                        //    if ((bool)CheckRecommanded.AllOk)
                        //    {
                        //        OnlyIcon.Foreground = Brushes.Green;
                        //    }
                        //}
                    }));
                }
                else
                {
                    this.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new ThreadStart(delegate
                    {
                        if (!PluginDatabase.IsViewOpen)
                        {
                            this.Visibility = Visibility.Collapsed;
                        }
                    }));
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
            }
        }
    }
}
