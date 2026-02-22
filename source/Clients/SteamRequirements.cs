using CommonPluginsShared;
using CommonPluginsStores.Steam;
using Playnite.SDK.Models;
using System;
using SystemChecker.Models;

namespace SystemChecker.Clients
{
	/// <summary>
	/// Retrieves system requirements directly from the Steam store API.
	/// </summary>
	public class SteamRequirements : RequirementMetadata
	{
		private readonly SteamApi _steamApi;

		/// <summary>Steam AppId for the current <see cref="RequirementMetadata.GameContext"/>.</summary>
		private uint _appId;


		public SteamRequirements()
		{
			_steamApi = new SteamApi(PluginDatabase.PluginName, PlayniteTools.ExternalPlugin.SystemChecker);
		}


		// -----------------------------------------------------------------------
		//  Public entry-point
		// -----------------------------------------------------------------------

		/// <summary>
		/// Fetches requirements for <paramref name="game"/> using the provided <paramref name="appId"/>.
		/// When <paramref name="appId"/> is 0, <see cref="Game.GameId"/> is parsed as the AppId.
		/// </summary>
		/// <param name="game">Target game. Must not be <see langword="null"/>.</param>
		/// <param name="appId">
		/// Explicit Steam AppId override. Pass 0 (default) to use <see cref="Game.GameId"/>.
		/// </param>
		public PluginGameRequirements GetRequirements(Game game, uint appId = 0)
		{
			Initialize(game);
			_appId = appId != 0 ? appId : uint.Parse(game.GameId);

			return GetRequirements();
		}


		// -----------------------------------------------------------------------
		//  RequirementMetadata overrides
		// -----------------------------------------------------------------------

		/// <inheritdoc/>
		/// <remarks>
		/// Caller must have invoked <see cref="GetRequirements(Game, uint)"/> first to set context.
		/// </remarks>
		public override PluginGameRequirements GetRequirements()
		{
			ResetRequirements();

			var apiResult = _steamApi.GetGameRequirements(_appId);
			return BuildRequirementsFromApiResult(apiResult, nameof(SteamRequirements), $"AppId: {_appId}");
		}

		/// <inheritdoc/>
		/// <exception cref="NotSupportedException">
		/// Always thrown. Steam requirements are identified by AppId, not by URL.
		/// </exception>
		public override PluginGameRequirements GetRequirements(string url)
		{
			throw new NotSupportedException(
				$"{nameof(SteamRequirements)} does not support URL-based retrieval. Use {nameof(GetRequirements)}(Game, uint) instead.");
		}
	}
}