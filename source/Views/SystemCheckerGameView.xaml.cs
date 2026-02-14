using Playnite.SDK.Models;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using SystemChecker.ViewModels;

namespace SystemChecker.Views
{
    public partial class SystemCheckerGameView : UserControl
    {
        private SystemCheckerGameViewModel _viewModel;

        public SystemCheckerGameView(Game gameSelected)
        {
            InitializeComponent();

            _viewModel = new SystemCheckerGameViewModel(gameSelected);
            DataContext = _viewModel;
        }

        public SystemCheckerGameViewModel ViewModel
        {
            get { return _viewModel; }
        }

        private void PART_SourceLink_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_viewModel.SourceUrl))
            {
                try
                {
                    Process.Start(_viewModel.SourceUrl);
                }
                catch (Exception ex)
                {
                    // Log error if needed
                }
            }
        }
    }
}