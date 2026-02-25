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

		// Pre-compiled regex for performance
		private static readonly Regex StoresRegex = new Regex(StoresPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
		private static readonly Regex StatusRegex = new Regex(StatusPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
		private static readonly Regex FlagsRegex = new Regex(FlagsPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);

		// Cached split chars to avoid repeated allocations
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
		/// Optimized: PLINQ parallelism + single SystemApi.CheckConfig() evaluation pass per game.
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

			try
			{
				SearchParameters searchParams = ParseSearchParameters(args.SearchTerm);
				SystemConfiguration systemConfiguration = _pluginDatabase.PC;
				GameSearchFilterSettings filterSettings = args.GameFilterSettings;

				return _pluginDatabase.GetAllCache()
							.AsParallel()
							.WithCancellation(args.CancelToken)
							.Where(x => x != null && MatchesSearchCriteria(x, searchParams, filterSettings))
							.Select(gameReq => BuildValidatedSearchItem(gameReq, searchParams, systemConfiguration))
							.Where(item => item != null)
							.ToList();
			}
			catch (OperationCanceledException)
			{
				return Enumerable.Empty<SearchItem>();
			}
			catch (Exception ex)
			{
				Common.LogError(ex, false);
				return Enumerable.Empty<SearchItem>();
			}
		}

		/// <summary>
		/// Evaluates requirements once and builds the search item if valid.
		/// Core optimization: SystemApi.CheckConfig() est appelé au maximum 2x par jeu
		/// au lieu de 4x dans l'implémentation originale.
		/// </summary>
		/// <param name="gameReq">Les données de requirements du jeu.</param>
		/// <param name="searchParams">Les paramètres de recherche parsés.</param>
		/// <param name="systemConfiguration">La configuration système actuelle.</param>
		/// <returns>Un <see cref="SearchItem"/> configuré, ou null si le jeu ne passe pas les filtres.</returns>
		private SearchItem BuildValidatedSearchItem(PluginGameRequirements gameReq, SearchParameters searchParams, SystemConfiguration systemConfiguration)
		{
			Game game = API.Instance.Database.Games.Get(gameReq.Id);
			if (game == null)
			{
				return null;
			}

			// Évaluation unique des requirements — résultats réutilisés pour le filtre ET la description
			RequirementCheckResult checkResult = EvaluateRequirements(
				game,
				gameReq.GetMinimum(),
				gameReq.GetRecommended(),
				systemConfiguration);

			bool hasSystemFilters = searchParams.HasMin || searchParams.HasRec || searchParams.HasAny;
			if (hasSystemFilters && !PassesSystemFilter(checkResult, searchParams))
			{
				return null;
			}

			string requirementStatus = BuildGameDescription(checkResult, gameReq, game);
			return CreateSearchItem(game, requirementStatus);
		}

		/// <summary>
		/// Checks if pre-computed requirement results satisfy the active system requirement flags.
		/// </summary>
		/// <param name="checkResult">Les résultats pré-calculés.</param>
		/// <param name="searchParams">Les paramètres de recherche.</param>
		/// <returns>True si le jeu satisfait au moins un des filtres actifs.</returns>
		private bool PassesSystemFilter(RequirementCheckResult checkResult, SearchParameters searchParams)
		{
			if ((searchParams.HasMin || searchParams.HasAny) && checkResult.MeetsMinimum)
			{
				return true;
			}

			if ((searchParams.HasRec || searchParams.HasAny) && checkResult.MeetsRecommended)
			{
				return true;
			}

			return false;
		}

		/// <summary>
		/// Creates a search item with primary and secondary actions.
		/// </summary>
		/// <param name="game">The game to create the search item for.</param>
		/// <param name="requirementStatus">The system requirement status description.</param>
		/// <returns>A configured <see cref="SearchItem"/> instance.</returns>
		private SearchItem CreateSearchItem(Game game, string requirementStatus)
		{
			return new SearchItem(
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

			parameters.CleanSearchTerm = CleanSearchTerm(searchTerm);
			return parameters;
		}

		/// <summary>
		/// Processes a single search term and updates parameters accordingly.
		/// </summary>
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
			switch (term.ToLowerInvariant())
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
		private bool MatchesSearchCriteria(PluginGameRequirements game, SearchParameters searchParams,
			GameSearchFilterSettings filterSettings)
		{
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
		/// Evaluates the current system against both minimum and recommended requirements.
		/// Called at most once per game — résultats partagés entre filtrage et description.
		/// </summary>
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
		/// Builds a comprehensive description of the game including requirements, install state, and play status.
		/// Accepts pre-computed <see cref="RequirementCheckResult"/> — aucun appel supplémentaire à SystemApi.
		/// </summary>
		private string BuildGameDescription(RequirementCheckResult checkResult,
			PluginGameRequirements gameReq, Game game)
		{
			// Capacité initiale connue = 4 éléments → évite les réallocations internes
			List<string> parts = new List<string>(4);

			parts.Add(PlayniteTools.GetSourceName(game));
			parts.Add(GetStatusMessage(checkResult, gameReq));

			parts.Add(game.IsInstalled
				? ResourceProvider.GetString("LOCGameIsGameInstalledTitle")
				: ResourceProvider.GetString("LOCGameIsUnInstalledTitle"));

			if (game.CompletionStatus != null)
			{
				parts.Add(game.CompletionStatus.Name);
			}
			else if (game.Playtime > 0)
			{
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

			return string.Join(" \u2022 ", parts);
		}

		/// <summary>
		/// Resolves status message from pre-computed results.
		/// Fallback vers "no data" si aucun requirement n'est disponible.
		/// </summary>
		private string GetStatusMessage(RequirementCheckResult checkResult, PluginGameRequirements gameReq)
		{
			if (!checkResult.HasMinimum && !checkResult.HasRecommended)
			{
				return ResourceProvider.GetString("LOCSystemCheckerSearchNoData");
			}

			return GetStatusMessage(checkResult);
		}

		/// <summary>
		/// Gets the appropriate status message based on requirement check results.
		/// Priority: Recommended > Minimum > Below Minimum.
		/// </summary>
		private string GetStatusMessage(RequirementCheckResult checkResult)
		{
			if (checkResult.MeetsRecommended)
			{
				return ResourceProvider.GetString("LOCSystemCheckerSearchMeetsRecommended");
			}

			if (checkResult.MeetsMinimum)
			{
				return ResourceProvider.GetString("LOCSystemCheckerSearchMeetsMinimum");
			}

			return ResourceProvider.GetString("LOCSystemCheckerSearchBelowMinimum");
		}

		/// <summary>
		/// Internal data structure for caching requirement check results.
		/// Évite la ré-évaluation entre le filtrage et la construction de la description.
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