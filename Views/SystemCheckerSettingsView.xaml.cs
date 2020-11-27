using Playnite.SDK;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using SystemChecker.Clients;
using SystemChecker.Services;

namespace SystemChecker.Views
{
    public partial class SystemCheckerSettingsView : UserControl
    {
        private static IResourceProvider resources = new ResourceProvider();

        private SystemCheckerDatabase PluginDatabase = SystemChecker.PluginDatabase;


        public SystemCheckerSettingsView()
        {
            InitializeComponent();
        }

        private void Checkbox_Click(object sender, RoutedEventArgs e)
        {
            CheckBox cb = (CheckBox)sender;

            if ((cb.Name == "Scheck_IntegrationInButton") && (bool)cb.IsChecked)
            {
                Scheck_IntegrationInButtonDetails.IsChecked = false;
                Scheck_IntegrationInCustomTheme.IsChecked = false;
            }
            if ((cb.Name == "Scheck_IntegrationInButtonDetails") && (bool)cb.IsChecked)
            {
                Scheck_IntegrationInButton.IsChecked = false;
                Scheck_IntegrationInCustomTheme.IsChecked = false;
            }

            if ((cb.Name == "Scheck_IntegrationInCustomTheme") && (bool)cb.IsChecked)
            {
                Scheck_IntegrationInButton.IsChecked = false;
                Scheck_IntegrationInButtonDetails.IsChecked = false;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            PluginDatabase.ClearDatabase();
        }
    }
}
