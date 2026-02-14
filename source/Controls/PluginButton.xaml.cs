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

		private readonly string IconOk = "\uea50";
		private readonly string IconKo = "\uea52";
		private readonly string IconMinimum = "\uea51";
		private readonly string IconEmpty = "\uea53";

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
			ControlDataContext.Text = IconEmpty;
		}

		public override void SetData(Game newContext, PluginDataBaseGameBase PluginGameData)
		{
			if (!ControlDataContext.DisplayDetails)
			{
				return;
			}

			PluginGameRequirements pluginGameRequirements = (PluginGameRequirements)PluginGameData;
			SystemConfiguration systemConfiguration = PluginDatabase.Database.PC;

			if (systemConfiguration == null)
			{
				return;
			}

			RequirementEntry systemMinimum = pluginGameRequirements.GetMinimum();
			RequirementEntry systemRecommanded = pluginGameRequirements.GetRecommanded();

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

			if (GameContext?.Id == CurrentGame.Id && ControlDataContext.Text != newIcon)
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

		#region Events

		private void PART_PluginButton_Click(object sender, RoutedEventArgs e)
		{
			WindowOptions windowOptions = new WindowOptions
			{
				ShowMinimizeButton = false,
				ShowMaximizeButton = false,
				ShowCloseButton = true,
				CanBeResizable = false,
				MinHeight = 500,
				Width = 1000
			};

			SystemCheckerGameView viewExtension = new SystemCheckerGameView(PluginDatabase.GameContext);
			Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(PluginDatabase.PluginName, viewExtension, windowOptions);
			windowExtension.ShowDialog();
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