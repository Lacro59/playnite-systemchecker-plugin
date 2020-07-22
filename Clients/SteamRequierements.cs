using AngleSharp.Dom.Html;
using AngleSharp.Parser.Html;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Playnite.Common.Web;
using Playnite.SDK;
using Playnite.SDK.Models;
using PluginCommon;
using Steam.Models;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using SystemChecker.Models;

namespace SystemChecker.Clients
{
    class SteamRequierements
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private uint appId { get; set; }

        private readonly string[] SizeSuffixes = { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };

        private string SizeSuffix(Int64 value)
        {
            if (value < 0) { return "-" + SizeSuffix(-value); }
            if (value == 0) { return "0.0 bytes"; }

            int mag = (int)Math.Log(value, 1024);
            decimal adjustedSize = (decimal)value / (1L << (mag * 10));

            return string.Format("{0:n1} {1}", adjustedSize, SizeSuffixes[mag]);
        }

        public SteamRequierements(Game game)
        {
            appId = uint.Parse(game.GameId);
        }

        private string GetSteamData()
        {
            var url = $"https://store.steampowered.com/api/appdetails?appids={appId}&l=english";
            return HttpDownloader.DownloadString(url);
        }

        public GameRequierements GetRequirements()
        {
            GameRequierements gameRequierements = new GameRequierements();

            try
            {
                string data = GetSteamData();
                var parsedData = JsonConvert.DeserializeObject<Dictionary<string, StoreAppDetailsResult>>(data);

                if (parsedData[appId.ToString()].data != null)
                {
                    JObject pc_requirements = JObject.FromObject(parsedData[appId.ToString()].data.pc_requirements);

                    logger.Debug($"SystemChecker - {appId} - " + JsonConvert.SerializeObject(pc_requirements));

                    if (pc_requirements["minimum"] != null)
                    {
                        gameRequierements.Minimum = ParseRequirement((string)pc_requirements["minimum"]);
                    }

                    if (pc_requirements["recommended"] != null)
                    {
                        gameRequierements.Recommanded = ParseRequirement((string)pc_requirements["recommended"]);

                        // Add missing
                        if (gameRequierements.Recommanded.Os.Count == 0)
                        {
                            gameRequierements.Recommanded.Os = gameRequierements.Minimum.Os;
                        }
                        if (gameRequierements.Recommanded.Cpu.Count == 0)
                        {
                            gameRequierements.Recommanded.Cpu = gameRequierements.Minimum.Cpu;
                        }
                        if (gameRequierements.Recommanded.Gpu.Count == 0)
                        {
                            gameRequierements.Recommanded.Gpu = gameRequierements.Minimum.Gpu;
                        }
                        if (gameRequierements.Recommanded.Ram == 0)
                        {
                            gameRequierements.Recommanded.Ram = gameRequierements.Minimum.Ram;
                            gameRequierements.Recommanded.RamUsage = gameRequierements.Minimum.RamUsage;
                        }
                        if (gameRequierements.Recommanded.Storage == 0)
                        {
                            gameRequierements.Recommanded.Storage = gameRequierements.Minimum.Storage;
                            gameRequierements.Recommanded.StorageUsage = gameRequierements.Minimum.StorageUsage;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "SystemChecker", "Error on GetRequirements()");
            }

            return gameRequierements;
        }

        private Requirement ParseRequirement(string pc_requirement)
        {
            Requirement requirement = new Requirement();

            HtmlParser parser = new HtmlParser();
            IHtmlDocument HtmlRequirement = parser.Parse(pc_requirement);

            // Only recent game
            foreach (var ElementRequirement in HtmlRequirement.QuerySelectorAll("li"))
            {
                //<strong>OS:</strong> Windows XP / 7 / 8 / 8.1 / 10 x32 and x64<br> </ li >
                if (ElementRequirement.InnerHtml.IndexOf("<strong>OS") > -1)
                {
                    string os = ElementRequirement.InnerHtml
                        .Replace("<strong>OS:</strong>", "")
                        .Replace("Windows", "")
                        .Replace("®", "")
                        .Replace("and above", "")
                        .Replace("x32", "")
                        .Replace("and", "")
                        .Replace("x64", "")
                        .Replace("32-bit", "")
                        .Replace("32Bit", "")
                        .Replace("32 Bit", "")
                        .Replace("64-bit", "")
                        .Replace("64Bit", "")
                        .Replace("64 Bit", "")
                        .Replace("latest Service Pack", "")
                        .Replace("latest service pack", "")
                        .Replace("32-bit/64-bit", "")
                        .Replace("32bit/64bit", "")
                        .Replace("64-bit Operating System Required", "")
                        .Replace("32-bit Operating System Required", "")
                        .Replace(" Operating System Required", "")
                        .Replace("Operating System Required", "")
                        .Replace(" equivalent or better", "")
                        .Replace(" or equivalent.", "")
                        .Replace(" or equivalent", "")
                        .Replace("()", "")
                        .Replace("<br>", "")
                        .Trim();
                    logger.Debug($"os: {os}");
                    foreach (string sTemp in os.Replace(",", "/").Split('/'))
                    {
                        requirement.Os.Add(sTemp.Trim());
                    }
                }

                //< li >< strong > Processor:</ strong > Intel Core2 Duo E6320 or equivalent /\t AMD Athlon 64 X2 5000 + (2 * 2.6 GHz) or equivalent<br></ li >
                if (ElementRequirement.InnerHtml.IndexOf("<strong>Processor") > -1)
                {
                    string cpu = ElementRequirement.InnerHtml
                            .Replace("<strong>Processor:</strong>", "")
                            .Replace("&nbsp;", "")
                            .Replace(" equivalent or faster processor", "")
                            .Replace(" equivalent or better", "")
                            .Replace("above", "")
                            .Replace("and up", "")
                            .Replace(" or equivalent.", "")
                            .Replace(" over", "")
                            .Replace(" or better", "")
                            .Replace(" or equivalent", "")
                            .Replace("4 CPUs", "")
                            .Replace(", ~2.4GHz", "")
                            .Replace(", ~3.1GHz", "")
                            .Replace("(not recommended for Intel HD Graphics cards)", ", not recommended for Intel HD Graphics cards")
                            .Replace("()", "")
                            .Replace("<br>", "")
                            .Trim();
                    logger.Debug($"cpu: {cpu}");
                    foreach (string sTemp in cpu.Replace(",", "/").Replace(" or ", "/").Split('/'))
                    {
                        requirement.Cpu.Add(sTemp.Trim());
                    }
                }

                //< li >< strong > Memory:</ strong > 2048 MB RAM<br></ li >
                if (ElementRequirement.InnerHtml.IndexOf("<strong>Memory") > -1)
                {
                    string ram = ElementRequirement.InnerHtml
                            .Replace("<strong>Memory:</strong>", "")
                            .Replace("RAM", "")
                            .Replace("of system", "")
                            .Replace("<br>", "")
                            .Trim();
                    logger.Debug($"ram: {ram}");
                    if (ram.IndexOf("MB") > -1)
                    {
                        requirement.Ram = 1024 * 1024 * long.Parse(ram.Replace("MB", "").Trim());
                    }
                    if (ram.IndexOf("GB") > -1)
                    {
                        requirement.Ram = 1024 * 1024 * 1024 * long.Parse(ram.Replace("GB", "").Trim());
                    }
                    requirement.RamUsage = SizeSuffix(requirement.Ram);
                }

                //< li >< strong > Graphics:</ strong > GeForce GT 440(1024 MB) or equivalent / Radeon HD 6450(512 MB) or equivalent / Iris Pro Graphics 5200(1792 MB) < br ></ li >
                if (ElementRequirement.InnerHtml.IndexOf("<strong>Graphics") > -1)
                {
                    string gpu = ElementRequirement.InnerHtml
                            .Replace("<strong>Graphics:</strong>", "")
                            .Replace("ATI or NVidia", "ATI - NVidia")
                            .Replace("w/", "with")
                            .Replace("(not recommended for Intel HD Graphics cards)", ", not recommended for Intel HD Graphics cards")
                            .Replace(" equivalent or better", "")
                            .Replace(" or equivalent.", "")
                            .Replace("or equivalent.", "")
                            .Replace(" or equivalent", "")
                            .Replace(" or better.", "")
                            .Replace("or better.", "")
                            .Replace(" or better", "")
                            .Replace("or better", "")
                            .Replace("or equivalent", "")
                            .Replace("level Graphics Card (requires support for SSE)", "")
                            .Replace("Laptop integrated ", "")
                            .Replace("with 3GB system ram", "")
                            .Replace(" or more and should be a DirectX 9-compatible with support for Pixel Shader 3.0", "")
                            .Replace("()", "")
                            .Replace("<br>", "")
                            .Replace(". Integrated Intel HD Graphics should work but is not supported; problems are generally solved with a driver update.", "")
                            .Trim();
                    logger.Debug($"gpu: {gpu}");
                    foreach (string sTemp in gpu.Replace(",", "/").Replace(" or ", "/").Split('/'))
                    {
                        requirement.Gpu.Add(sTemp.Trim());
                    }
                }

                //< li >< strong > DirectX:</ strong > Version 10 < br ></ li >
                //< li >< strong > Network:</ strong > Broadband Internet connection<br></ li >

                //< li >< strong > Storage:</ strong > 350 MB available space </ li >
                if (ElementRequirement.InnerHtml.IndexOf("<strong>Storage") > -1 || ElementRequirement.InnerHtml.IndexOf("<strong>Hard Drive") > -1)
                {
                    string storage = ElementRequirement.InnerHtml
                        .Replace("<strong>Storage:</strong>", "")
                        .Replace("<strong>Hard Drive:</strong>", "")
                        .Replace(" available space", "")
                        .Replace(" equivalent or better", "")
                        .Replace(" or equivalent", "")
                        .Replace(" HD space", "")
                        .Replace("<br>", "")
                        .Trim();
                    logger.Debug($"storage: {storage}");
                    if (storage.IndexOf("MB") > -1)
                    {
                        requirement.Storage = 1024 * 1024 * long.Parse(storage.Replace("MB", "").Trim());
                    }
                    if (storage.IndexOf("GB") > -1)
                    {
                        requirement.Storage = 1024 * 1024 * 1024 * long.Parse(storage.Replace("GB", "").Trim());
                    }
                    requirement.StorageUsage = SizeSuffix(requirement.Storage);
                }
            }

            return requirement;
        }
    }
}
