using CommonPluginsShared;
using CommonPluginsShared.Collections;
using CommonPluginsShared.Controls;
using CommonPluginsShared.Interfaces;
using CommonPluginsStores.Models;
using Playnite.SDK;
using Playnite.SDK.Models;
using System.Collections.Generic;
using System.Windows;
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
		/// Attaches static event handlers for the SystemChecker plugin.
		/// Plugin-specific handlers are guarded by <see cref="AttachPluginEvents"/> to prevent
		/// double-subscription when multiple instances of this control exist simultaneously.
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

		/// <summary>
		/// Updates the button icon based on the game's system requirements.
		/// At this point the game context has already been validated by <see cref="PluginUserControlExtend.UpdateDataAsync"/>.
		/// </summary>
		public override void SetData(Game newContext, PluginDataBaseGameBase pluginGameData)
		{
			if (!ControlDataContext.DisplayDetails)
			{
				return;
			}

			string newIcon = PluginControlHelper.ResolveIcon(newContext, pluginGameData, PluginDatabase);
			if (newIcon != null && ControlDataContext.Text != newIcon)
			{
				ControlDataContext.Text = newIcon;
			}
		}

		#region Events

		private void PART_PluginButton_Click(object sender, RoutedEventArgs e)
		{
			PluginDatabase.PluginWindows.ShowPluginGameDataWindow(CurrentGame);
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