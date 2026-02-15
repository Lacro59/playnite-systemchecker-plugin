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
using System.Windows.Threading;
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
			DataContext = ControlDataContext;
			Loaded += OnLoaded;
		}

		/// <summary>
		/// Attaches static event handlers specific to SystemChecker plugin.
		/// Uses AttachPluginEvents to ensure handlers are attached only once globally.
		/// Shares the same event handlers as PluginButton since they use the same plugin key.
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
			ControlDataContext.Text = IconEmpty;
			ControlDataContext.Foreground = (SolidColorBrush)ResourceProvider.GetResource("GlyphBrush");
		}

		public override void SetData(Game newContext, PluginDataBaseGameBase PluginGameData)
		{
			PluginGameRequirements pluginGameRequirements = (PluginGameRequirements)PluginGameData;
			SystemConfiguration systemConfiguration = PluginDatabase.Database.PC;

			if (systemConfiguration == null)
			{
				return;
			}

			RequirementEntry systemMinimum = pluginGameRequirements.GetMinimum();
			RequirementEntry systemRecommended = pluginGameRequirements.GetRecommended();

			bool hasMinimum = systemMinimum.HasData;
			bool hasRecommended = systemRecommended.HasData;

			if (!hasMinimum && !hasRecommended)
			{
				return;
			}
			
			CheckSystem checkMinimum = hasMinimum
				? SystemApi.CheckConfig(newContext, systemMinimum, systemConfiguration, newContext.IsInstalled)
				: null;
			
			CheckSystem checkRecommended = hasRecommended
				? SystemApi.CheckConfig(newContext, systemRecommended, systemConfiguration, newContext.IsInstalled)
				: null;

			string newIcon = DetermineIcon(hasMinimum, hasRecommended, checkMinimum, checkRecommended);

			if (GameContext?.Id == CurrentGame.Id && ControlDataContext.Text != newIcon)
			{
				ControlDataContext.Text = newIcon;
			}
		}

		/// <summary>
		/// Determines which icon to display based on system requirements check results
		/// </summary>
		private string DetermineIcon(bool hasMinimum, bool hasRecommended, CheckSystem checkMinimum, CheckSystem checkRecommended)
		{
			if (hasMinimum)
			{
				if (!(checkMinimum?.AllOk ?? false))
				{
					return IconKo;
				}

				if (hasRecommended)
				{
					return (checkRecommended?.AllOk ?? false) ? IconOk : IconMinimum;
				}

				return IconOk;
			}

			if (hasRecommended)
			{
				return (checkRecommended?.AllOk ?? false) ? IconOk : IconKo;
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