using CommonPluginsShared;
using CommonPluginsShared.Collections;
using CommonPluginsShared.Controls;
using CommonPluginsShared.Interfaces;
using CommonPluginsShared.SystemInfo;
using CommonPluginsStores.Models;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using SystemChecker.Models;
using SystemChecker.Services;
using SystemChecker.Views;

namespace SystemChecker.Controls
{
	/// <summary>
	/// Interaction logic for PluginButton.xaml
	/// </summary>
	public partial class PluginButton : PluginUserControlExtend
	{
		private SystemCheckerDatabase PluginDatabase => SystemChecker.PluginDatabase;
		protected override IPluginDatabase pluginDatabase => PluginDatabase;

		public PluginButtonDataContext ControlDataContext = new PluginButtonDataContext();
		protected override IDataContext controlDataContext
		{
			get => ControlDataContext;
			set => ControlDataContext = (PluginButtonDataContext)value;
		}

		public PluginButton()
		{
			InitializeComponent();
			DataContext = ControlDataContext;
			Loaded += OnLoaded;
		}

		/// <summary>
		/// Attaches static event handlers specific to SystemChecker plugin.
		/// Uses AttachPluginEvents to ensure handlers are attached only once globally.
		/// </summary>
		protected override void AttachStaticEvents()
		{
			base.AttachStaticEvents();

			AttachPluginEvents(PluginDatabase.PluginName, () =>
			{
				PluginDatabase.PluginSettings.PropertyChanged += CreatePluginSettingsHandler();
				PluginDatabase.Database.ItemUpdated += CreateDatabaseItemUpdatedHandler<PluginGameRequirements>();
				PluginDatabase.Database.ItemCollectionChanged += CreateDatabaseCollectionChangedHandler<PluginGameRequirements>();
			});
		}

		public override void SetDefaultDataContext()
		{
			ControlDataContext.IsActivated = PluginDatabase.PluginSettings.Settings.EnableIntegrationButton;
			ControlDataContext.DisplayDetails = PluginDatabase.PluginSettings.Settings.EnableIntegrationButtonDetails;
			ControlDataContext.Text = PluginControlHelper.IconEmpty;
		}

		public override void SetData(Game newContext, PluginDataBaseGameBase pluginGameData)
		{
			if (!ControlDataContext.DisplayDetails)
			{
				return;
			}

			string newIcon = PluginControlHelper.ResolveIcon(newContext, pluginGameData, PluginDatabase);
			if (newIcon != null && GameContext?.Id == CurrentGame.Id && ControlDataContext.Text != newIcon)
			{
				ControlDataContext.Text = newIcon;
			}
		}

		#region Events

		private void PART_PluginButton_Click(object sender, RoutedEventArgs e)
		{
			PluginDatabase.WindowPluginService.ShowPluginGameDataWindow(CurrentGame);
		}

		#endregion
	}

	public class PluginButtonDataContext : ObservableObject, IDataContext
	{
		private bool _isActivated;
		public bool IsActivated { get => _isActivated; set => SetValue(ref _isActivated, value); }

		private bool _displayDetails;
		public bool DisplayDetails { get => _displayDetails; set => SetValue(ref _displayDetails, value); }

		private string _text;
		public string Text { get => _text; set => SetValue(ref _text, value); }
	}
}