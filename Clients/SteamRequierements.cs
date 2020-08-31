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
    class SteamRequierements : RequierementMetadata
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private uint appId { get; set; }


        public SteamRequierements(Game game, uint appId = 0)
        {
            this.appId = appId;
            if (appId == 0)
            {
                this.appId = uint.Parse(game.GameId);
            }
        }

        private string GetSteamData()
        {
            var url = $"https://store.steampowered.com/api/appdetails?appids={appId}&l=english";
            return HttpDownloader.DownloadString(url);
        }

        public override GameRequierements GetRequirements()
        {
            try
            {
                string data = GetSteamData();
                var parsedData = JsonConvert.DeserializeObject<Dictionary<string, StoreAppDetailsResult>>(data);

                if (parsedData[appId.ToString()].data != null && JsonConvert.SerializeObject(parsedData[appId.ToString()].data.pc_requirements) != "[]")
                {
                    logger.Debug(JsonConvert.SerializeObject(parsedData[appId.ToString()].data.pc_requirements));

                    JObject pc_requirements = JObject.FromObject(parsedData[appId.ToString()].data.pc_requirements);

                    //logger.Debug($"SystemChecker - {appId} - " + JsonConvert.SerializeObject(pc_requirements));

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
                Common.LogError(ex, "SystemChecker", "Error on SteamRequierements.GetRequirements()");
            }

            return gameRequierements;
        }

        public override GameRequierements GetRequirements(string url)
        {
            throw new NotImplementedException();
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
                        .Replace("\t", " ")
                        .Replace("<strong>OS:</strong>", "")
                        .Replace("with Platform Update for  7 ( versions only)", "")
                        .Replace("Win ", "")
                        .Replace("Windows", "")
                        .Replace(", 32-bit", "")
                        .Replace(", 32bit", "")
                        .Replace(", 64-bit", "")
                        .Replace(", 64bit", "")
                        .Replace("®", "")
                        .Replace("+", "")
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
                        .Replace(" or Newer", "")
                        .Replace(" or newer", "")
                        .Replace("or Newer", "")
                        .Replace("or newer", "")
                        .Replace("or later", "")
                        .Replace("()", "")
                        .Replace("<br>", "")
                        .Trim();
                    logger.Debug($"os: {os}");
                    foreach (string sTemp in os.Replace(",", "¤").Replace(" or ", "¤").Replace("/", "¤").Split('¤'))
                    {
                        requirement.Os.Add(sTemp.Trim());
                    }
                }

                //< li >< strong > Processor:</ strong > Intel Core2 Duo E6320 or equivalent /\t AMD Athlon 64 X2 5000 + (2 * 2.6 GHz) or equivalent<br></ li >
                if (ElementRequirement.InnerHtml.IndexOf("<strong>Processor") > -1)
                {
                    string cpu = ElementRequirement.InnerHtml
                            .Replace("\t", " ")
                            .Replace("<strong>Processor:</strong>", "")
                            .Replace("&nbsp;", "")
                            .Replace("- Low budget CPUs such as Celeron or Duron needs to be at about twice the CPU speed", "")
                            .Replace(" equivalent or faster processor", "")
                            .Replace(" equivalent or better", "")
                            .Replace("above", "")
                            .Replace("and up", "")
                            .Replace("(or higher)", "")
                            .Replace("or higher", "")
                            .Replace(" or equivalent.", "")
                            .Replace(" over", "")
                            .Replace(" or faster", "")
                            .Replace(" or better", "")
                            .Replace(" or equivalent", "")
                            .Replace("4 CPUs", "")
                            .Replace(", ~2.4GHz", "")
                            .Replace(", ~3.1GHz", "")
                            .Replace("ghz", "GHz")
                            .Replace("Ghz", "GHz")
                            .Replace("(not recommended for Intel HD Graphics cards)", ", not recommended for Intel HD Graphics cards")
                            .Replace("()", "")
                            .Replace("<br>", "")
                            .Trim();
                    logger.Debug($"cpu: {cpu}");
                    cpu = Regex.Replace(cpu, ", ([0-9])", " $1");
                    cpu = Regex.Replace(cpu, "([0-9]),([0-9] GHz)", "$1.$2");
                    cpu = Regex.Replace(cpu, "([0-9]) GHz", "$1GHz");
                    cpu = Regex.Replace(cpu, "([0-9999])k", "$1K");
                    cpu = cpu.Replace(",", "¤").Replace(" / ", "¤").Replace(" or ", "¤").Replace(" OR ", "¤")
                        .Replace(" and ", "¤").Replace(" AND ", "¤").Replace(" | ", "¤");
                    foreach (string sTemp in cpu.Split('¤'))
                    {
                        requirement.Cpu.Add(sTemp.Trim());
                    }
                }

                //< li >< strong > Memory:</ strong > 2048 MB RAM<br></ li >
                if (ElementRequirement.InnerHtml.IndexOf("<strong>Memory") > -1)
                {
                    string ram = ElementRequirement.InnerHtml.ToLower()
                            .Replace("\t", " ")
                            .Replace("<strong>memory:</strong>", "")
                            .Replace("ram", "")
                            .Replace("of system", "")
                            .Replace("<br>", "")
                            .Trim();
                    ram = ram.Split('/')[ram.Split('/').Length - 1];
                    logger.Debug($"ram: {ram}");
                    if (ram.ToLower().IndexOf("mb") > -1)
                    {
                        requirement.Ram = 1024 * 1024 * long.Parse(ram.ToLower().Replace("mb", "").Trim());
                    }
                    if (ram.ToLower().IndexOf("gb") > -1)
                    {
                        requirement.Ram = 1024 * 1024 * 1024 * long.Parse(ram.ToLower().Replace("gb", "").Trim());
                    }
                    requirement.RamUsage = SizeSuffix(requirement.Ram);
                }

                //< li >< strong > Graphics:</ strong > GeForce GT 440(1024 MB) or equivalent / Radeon HD 6450(512 MB) or equivalent / Iris Pro Graphics 5200(1792 MB) < br ></ li >
                if (ElementRequirement.InnerHtml.IndexOf("<strong>Graphics") > -1)
                {
                    string gpu = ElementRequirement.InnerHtml
                            .Replace("\t", " ")
                            .Replace("<strong>Graphics:</strong>", "")
                            .Replace("ATI or NVidia card w/ 1024 MB RAM (NVIDIA GeForce GTX 260 or ATI HD 4890)", "NVIDIA GeForce GTX 260 or ATI HD 4890")
                            .Replace("Video card must be 128 MB or more and should be a DirectX 9-compatible with support for Pixel Shader 2.0b (", "")
                            .Replace("- *NOT* an Express graphics card).", "")
                            .Replace("DirectX 11 class GPU with 1GB VRAM (", "")
                            //.Replace(")<br>", "")
                            .Replace("/320M 512MB VRAM", "")
                            .Replace(" 512MB VRAM (Intel integrated GPUs are not supported!)", " / Intel integrated GPUs are not supported!")
                            .Replace("(not recommended for Intel HD Graphics cards)", ", not recommended for Intel HD Graphics cards")
                            .Replace("or similar (no support for onboard cards)", "")
                            .Replace("level Graphics Card (requires support for SSE)", "")
                            .Replace("- Integrated graphics and very low budget cards might not work.", "")

                            .Replace("ATI or NVidia card", "Card")
                            .Replace("w/", "with")
                            .Replace("Graphics: ", "")
                            .Replace(" equivalent or better", "")
                            .Replace(" or equivalent.", "")
                            .Replace("or equivalent.", "")
                            .Replace(" or equivalent", "")
                            .Replace(" or better.", "")
                            .Replace("or better.", "")
                            .Replace(" or better", "")
                            .Replace(" or newer", "")
                            .Replace("or newer", "")
                            .Replace("or higher", "")
                            .Replace("or better", "")
                            .Replace("or equivalent", "")
                            .Replace("Mid-range", "")
                            .Replace(" Memory Minimum", "")
                            .Replace(" memory minimum", "")
                            .Replace(" Memory Recommended", "")
                            .Replace(" memory recommended", "")
                            .Replace("e.g.", "")
                            .Replace("Laptop integrated ", "")
                            .Replace("GPU 1GB VRAM", "GPU 1 GB VRAM")
                            .Replace("with 3GB system ram", "(3 GB)")
                            .Replace("with 512MB", "(512 MB)")
                            .Replace("(1Gb)", "(1 GB)")
                            .Replace("(1GB)", "(1 GB)")
                            .Replace(" 1GB", " (1 GB)")
                            .Replace(" 2GB", " (2 GB)")
                            .Replace("(2GB)", " (2 GB)")
                            .Replace("(3GB)", " (3 GB)")
                            .Replace("(4GB)", " (4 GB)")
                            .Replace(" 6GB", " (6 GB)")
                            .Replace(" 4GB", " (4 GB)")
                            .Replace("8GB Memory 8 GB RAM", "(8 GB)")
                            .Replace(" or more and should be a DirectX 9-compatible with support for Pixel Shader 3.0", "")
                            .Replace(", or ", "")
                            .Replace("()", "")
                            .Replace("<br>", "")
                            .Replace("  ", " ")
                            .Replace(". Integrated Intel HD Graphics should work but is not supported; problems are generally solved with a driver update.", "")
                            .Trim();
                    logger.Debug($"gpu: {gpu}");
                    gpu = Regex.Replace(gpu, " - ([0-9]) GB", " ($1 GB)");
                    //gpu = Regex.Replace(gpu, "([0-9])Gb", "($1 GB)");
                    gpu = gpu.Replace(",", "¤").Replace(" or ", "¤").Replace(" OR ", "¤").Replace(" / ", "¤").Replace(" | ", "¤");
                    foreach (string sTemp in gpu.Split('¤'))
                    {
                        requirement.Gpu.Add(sTemp.Trim());
                    }
                }

                //< li >< strong > DirectX:</ strong > Version 10 < br ></ li >
                //< li >< strong > Network:</ strong > Broadband Internet connection<br></ li >

                //< li >< strong > Storage:</ strong > 350 MB available space </ li >
                if (ElementRequirement.InnerHtml.IndexOf("<strong>Storage") > -1 || ElementRequirement.InnerHtml.IndexOf("<strong>Hard Drive") > -1)
                {
                    string storage = ElementRequirement.InnerHtml.ToLower()
                        .Replace("\t", " ")
                        .Replace("<strong>storage:</strong>", "")
                        .Replace("<strong>hard drive:</strong>", "")
                        .Replace("available space", "")
                        .Replace("equivalent or better", "")
                        .Replace("or equivalent", "")
                        .Replace("hd space", "")
                        .Replace("free space", "")
                        .Replace("free hard drive space", "")
                        .Replace("<br>", "")
                        .Trim();
                    logger.Debug($"storage: {storage}");
                    if (storage.IndexOf("mb") > -1)
                    {
                        requirement.Storage = 1024 * 1024 * long.Parse(storage.Replace("mb", "").Trim());
                    }
                    if (storage.IndexOf("gb") > -1)
                    {
                        requirement.Storage = 1024 * 1024 * 1024 * long.Parse(storage.Replace("gb", "").Trim());
                    }
                    requirement.StorageUsage = SizeSuffix(requirement.Storage);
                }
            }

            return requirement;
        }
    }
}
