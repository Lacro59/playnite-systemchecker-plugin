using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace SystemChecker.Views
{
    public partial class SystemCheckerSettingsView : UserControl
    {
        private string PluginUserDataPath { get; set; }


        public SystemCheckerSettingsView(string PluginUserDataPath)
        {
            this.PluginUserDataPath = PluginUserDataPath;

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
            string PluginDirectory = PluginUserDataPath + "\\SystemChecker\\";
            if (Directory.Exists(PluginDirectory))
            {
                Directory.Delete(PluginDirectory, true);
                Directory.CreateDirectory(PluginDirectory);
            }
        }
    }
}
