using CommonPluginsShared;
using CommonPluginsShared.Collections;
using CommonPluginsShared.Controls;
using CommonPluginsShared.Interfaces;
using Playnite.SDK;
using Playnite.SDK.Models;
using System.Collections.Generic;
using System.Windows.Media;
using SystemChecker.Models;
using SystemChecker.Services;

namespace SystemChecker.Controls
{
	/// <summary>
	/// Interaction logic for PluginViewItem.xaml
	/// </summary>
	public partial class PluginViewItem : PluginUserControlExtend
	{
		private SystemCheckerDatabase PluginDatabase => SystemChecker.PluginDatabase;
		protected override IPluginDatabase pluginDatabase => PluginDatabase;

		private PluginViewItemDataContext ControlDataContext = new PluginViewItemDataContext();
		protected override IDataContext controlDataContext
		{
			get => ControlDataContext;
			set => ControlDataContext = (PluginViewItemDataContext)value;
		}

		public PluginViewItem()
		{
#if DEBUG
			var timer = new DebugTimer("PluginViewItem.ctor");
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
		/// Shares the same plugin key as <see cref="PluginButton"/>, so
		/// <see cref="PluginUserControlExtendBase.AttachPluginEvents"/> will no-op for whichever control type loads second — preventing duplicate subscriptions.
		/// </summary>
		protected override void AttachStaticEvents()
		{
#if DEBUG
			var timer = new DebugTimer("PluginViewItem.AttachStaticEvents");
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
			var timer = new DebugTimer("PluginViewItem.SetDefaultDataContext");
#endif

			ControlDataContext.IsActivated = PluginDatabase.PluginSettings.EnableIntegrationViewItem;
			ControlDataContext.Text = PluginControlHelper.IconEmpty;
			ControlDataContext.Foreground = (SolidColorBrush)ResourceProvider.GetResource("GlyphBrush");

#if DEBUG
			timer.Stop(string.Format("IsActivated={0}", ControlDataContext.IsActivated));
#endif
		}

		/// <summary>
		/// Updates the view item icon based on the game's system requirements.
		/// At this point the game context has already been validated by <see cref="PluginUserControlExtend.UpdateDataAsync"/>.
		/// </summary>
		public override void SetData(Game newContext, PluginGameEntry pluginGameData)
		{
#if DEBUG
			var timer = new DebugTimer(string.Format("PluginViewItem.SetData(game='{0}')", newContext?.Name ?? "null"));
#endif

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
	}

	public class PluginViewItemDataContext : ObservableObject, IDataContext
	{
		private bool _isActivated;
		public bool IsActivated { get => _isActivated; set => SetValue(ref _isActivated, value); }

		private string _text;
		public string Text { get => _text; set => SetValue(ref _text, value); }

		private SolidColorBrush _foreground;
		public SolidColorBrush Foreground { get => _foreground; set => SetValue(ref _foreground, value); }
	}
}