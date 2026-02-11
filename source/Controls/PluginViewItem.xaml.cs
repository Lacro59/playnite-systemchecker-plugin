using CommonPluginsShared;
using CommonPluginsShared.Collections;
using CommonPluginsShared.Controls;
using CommonPluginsShared.Interfaces;
using CommonPluginsStores.Models;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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

		private readonly string IconOk = "\uea50";
		private readonly string IconKo = "\uea52";
		private readonly string IconMinimum = "\uea51";
		private readonly string IconEmpty = "\uea53";

		public PluginViewItem()
		{
			InitializeComponent();
			this.DataContext = ControlDataContext;

			_ = Task.Run(() =>
			{
				_ = System.Threading.SpinWait.SpinUntil(() => PluginDatabase.IsLoaded, -1);

				this.Dispatcher.BeginInvoke((Action)delegate
				{
					InitializeStaticEvents();
					PluginSettings_PropertyChanged(null, null);
				});
			});
		}

		#region StaticEvents

		/// <summary>
		/// Attaches static event handlers specific to SystemChecker plugin.
		/// Shares the same event handlers as PluginButton to avoid duplication.
		/// </summary>
		protected override void AttachStaticEvents()
		{
			base.AttachStaticEvents();

			PluginDatabase.PluginSettings.PropertyChanged += OnStaticPluginSettingsChanged;
			PluginDatabase.Database.ItemUpdated += OnStaticDatabaseItemUpdated;
			PluginDatabase.Database.ItemCollectionChanged += OnStaticDatabaseCollectionChanged;
		}

		/// <summary>
		/// Static event handler for plugin settings changes
		/// </summary>
		private static void OnStaticPluginSettingsChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			NotifyAllInstances(instance =>
			{
				if (instance is PluginViewItem viewItem)
				{
					viewItem.PluginSettings_PropertyChanged(sender, e);
				}
			});
		}

		/// <summary>
		/// Static event handler for database item updates with generic type handling
		/// </summary>
		private static void OnStaticDatabaseItemUpdated(object sender, object e)
		{
			NotifyAllInstances(instance =>
			{
				if (instance is PluginViewItem viewItem)
				{
					viewItem.HandleDatabaseItemUpdated(sender, e);
				}
			});
		}

		/// <summary>
		/// Static event handler for database collection changes with generic type handling
		/// </summary>
		private static void OnStaticDatabaseCollectionChanged(object sender, object e)
		{
			NotifyAllInstances(instance =>
			{
				if (instance is PluginViewItem viewItem)
				{
					viewItem.HandleDatabaseCollectionChanged(sender, e);
				}
			});
		}

		#endregion

		public override void SetDefaultDataContext()
		{
			ControlDataContext.IsActivated = PluginDatabase.PluginSettings.Settings.EnableIntegrationViewItem;
			ControlDataContext.Text = IconEmpty;
			ControlDataContext.Foreground = (SolidColorBrush)ResourceProvider.GetResource("GlyphBrush");
		}

		public override void SetData(Game newContext, PluginDataBaseGameBase PluginGameData)
		{
			PluginGameRequierements gameRequierements = (PluginGameRequierements)PluginGameData;
			SystemConfiguration systemConfiguration = PluginDatabase.Database.PC;

			if (systemConfiguration == null)
			{
				return;
			}

			RequirementEntry systemMinimum = gameRequierements.GetMinimum();
			RequirementEntry systemRecommanded = gameRequierements.GetRecommanded();

			bool hasMinimum = systemMinimum.HasData;
			bool hasRecommanded = systemRecommanded.HasData;

			if (!hasMinimum && !hasRecommanded)
			{
				return;
			}

			CheckSystem checkMinimum = hasMinimum
				? SystemApi.CheckConfig(newContext, systemMinimum, systemConfiguration, newContext.IsInstalled)
				: null;

			CheckSystem checkRecommanded = hasRecommanded
				? SystemApi.CheckConfig(newContext, systemRecommanded, systemConfiguration, newContext.IsInstalled)
				: null;

			string newIcon = DetermineIcon(hasMinimum, hasRecommanded, checkMinimum, checkRecommanded);

			if (ControlDataContext.Text != newIcon)
			{
				ControlDataContext.Text = newIcon;
			}
		}

		/// <summary>
		/// Determines which icon to display based on system requirements check results
		/// </summary>
		private string DetermineIcon(bool hasMinimum, bool hasRecommanded, CheckSystem checkMinimum, CheckSystem checkRecommanded)
		{
			if (hasMinimum)
			{
				if (!(checkMinimum?.AllOk ?? false))
				{
					return IconKo;
				}

				if (hasRecommanded)
				{
					return (checkRecommanded?.AllOk ?? false) ? IconOk : IconMinimum;
				}

				return IconOk;
			}

			if (hasRecommanded)
			{
				return (checkRecommanded?.AllOk ?? false) ? IconOk : IconKo;
			}

			return IconEmpty;
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