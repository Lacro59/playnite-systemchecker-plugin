using AngleSharp.Dom.Html;
using AngleSharp.Parser.Html;
using Newtonsoft.Json;
using Playnite.Common.Web;
using Playnite.SDK;
using Playnite.SDK.Models;
using PluginCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SystemChecker.Models;

namespace SystemChecker.Clients
{
    class PCGamingWikiRequierements
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private IPlayniteAPI PlayniteApi;

        private readonly string urlSteamId = "https://pcgamingwiki.com/api/appid.php?appid={0}";
        private string urlPCGamingWiki = "";
        private int SteamId = 0;
        private Game game;



        private readonly string[] SizeSuffixes = { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };

        private string SizeSuffix(Int64 value)
        {
            if (value < 0) { return "-" + SizeSuffix(-value); }
            if (value == 0) { return "0.0 bytes"; }

            int mag = (int)Math.Log(value, 1024);
            decimal adjustedSize = (decimal)value / (1L << (mag * 10));

            return string.Format("{0:n1} {1}", adjustedSize, SizeSuffixes[mag]);
        }




        public PCGamingWikiRequierements(Game game, string PluginUserDataPath, IPlayniteAPI PlayniteApi)
        {
            this.PlayniteApi = PlayniteApi;
            this.game = game;

            SteamApi steamApi = new SteamApi(PluginUserDataPath);
            SteamId = steamApi.GetSteamId(game.Name);

            foreach (Link link in game.Links)
            {
                if (link.Name.ToLower() == "pcgamingwiki")
                {
                    urlPCGamingWiki = link.Url;
                }
            }
        }

        public GameRequierements GetRequirements(string url = "")
        {
            if (string.IsNullOrEmpty(url))
            {
                if (SteamId != 0)
                {
                    var result = GetRequirements(string.Format(urlSteamId, SteamId));
                    if (result.Minimum != new Requirement())
                    {
                        return result;
                    }
                }
                if (!urlPCGamingWiki.IsNullOrEmpty())
                {
                    var result = GetRequirements(urlPCGamingWiki);
                    if (result.Minimum != new Requirement())
                    {
                        return result;
                    }
                }
                if (url.IsNullOrEmpty())
                {
                    logger.Warn($"SystemChecker - Url not find for {game.Name}");
                }
            }






            GameRequierements gameRequierements = new GameRequierements();
            gameRequierements.Minimum = new Requirement();
            gameRequierements.Recommanded = new Requirement();

            try
            {
                logger.Debug($"SystemChecker - url {url}");

                // Get data & parse
                var webView = PlayniteApi.WebViews.CreateOffscreenView();
                webView.NavigateAndWait(url);

                HtmlParser parser = new HtmlParser();
                IHtmlDocument HtmlRequirement = parser.Parse(webView.GetPageSource());

                var systemRequierement = HtmlRequirement.QuerySelector("div.sysreq_Windows");
                if (systemRequierement != null)
                {
                    gameRequierements.Link = url;

                    foreach (var row in systemRequierement.QuerySelectorAll(".table-sysreqs-body-row"))
                    {
                        string dataTitle = row.QuerySelector(".table-sysreqs-body-parameter").InnerHtml.ToLower();
                        string dataMinimum = row.QuerySelector(".table-sysreqs-body-minimum").InnerHtml.Trim();
                        string dataRecommended = row.QuerySelector(".table-sysreqs-body-recommended").InnerHtml.Trim();

                        logger.Debug($"SystemChecker - dataMinimum: {dataMinimum}");
                        logger.Debug($"SystemChecker - dataRecommended: {dataRecommended}");

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

                                    foreach(var CPU in gameRequierements.Minimum.Cpu)
                                    {
                                        Cpu cpu = new Cpu(null, null);
                                        logger.Debug(JsonConvert.SerializeObject(cpu.SetProcessor(CPU)));
                                    }


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
