using CommonPluginsShared;
using CommonPluginsShared.Collections;
using CommonPluginsShared.Controls;
using CommonPluginsShared.Interfaces;
using Playnite.SDK.Models;
using System.Collections.Generic;
using System.Windows;
using SystemChecker.Models;
using SystemChecker.Services;

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
#if DEBUG
			var timer = new DebugTimer("PluginButton.ctor");
#endif

			InitializeComponent();

#if DEBUG
			timer.Step("InitializeComponent done");
#endif

			DataContext = ControlDataContext;
			Loaded += OnLoaded;

#if DEBUG
			timer.Stop();
#endif
		}

		/// <summary>
		/// Attaches static event handlers for the SystemChecker plugin.
		/// Plugin-specific handlers are guarded by <see cref="PluginUserControlExtendBase.AttachPluginEvents"/> to prevent
		/// double-subscription when multiple instances of this control exist simultaneously.
		/// </summary>
		protected override void AttachStaticEvents()
		{
#if DEBUG
			var timer = new DebugTimer("PluginButton.AttachStaticEvents");
#endif

			base.AttachStaticEvents();

#if DEBUG
			timer.Step("base done");
#endif

			AttachPluginEvents(PluginDatabase.PluginName, () =>
			{
#if DEBUG
				timer.Step("registering plugin-specific handlers");
#endif

				PluginDatabase.PluginSettings.PropertyChanged += CreatePluginSettingsHandler(); 
				PluginDatabase.DatabaseItemUpdated += CreateDatabaseItemUpdatedHandler<PluginGameRequirements>();
				PluginDatabase.DatabaseItemCollectionChanged += CreateDatabaseCollectionChangedHandler<PluginGameRequirements>();
			});

#if DEBUG
			timer.Stop();
#endif
		}

		public override void SetDefaultDataContext()
		{
#if DEBUG
			var timer = new DebugTimer("PluginButton.SetDefaultDataContext");
#endif

			ControlDataContext.IsActivated = PluginDatabase.PluginSettings.EnableIntegrationButton;
			ControlDataContext.DisplayDetails = PluginDatabase.PluginSettings.EnableIntegrationButtonDetails;
			ControlDataContext.Text = PluginControlHelper.IconEmpty;

#if DEBUG
			timer.Stop(string.Format("IsActivated={0}, DisplayDetails={1}", ControlDataContext.IsActivated, ControlDataContext.DisplayDetails));
#endif
		}

		/// <summary>
		/// Updates the button icon based on the game's system requirements.
		/// At this point the game context has already been validated by <see cref="PluginUserControlExtend.UpdateDataAsync"/>.
		/// </summary>
		public override void SetData(Game newContext, PluginGameEntry pluginGameData)
		{
#if DEBUG
			var timer = new DebugTimer(string.Format("PluginButton.SetData(game='{0}')", newContext?.Name ?? "null"));
#endif

			if (!ControlDataContext.DisplayDetails)
			{
#if DEBUG
				timer.Stop("DisplayDetails=false, skip");
#endif
				return;
			}

			string newIcon = PluginControlHelper.ResolveIcon(newContext, pluginGameData, PluginDatabase);

#if DEBUG
			timer.Step(string.Format("ResolveIcon done, icon='{0}'", newIcon));
#endif

			if (newIcon != null && ControlDataContext.Text != newIcon)
			{
				ControlDataContext.Text = newIcon;
			}

#if DEBUG
			timer.Stop();
#endif
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