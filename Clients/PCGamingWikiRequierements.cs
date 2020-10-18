using AngleSharp.Dom.Html;
using AngleSharp.Parser.Html;
using Playnite.SDK;
using Playnite.SDK.Models;
using PluginCommon;
using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using SystemChecker.Models;

namespace SystemChecker.Clients
{
    class PCGamingWikiRequierements: RequierementMetadata
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private readonly IPlayniteAPI _PlayniteApi;

        private readonly string UrlSteamId = "https://pcgamingwiki.com/api/appid.php?appid={0}";
        private string UrlPCGamingWiki { get; set; } = string.Empty;
        private int SteamId { get; set; } = 0;


        public PCGamingWikiRequierements(Game game, string PluginUserDataPath, IPlayniteAPI PlayniteApi)
        {
            _PlayniteApi = PlayniteApi;
            _game = game;

            if (_game.SourceId != Guid.Parse("00000000-0000-0000-0000-000000000000"))
            {
                if (_game.Source.Name.ToLower() == "steam")
                {
                    SteamId = int.Parse(_game.GameId);
                }
            }
            if (SteamId == 0)
            {
                SteamApi steamApi = new SteamApi(PluginUserDataPath);
                SteamId = steamApi.GetSteamId(_game.Name);
            }

            if (_game.Links != null)
            {
                foreach (Link link in _game.Links)
                {
                    if (link.Url.ToLower().Contains("pcgamingwiki"))
                    {
                        UrlPCGamingWiki = link.Url;
                    }
                }
            }

            if (UrlPCGamingWiki.IsNullOrEmpty() && _game.ReleaseDate != null)
            {
                UrlPCGamingWiki = @"https://pcgamingwiki.com/w/index.php?search=" + _game.Name + $"+%28{((DateTime)_game.ReleaseDate).ToString("yyyy")}%29";
            }

#if DEBUG
            logger.Debug($"SystemChecker - PCGamingWikiRequierements - {_game.Name} - SteamId: {SteamId} - UrlPCGamingWiki: {UrlPCGamingWiki}");
#endif
        }

        public override GameRequierements GetRequirements()
        {
            // Search data with SteamId (is find) or game url (if defined)
            if (SteamId != 0)
            {
                gameRequierements = GetRequirements(string.Format(UrlSteamId, SteamId));
                if (IsFind())
                {
#if DEBUG
                    logger.Debug($"SystemChecker - PCGamingWikiRequierements.IsFind - SteamId: {SteamId}");
#endif

                    return gameRequierements;
                }
            }
            if (!UrlPCGamingWiki.IsNullOrEmpty())
            {
                gameRequierements = GetRequirements(UrlPCGamingWiki);
                if (IsFind())
                {
#if DEBUG
                    logger.Debug($"SystemChecker - PCGamingWikiRequierements.IsFind - UrlPCGamingWiki: {UrlPCGamingWiki}");
#endif

                    return gameRequierements;
                }
            }

            logger.Warn($"SystemChecker - PCGamingWikiRequierements - Not find for {_game.Name}");

            return gameRequierements; 
        }

