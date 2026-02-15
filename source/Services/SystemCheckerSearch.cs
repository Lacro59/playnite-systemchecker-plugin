using CommonPluginsShared;
using CommonPluginsShared.Extensions;
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
	public class SystemCheckerSearch : SearchContext
	{
		private readonly SystemCheckerDatabase PluginDatabase = SystemChecker.PluginDatabase;

		private static readonly Regex StoresRegex = new Regex(@"-stores=([\w*,]*\w*)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
		private static readonly Regex StatusRegex = new Regex(@"-status=([\w*,]*\w*)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
		private static readonly Regex FlagsRegex = new Regex(@"-\w+", RegexOptions.IgnoreCase | RegexOptions.Compiled);

		public SystemCheckerSearch()
		{
			Description = ResourceProvider.GetString("LOCSystemCheckerSearchDescription");
			Label = PluginDatabase.PluginName;
			Hint = ResourceProvider.GetString("LOCSystemCheckerSearchHint");
			Delay = 500;
		}

		public override IEnumerable<SearchItem> GetSearchResults(GetSearchResultsArgs args)
		{
			List<SearchItem> searchItems = new List<SearchItem>();

			try
			{
				SearchParameters searchParams = ParseSearchParameters(args.SearchTerm);
				SystemConfiguration systemConfiguration = PluginDatabase.Database.PC;
				GameSearchFilterSettings filterSettings = args.GameFilterSettings;

				IEnumerable<PluginGameRequirements> filteredGames = PluginDatabase.Database
					.Where(x => MatchesSearchCriteria(x, searchParams, filterSettings));

				foreach (PluginGameRequirements gameReq in filteredGames)
				{
					if (args.CancelToken.IsCancellationRequested)
					{
						return null;
					}

					if (IsGameValid(gameReq, searchParams, systemConfiguration, out Game game))
					{
						searchItems.Add(new GameSearchItem(
							game,
							ResourceProvider.GetString("LOCGameSearchItemActionSwitchTo"),
							() => API.Instance.MainView.SelectGame(game.Id)
						));
					}
				}
			}
			catch (Exception ex)
			{
				Common.LogError(ex, false);
				return null;
			}

			return searchItems;
		}

		private SearchParameters ParseSearchParameters(string searchTerm)
		{
			SearchParameters parameters = new SearchParameters();
			string[] terms = searchTerm.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

			foreach (string term in terms)
			{
				if (term.Length > 1 && term[0] == '-')
				{
					string flag = term.ToLowerInvariant();
					switch (flag)
					{
						case "-min":
							parameters.HasMin = true;
							continue;
						case "-rec":
							parameters.HasRec = true;
							continue;
						case "-any":
							parameters.HasAny = true;
							continue;
						case "-np":
							parameters.HasNp = true;
							continue;
						case "-fav":
							parameters.HasFav = true;
							continue;
					}
				}

				// Check for stores parameter
				if (term.StartsWith("-stores=", StringComparison.OrdinalIgnoreCase))
				{
					string storesValue = term.Substring(8); // "-stores=".Length
					if (!string.IsNullOrEmpty(storesValue))
					{
						parameters.Stores = storesValue.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
					}
					continue;
				}

				// Check for status parameter
				if (term.StartsWith("-status=", StringComparison.OrdinalIgnoreCase))
				{
					string statusValue = term.Substring(8); // "-status=".Length
					if (!string.IsNullOrEmpty(statusValue))
					{
						parameters.Status = statusValue.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
					}
					continue;
				}
			}

			// Clean search term using pre-compiled regex (much faster)
			string cleaned = StoresRegex.Replace(searchTerm, string.Empty);
			cleaned = StatusRegex.Replace(cleaned, string.Empty);
			cleaned = FlagsRegex.Replace(cleaned, string.Empty).Trim();
			parameters.CleanSearchTerm = cleaned;

			return parameters;
		}

		private bool MatchesSearchCriteria(PluginGameRequirements game, SearchParameters searchParams, GameSearchFilterSettings filterSettings)
		{
			// Early exit for deleted games
			if (game.IsDeleted)
			{
				return false;
			}

			// Check name match first (most likely to fail)
			if (!string.IsNullOrEmpty(searchParams.CleanSearchTerm) &&
				!game.Name.Contains(searchParams.CleanSearchTerm, StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}

			// Check install status
			if (!filterSettings.Uninstalled && !game.IsInstalled)
			{
				return false;
			}

			// Check hidden status
			if (!filterSettings.Hidden && game.Hidden)
			{
				return false;
			}

			// Check not played filter
			if (searchParams.HasNp && game.Playtime != 0)
			{
				return false;
			}

			// Check favorite filter
			if (searchParams.HasFav && !game.Favorite)
			{
				return false;
			}

			// Check stores filter
			if (searchParams.Stores.Count > 0)
			{
				string sourceName = game.Source?.Name;
				if (string.IsNullOrEmpty(sourceName))
				{
					return false;
				}

				bool storeMatched = false;
				foreach (string store in searchParams.Stores)
				{
					if (sourceName.IndexOf(store, StringComparison.OrdinalIgnoreCase) >= 0)
					{
						storeMatched = true;
						break;
					}
				}

				if (!storeMatched)
				{
					return false;
				}
			}

			// Check completion status filter
			if (searchParams.Status.Count > 0)
			{
				string completionStatusName = game.Game?.CompletionStatus?.Name;
				if (string.IsNullOrEmpty(completionStatusName))
				{
					return false;
				}

				bool statusMatched = false;
				foreach (string status in searchParams.Status)
				{
					if (completionStatusName.IndexOf(status, StringComparison.OrdinalIgnoreCase) >= 0)
					{
						statusMatched = true;
						break;
					}
				}

				if (!statusMatched)
				{
					return false;
				}
			}

			return true;
		}

		private bool IsGameValid(PluginGameRequirements gameReq, SearchParameters searchParams, SystemConfiguration systemConfiguration, out Game game)
		{
			game = API.Instance.Database.Games.Get(gameReq.Id);

			// If no system requirement filters, accept all
			if (!searchParams.HasMin && !searchParams.HasRec && !searchParams.HasAny)
			{
				return true;
			}

			RequirementEntry systemMinimum = gameReq.GetMinimum();
			RequirementEntry systemRecommended = gameReq.GetRecommended();

			// Check minimum requirements
			if (systemMinimum.HasData && (searchParams.HasMin || searchParams.HasAny))
			{
				CheckSystem checkMinimum = SystemApi.CheckConfig(game, systemMinimum, systemConfiguration, game.IsInstalled);
				if ((bool)checkMinimum.AllOk)
				{
					return true;
				}
			}

			// Check recommended requirements
			if (systemRecommended.HasData && (searchParams.HasRec || searchParams.HasAny))
			{
				CheckSystem checkRecommended = SystemApi.CheckConfig(game, systemRecommended, systemConfiguration, game.IsInstalled);
				if ((bool)checkRecommended.AllOk)
				{
					return true;
				}
			}

			return false;
		}

		private class SearchParameters
		{
			public bool HasMin { get; set; }
			public bool HasRec { get; set; }
			public bool HasAny { get; set; }
			public bool HasNp { get; set; }
			public bool HasFav { get; set; }
			public List<string> Stores { get; set; } = new List<string>();
			public List<string> Status { get; set; } = new List<string>();
			public string CleanSearchTerm { get; set; } = string.Empty;
		}
	}
}