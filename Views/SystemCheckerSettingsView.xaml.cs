using System.Windows;
using System.Windows.Controls;

namespace SystemChecker.Views
{
    public partial class SystemCheckerSettingsView : UserControl
    {
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
            }
            if ((cb.Name == "Scheck_IntegrationInButtonDetails") && (bool)cb.IsChecked)
            {
                Scheck_IntegrationInButton.IsChecked = false;
            }
        }
    }
}
