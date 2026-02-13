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
	public class SteamRequirements : RequirementMetadata 
	{
		private readonly SteamApi _steamApi;

		private uint AppId { get; set; }


		public SteamRequirements()
		{
			_steamApi = new SteamApi(PluginDatabase.PluginName, PlayniteTools.ExternalPlugin.SystemChecker);
		}


		public override PluginGameRequirements GetRequirements()
		{
			PluginGameRequirements = SystemChecker.PluginDatabase.GetDefault(GameContext);

			GameRequirements apiResult = _steamApi.GetGameRequirements(AppId);
			if (apiResult == null)
			{
				Logger.Warn($"SteamRequirements - No data for {GameContext.Name} (AppId: {AppId})");
				return PluginGameRequirements;
			}

			PluginGameRequirements.Items = new List<RequirementEntry> { apiResult.Minimum, apiResult.Recommended };
			PluginGameRequirements.SourcesLink = apiResult.SourceLink;

			return PluginGameRequirements;
		}

		public PluginGameRequirements GetRequirements(Game game, uint appId = 0)
		{
			GameContext = game;
			AppId = appId != 0 ? appId : uint.Parse(GameContext.GameId);
			return GetRequirements();
		}

		public override PluginGameRequirements GetRequirements(string url)
		{
			throw new System.NotImplementedException();
		}
	}
}