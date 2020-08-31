using AngleSharp.Dom.Html;
using AngleSharp.Parser.Html;
using Playnite.SDK;
using Playnite.SDK.Models;
using PluginCommon;
using System;
using System.Linq;
using SystemChecker.Models;

namespace SystemChecker.Clients
{
    class PCGamingWikiRequierements: RequierementMetadata
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private IPlayniteAPI PlayniteApi;

        private readonly string urlSteamId = "https://pcgamingwiki.com/api/appid.php?appid={0}";
        private string urlPCGamingWiki = "";
        private int SteamId = 0;


        public PCGamingWikiRequierements(Game game, string PluginUserDataPath, IPlayniteAPI PlayniteApi)
        {
            this.PlayniteApi = PlayniteApi;
            this.game = game;

            if (game.SourceId != Guid.Parse("00000000-0000-0000-0000-000000000000"))
            {
                if (game.Source.Name.ToLower() == "steam")
                {
                    SteamId = int.Parse(game.GameId);
                }
            }
            if (SteamId == 0)
            {
                SteamApi steamApi = new SteamApi(PluginUserDataPath);
                SteamId = steamApi.GetSteamId(game.Name);
            }

            foreach (Link link in game.Links)
            {
                if (link.Url.ToLower().Contains("pcgamingwiki"))
                {
                    urlPCGamingWiki = link.Url;
                }
            }

            logger.Debug($"SystemChecker - PCGamingWikiRequierements - {game.Name} - SteamId: {SteamId} - urlPCGamingWiki: {urlPCGamingWiki}");
        }

        public override GameRequierements GetRequirements()
        {
            // Search data with SteamId (is find) or game url (if defined)
            if (SteamId != 0)
            {
                gameRequierements = GetRequirements(string.Format(urlSteamId, SteamId));
                if (isFind())
                {
                    return gameRequierements;
                }
            }
            if (!urlPCGamingWiki.IsNullOrEmpty())
            {
                gameRequierements = GetRequirements(urlPCGamingWiki);
                if (isFind())
                {
                    return gameRequierements;
                }
            }

            logger.Warn($"SystemChecker - Not find for {game.Name}");

            return gameRequierements; 
        }

