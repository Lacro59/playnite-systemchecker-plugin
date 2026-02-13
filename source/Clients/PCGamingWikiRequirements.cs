using CommonPluginsShared;
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
	public class PCGamingWikiRequirements : RequirementMetadata
	{
		private readonly PCGamingWikiApi _pcGamingWikiApi;

		private uint SteamAppId { get; set; } = 0;


		public PCGamingWikiRequirements()
		{
			_pcGamingWikiApi = new PCGamingWikiApi(PluginDatabase.PluginName, PlayniteTools.ExternalPlugin.SystemChecker);
		}


		public override PluginGameRequirements GetRequirements()
		{
			PluginGameRequirements = SystemChecker.PluginDatabase.GetDefault(GameContext);

			string url = _pcGamingWikiApi.FindGoodUrl(GameContext);

			if (!url.IsNullOrEmpty())
			{
				PluginGameRequirements = GetRequirements(url);
			}
			else
			{
				Logger.Warn($"PCGamingWikiRequirements - Not found for {GameContext.Name}");
			}

			return PluginGameRequirements;
		}

		public PluginGameRequirements GetRequirements(Game game)
		{
			GameContext = game;
			SteamAppId = 0;

			if (GameContext.SourceId == PlayniteTools.GetPluginId(PlayniteTools.ExternalPlugin.SteamLibrary))
			{
				SteamAppId = uint.Parse(game.GameId);
			}
			if (SteamAppId == 0)
			{
				SteamApi steamApi = new SteamApi(PluginDatabase.PluginName, PlayniteTools.ExternalPlugin.SystemChecker);
				SteamAppId = steamApi.GetAppId(game);
			}

			return GetRequirements();
		}

		public override PluginGameRequirements GetRequirements(string url)
		{
			GameRequirements apiResult = _pcGamingWikiApi.GetGameRequirements(url);
			if (apiResult == null)
			{
				Logger.Warn($"PCGamingWikiRequirements - No data for {GameContext.Name} at {url}");
				return PluginGameRequirements;
			}

			PluginGameRequirements.Items = new List<RequirementEntry> { apiResult.Minimum, apiResult.Recommended };
			PluginGameRequirements.SourcesLink = apiResult.SourceLink;

			return PluginGameRequirements;
		}
	}
}