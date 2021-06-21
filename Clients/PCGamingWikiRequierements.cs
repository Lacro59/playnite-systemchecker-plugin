using AngleSharp.Dom.Html;
using AngleSharp.Parser.Html;
using CommonPluginsShared;
using CommonPluginsStores;
using Newtonsoft.Json;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using SystemChecker.Models;

namespace SystemChecker.Clients
{
    class PCGamingWikiRequierements : RequierementMetadata
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private readonly IPlayniteAPI _PlayniteApi;

        private SteamApi steamApi;

        private readonly string UrlSteamId = "https://pcgamingwiki.com/api/appid.php?appid={0}";
        private string UrlPCGamingWiki { get; set; } = string.Empty;
        private string UrlPCGamingWikiSearch { get; set; } = @"https://pcgamingwiki.com/w/index.php?search=";
        private int SteamId { get; set; } = 0;


        public PCGamingWikiRequierements(IPlayniteAPI PlayniteApi, string PluginUserDataPath)
        {
            _PlayniteApi = PlayniteApi;
            steamApi = new SteamApi();
        }


        private string FindGoodUrl(Game game, int SteamId = 0)
        {
            string url = string.Empty;
            string WebResponse = string.Empty;


            url = string.Empty;
            if (SteamId != 0)
            {
                url = string.Format(UrlSteamId, SteamId);

                WebResponse = Web.DownloadStringData(url).GetAwaiter().GetResult();
                if (!WebResponse.ToLower().Contains("search results"))
                {
                    return url;
                }
            }


            url = string.Empty;
            if (game.Links != null)
            {
                foreach (Link link in game.Links)
                {
                    if (link.Url.ToLower().Contains("pcgamingwiki"))
                    {
                        url = link.Url;

                        if (url.Contains(UrlPCGamingWikiSearch))
                        {
                            url = UrlPCGamingWikiSearch + WebUtility.UrlEncode(url.Replace(UrlPCGamingWikiSearch, string.Empty));
                        }
                        if (url.Contains(@"http://pcgamingwiki.com/w/index.php?search="))
                        {
                            url = UrlPCGamingWikiSearch + WebUtility.UrlEncode(url.Replace(@"http://pcgamingwiki.com/w/index.php?search=", string.Empty));
                        }
                    }
                }

                if (!url.IsNullOrEmpty())
                {
                    WebResponse = Web.DownloadStringData(url).GetAwaiter().GetResult();
                    if (!WebResponse.ToLower().Contains("search results"))
                    {
                        return url;
                    }
                }
            }


            url = UrlPCGamingWikiSearch + WebUtility.UrlEncode(game.Name);
            if (game.ReleaseDate != null)
            {
                 url += $"+%28{((DateTime)game.ReleaseDate).ToString("yyyy")}%29";
            }

            WebResponse = Web.DownloadStringData(url).GetAwaiter().GetResult();
            if (!WebResponse.ToLower().Contains("search results"))
            {
                return url;
            }


            string Name = Regex.Replace(game.Name, @"([ ]demo\b)", string.Empty, RegexOptions.IgnoreCase);
            Name = Regex.Replace(Name, @"(demo[ ])", string.Empty, RegexOptions.IgnoreCase);

            url = string.Empty;
            url = UrlPCGamingWikiSearch + WebUtility.UrlEncode(Name);

            WebResponse = Web.DownloadStringData(url).GetAwaiter().GetResult();
            if (!WebResponse.ToLower().Contains("search results"))
            {
                return url;
            }


            return string.Empty;
        }


        public override GameRequierements GetRequirements()
        {
            gameRequierements = SystemChecker.PluginDatabase.GetDefault(_game);

            UrlPCGamingWiki = FindGoodUrl(_game, SteamId);

            if (!UrlPCGamingWiki.IsNullOrEmpty())
            {
                gameRequierements = GetRequirements(UrlPCGamingWiki);
            }
            else
            {
                logger.Warn($"PCGamingWikiRequierements - Not find for {_game.Name}");
            }

            return gameRequierements;
        }