        public override GameRequierements GetRequirements(string url)
        {
            try
            {
#if DEBUG
                logger.Debug($"SystemChecker PCGamingWikiRequierements.GetRequirements - - url {url}");
#endif

                // Get data & parse
                string ResultWeb = string.Empty;
                try
                {
                    ResultWeb =  Web.DownloadStringData(url).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, "SystemChecker", $"Failed to download {url}");
                }


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

                        string dataRecommended = string.Empty;
                        if (row.QuerySelector(".table-sysreqs-body-recommended") != null)
                        {
                            dataRecommended = row.QuerySelector(".table-sysreqs-body-recommended").InnerHtml.Trim();
                        }

#if DEBUG
                        logger.Debug($"SystemChecker - PCGamingWikiRequierements - dataMinimum: {dataMinimum}");
                        logger.Debug($"SystemChecker - PCGamingWikiRequierements - dataRecommended: {dataRecommended}");
#endif

                        switch (dataTitle)
                        {
                            case "operating system (os)":
                                if (!dataMinimum.IsNullOrEmpty())
                                {
                                    dataMinimum = dataMinimum.Replace("(1803 or later)", string.Empty)
                                        .Replace(" (Only inclusive until patch 1.16.1. Patch 1.17+ Needs XP and greater.)", string.Empty);
                                    gameRequierements.Minimum.Os = dataMinimum.Split(',').Select(x => x.Trim()).ToList();
                                }
                                if (!dataRecommended.IsNullOrEmpty())
                                {
                                    dataRecommended = dataRecommended.Replace("(1803 or later)", string.Empty)
                                        .Replace(" (Only inclusive until patch 1.16.1. Patch 1.17+ Needs XP and greater.)", string.Empty);
                                    gameRequierements.Recommanded.Os = dataRecommended.Split(',').Select(x => x.Trim()).ToList();
                                }
                                break;

                            case "processor (cpu)":
                                if (!dataMinimum.IsNullOrEmpty())
                                {
                                    dataMinimum = dataMinimum.Replace("(or equivalent)", string.Empty).Replace("or equivalent", string.Empty)
                                        .Replace("or AMD equivalent", string.Empty)
                                        .Replace(" / ", "¤").Replace("<br>", "¤");
                                    gameRequierements.Minimum.Cpu = dataMinimum.Split('¤').Select(x => x.Trim()).ToList();
                                }
                                if (!dataRecommended.IsNullOrEmpty())
                                {
                                    dataRecommended = dataRecommended.Replace("(or equivalent)", string.Empty).Replace("or equivalent", string.Empty)
                                        .Replace("or AMD equivalent", string.Empty)
                                        .Replace(" / ", "¤").Replace("<br>", "¤");
                                    gameRequierements.Recommanded.Cpu = dataMinimum.Split('¤').Select(x => x.Trim()).ToList();
                                }
                                break;

                            case "system memory (ram)":
                                if (!dataMinimum.IsNullOrEmpty())
                                {
                                    if (dataMinimum.ToLower().IndexOf("mb") > -1)
                                    {
                                        dataMinimum = dataMinimum.Substring(0, dataMinimum.ToLower().IndexOf("mb"));
                                        gameRequierements.Minimum.Ram = 1024 * 1024 * long.Parse(dataMinimum.ToLower().Replace("mb", string.Empty).Trim());
                                    }
                                    if (dataMinimum.ToLower().IndexOf("gb") > -1)
                                    {
                                        dataMinimum = dataMinimum.Substring(0, dataMinimum.ToLower().IndexOf("gb"));
                                        gameRequierements.Minimum.Ram = 1024 * 1024 * 1024 * long.Parse(dataMinimum.ToLower().Replace("gb", string.Empty).Trim());
                                    }
                                    gameRequierements.Minimum.RamUsage = SizeSuffix(gameRequierements.Minimum.Ram, true);
                                }
                                if (!dataRecommended.IsNullOrEmpty())
                                {
                                    if (dataRecommended.ToLower().IndexOf("mb") > -1)
                                    {
                                        dataRecommended = dataRecommended.Substring(0, dataRecommended.ToLower().IndexOf("mb"));
                                        gameRequierements.Recommanded.Ram = 1024 * 1024 * long.Parse(dataRecommended.ToLower().Replace("mb", string.Empty).Trim());
                                    }
                                    if (dataRecommended.ToLower().IndexOf("gb") > -1)
                                    {
                                        dataRecommended = dataRecommended.Substring(0, dataRecommended.ToLower().IndexOf("gb"));
                                        gameRequierements.Recommanded.Ram = 1024 * 1024 * 1024 * long.Parse(dataRecommended.ToLower().Replace("gb", string.Empty).Trim());
                                    }
                                    gameRequierements.Recommanded.RamUsage = SizeSuffix(gameRequierements.Recommanded.Ram, true);
                                }
                                break;

                            case "hard disk drive (hdd)":
                                double hdd = 0;
                                if (!dataMinimum.IsNullOrEmpty())
                                {
                                    if (dataMinimum.ToLower().IndexOf("mb") > -1)
                                    {
                                        dataMinimum = dataMinimum.Substring(0, dataMinimum.ToLower().IndexOf("mb"));

                                        Double.TryParse(dataMinimum.ToLower().Replace("mb", string.Empty).Trim()
                                            .Replace(".", CultureInfo.CurrentUICulture.NumberFormat.NumberDecimalSeparator).Trim()
                                            .Replace(",", CultureInfo.CurrentUICulture.NumberFormat.NumberDecimalSeparator).Trim(), out hdd);

                                        gameRequierements.Minimum.Storage = (long)(1024 * 1024 * hdd);
                                    }
                                    if (dataMinimum.ToLower().IndexOf("gb") > -1)
                                    {
                                        dataMinimum = dataMinimum.Substring(0, dataMinimum.ToLower().IndexOf("gb"));

                                        Double.TryParse(dataMinimum.ToLower().Replace("mb", string.Empty).Trim()
                                            .Replace(".", CultureInfo.CurrentUICulture.NumberFormat.NumberDecimalSeparator).Trim()
                                            .Replace(",", CultureInfo.CurrentUICulture.NumberFormat.NumberDecimalSeparator).Trim(), out hdd);

                                        gameRequierements.Minimum.Storage = (long)(1024 * 1024 * 1024 * hdd);
                                    }
                                    gameRequierements.Minimum.StorageUsage = SizeSuffix(gameRequierements.Minimum.Storage);
                                }
                                if (!dataRecommended.IsNullOrEmpty())
                                {
                                    if (dataRecommended.ToLower().IndexOf("mb") > -1)
                                    {
                                        dataRecommended = dataRecommended.Substring(0, dataRecommended.ToLower().IndexOf("mb"));

                                        Double.TryParse(dataMinimum.ToLower().Replace("mb", string.Empty).Trim()
                                           .Replace(".", CultureInfo.CurrentUICulture.NumberFormat.NumberDecimalSeparator).Trim()
                                           .Replace(",", CultureInfo.CurrentUICulture.NumberFormat.NumberDecimalSeparator).Trim(), out hdd);

                                        gameRequierements.Minimum.Storage = (long)(1024 * 1024 * hdd);
                                    }
                                    if (dataRecommended.ToLower().IndexOf("gb") > -1)
                                    {
                                        dataRecommended = dataRecommended.Substring(0, dataRecommended.ToLower().IndexOf("gb"));

                                        Double.TryParse(dataMinimum.ToLower().Replace("mb", string.Empty).Trim()
                                           .Replace(".", CultureInfo.CurrentUICulture.NumberFormat.NumberDecimalSeparator).Trim()
                                           .Replace(",", CultureInfo.CurrentUICulture.NumberFormat.NumberDecimalSeparator).Trim(), out hdd);

                                        gameRequierements.Minimum.Storage = (long)(1024 * 1024 * 1024 * hdd);
                                    }
                                    gameRequierements.Recommanded.StorageUsage = SizeSuffix(gameRequierements.Recommanded.Storage);
                                }
                                break;

                            case "video card (gpu)":
                                if (!dataMinimum.IsNullOrEmpty())
                                {
                                    dataMinimum = dataMinimum.Replace("(or equivalent)", string.Empty).Replace("or equivalent", string.Empty)
                                        .Replace("Integrated", string.Empty).Replace("Dedicated", string.Empty)
                                        .Replace("+ compatible", string.Empty).Replace("compatible", string.Empty)
                                        .Replace("that supports DirectDraw at 640x480 resolution, 256 colors", string.Empty)
                                        .Replace(" / ", "¤").Replace("<br>", "¤");

                                    dataMinimum = Regex.Replace(dataMinimum, "(</[^>]*>)", "");
                                    dataMinimum = Regex.Replace(dataMinimum, "(<[^>]*>)", "");

                                    gameRequierements.Minimum.Gpu = dataMinimum.Split('¤')
                                        .Select(x => x.Trim()).ToList()
                                        .Where(x => x.ToLower().IndexOf("shader") == -1)
                                        .Where(x => x.Trim() != string.Empty).ToList();
                                }
                                if (!dataRecommended.IsNullOrEmpty())
                                {
                                    dataRecommended = dataRecommended.Replace("(or equivalent)", string.Empty).Replace("or equivalent", string.Empty)
                                        .Replace("Integrated", string.Empty).Replace("Dedicated", string.Empty)
                                        .Replace("+ compatible", string.Empty).Replace("compatible", string.Empty)
                                        .Replace("that supports DirectDraw at 640x480 resolution, 256 colors", string.Empty)
                                        .Replace(" / ", "¤").Replace("<br>", "¤");

                                    dataRecommended = Regex.Replace(dataRecommended, "(</[^>]*>)", "");
                                    dataRecommended = Regex.Replace(dataRecommended, "(<[^>]*>)", "");

                                    gameRequierements.Recommanded.Gpu = dataRecommended.Split('¤')
                                        .Select(x => x.Trim()).ToList()
                                        .Where(x => x.ToLower().IndexOf("shader") == -1).ToList()
                                        .Where(x => x.Trim() != string.Empty).ToList();
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
                    logger.Warn($"SystemChecker - No data find for {_game.Name} in {url}");
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
