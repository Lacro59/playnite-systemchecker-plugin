using CommonPluginsShared;
using CommonPluginsShared.Extensions;
using CommonPluginsShared.SystemInfo;
using CommonPluginsStores.Models;
using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SystemChecker.Models;

namespace SystemChecker.Services
{
	/// <summary>
	/// Provides search functionality for games based on system requirements and custom filters.
	/// Supports advanced query syntax with flags like -min, -rec, -stores=, -status=, etc.
	/// </summary>
	public class SystemCheckerSearch : SearchContext
	{
		private const int SearchDelayMs = 500;

		// Regex patterns for parsing search query parameters
		private const string StoresPattern = @"-stores=([\w*,]*\w*)";
		private const string StatusPattern = @"-status=([\w*,]*\w*)";
		private const string FlagsPattern = @"-\w+";
		private const string StoresPrefix = "-stores=";
		private const string StatusPrefix = "-status=";

		// Pre-compiled regex for performance optimization
		private static readonly Regex StoresRegex = new Regex(StoresPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
		private static readonly Regex StatusRegex = new Regex(StatusPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
		private static readonly Regex FlagsRegex = new Regex(FlagsPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);

		// Cached split characters to avoid repeated allocations
		private static readonly char[] SplitChars = new char[] { ' ' };
		private static readonly char[] CommaSplitChars = new char[] { ',' };

		private readonly SystemCheckerDatabase _pluginDatabase;

		/// <summary>
		/// Initializes a new instance of the <see cref="SystemCheckerSearch"/> class.
		/// </summary>
		public SystemCheckerSearch()
		{
			_pluginDatabase = SystemChecker.PluginDatabase;
			Description = ResourceProvider.GetString("LOCSystemCheckerSearchDescription");
			Label = _pluginDatabase.PluginName;
			Hint = ResourceProvider.GetString("LOCSystemCheckerSearchHint");
			Delay = SearchDelayMs;
		}

		/// <summary>
		/// Gets search results based on the provided search term and filters.
		/// </summary>
		/// <param name="args">Search arguments containing the search term, filters, and cancellation token.</param>
		/// <returns>A collection of search items matching the criteria, or an empty collection if no matches found.</returns>
		/// <remarks>
		/// Supported flags:
		/// -min: Games meeting minimum requirements
		/// -rec: Games meeting recommended requirements
		/// -any: Games meeting either minimum or recommended requirements
		/// -np: Games not played (zero playtime)
		/// -fav: Favorite games only
		/// -stores=steam,gog: Filter by specific stores (comma-separated)
		/// -status=completed,playing: Filter by completion status (comma-separated)
		/// </remarks>
		public override IEnumerable<SearchItem> GetSearchResults(GetSearchResultsArgs args)
		{
			if (args == null || string.IsNullOrWhiteSpace(args.SearchTerm))
			{
				return Enumerable.Empty<SearchItem>();
			}

			List<SearchItem> searchItems = new List<SearchItem>();

			try
			{
				SearchParameters searchParams = ParseSearchParameters(args.SearchTerm);
				SystemConfiguration systemConfiguration = _pluginDatabase.Database.PC;
				GameSearchFilterSettings filterSettings = args.GameFilterSettings;

				IEnumerable<PluginGameRequirements> filteredGames = _pluginDatabase.Database
					.Where(x => MatchesSearchCriteria(x, searchParams, filterSettings));

				foreach (PluginGameRequirements gameReq in filteredGames)
				{
					if (args.CancelToken.IsCancellationRequested)
					{
						break;
					}

					if (IsGameValid(gameReq, searchParams, systemConfiguration, out Game game, out string requirementStatus))
					{
						searchItems.Add(CreateSearchItem(game, requirementStatus));
					}
				}
			}
			catch (Exception ex)
			{
				Common.LogError(ex, false);
				return Enumerable.Empty<SearchItem>();
			}

			return searchItems;
		}

		/// <summary>
		/// Creates a search item with primary and secondary actions.
		/// </summary>
		/// <param name="game">The game to create the search item for.</param>
		/// <param name="requirementStatus">The system requirement status description.</param>
		/// <returns>A configured <see cref="SearchItem"/> instance.</returns>
		private SearchItem CreateSearchItem(Game game, string requirementStatus)
		{
			SearchItem searchItem = new SearchItem(
				game.Name,
				new SearchItemAction(
					ResourceProvider.GetString("LOCGameSearchItemActionSwitchTo"),
					() => API.Instance.MainView.SelectGame(game.Id)))
			{
				SecondaryAction = new SearchItemAction(
					ResourceProvider.GetString("LOCSystemCheckerSearchSecondaryAction"),
					() => _pluginDatabase.PluginWindows.ShowPluginGameDataWindow(game)),
				Description = requirementStatus,
				Icon = game.Icon
			};

			return searchItem;
		}

		/// <summary>
		/// Parses the search term into structured parameters including flags and filters.
		/// </summary>
		/// <param name="searchTerm">The raw search term from the user.</param>
		/// <returns>A <see cref="SearchParameters"/> object containing parsed flags and cleaned search text.</returns>
		private SearchParameters ParseSearchParameters(string searchTerm)
		{
			SearchParameters parameters = new SearchParameters();
			string[] terms = searchTerm.Split(SplitChars, StringSplitOptions.RemoveEmptyEntries);

			foreach (string term in terms)
			{
				ProcessSearchTerm(term, parameters);
			}

			// Remove all flags from search term to get the actual game name query
			parameters.CleanSearchTerm = CleanSearchTerm(searchTerm);
			return parameters;
		}

		/// <summary>
		/// Processes a single search term and updates parameters accordingly.
		/// </summary>
		/// <param name="term">The term to process.</param>
		/// <param name="parameters">The parameters object to update.</param>
		/// <returns>True if the term was processed as a flag or parameter; otherwise, false.</returns>
		private bool ProcessSearchTerm(string term, SearchParameters parameters)
		{
			if (term.Length <= 1 || term[0] != '-')
			{
				return false;
			}

			if (TryParseStoresParameter(term, parameters))
			{
				return true;
			}

			if (TryParseStatusParameter(term, parameters))
			{
				return true;
			}

			return TryParseFlag(term, parameters);
		}

		/// <summary>
		/// Attempts to parse a flag parameter (e.g., -min, -rec, -fav).
		/// </summary>
		private bool TryParseFlag(string term, SearchParameters parameters)
		{
			string flag = term.ToLowerInvariant();

			switch (flag)
			{
				case "-min":
					parameters.HasMin = true;
					return true;
				case "-rec":
					parameters.HasRec = true;
					return true;
				case "-any":
					parameters.HasAny = true;
					return true;
				case "-np":
					parameters.HasNp = true;
					return true;
				case "-fav":
					parameters.HasFav = true;
					return true;
				default:
					return false;
			}
		}

		/// <summary>
		/// Attempts to parse the stores parameter (e.g., -stores=steam,gog).
		/// </summary>
		private bool TryParseStoresParameter(string term, SearchParameters parameters)
		{
			if (!term.StartsWith(StoresPrefix, StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}

			string storesValue = term.Substring(StoresPrefix.Length);
			if (!string.IsNullOrEmpty(storesValue))
			{
				parameters.Stores = storesValue.Split(CommaSplitChars, StringSplitOptions.RemoveEmptyEntries).ToList();
			}

			return true;
		}

		/// <summary>
		/// Attempts to parse the status parameter (e.g., -status=completed,playing).
		/// </summary>
		private bool TryParseStatusParameter(string term, SearchParameters parameters)
		{
			if (!term.StartsWith(StatusPrefix, StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}

			string statusValue = term.Substring(StatusPrefix.Length);
			if (!string.IsNullOrEmpty(statusValue))
			{
				parameters.Status = statusValue.Split(CommaSplitChars, StringSplitOptions.RemoveEmptyEntries).ToList();
			}

			return true;
		}

		/// <summary>
		/// Removes all flags and parameters from the search term using regex replacements.
		/// </summary>
		/// <param name="searchTerm">The original search term.</param>
		/// <returns>The cleaned search term containing only the game name query.</returns>
		private string CleanSearchTerm(string searchTerm)
		{
			string cleaned = StoresRegex.Replace(searchTerm, string.Empty);
			cleaned = StatusRegex.Replace(cleaned, string.Empty);
			cleaned = FlagsRegex.Replace(cleaned, string.Empty).Trim();
			return cleaned;
		}

		/// <summary>
		/// Determines whether a game matches all specified search criteria.
		/// Uses early exit pattern for optimal performance.
		/// </summary>
		private bool MatchesSearchCriteria(PluginGameRequirements game, SearchParameters searchParams, GameSearchFilterSettings filterSettings)
		{
			// Early exits ordered by likelihood of rejection for performance
			if (game.IsDeleted)
			{
				return false;
			}

			if (!MatchesNameFilter(game, searchParams.CleanSearchTerm))
			{
				return false;
			}

			if (!MatchesInstallFilter(game, filterSettings))
			{
				return false;
			}

			if (!MatchesHiddenFilter(game, filterSettings))
			{
				return false;
			}

			if (!MatchesPlaytimeFilter(game, searchParams))
			{
				return false;
			}

			if (!MatchesFavoriteFilter(game, searchParams))
			{
				return false;
			}

			if (!MatchesStoresFilter(game, searchParams))
			{
				return false;
			}

			if (!MatchesStatusFilter(game, searchParams))
			{
				return false;
			}

			return true;
		}

		private bool MatchesNameFilter(PluginGameRequirements game, string cleanSearchTerm)
		{
			if (string.IsNullOrEmpty(cleanSearchTerm))
			{
				return true;
			}

			return game.Name.Contains(cleanSearchTerm, StringComparison.OrdinalIgnoreCase);
		}

		private bool MatchesInstallFilter(PluginGameRequirements game, GameSearchFilterSettings filterSettings)
		{
			return filterSettings.Uninstalled || game.IsInstalled;
		}

		private bool MatchesHiddenFilter(PluginGameRequirements game, GameSearchFilterSettings filterSettings)
		{
			return filterSettings.Hidden || !game.Hidden;
		}

		private bool MatchesPlaytimeFilter(PluginGameRequirements game, SearchParameters searchParams)
		{
			return !searchParams.HasNp || game.Playtime == 0;
		}

		private bool MatchesFavoriteFilter(PluginGameRequirements game, SearchParameters searchParams)
		{
			return !searchParams.HasFav || game.Favorite;
		}

		/// <summary>
		/// Checks if the game's source matches any of the specified stores.
		/// Supports partial matching (e.g., "steam" matches "Steam" or "SteamDeck").
		/// </summary>
		private bool MatchesStoresFilter(PluginGameRequirements game, SearchParameters searchParams)
		{
			if (searchParams.Stores.Count == 0)
			{
				return true;
			}

			string sourceName = game.Source?.Name;
			if (string.IsNullOrEmpty(sourceName))
			{
				return false;
			}

			return searchParams.Stores.Any(store =>
				sourceName.Contains(store, StringComparison.OrdinalIgnoreCase));
		}

		/// <summary>
		/// Checks if the game's completion status matches any of the specified statuses.
		/// Uses IndexOf for partial matching support.
		/// </summary>
		private bool MatchesStatusFilter(PluginGameRequirements game, SearchParameters searchParams)
		{
			if (searchParams.Status.Count == 0)
			{
				return true;
			}

			string completionStatusName = game.Game?.CompletionStatus?.Name;
			if (string.IsNullOrEmpty(completionStatusName))
			{
				return false;
			}

			return searchParams.Status.Any(status =>
				completionStatusName.IndexOf(status, StringComparison.OrdinalIgnoreCase) >= 0);
		}

		/// <summary>
		/// Validates if a game meets the system requirement filters specified in the search.
		/// </summary>
		/// <param name="gameReq">The game requirements data.</param>
		/// <param name="searchParams">The parsed search parameters.</param>
		/// <param name="systemConfiguration">The current system configuration.</param>
		/// <param name="game">Output parameter containing the game object if valid.</param>
		/// <param name="requirementStatus">Output parameter containing the requirement status description.</param>
		/// <returns>True if the game is valid and meets the requirement filters; otherwise, false.</returns>
		private bool IsGameValid(PluginGameRequirements gameReq, SearchParameters searchParams,
			SystemConfiguration systemConfiguration, out Game game, out string requirementStatus)
		{
			game = API.Instance.Database.Games.Get(gameReq.Id);
			requirementStatus = string.Empty;

			// If no system requirement filters specified, include all games
			bool hasSystemFilters = searchParams.HasMin || searchParams.HasRec || searchParams.HasAny;
			if (!hasSystemFilters)
			{
				requirementStatus = BuildGameDescription(gameReq, systemConfiguration, game);
				return true;
			}

			RequirementEntry systemMinimum = gameReq.GetMinimum();
			RequirementEntry systemRecommended = gameReq.GetRecommended();

			// Check if game meets minimum requirements (when -min or -any flag is present)
			if (CheckRequirementLevel(game, systemMinimum, systemConfiguration, searchParams.HasMin, searchParams.HasAny))
			{
				requirementStatus = BuildGameDescription(gameReq, systemConfiguration, game);
				return true;
			}

			// Check if game meets recommended requirements (when -rec or -any flag is present)
			if (CheckRequirementLevel(game, systemRecommended, systemConfiguration, searchParams.HasRec, searchParams.HasAny))
			{
				requirementStatus = BuildGameDescription(gameReq, systemConfiguration, game);
				return true;
			}

			return false;
		}

		/// <summary>
		/// Checks if the current system meets a specific requirement level.
		/// </summary>
		/// <param name="game">The game to check.</param>
		/// <param name="requirement">The requirement entry (minimum or recommended).</param>
		/// <param name="systemConfiguration">The current system configuration.</param>
		/// <param name="checkLevel">Whether to check this specific level (e.g., minimum).</param>
		/// <param name="checkAny">Whether the "any" flag is active (checks both levels).</param>
		/// <returns>True if the system meets the requirement level; otherwise, false.</returns>
		private bool CheckRequirementLevel(Game game, RequirementEntry requirement,
			SystemConfiguration systemConfiguration, bool checkLevel, bool checkAny)
		{
			if (!requirement.HasData || (!checkLevel && !checkAny))
			{
				return false;
			}

			CheckSystem checkResult = SystemApi.CheckConfig(game, requirement, systemConfiguration, game.IsInstalled);
			return (bool)checkResult.AllOk;
		}

		/// <summary>
		/// Builds a comprehensive description of the game including requirements, install state, and play status.
		/// </summary>
		/// <returns>A formatted string with status information.</returns>
		private string BuildGameDescription(PluginGameRequirements gameReq,
			SystemConfiguration systemConfiguration, Game game)
		{
			List<string> parts = new List<string>();

			// 1. Requirement Status
			parts.Add(GetRequirementStatus(gameReq, systemConfiguration, game));

			// 2. Installation Status
			if (game.IsInstalled)
			{
				parts.Add(ResourceProvider.GetString("LOCGameIsGameInstalledTitle"));
			}
			else
			{
				parts.Add(ResourceProvider.GetString("LOCGameIsUnInstalledTitle"));
			}

			// 3. Play Status (Completion Status or Playtime)
			if (game.CompletionStatus != null)
			{
				parts.Add(game.CompletionStatus.Name);
			}
			else if (game.Playtime > 0)
			{
				// Format playtime (e.g., "Played 5h 30m")
				TimeSpan timePlayed = TimeSpan.FromSeconds(game.Playtime);
				string timeString = timePlayed.TotalHours >= 1
					? $"{(int)timePlayed.TotalHours}h {timePlayed.Minutes}m"
					: $"{timePlayed.Minutes}m";

				parts.Add($"{ResourceProvider.GetString("LOCTimePlayed")} ({timeString})");
			}
			else
			{
				parts.Add(ResourceProvider.GetString("LOCPlayedNone"));
			}

			return string.Join(" • ", parts);
		}

		/// <summary>
		/// Gets determining status requirement message.
		/// </summary>
		private string GetRequirementStatus(PluginGameRequirements gameReq, SystemConfiguration systemConfiguration, Game game)
		{
			RequirementEntry systemMinimum = gameReq.GetMinimum();
			RequirementEntry systemRecommended = gameReq.GetRecommended();

			if (!systemMinimum.HasData && !systemRecommended.HasData)
			{
				return ResourceProvider.GetString("LOCSystemCheckerSearchNoData");
			}

			RequirementCheckResult checkResult = EvaluateRequirements(
				game, systemMinimum, systemRecommended, systemConfiguration);

			return GetStatusMessage(checkResult);
		}

		/// <summary>
		/// Evaluates the current system against both minimum and recommended requirements.
		/// </summary>
		/// <returns>A <see cref="RequirementCheckResult"/> containing evaluation results.</returns>
		private RequirementCheckResult EvaluateRequirements(Game game, RequirementEntry systemMinimum,
			RequirementEntry systemRecommended, SystemConfiguration systemConfiguration)
		{
			RequirementCheckResult result = new RequirementCheckResult
			{
				HasMinimum = systemMinimum.HasData,
				HasRecommended = systemRecommended.HasData
			};

			if (result.HasMinimum)
			{
				CheckSystem checkMinimum = SystemApi.CheckConfig(game, systemMinimum, systemConfiguration, game.IsInstalled);
				result.MeetsMinimum = (bool)checkMinimum.AllOk;
			}

			if (result.HasRecommended)
			{
				CheckSystem checkRecommended = SystemApi.CheckConfig(game, systemRecommended, systemConfiguration, game.IsInstalled);
				result.MeetsRecommended = (bool)checkRecommended.AllOk;
			}

			return result;
		}

		/// <summary>
		/// Gets the appropriate status message based on requirement check results.
		/// Priority: Recommended > Minimum > Below Minimum.
		/// </summary>
		private string GetStatusMessage(RequirementCheckResult result)
		{
			if (result.MeetsRecommended)
			{
				return ResourceProvider.GetString("LOCSystemCheckerSearchMeetsRecommended");
			}

			if (result.MeetsMinimum)
			{
				return ResourceProvider.GetString("LOCSystemCheckerSearchMeetsMinimum");
			}

			return ResourceProvider.GetString("LOCSystemCheckerSearchBelowMinimum");
		}

		/// <summary>
		/// Internal data structure for caching requirement check results.
		/// </summary>
		private class RequirementCheckResult
		{
			public bool HasMinimum { get; set; }
			public bool HasRecommended { get; set; }
			public bool MeetsMinimum { get; set; }
			public bool MeetsRecommended { get; set; }
		}
	}
}