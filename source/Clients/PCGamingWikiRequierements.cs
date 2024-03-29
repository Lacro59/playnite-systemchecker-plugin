﻿using AngleSharp.Dom.Html;
using AngleSharp.Parser.Html;
using CommonPluginsShared;
using CommonPluginsShared.Models;
using Playnite.SDK.Models;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using SystemChecker.Models;
using SystemChecker.Services;
using CommonPluginsStores.Steam;
using CommonPluginsStores.PCGamingWiki;

namespace SystemChecker.Clients
{
    public class PCGamingWikiRequierements : RequierementMetadata
    {
        private SystemCheckerDatabase PluginDatabase => SystemChecker.PluginDatabase;
        private SteamApi SteamApi => new SteamApi("SystemChecker");
        private PCGamingWikiApi PcGamingWikiApi => new PCGamingWikiApi("SystemChecker");

        private int SteamId { get; set; } = 0;


        public PCGamingWikiRequierements()
        {

        }


        public override GameRequierements GetRequirements()
        {
            GameRequierements = SystemChecker.PluginDatabase.GetDefault(GameContext);

            string UrlPCGamingWiki = PcGamingWikiApi.FindGoodUrl(GameContext, SteamId);

            if (!UrlPCGamingWiki.IsNullOrEmpty())
            {
                GameRequierements = GetRequirements(UrlPCGamingWiki);
            }
            else
            {
                logger.Warn($"PCGamingWikiRequierements - Not find for {GameContext.Name}");
            }

            return GameRequierements;
        }

        public GameRequierements GetRequirements(Game game)
        {
            GameContext = game;
            SteamId = 0;

            if (GameContext.SourceId == PlayniteTools.GetPluginId(PlayniteTools.ExternalPlugin.SteamLibrary))
            {
                SteamId = int.Parse(game.GameId);
            }
            if (SteamId == 0)
            {
                SteamId = SteamApi.GetAppId(game.Name);
            }

            return GetRequirements();
        }

        public override GameRequierements GetRequirements(string url)
        {
            try
            {
                Common.LogDebug(true, $"PCGamingWikiRequierements.GetRequirements - url {url}");

                // Get data & parse
                string ResultWeb = string.Empty;
                try
                {
                    ResultWeb = Web.DownloadStringData(url).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, $"Failed to download {url}", true, PluginDatabase.PluginName);
                }


                HtmlParser parser = new HtmlParser();
                IHtmlDocument HtmlRequirement = parser.Parse(ResultWeb);

                var systemRequierement = HtmlRequirement.QuerySelector("div.sysreq_Windows");
                if (systemRequierement != null)
                {
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
                                    Minimum.Os = ReplaceOS(dataMinimum);
                                }
                                if (!dataRecommended.IsNullOrEmpty())
                                {
                                    Recommanded.Os = ReplaceOS(dataRecommended);
                                }
                                break;

                            case "processor (cpu)":
                                if (!dataMinimum.IsNullOrEmpty())
                                {
                                    Minimum.Cpu = ReplaceCPU(dataMinimum);
                                }
                                if (!dataRecommended.IsNullOrEmpty())
                                {
                                    Recommanded.Cpu = ReplaceCPU(dataRecommended);
                                }
                                break;

                            case "system memory (ram)":
                                if (!dataMinimum.IsNullOrEmpty())
                                {
                                    Minimum.Ram = ReplaceRAM(dataMinimum);
                                    Minimum.RamUsage = Tools.SizeSuffix(Minimum.Ram, true);
                                }
                                if (!dataRecommended.IsNullOrEmpty())
                                {
                                    Recommanded.Ram = ReplaceRAM(dataMinimum);
                                    Recommanded.RamUsage = Tools.SizeSuffix(Recommanded.Ram, true);
                                }
                                break;

                            case "hard disk drive (hdd)":
                                if (!dataMinimum.IsNullOrEmpty())
                                {
                                    Minimum.Storage = ReplaceHDD(dataMinimum);
                                    Minimum.StorageUsage = Tools.SizeSuffix(Minimum.Storage);
                                }
                                if (!dataRecommended.IsNullOrEmpty())
                                {
                                    Recommanded.Storage = ReplaceHDD(dataRecommended);
                                    Recommanded.StorageUsage = Tools.SizeSuffix(Recommanded.Storage);
                                }
                                break;

                            case "video card (gpu)":
                                if (!dataMinimum.IsNullOrEmpty())
                                {
                                    Minimum.Gpu = ReplaceGPU(dataMinimum);
                                }
                                if (!dataRecommended.IsNullOrEmpty())
                                {
                                    Recommanded.Gpu = ReplaceGPU(dataRecommended);
                                }
                                break;

                            default:
                                logger.Warn($"No treatment for {dataTitle}");
                                break;
                        }
                    }