        public override GameRequierements GetRequirements(string url)
        {
            try
            {
                logger.Debug($"SystemChecker - url {url}");

                // Get data & parse
                string ResultWeb = DonwloadStringData(url).GetAwaiter().GetResult(); ;
                HtmlParser parser = new HtmlParser();
                IHtmlDocument HtmlRequirement = parser.Parse(ResultWeb);

                var systemRequierement = HtmlRequirement.QuerySelector("div.sysreq_Windows");
                if (systemRequierement != null)
                {
                    gameRequierements.Link = url;

                    foreach (var row in systemRequierement.QuerySelectorAll(".table-sysreqs-body-row"))
                    {
                        string dataTitle = row.QuerySelector(".table-sysreqs-body-parameter").InnerHtml.ToLower();
                        string dataMinimum = row.QuerySelector(".table-sysreqs-body-minimum").InnerHtml.Trim();
                        string dataRecommended = row.QuerySelector(".table-sysreqs-body-recommended").InnerHtml.Trim();

                        logger.Debug($"SystemChecker - PCGamingWikiRequierements - dataMinimum: {dataMinimum}");
                        logger.Debug($"SystemChecker - PCGamingWikiRequierements - dataRecommended: {dataRecommended}");

                        switch (dataTitle)
                        {
                            case "operating system (os)":
                                if (!dataMinimum.IsNullOrEmpty())
                                {
                                    gameRequierements.Minimum.Os = dataMinimum.Split(',').Select(x => x.Trim()).ToList();
                                    gameRequierements.Recommanded.Os = gameRequierements.Minimum.Os;
                                }
                                if (!dataRecommended.IsNullOrEmpty())
                                {
                                    gameRequierements.Recommanded.Os = dataRecommended.Split(',').Select(x => x.Trim()).ToList();
                                }
                                break;

                            case "processor (cpu)":
                                if (!dataMinimum.IsNullOrEmpty())
                                {
                                    dataMinimum = dataMinimum.Replace("or equivalent", "")
                                        .Replace(" / ", "¤").Replace("<br>", "¤");
                                    gameRequierements.Minimum.Cpu = dataMinimum.Split('¤').Select(x => x.Trim()).ToList();
                                }
                                if (!dataRecommended.IsNullOrEmpty())
                                {
                                    dataRecommended = dataRecommended.Replace("or equivalent", "")
                                        .Replace(" / ", "¤").Replace("<br>", "¤");
                                    gameRequierements.Recommanded.Cpu = dataMinimum.Split('¤').Select(x => x.Trim()).ToList();
                                }
                                break;

                            case "system memory (ram)":
                                if (!dataMinimum.IsNullOrEmpty())
                                {
                                    if (dataMinimum.ToLower().IndexOf("mb") > -1)
                                    {
                                        gameRequierements.Minimum.Ram = 1024 * 1024 * long.Parse(dataMinimum.ToLower().Replace("mb", "").Trim());
                                    }
                                    if (dataMinimum.ToLower().IndexOf("gb") > -1)
                                    {
                                        gameRequierements.Minimum.Ram = 1024 * 1024 * 1024 * long.Parse(dataMinimum.ToLower().Replace("gb", "").Trim());
                                    }
                                    gameRequierements.Minimum.RamUsage = SizeSuffix(gameRequierements.Minimum.Ram);
                                }
                                if (!dataRecommended.IsNullOrEmpty())
                                {
                                    if (dataRecommended.ToLower().IndexOf("mb") > -1)
                                    {
                                        gameRequierements.Recommanded.Ram = 1024 * 1024 * long.Parse(dataRecommended.ToLower().Replace("mb", "").Trim());
                                    }
                                    if (dataRecommended.ToLower().IndexOf("gb") > -1)
                                    {
                                        gameRequierements.Recommanded.Ram = 1024 * 1024 * 1024 * long.Parse(dataRecommended.ToLower().Replace("gb", "").Trim());
                                    }
                                    gameRequierements.Recommanded.RamUsage = SizeSuffix(gameRequierements.Recommanded.Ram);
                                }
                                break;

                            case "hard disk drive (hdd)":
                                if (!dataMinimum.IsNullOrEmpty())
                                {
                                    if (dataMinimum.ToLower().IndexOf("mb") > -1)
                                    {
                                        gameRequierements.Minimum.Storage = 1024 * 1024 * long.Parse(dataMinimum.ToLower().Replace("mb", "").Trim());
                                        gameRequierements.Recommanded.Storage = 1024 * 1024 * long.Parse(dataMinimum.ToLower().Replace("mb", "").Trim());
                                    }
                                    if (dataMinimum.ToLower().IndexOf("gb") > -1)
                                    {
                                        gameRequierements.Minimum.Storage = 1024 * 1024 * 1024 * long.Parse(dataMinimum.ToLower().Replace("gb", "").Trim());
                                        gameRequierements.Recommanded.Storage = 1024 * 1024 * 1024 * long.Parse(dataMinimum.ToLower().Replace("gb", "").Trim());
                                    }
                                    gameRequierements.Minimum.StorageUsage = SizeSuffix(gameRequierements.Minimum.Storage);
                                    gameRequierements.Recommanded.StorageUsage = SizeSuffix(gameRequierements.Recommanded.Storage);
                                }
                                if (!dataRecommended.IsNullOrEmpty())
                                {
                                    if (dataRecommended.ToLower().IndexOf("mb") > -1)
                                    {
                                        gameRequierements.Minimum.Storage = 1024 * 1024 * long.Parse(dataRecommended.ToLower().Replace("mb", "").Trim());
                                    }
                                    if (dataRecommended.ToLower().IndexOf("gb") > -1)
                                    {
                                        gameRequierements.Minimum.Storage = 1024 * 1024 * 1024 * long.Parse(dataRecommended.ToLower().Replace("gb", "").Trim());
                                    }
                                    gameRequierements.Recommanded.StorageUsage = SizeSuffix(gameRequierements.Recommanded.Storage);
                                }
                                break;

                            case "video card (gpu)":
                                if (!dataMinimum.IsNullOrEmpty())
                                {
                                    dataMinimum = dataMinimum.Replace("or equivalent", "")
                                        .Replace(" / ", "¤").Replace("<br>", "¤");
                                    gameRequierements.Minimum.Gpu = dataMinimum.Split('¤').Select(x => x.Trim()).ToList()
                                        .Where(x => x.ToLower().IndexOf("shader") == -1).ToList();
                                }
                                if (!dataRecommended.IsNullOrEmpty())
                                {
                                    dataRecommended = dataRecommended.Replace("or equivalent", "")
                                        .Replace(" / ", "¤").Replace("<br>", "¤");
                                    gameRequierements.Recommanded.Gpu = dataMinimum.Split('¤').Select(x => x.Trim()).ToList()
                                        .Where(x => x.ToLower().IndexOf("shader") == -1).ToList();
                                }
                                break;

                            default :
                                logger.Warn($"SystemChecker - No treatment for {dataTitle}");
                                break;
                        }
                    }
                }
                else
                {
                    logger.Warn($"SystemChecker - No data find for {game.Name} in {url}");
                }

            }
            catch (Exception ex)
            {
                Common.LogError(ex, "SystemChecker", "Error on PCGamingWikiRequierements.GetRequirements()");
            }

            return gameRequierements;
        }



    }
}
