using CommonPluginsStores.Models;
using Playnite.SDK;
using Playnite.SDK.Models;
using System.Collections.Generic;
using SystemChecker.Services;

namespace SystemChecker.Models
{
	/// <summary>
	/// Abstract base class for all system requirement metadata providers.
	/// Subclasses implement source-specific retrieval logic while this class
	/// handles shared initialization, logging patterns, and result assembly.
	/// </summary>
	public abstract class RequirementMetadata
	{
		/// <summary>Shared logger instance for all requirement metadata providers.</summary>
		protected static readonly ILogger Logger = LogManager.GetLogger();

		/// <summary>Plugin database reference used to build default requirement entries.</summary>
		protected static readonly SystemCheckerDatabase PluginDatabase = SystemChecker.PluginDatabase;

		/// <summary>The game whose requirements are being retrieved. Must be set via <see cref="Initialize"/> before calling <see cref="GetRequirements()"/>.</summary>
		protected Game GameContext { get; private set; }

		/// <summary>Accumulated requirements result for the current <see cref="GameContext"/>.</summary>
		protected PluginGameRequirements PluginGameRequirements { get; private set; } = new PluginGameRequirements();


		// -----------------------------------------------------------------------
		//  Public API
		// -----------------------------------------------------------------------

		/// <summary>
		/// Returns <see langword="true"/> when at least minimum requirements were found for the current game.
		/// </summary>
		public bool IsFind()
		{
			return PluginGameRequirements.GetMinimum().HasData;
		}

		/// <summary>
		/// Fetches requirements from the source using whatever identifiers the subclass has already resolved.
		/// Must be called after <see cref="Initialize"/>.
		/// </summary>
		public abstract PluginGameRequirements GetRequirements();

		/// <summary>
		/// Fetches requirements from the source using an explicit URL.
		/// Not all providers support URL-based retrieval; unsupported providers should throw <see cref="System.NotSupportedException"/>.
		/// </summary>
		/// <param name="url">Direct URL to the requirements page on the source website.</param>
		public abstract PluginGameRequirements GetRequirements(string url);


		// -----------------------------------------------------------------------
		//  Protected helpers — shared logic factorized from subclasses
		// -----------------------------------------------------------------------

		/// <summary>
		/// Initializes shared state for a new retrieval cycle.
		/// Must be called by every subclass entry-point before <see cref="GetRequirements()"/>.
		/// Resets <see cref="PluginGameRequirements"/> to the database default for <paramref name="game"/>.
		/// </summary>
		/// <param name="game">The game whose requirements will be fetched.</param>
		protected void Initialize(Game game)
		{
			GameContext = game;
			PluginGameRequirements = PluginDatabase.GetDefault(game);
		}

		/// <summary>
		/// Resets <see cref="PluginGameRequirements"/> to the database default for the current <see cref="GameContext"/>.
		/// Use at the start of <see cref="GetRequirements()"/> overrides when <see cref="Initialize"/> was called by a different entry-point.
		/// </summary>
		protected void ResetRequirements()
		{
			PluginGameRequirements = PluginDatabase.GetDefault(GameContext);
		}

		/// <summary>
		/// Builds the <see cref="PluginGameRequirements"/> result from a <see cref="GameRequirements"/> API response.
		/// Logs a warning and returns the current (default) value when <paramref name="apiResult"/> is <see langword="null"/>.
		/// </summary>
		/// <param name="apiResult">Raw API response. May be <see langword="null"/>.</param>
		/// <param name="sourceLabel">Human-readable label used in the warning message (e.g. "SteamRequirements").</param>
		/// <param name="sourceDetail">Additional context for the warning (e.g. URL or AppId).</param>
		/// <returns>
		/// Populated <see cref="PluginGameRequirements"/> on success;
		/// the unchanged default instance when <paramref name="apiResult"/> is <see langword="null"/>.
		/// </returns>
		protected PluginGameRequirements BuildRequirementsFromApiResult(
			GameRequirements apiResult,
			string sourceLabel,
			string sourceDetail = null)
		{
			if (apiResult == null)
			{
				string detail = sourceDetail != null ? $" ({sourceDetail})" : string.Empty;
				Logger.Warn($"{sourceLabel} - No data for {GameContext.Name}{detail}");
				return PluginGameRequirements;
			}

			PluginGameRequirements.Items = new List<RequirementEntry>
			{
				apiResult.Minimum,
				apiResult.Recommended
			};
			PluginGameRequirements.SourcesLink = apiResult.SourceLink;

			return PluginGameRequirements;
		}
	}
}