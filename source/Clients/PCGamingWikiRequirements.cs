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

		public PCGamingWikiRequirements(SteamApi steamApi = null)
		{
			_pcGamingWikiApi = new PCGamingWikiApi(
				PluginDatabase.PluginName,
				PlayniteTools.ExternalPlugin.SystemChecker,
				steamApi);
		}


		// -----------------------------------------------------------------------
		//  Public entry-point
		// -----------------------------------------------------------------------

		/// <summary>
		/// Resolves requirements for <paramref name="game"/> by first finding the best PCGamingWiki URL,
		/// then delegating to <see cref="GetRequirements(string)"/>.
		/// </summary>
		/// <param name="game">Target game. Must not be <see langword="null"/>.</param>
		/// <param name="steamAppId">
		/// Optional pre-resolved Steam AppId, updated when resolved during URL lookup
		/// so a subsequent Steam fallback can reuse it.
		/// </param>
		/// <param name="steamAppIdLookupAttempted">
		/// Updated when AppId resolution runs during PCGamingWiki URL lookup.
		/// </param>
		public PluginGameRequirements GetRequirements(Game game, ref uint steamAppId, ref bool steamAppIdLookupAttempted)
		{
			Initialize(game);
			return GetRequirements(ref steamAppId, ref steamAppIdLookupAttempted);
		}

		/// <summary>
		/// Resolves requirements for <paramref name="game"/> without sharing a Steam AppId with a fallback provider.
		/// </summary>
		public PluginGameRequirements GetRequirements(Game game)
		{
			uint steamAppId = 0;
			bool steamAppIdLookupAttempted = false;
			return GetRequirements(game, ref steamAppId, ref steamAppIdLookupAttempted);
		}


		// -----------------------------------------------------------------------
		//  RequirementMetadata overrides
		// -----------------------------------------------------------------------

		/// <inheritdoc/>
		/// <remarks>
		/// Resolves the PCGamingWiki URL via <see cref="PCGamingWikiApi.FindGoodUrl"/> and
		/// delegates to <see cref="GetRequirements(string)"/>.
		/// Caller must have invoked <see cref="GetRequirements(Game, ref uint)"/> first to set context.
		/// </remarks>
		public PluginGameRequirements GetRequirements(ref uint steamAppId, ref bool steamAppIdLookupAttempted)
		{
			ResetRequirements();

			string url = _pcGamingWikiApi.FindGoodUrl(GameContext, ref steamAppId, ref steamAppIdLookupAttempted);

			if (url.IsNullOrEmpty())
			{
				Logger.Warn($"PCGamingWikiRequirements - No URL found for {GameContext.Name}");
				return PluginGameRequirements;
			}

			return GetRequirements(url);
		}

		/// <inheritdoc/>
		public override PluginGameRequirements GetRequirements()
		{
			uint steamAppId = 0;
			bool steamAppIdLookupAttempted = false;
			return GetRequirements(ref steamAppId, ref steamAppIdLookupAttempted);
		}

		/// <inheritdoc/>
		/// <param name="url">Direct URL to the game's PCGamingWiki requirements page.</param>
		public override PluginGameRequirements GetRequirements(string url)
		{
			var apiResult = _pcGamingWikiApi.GetGameRequirements(url);
			return BuildRequirementsFromApiResult(apiResult, nameof(PCGamingWikiRequirements), $"url: {url}");
		}
	}
}