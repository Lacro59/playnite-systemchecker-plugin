using CommonPluginsControls.Views;
using CommonPluginsShared;
using CommonPluginsShared.Interfaces;
using CommonPluginsShared.Services;
using Playnite.SDK;
using Playnite.SDK.Models;
using System.Windows;
using SystemChecker.Views;

namespace SystemChecker.Services
{
    public class WindowPluginService : IWindowPluginService
	{
		private static readonly ILogger Logger = LogManager.GetLogger();

		public string PluginName { get; private set; }

        public IPluginDatabase PluginDatabase { get; private set; }

		public WindowPluginService(string pluginName, IPluginDatabase pluginDatabase)
		{
			PluginName = pluginName;
            PluginDatabase = pluginDatabase;

            if (PluginDatabase == null)
            {
                Logger.Warn("WindowPluginService created with a null PluginDatabase instance.");
            }
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

		public void ShowPluginGameNoDataWindow()
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