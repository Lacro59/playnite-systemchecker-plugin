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

		public PCGamingWikiRequirements()
		{
			_pcGamingWikiApi = new PCGamingWikiApi(PluginDatabase.PluginName, PlayniteTools.ExternalPlugin.SystemChecker);
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
	}
}