                    Minimum.IsMinimum = true;

                    Common.LogDebug(true, $"PCGamingWikiRequierements - Minimum: {Serialization.ToJson(Minimum)}");
                    Common.LogDebug(true, $"PCGamingWikiRequierements - Recommanded: {Serialization.ToJson(Recommanded)}");

                    GameRequierements.Items = new List<Requirement> { Minimum, Recommanded };

                    GameRequierements.SourcesLink = new SourceLink
                    {
                        Name = "PCGamingWiki",
                        GameName = HtmlRequirement.QuerySelector("h1.article-title").InnerHtml,
                        Url = url
                    };
                }
                else
                {
                    logger.Warn($"No data find for {GameContext.Name} in {url}");
                }

            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }

            return GameRequierements;
        }


        #region Parser
        private List<string> ReplaceOS(string data)
        {
            data = data
                .Replace("Anything made in the last decade", string.Empty)
                .Replace("(latest service pack)", string.Empty)
                .Replace("(1803 or later)", string.Empty)
                .Replace(" (Only inclusive until patch 1.16.1. Patch 1.17+ Needs XP and greater.)", string.Empty)
                .Replace("Windows", string.Empty)
                .Replace("10 October 2018 Update", string.Empty)
                .Replace("<b>(DXR)</b>", string.Empty)
                .Replace("or better", string.Empty)
                .Replace(",", "¤").Replace(" or ", "¤").Replace("/", "¤");
            return data.Split('¤').Select(x => x.Trim()).Where(x => !x.Trim().IsNullOrEmpty()).ToList();
        }

        private List<string> ReplaceCPU(string data)
        {
            data = data
                .Replace("one that works", string.Empty)
                .Replace("(approx)", string.Empty)
                .Replace("64 bit processor", string.Empty)
                .Replace("GHz+", "GHz")
                .Replace("Dual-core CPU (2 GHz or greater speed)", "Dual-core CPU")
                .Replace("-2XXX @ 2.0 GHz", string.Empty)
                .Replace("2009 or newer dual-core Intel or AMD", string.Empty)
                .Replace("2011 or newer Intel Core i3, i5 or i7", string.Empty)
                .Replace("SSE2 instruction set support", string.Empty)
                .Replace("(or equivalent)", string.Empty)
                .Replace("or equivalent", string.Empty)
                .Replace("(4 CPUs), ~2.4 GHz", string.Empty)
                .Replace("(4 CPUs), ~3.1 GHz", string.Empty)
                .Replace("<b>(DXR)</b>", string.Empty)
                .Replace(" from Intel or AMD at", string.Empty)
                .Replace("with SSE2 instruction set support", string.Empty)
                .Replace("faster", string.Empty)
                .Replace("(and graphics card with T&amp;L)", string.Empty)
                .Replace("(1.5 GHz if graphics card does not support T&amp;L)", string.Empty)
                .Replace("or AMD equivalent", string.Empty)
                .Replace("or better", string.Empty)
                .Replace("or higher", string.Empty)
                .Replace("(D3D)/300", string.Empty)
                .Replace("(with 3D acceleration)", string.Empty)
                .Replace("(software)", string.Empty)
                .Replace(", x86", string.Empty)
                .Replace(" / ", "¤").Replace("<br>", "¤").Replace(" or ", "¤");
            return data.Split('¤').Select(x => x.Trim()).Where(x => !x.Trim().IsNullOrEmpty()).ToList();
        }

        private long ReplaceRAM(string data)
        {
            data = data.ToLower().Replace("ram mb ram", string.Empty);
            data = data.ToLower().Replace("ram", string.Empty);

            double hdd = 0;
            if (data.Contains("mb", StringComparison.InvariantCultureIgnoreCase))
            {
                data = data.Substring(0, data.ToLower().IndexOf("mb"));

                double.TryParse(data
                    .Replace(".", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator).Trim()
                    .Replace(",", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator).Trim(), out hdd);

                return (long)(1024 * 1024 * hdd);
            }
            if (data.Contains("gb", StringComparison.InvariantCultureIgnoreCase))
            {
                data = data.Substring(0, data.ToLower().IndexOf("gb"));

                double.TryParse(data
                    .Replace(".", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator).Trim()
                    .Replace(",", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator).Trim(), out hdd);

                return (long)(1024 * 1024 * 1024 * hdd);
            }

            return 0;
        }

        private long ReplaceHDD(string data)
        {
            double hdd = 0;
            if (data.Contains("mb", StringComparison.InvariantCultureIgnoreCase))
            {
                data = data.Substring(0, data.ToLower().IndexOf("mb"));

                double.TryParse(data
                    .Replace(".", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator).Trim()
                    .Replace(",", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator).Trim(), out hdd);

               return (long)(1024 * 1024 * hdd);
            }
            if (data.Contains("gb", StringComparison.InvariantCultureIgnoreCase))
            {
                data = data.Substring(0, data.ToLower().IndexOf("gb"));

                double.TryParse(data
                    .Replace(".", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator).Trim()
                    .Replace(",", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator).Trim(), out hdd);

                return (long)(1024 * 1024 * 1024 * hdd);
            }

            return 0;
        }

        private List<string> ReplaceGPU(string data)
        {
            data = data.Replace("<br>", "¤");
            data = Regex.Replace(data, "<[^>]*>", string.Empty);
            data = Regex.Replace(data, @"[(]\d+x\d+[)]", string.Empty);

            data = data.Replace("(or equivalent)", string.Empty).Replace("or equivalent", string.Empty)
                .Replace("a toaster - you really shouldn't have trouble", string.Empty)
                .Replace("Non-Dedicated (shared) video card", string.Empty)
                .Replace("Onboard graphics chipset", string.Empty)
                .Replace("XNA 4.0 compatible", string.Empty)
                .Replace("compatible hardware", string.Empty)
                .Replace("AMD Radeon or Nvidia GeForce recommended", string.Empty)
                .Replace("A dedicated GPU (Nvidia or AMD) with at least", string.Empty)
                .Replace("strongly recommended", string.Empty)
                .Replace("XNA Hi Def Profile Compatible GPU", string.Empty)
                .Replace("(GTX 970 or above required for VR)", string.Empty)
                .Replace("DirectX-compliant", string.Empty)
                .Replace("Mobile or dedicated", string.Empty)
                .Replace("DirectX compatible card", string.Empty)
                .Replace("or better", string.Empty)
                .Replace("(shader model 4.0)+", string.Empty)
                .Replace("of VRAM", "VRAM")

                .Replace("(Shared Memory is not recommended)", string.Empty)
                .Replace("<b>(DXR)</b>", string.Empty)
                .Replace("TnL support", string.Empty)
                .Replace("Integrated graphics, monitor with resolution of 1280x720.", "1280x720")
                .Replace("Integrated graphics", string.Empty)
                .Replace("Integrated", string.Empty).Replace("Dedicated", string.Empty)
                .Replace("+ compatible", string.Empty).Replace("compatible", string.Empty)
                .Replace("that supports DirectDraw at 640x480 resolution, 256 colors", string.Empty)
                .Replace("or higher", string.Empty)
                .Replace("  ", " ")
                .Replace(" / ", "¤").Replace(" or ", "¤").Replace(", ", "¤");

            data = Regex.Replace(data, @"DX(\d+)[+]?", "DirectX $1");

            return data.Split('¤')
                .Select(x => x.Trim()).ToList()
                .Where(x => x.Length > 4)
                .Where(x => x.ToLower().IndexOf("shader") == -1)
                .Where(x => x.ToLower().IndexOf("anything") == -1)
                .Where(x => x.ToLower().IndexOf("any card") == -1)
                .Where(x => x.Trim() != string.Empty).Where(x => !x.Trim().IsNullOrEmpty()).ToList();
        }
        #endregion
    }
}
