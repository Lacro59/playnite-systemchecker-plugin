using CommonPluginsShared;
using CommonPluginsShared.Models;
using CommonPluginsStores.Models;
using CommonPluginsStores.Steam;
using CommonPluginsStores.Steam.Models;
using Playnite.SDK.Models;
using System.Collections.Generic;
using SystemChecker.Models;
using SystemChecker.Services;
using static CommonPlayniteShared.PluginLibrary.SteamLibrary.SteamShared.StoreAppDetailsResult.AppDetails;

namespace SystemChecker.Clients
{
	public class SteamRequierements : RequierementMetadata 
	{
		private readonly SteamApi _steamApi;

		private uint AppId { get; set; }


		public SteamRequierements()
		{
			_steamApi = new SteamApi(PluginDatabase.PluginName, PlayniteTools.ExternalPlugin.SystemChecker);
		}


		public override PluginGameRequierements GetRequirements()
		{
			PluginGameRequierements = SystemChecker.PluginDatabase.GetDefault(GameContext);

			GameRequirements apiResult = _steamApi.GetGameRequirements(AppId);
			if (apiResult == null)
			{
				Logger.Warn($"SteamRequierements - No data for {GameContext.Name} (AppId: {AppId})");
				return PluginGameRequierements;
			}

			PluginGameRequierements.Items = new List<RequirementEntry> { apiResult.Minimum, apiResult.Recommended };
			PluginGameRequierements.SourcesLink = apiResult.SourceLink;

			return PluginGameRequierements;
		}

		public PluginGameRequierements GetRequirements(Game game, uint appId = 0)
		{
			GameContext = game;
			AppId = appId != 0 ? appId : uint.Parse(GameContext.GameId);
			return GetRequirements();
		}

		public override PluginGameRequierements GetRequirements(string url)
		{
			throw new System.NotImplementedException();
		}
	}
}