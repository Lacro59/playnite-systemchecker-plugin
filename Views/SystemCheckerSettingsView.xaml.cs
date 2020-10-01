using Playnite.SDK;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace SystemChecker.Views
{
    public partial class SystemCheckerSettingsView : UserControl
    {
        private readonly IPlayniteAPI _PlayniteAPI;
        private static IResourceProvider resources = new ResourceProvider();

        private string _PluginUserDataPath { get; set; }


        public SystemCheckerSettingsView(IPlayniteAPI PlayniteAPI, string PluginUserDataPath)
        {
            _PlayniteAPI = PlayniteAPI;
            _PluginUserDataPath = PluginUserDataPath;

            InitializeComponent();
        }

        private void Checkbox_Click(object sender, RoutedEventArgs e)
        {
            CheckBox cb = (CheckBox)sender;

            if ((cb.Name == "Scheck_IntegrationInButton") && (bool)cb.IsChecked)
            {
                Scheck_IntegrationInButtonDetails.IsChecked = false;
            }
            if ((cb.Name == "Scheck_IntegrationInButtonDetails") && (bool)cb.IsChecked)
            {
                Scheck_IntegrationInButton.IsChecked = false;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string PluginDirectory = _PluginUserDataPath + "\\SystemChecker\\";
            if (Directory.Exists(PluginDirectory))
            {
                try
                {
                    Directory.Delete(PluginDirectory, true);
                    Directory.CreateDirectory(PluginDirectory);
                }
                catch
                {
                    _PlayniteAPI.Dialogs.ShowErrorMessage(resources.GetString("LOCSystemCheckerErrorRemove"), "SystemChecker");
                }
            }
        }
    }
}
