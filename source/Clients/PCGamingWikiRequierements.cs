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
	public class PCGamingWikiRequierements : RequierementMetadata
	{
		private readonly PCGamingWikiApi _pcGamingWikiApi;

		private uint SteamAppId { get; set; } = 0;


		public PCGamingWikiRequierements()
		{
			_pcGamingWikiApi = new PCGamingWikiApi(PluginDatabase.PluginName, PlayniteTools.ExternalPlugin.SystemChecker);
		}


		public override PluginGameRequierements GetRequirements()
		{
			PluginGameRequierements = SystemChecker.PluginDatabase.GetDefault(GameContext);

			string url = _pcGamingWikiApi.FindGoodUrl(GameContext);

			if (!url.IsNullOrEmpty())
			{
				PluginGameRequierements = GetRequirements(url);
			}
			else
			{
				Logger.Warn($"PCGamingWikiRequierements - Not found for {GameContext.Name}");
			}

			return PluginGameRequierements;
		}

		public PluginGameRequierements GetRequirements(Game game)
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

		public override PluginGameRequierements GetRequirements(string url)
		{
			GameRequirements apiResult = _pcGamingWikiApi.GetGameRequirements(url);
			if (apiResult == null)
			{
				Logger.Warn($"PCGamingWikiRequierements - No data for {GameContext.Name} at {url}");
				return PluginGameRequierements;
			}

			PluginGameRequierements.Items = new List<RequirementEntry> { apiResult.Minimum, apiResult.Recommended };
			PluginGameRequierements.SourcesLink = apiResult.SourceLink;

			return PluginGameRequierements;
		}
	}
}