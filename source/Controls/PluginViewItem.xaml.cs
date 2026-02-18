using CommonPluginsShared;
using CommonPluginsShared.Collections;
using CommonPluginsShared.Controls;
using CommonPluginsShared.Interfaces;
using CommonPluginsStores.Models;
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
			InitializeComponent();
			DataContext = ControlDataContext;
			Loaded += OnLoaded;
		}

		/// <summary>
		/// Attaches static event handlers for the SystemChecker plugin.
		/// Shares the same plugin key as <see cref="PluginButton"/>, so <see cref="AttachPluginEvents"/>
		/// will no-op for whichever control type loads second — preventing duplicate subscriptions.
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
			ControlDataContext.IsActivated = PluginDatabase.PluginSettings.Settings.EnableIntegrationViewItem;
			ControlDataContext.Text = PluginControlHelper.IconEmpty;
			ControlDataContext.Foreground = (SolidColorBrush)ResourceProvider.GetResource("GlyphBrush");
		}

		/// <summary>
		/// Updates the view item icon based on the game's system requirements.
		/// At this point the game context has already been validated by <see cref="PluginUserControlExtend.UpdateDataAsync"/>.
		/// </summary>
		public override void SetData(Game newContext, PluginDataBaseGameBase pluginGameData)
		{
			string newIcon = PluginControlHelper.ResolveIcon(newContext, pluginGameData, PluginDatabase);
			if (newIcon != null && ControlDataContext.Text != newIcon)
			{
				ControlDataContext.Text = newIcon;
			}
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