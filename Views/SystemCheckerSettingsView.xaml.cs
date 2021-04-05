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
        

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            PluginDatabase.ClearDatabase();
        }
    }
}