        public GameRequierements GetRequirements(Game game)
        {
            _game = game;
            SteamId = 0;
            UrlPCGamingWiki = string.Empty;


            if (_game.SourceId != default(Guid))
            {
                if (game.Source.Name.ToLower() == "steam")
                {
                    SteamId = int.Parse(game.GameId);
                }
            }
            if (SteamId == 0)
            {
                SteamId = steamApi.GetSteamId(game.Name);
            }


            return GetRequirements();
        }

        public override GameRequierements GetRequirements(string url)
        {
            try
            {
#if DEBUG
                logger.Debug($"PCGamingWikiRequierements.GetRequirements - url {url}");
#endif

                // Get data & parse
                string ResultWeb = string.Empty;
                try
                {
                    ResultWeb = Web.DownloadStringData(url).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, $"Failed to download {url}");
                }


                HtmlParser parser = new HtmlParser();
                IHtmlDocument HtmlRequirement = parser.Parse(ResultWeb);

                var systemRequierement = HtmlRequirement.QuerySelector("div.sysreq_Windows");
                if (systemRequierement != null)
                {
                    gameRequierements.Link = url;

                    Requirement Minimum = new Requirement();
                    Requirement Recommanded = new Requirement();

                    foreach (var row in systemRequierement.QuerySelectorAll(".table-sysreqs-body-row"))
                    {
                        string dataTitle = row.QuerySelector(".table-sysreqs-body-parameter").InnerHtml.ToLower();
                        string dataMinimum = row.QuerySelector(".table-sysreqs-body-minimum").InnerHtml.Trim();

                        string dataRecommended = string.Empty;
                        if (row.QuerySelector(".table-sysreqs-body-recommended") != null)
                        {
                            dataRecommended = row.QuerySelector(".table-sysreqs-body-recommended").InnerHtml.Trim();
                        }

                        Common.LogDebug(true, $"PCGamingWikiRequierements - dataMinimum: {dataMinimum}");
                        Common.LogDebug(true, $"PCGamingWikiRequierements - dataRecommended: {dataRecommended}");

                        switch (dataTitle)
                        {
                            case "operating system (os)":
                                if (!dataMinimum.IsNullOrEmpty())
                                {
                                    dataMinimum = dataMinimum.Replace("(1803 or later)", string.Empty)
                                        .Replace(" (Only inclusive until patch 1.16.1. Patch 1.17+ Needs XP and greater.)", string.Empty)
                                        .Replace("Windows", string.Empty)
                                        .Replace("or better", string.Empty)
                                        .Replace(",", "¤").Replace(" or ", "¤").Replace("/", "¤");
                                    Minimum.Os = dataMinimum.Split('¤').Select(x => x.Trim()).ToList();
                                }
                                if (!dataRecommended.IsNullOrEmpty())
                                {
                                    dataRecommended = dataRecommended.Replace("(1803 or later)", string.Empty)
                                        .Replace(" (Only inclusive until patch 1.16.1. Patch 1.17+ Needs XP and greater.)", string.Empty)
                                        .Replace("Windows", string.Empty)
                                        .Replace("or better", string.Empty)
                                        .Replace(",", "¤").Replace(" or ", "¤").Replace("/", "¤");
                                    Recommanded.Os = dataRecommended.Split('¤').Select(x => x.Trim()).ToList();
                                }
                                break;

                            case "processor (cpu)":
                                if (!dataMinimum.IsNullOrEmpty())
                                {
                                    dataMinimum = dataMinimum.Replace("(or equivalent)", string.Empty).Replace("or equivalent", string.Empty)
                                        .Replace("(4 CPUs), ~2.4 GHz", string.Empty)
                                        .Replace("(4 CPUs), ~3.1 GHz", string.Empty)
                                        .Replace(" from Intel or AMD at", string.Empty)
                                        .Replace("with SSE2 instruction set support", string.Empty)
                                        .Replace("(and graphics card with T&amp;L)", string.Empty)
                                        .Replace("(1.5 GHz if graphics card does not support T&amp;L)", string.Empty)
                                        .Replace("or AMD equivalent", string.Empty)
                                        .Replace("or better", string.Empty)
                                        .Replace("or higher", string.Empty)
                                        .Replace("(D3D)/300", string.Empty)
                                        .Replace("(with 3D acceleration)", string.Empty)
                                        .Replace("(software)", string.Empty)
                                        .Replace(" / ", "¤").Replace("<br>", "¤").Replace(" or ", "¤");
                                    Minimum.Cpu = dataMinimum.Split('¤').Select(x => x.Trim()).ToList();
                                }
                                if (!dataRecommended.IsNullOrEmpty())
                                {
                                    dataRecommended = dataRecommended.Replace("(or equivalent)", string.Empty).Replace("or equivalent", string.Empty)
                                        .Replace("(4 CPUs), ~2.4 GHz", string.Empty)
                                        .Replace("(4 CPUs), ~3.1 GHz", string.Empty)
                                        .Replace(" from Intel or AMD at", string.Empty)
                                        .Replace("with SSE2 instruction set support", string.Empty)
                                        .Replace("(and graphics card with T&amp;L)", string.Empty)
                                        .Replace("(1.5 GHz if graphics card does not support T&amp;L)", string.Empty)
                                        .Replace("or AMD equivalent", string.Empty)
                                        .Replace("or better", string.Empty)
                                        .Replace("or higher", string.Empty)
                                        .Replace("(D3D)/300", string.Empty)
                                        .Replace("(with 3D acceleration)", string.Empty)
                                        .Replace("(software)", string.Empty)
                                        .Replace(" / ", "¤").Replace("<br>", "¤").Replace(" or ", "¤");
                                    Recommanded.Cpu = dataRecommended.Split('¤').Select(x => x.Trim()).ToList();
                                }
                                break;

                            case "system memory (ram)":
                                if (!dataMinimum.IsNullOrEmpty())
                                {
                                    dataMinimum = dataMinimum.ToLower().Replace("ram mb ram", string.Empty);
                                    dataMinimum = dataMinimum.ToLower().Replace("ram", string.Empty);
                                    if (dataMinimum.ToLower().IndexOf("mb") > -1)
                                    {
                                        dataMinimum = dataMinimum.Substring(0, dataMinimum.ToLower().IndexOf("mb"));
                                        dataMinimum = dataMinimum.Replace(".", CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalSeparator);
                                        Minimum.Ram = 1024 * 1024 * double.Parse(dataMinimum.ToLower().Replace("mb", string.Empty).Trim());
                                    }
                                    if (dataMinimum.ToLower().IndexOf("gb") > -1)
                                    {
                                        dataMinimum = dataMinimum.Substring(0, dataMinimum.ToLower().IndexOf("gb"));
                                        dataMinimum = dataMinimum.Replace(".", CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalSeparator);
                                        Minimum.Ram = 1024 * 1024 * 1024 * double.Parse(dataMinimum.ToLower().Replace("gb", string.Empty).Trim());
                                    }
                                    Minimum.RamUsage = Tools.SizeSuffix(Minimum.Ram, true);
                                }
                                if (!dataRecommended.IsNullOrEmpty())
                                {
                                    dataRecommended = dataRecommended.ToLower().Replace("ram mb ram", string.Empty);
                                    dataRecommended = dataRecommended.ToLower().Replace("ram", string.Empty);
                                    if (dataRecommended.ToLower().IndexOf("mb") > -1)
                                    {
                                        dataRecommended = dataRecommended.Substring(0, dataRecommended.ToLower().IndexOf("mb"));
                                        dataRecommended = dataRecommended.Replace(".", CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalSeparator);
                                        Recommanded.Ram = 1024 * 1024 * double.Parse(dataRecommended.ToLower().Replace("mb", string.Empty).Trim());
                                    }
                                    if (dataRecommended.ToLower().IndexOf("gb") > -1)
                                    {
                                        dataRecommended = dataRecommended.Substring(0, dataRecommended.ToLower().IndexOf("gb"));
                                        dataRecommended = dataRecommended.Replace(".", CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalSeparator);
                                        Recommanded.Ram = 1024 * 1024 * 1024 * double.Parse(dataRecommended.ToLower().Replace("gb", string.Empty).Trim());
                                    }
                                    Recommanded.RamUsage = Tools.SizeSuffix(Recommanded.Ram, true);
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

                                        Minimum.Storage = (long)(1024 * 1024 * hdd);
                                    }
                                    if (dataMinimum.ToLower().IndexOf("gb") > -1)
                                    {
                                        dataMinimum = dataMinimum.Substring(0, dataMinimum.ToLower().IndexOf("gb"));

                                        Double.TryParse(dataMinimum.ToLower().Replace("mb", string.Empty).Trim()
                                            .Replace(".", CultureInfo.CurrentUICulture.NumberFormat.NumberDecimalSeparator).Trim()
                                            .Replace(",", CultureInfo.CurrentUICulture.NumberFormat.NumberDecimalSeparator).Trim(), out hdd);

                                        Minimum.Storage = (long)(1024 * 1024 * 1024 * hdd);
                                    }
                                    Minimum.StorageUsage = Tools.SizeSuffix(Minimum.Storage);
                                }
                                if (!dataRecommended.IsNullOrEmpty())
                                {
                                    if (dataRecommended.ToLower().IndexOf("mb") > -1)
                                    {
                                        dataRecommended = dataRecommended.Substring(0, dataRecommended.ToLower().IndexOf("mb"));

                                        Double.TryParse(dataRecommended.ToLower().Replace("mb", string.Empty).Trim()
                                           .Replace(".", CultureInfo.CurrentUICulture.NumberFormat.NumberDecimalSeparator).Trim()
                                           .Replace(",", CultureInfo.CurrentUICulture.NumberFormat.NumberDecimalSeparator).Trim(), out hdd);

                                        Recommanded.Storage = (long)(1024 * 1024 * hdd);
                                    }
                                    if (dataRecommended.ToLower().IndexOf("gb") > -1)
                                    {
                                        dataRecommended = dataRecommended.Substring(0, dataRecommended.ToLower().IndexOf("gb"));

                                        Double.TryParse(dataRecommended.ToLower().Replace("mb", string.Empty).Trim()
                                           .Replace(".", CultureInfo.CurrentUICulture.NumberFormat.NumberDecimalSeparator).Trim()
                                           .Replace(",", CultureInfo.CurrentUICulture.NumberFormat.NumberDecimalSeparator).Trim(), out hdd);

                                        Minimum.Storage = (long)(1024 * 1024 * 1024 * hdd);
                                    }
                                    Recommanded.StorageUsage = Tools.SizeSuffix(Recommanded.Storage);
                                }
                                break;

                            case "video card (gpu)":
                                if (!dataMinimum.IsNullOrEmpty())
                                {
                                    dataMinimum = dataMinimum.Replace(" / ", "¤").Replace("<br>", "¤");

                                    dataMinimum = Regex.Replace(dataMinimum, "(</[^>]*>)", string.Empty);
                                    dataMinimum = Regex.Replace(dataMinimum, "(<[^>]*>)", string.Empty);

                                    dataMinimum = dataMinimum.Replace("(or equivalent)", string.Empty).Replace("or equivalent", string.Empty)
                                        .Replace("DirectX-compliant", string.Empty)
                                        .Replace("DirectX compatible card", string.Empty)
                                        .Replace("or better", string.Empty)
                                        .Replace("of VRAM", "VRAM")
                                        .Replace("TnL support", string.Empty)
                                        .Replace("Integrated graphics, monitor with resolution of 1280x720.", "1280x720")
                                        .Replace("Integrated graphics", string.Empty)
                                        .Replace("Integrated", string.Empty).Replace("Dedicated", string.Empty)
                                        .Replace("+ compatible", string.Empty).Replace("compatible", string.Empty)
                                        .Replace("that supports DirectDraw at 640x480 resolution, 256 colors", string.Empty)
                                        .Replace("or higher", string.Empty)
                                        .Replace(" / ", "¤").Replace("<br>", "¤");

                                    Minimum.Gpu = dataMinimum.Split('¤')
                                        .Select(x => x.Trim()).ToList()
                                        .Where(x => x.Length > 4)
                                        .Where(x => x.ToLower().IndexOf("shader") == -1)
                                        .Where(x => x.ToLower().IndexOf("anything") == -1)
                                        .Where(x => x.ToLower().IndexOf("any card") == -1)
                                        .Where(x => x.Trim() != string.Empty).ToList();
                                }
                                if (!dataRecommended.IsNullOrEmpty())
                                {
                                    dataRecommended = dataRecommended.Replace(" / ", "¤").Replace("<br>", "¤");

                                    dataRecommended = Regex.Replace(dataRecommended, "(</[^>]*>)", "");
                                    dataRecommended = Regex.Replace(dataRecommended, "(<[^>]*>)", "");

                                    dataRecommended = dataRecommended.Replace("(or equivalent)", string.Empty).Replace("or equivalent", string.Empty)
                                        .Replace("DirectX-compliant", string.Empty)
                                        .Replace("DirectX compatible card", string.Empty)
                                        .Replace("or better", string.Empty)
                                        .Replace("of VRAM", "VRAM")
                                        .Replace("TnL support", string.Empty)
                                        .Replace("Integrated graphics, monitor with resolution of 1280x720.", "1280x720")
                                        .Replace("Integrated graphics", string.Empty)
                                        .Replace("Integrated", string.Empty).Replace("Dedicated", string.Empty)
                                        .Replace("+ compatible", string.Empty).Replace("compatible", string.Empty)
                                        .Replace("that supports DirectDraw at 640x480 resolution, 256 colors", string.Empty)
                                        .Replace("or higher", string.Empty)
                                        .Replace(" / ", "¤").Replace("<br>", "¤");

                                    Recommanded.Gpu = dataRecommended.Split('¤')
                                        .Select(x => x.Trim()).ToList()
                                        .Where(x => x.Length > 4)
                                        .Where(x => x.ToLower().IndexOf("shader") == -1).ToList()
                                        .Where(x => x.ToLower().IndexOf("anything") == -1).ToList()
                                        .Where(x => x.ToLower().IndexOf("any card") == -1)
                                        .Where(x => x.Trim() != string.Empty).ToList();
                                }
                                break;

                            default:
                                logger.Warn($"No treatment for {dataTitle}");
                                break;
                        }
                    }

                    Minimum.IsMinimum = true;

                    Common.LogDebug(true, $"PCGamingWikiRequierements - Minimum: {JsonConvert.SerializeObject(Minimum)}");
                    Common.LogDebug(true, $"PCGamingWikiRequierements - Recommanded: {JsonConvert.SerializeObject(Recommanded)}");

                    gameRequierements.Items = new List<Requirement> { Minimum, Recommanded };

                    gameRequierements.SourceName = "PCGamingWiki";
                    gameRequierements.SourceGameName = HtmlRequirement.QuerySelector("h1.article-title").InnerHtml;
                }
                else
                {
                    logger.Warn($"No data find for {_game.Name} in {url}");
                }

            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
            }

            return gameRequierements;
        }
    }
}
