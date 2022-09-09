using CommonPluginsShared;
using CommonPluginsShared.Extensions;
using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SystemChecker.Models;

namespace SystemChecker.Services
{
    public class SystemCheckerSearch : SearchContext
    {
        private static IResourceProvider resources = new ResourceProvider();
        private readonly SystemCheckerDatabase PluginDatabase = SystemChecker.PluginDatabase;


        public SystemCheckerSearch()
        {
            Description = resources.GetString("LOCSystemCheckerSearchDescription");
            Label = PluginDatabase.PluginName;
            Hint = resources.GetString("LOCSystemCheckerSearchHint");
            Delay = 500;
        }

        public override IEnumerable<SearchItem> GetSearchResults(GetSearchResultsArgs args)
        {
            List<SearchItem> searchItems = new List<SearchItem>();

            try
            {
                // Parameters
                bool hasMin = false;
                bool hasRec = false;
                bool hasAny = false;
                bool hasNp = false;
                bool hasFav = false;
                List<string> stores = new List<string>();

                args.SearchTerm.Split(' ').ForEach(x => 
                {
                    if (!hasMin) hasMin = x.IsEqual("-min");
                    if (!hasRec) hasRec = x.IsEqual("-rec");
                    if (!hasAny) hasAny = x.IsEqual("-any");
                    if (!hasNp) hasNp = x.IsEqual("-np");
                    if (!hasFav) hasFav = x.IsEqual("-fav");

                    if (x.Contains("-stores=", StringComparison.InvariantCultureIgnoreCase))
                    {
                        stores = x.Replace("-stores=", string.Empty, StringComparison.InvariantCultureIgnoreCase).Split(',').ToList();
                    }
                });
                
                string SearchTerm = Regex.Replace(args.SearchTerm, @"-stores=(\w*,)*\w*", string.Empty, RegexOptions.IgnoreCase).Trim();
                SearchTerm = Regex.Replace(SearchTerm, @"-\w*", string.Empty, RegexOptions.IgnoreCase).Trim();


                SystemConfiguration systemConfiguration = PluginDatabase.Database.PC;


                // Search
                PluginDatabase.Database
                    .Where(x => x.Name.Contains(SearchTerm, StringComparison.InvariantCultureIgnoreCase)
                                && !x.IsDeleted
                                && (args.GameFilterSettings.Uninstalled || x.IsInstalled)
                                && (args.GameFilterSettings.Hidden || !x.Hidden)
                                && (!hasNp || x.Playtime == 0)
                                && (!hasFav || x.Favorite)
                                && (stores.Any(y => x.Source?.Name?.Contains(y, StringComparison.InvariantCultureIgnoreCase) ?? false))
                                )
                    .ForEach(x =>
                    {
                        Game game = API.Instance.Database.Games.Get(x.Id);
                        bool isOK = false;

                        // Calcul if necessary
                        if (hasMin || hasRec || hasAny)
                        {
                            Requirement systemMinimum = x.GetMinimum();
                            Requirement systemRecommanded = x.GetRecommanded();

                            CheckSystem CheckMinimum = SystemApi.CheckConfig(game, systemMinimum, systemConfiguration, game.IsInstalled);
                            CheckSystem CheckRecommanded = SystemApi.CheckConfig(game, systemRecommanded, systemConfiguration, game.IsInstalled);

                            if (systemMinimum.HasData && (hasMin || hasAny) && (bool)CheckMinimum.AllOk)
                            {
                                isOK = true;
                            }

                            if (systemRecommanded.HasData && (bool)CheckRecommanded.AllOk && (hasRec || hasAny))
                            {
                                isOK = true;
                            }
                        }
                        else
                        {
                            isOK = true;
                        }


                        if (isOK)
                        {
                            searchItems.Add(new GameSearchItem(game, resources.GetString("LOCGameSearchItemActionSwitchTo"), () => API.Instance.MainView.SelectGame(game.Id)));
                        }
                    });

                if (args.CancelToken.IsCancellationRequested)
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
                return null;
            }

            return searchItems;
        }
    }
}
