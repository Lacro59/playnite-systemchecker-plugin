using CommonPluginsControls.Views;
using CommonPluginsShared;
using CommonPluginsShared.Interfaces;
using CommonPluginsShared.Plugins;
using CommonPluginsShared.Services;
using Playnite.SDK;
using Playnite.SDK.Models;
using System.Windows;
using SystemChecker.Views;

namespace SystemChecker.Services
{
    public class SystemCheckWindows : PluginWindows
	{
		public SystemCheckWindows(string pluginName, IPluginDatabase pluginDatabase) : base(pluginName, pluginDatabase)
		{
		}

		public override void ShowPluginGameDataWindow(Game gameContext)
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

			var viewExtension = new SystemCheckerGameView(gameContext);
			Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(
				PluginName,
				viewExtension,
				windowOptions);
			windowExtension.ShowDialog();
		}

		public override void ShowPluginGameNoDataWindow()
		{
			WindowOptions windowOptions = new WindowOptions
			{
				ShowMinimizeButton = false,
				ShowMaximizeButton = false,
				ShowCloseButton = true,
				CanBeResizable = false,
				Height = 700,
				Width = 1000
			};

			var viewExtension = new ListWithNoData(PluginDatabase);
			Window windowExtension = PlayniteUiHelper.CreateExtensionWindow(
				PluginName,
				viewExtension,
				windowOptions);
			windowExtension.ShowDialog();
		}
    }
}