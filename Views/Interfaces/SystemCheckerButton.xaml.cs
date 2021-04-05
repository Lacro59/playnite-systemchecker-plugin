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
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using SystemChecker.Services;

namespace SystemChecker.Views.Interfaces
{
    /// <summary>
    /// Logique d'interaction pour SystemCheckerButton.xaml
    /// </summary>
    public partial class SystemCheckerButton : Button
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private SystemCheckerDatabase PluginDatabase = SystemChecker.PluginDatabase;


        public SystemCheckerButton()
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
