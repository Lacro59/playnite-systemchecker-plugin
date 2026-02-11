using CommonPluginsShared;
using CommonPluginsShared.Models;
using CommonPluginsStores.Models;
using CommonPluginsStores.PCGamingWiki;
using CommonPluginsStores.Steam;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using SystemChecker.Models;
using SystemChecker.Services;

namespace SystemChecker.Clients
{
	public class PCGamingWikiRequierements : RequierementMetadata
	{
		private SystemCheckerDatabase PluginDatabase => SystemChecker.PluginDatabase;
		private PCGamingWikiApi PcGamingWikiApi => new PCGamingWikiApi(PluginDatabase.PluginName, PlayniteTools.ExternalPlugin.SystemChecker);

		private uint SteamId { get; set; } = 0;


		public PCGamingWikiRequierements()
		{
		}


		public override PluginGameRequierements GetRequirements()
		{
			PluginGameRequierements = SystemChecker.PluginDatabase.GetDefault(GameContext);

			string url = PcGamingWikiApi.FindGoodUrl(GameContext);

			if (!url.IsNullOrEmpty())
			{
				PluginGameRequierements = GetRequirements(url);
			}
			else
			{
				logger.Warn($"PCGamingWikiRequierements - Not found for {GameContext.Name}");
			}

			return PluginGameRequierements;
		}

		public PluginGameRequierements GetRequirements(Game game)
		{
			GameContext = game;
			SteamId = 0;

			if (GameContext.SourceId == PlayniteTools.GetPluginId(PlayniteTools.ExternalPlugin.SteamLibrary))
			{
				SteamId = uint.Parse(game.GameId);
			}
			if (SteamId == 0)
			{
				SteamApi steamApi = new SteamApi(PluginDatabase.PluginName, PlayniteTools.ExternalPlugin.SystemChecker);
				SteamId = steamApi.GetAppId(game);
			}

			return GetRequirements();
		}

		public override PluginGameRequierements GetRequirements(string url)
		{
			GameRequirements apiResult = PcGamingWikiApi.GetGameRequirements(url);

			if (apiResult == null)
			{
				logger.Warn($"PCGamingWikiRequierements - No data for {GameContext.Name} at {url}");
				return PluginGameRequierements;
			}

			PluginGameRequierements.Items = new List<RequirementEntry> { apiResult.Minimum, apiResult.Recommended };
			PluginGameRequierements.SourcesLink = apiResult.SourceLink;

			return PluginGameRequierements;
		}
	}
}