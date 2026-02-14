using CommonPluginsShared;
using CommonPluginsShared.Services;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using SystemChecker.Views;

namespace SystemChecker.Services
{
    public class WindowPluginService : IWindowPluginService
	{
		public ILogger Logger = LogManager.GetLogger();

		public string PluginName { get; private set; }

		public WindowPluginService(string pluginName)
		{
			PluginName = pluginName;
		}

		public void ShowPluginGameDataWindow(Game gameContext)
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
	}
}