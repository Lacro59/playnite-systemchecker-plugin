using CommonPluginsShared;
using CommonPluginsStores.PCGamingWiki;
using CommonPluginsStores.Steam;
using Playnite.SDK.Models;
using System;
using SystemChecker.Models;

namespace SystemChecker.Clients
{
	/// <summary>
	/// Retrieves system requirements from PCGamingWiki.
	/// Falls back to a Steam AppId lookup when the game is not a native Steam title,
	/// as PCGamingWiki pages are often indexed by Steam AppId.
	/// </summary>
	public class PCGamingWikiRequirements : RequirementMetadata
	{
		private readonly PCGamingWikiApi _pcGamingWikiApi;
		private readonly SteamApi _steamApi;

		/// <summary>Steam AppId resolved during <see cref="GetRequirements(Game)"/>. 0 when unavailable.</summary>
		private uint _steamAppId;


		public PCGamingWikiRequirements()
		{
			_pcGamingWikiApi = new PCGamingWikiApi(PluginDatabase.PluginName, PlayniteTools.ExternalPlugin.SystemChecker);
			_steamApi = new SteamApi(PluginDatabase.PluginName, PlayniteTools.ExternalPlugin.SystemChecker);
		}


		// -----------------------------------------------------------------------
		//  Public entry-point
		// -----------------------------------------------------------------------

		/// <summary>
		/// Resolves requirements for <paramref name="game"/> by first finding the best PCGamingWiki URL,
		/// then delegating to <see cref="GetRequirements(string)"/>.
		/// </summary>
		/// <param name="game">Target game. Must not be <see langword="null"/>.</param>
		public PluginGameRequirements GetRequirements(Game game)
		{
			Initialize(game);
			_steamAppId = ResolveSteamAppId(game);

			return GetRequirements();
		}


		// -----------------------------------------------------------------------
		//  RequirementMetadata overrides
		// -----------------------------------------------------------------------

		/// <inheritdoc/>
		/// <remarks>
		/// Resolves the PCGamingWiki URL via <see cref="PCGamingWikiApi.FindGoodUrl"/> and
		/// delegates to <see cref="GetRequirements(string)"/>.
		/// Caller must have invoked <see cref="GetRequirements(Game)"/> first to set context.
		/// </remarks>
		public override PluginGameRequirements GetRequirements()
		{
			ResetRequirements();

			string url = _pcGamingWikiApi.FindGoodUrl(GameContext);

			if (url.IsNullOrEmpty())
			{
				Logger.Warn($"PCGamingWikiRequirements - No URL found for {GameContext.Name}");
				return PluginGameRequirements;
			}

			return GetRequirements(url);
		}

		/// <inheritdoc/>
		/// <param name="url">Direct URL to the game's PCGamingWiki requirements page.</param>
		public override PluginGameRequirements GetRequirements(string url)
		{
			var apiResult = _pcGamingWikiApi.GetGameRequirements(url);
			return BuildRequirementsFromApiResult(apiResult, nameof(PCGamingWikiRequirements), $"url: {url}");
		}


		// -----------------------------------------------------------------------
		//  Private helpers
		// -----------------------------------------------------------------------

		/// <summary>
		/// Resolves the Steam AppId for <paramref name="game"/>.
		/// Reads <see cref="Game.GameId"/> directly when the source is the Steam library;
		/// otherwise queries <see cref="SteamApi"/> for a cross-library match.
		/// </summary>
		/// <returns>Resolved AppId, or 0 when none is found.</returns>
		private uint ResolveSteamAppId(Game game)
		{
			if (game.SourceId == PlayniteTools.GetPluginId(PlayniteTools.ExternalPlugin.SteamLibrary))
			{
				return uint.Parse(game.GameId);
			}

			return _steamApi.GetAppId(game);
		}
	}
}