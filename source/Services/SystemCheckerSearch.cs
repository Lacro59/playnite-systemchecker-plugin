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

				IEnumerable<PluginGameRequierements> filteredGames = PluginDatabase.Database
					.Where(x => MatchesSearchCriteria(x, searchParams, args.GameFilterSettings));

				foreach (PluginGameRequierements x in filteredGames)
				{
					if (args.CancelToken.IsCancellationRequested)
					{
						return null;
					}

					if (IsGameValid(x, searchParams, systemConfiguration, out Game game))
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
			string[] terms = searchTerm.Split(' ');

			foreach (string term in terms)
			{
				if (!parameters.HasMin) parameters.HasMin = term.IsEqual("-min");
				if (!parameters.HasRec) parameters.HasRec = term.IsEqual("-rec");
				if (!parameters.HasAny) parameters.HasAny = term.IsEqual("-any");
				if (!parameters.HasNp) parameters.HasNp = term.IsEqual("-np");
				if (!parameters.HasFav) parameters.HasFav = term.IsEqual("-fav");

				if (term.Contains("-stores=", StringComparison.InvariantCultureIgnoreCase))
				{
					parameters.Stores = term
						.Replace("-stores=", string.Empty, StringComparison.InvariantCultureIgnoreCase)
						.Split(',')
						.ToList();
				}

				if (term.Contains("-status=", StringComparison.InvariantCultureIgnoreCase))
				{
					parameters.Status = term
						.Replace("-status=", string.Empty, StringComparison.InvariantCultureIgnoreCase)
						.Split(',')
						.ToList();
				}
			}

			parameters.CleanSearchTerm = Regex.Replace(searchTerm, @"-stores=(\w*,)*\w*", string.Empty, RegexOptions.IgnoreCase);
			parameters.CleanSearchTerm = Regex.Replace(parameters.CleanSearchTerm, @"-status=(\w*,)*\w*", string.Empty, RegexOptions.IgnoreCase);
			parameters.CleanSearchTerm = Regex.Replace(parameters.CleanSearchTerm, @"-\w*", string.Empty, RegexOptions.IgnoreCase).Trim();

			return parameters;
		}

		private bool MatchesSearchCriteria(PluginGameRequierements game, SearchParameters searchParams, GameSearchFilterSettings filterSettings)
		{
			if (!game.Name.Contains(searchParams.CleanSearchTerm, StringComparison.InvariantCultureIgnoreCase) || game.IsDeleted)
			{
				return false;
			}

			if (!filterSettings.Uninstalled && !game.IsInstalled)
			{
				return false;
			}

			if (!filterSettings.Hidden && game.Hidden)
			{
				return false;
			}

			if (searchParams.HasNp && game.Playtime != 0)
			{
				return false;
			}

			if (searchParams.HasFav && !game.Favorite)
			{
				return false;
			}

			if (searchParams.Stores.Count > 0 && !searchParams.Stores.Any(y => game.Source?.Name?.Contains(y, StringComparison.InvariantCultureIgnoreCase) ?? false))
			{
				return false;
			}

			if (searchParams.Status.Count > 0 && !searchParams.Status.Any(y => game.Game?.CompletionStatus?.Name?.Contains(y, StringComparison.InvariantCultureIgnoreCase) ?? false))
			{
				return false;
			}

			return true;
		}

		private bool IsGameValid(PluginGameRequierements gameReq, SearchParameters searchParams, SystemConfiguration systemConfiguration, out Game game)
		{
			game = API.Instance.Database.Games.Get(gameReq.Id);

			if (!searchParams.HasMin && !searchParams.HasRec && !searchParams.HasAny)
			{
				return true;
			}

			RequirementEntry systemMinimum = gameReq.GetMinimum();
			RequirementEntry systemRecommanded = gameReq.GetRecommanded();

			if (systemMinimum.HasData && (searchParams.HasMin || searchParams.HasAny))
			{
				CheckSystem checkMinimum = SystemApi.CheckConfig(game, systemMinimum, systemConfiguration, game.IsInstalled);
				if ((bool)checkMinimum.AllOk)
				{
					return true;
				}
			}

			if (systemRecommanded.HasData && (searchParams.HasRec || searchParams.HasAny))
			{
				CheckSystem checkRecommanded = SystemApi.CheckConfig(game, systemRecommanded, systemConfiguration, game.IsInstalled);
				if ((bool)checkRecommanded.AllOk)
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