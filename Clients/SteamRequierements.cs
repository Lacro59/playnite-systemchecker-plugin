using AngleSharp.Dom.Html;
using AngleSharp.Parser.Html;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Playnite.SDK;
using Playnite.SDK.Models;
using PluginCommon;
using PluginCommon.PlayniteResources;
using PluginCommon.PlayniteResources.API;
using PluginCommon.PlayniteResources.Common;
using PluginCommon.PlayniteResources.Converters;
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
            string url = string.Empty;
            try
            {
                url = $"https://store.steampowered.com/api/appdetails?appids={appId}&l=english";
                return Web.DownloadStringData(url).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "SystemChecker", $"Failed to download {url}");
                return string.Empty;
            }
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

                    if (pc_requirements["minimum"] != null)
                    {
                        gameRequierements.Minimum = ParseRequirement((string)pc_requirements["minimum"]);
                    }

                    if (pc_requirements["recommended"] != null)
                    {
                        gameRequierements.Recommanded = ParseRequirement((string)pc_requirements["recommended"]);
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
#if DEBUG
                logger.Debug($"SteamRequierements - {ElementRequirement.InnerHtml}");
#endif

                //<strong>OS:</strong> Windows XP / 7 / 8 / 8.1 / 10 x32 and x64<br> </ li >
                if (ElementRequirement.InnerHtml.IndexOf("<strong>OS") > -1)
                {
                    string os = ElementRequirement.InnerHtml
                        .Replace("\t", " ")
                        .Replace("<strong>OS:</strong>", string.Empty)
                        .Replace("with Platform Update for  7 ( versions only)", string.Empty)
                        .Replace("Win ", string.Empty)
                        .Replace("Windows", string.Empty)
                        .Replace(", 32-bit", string.Empty)
                        .Replace(", 32bit", string.Empty)
                        .Replace(", 64-bit", string.Empty)
                        .Replace(", 64bit", string.Empty)
                        .Replace("®", string.Empty)
                        .Replace("+", string.Empty)
                        .Replace("and above", string.Empty)
                        .Replace("x32", string.Empty)
                        .Replace("and", string.Empty)
                        .Replace("x64", string.Empty)
                        .Replace("32-bit", string.Empty)
                        .Replace("32Bit", string.Empty)
                        .Replace("32 Bit", string.Empty)
                        .Replace("64-bit", string.Empty)
                        .Replace("64Bit", string.Empty)
                        .Replace("64 Bit", string.Empty)
                        .Replace("latest Service Pack", string.Empty)
                        .Replace("latest service pack", string.Empty)
                        .Replace("32-bit/64-bit", string.Empty)
                        .Replace("32bit/64bit", string.Empty)
                        .Replace("64-bit Operating System Required", string.Empty)
                        .Replace("32-bit Operating System Required", string.Empty)
                        .Replace(" Operating System Required", string.Empty)
                        .Replace("Operating System Required", string.Empty)
                        .Replace(" equivalent or better", string.Empty)
                        .Replace(" or equivalent.", string.Empty)
                        .Replace(" or equivalent", string.Empty)
                        .Replace(" or Newer", string.Empty)
                        .Replace(" or newer", string.Empty)
                        .Replace("or Newer", string.Empty)
                        .Replace("or newer", string.Empty)
                        .Replace("or later", string.Empty)
                        .Replace("or higher", string.Empty)
                        .Replace("()", string.Empty)
                        .Replace("<br>", string.Empty)
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
                            .Replace("<strong>Processor:</strong>", string.Empty)
                            .Replace("&nbsp;", string.Empty)
                            .Replace("- Low budget CPUs such as Celeron or Duron needs to be at about twice the CPU speed", string.Empty)
                            .Replace(" equivalent or faster processor", string.Empty)
                            .Replace(" equivalent or better", string.Empty)
                            .Replace("above", string.Empty)
                            .Replace("and up", string.Empty)
                            .Replace("(or higher)", string.Empty)
                            .Replace("or higher", string.Empty)
                            .Replace(" or equivalent.", string.Empty)
                            .Replace(" over", string.Empty)
                            .Replace(" or faster", string.Empty)
                            .Replace(" or better", string.Empty)
                            .Replace(" or equivalent", string.Empty)
                            .Replace("4 CPUs", string.Empty)
                            .Replace(", ~2.4GHz", string.Empty)
                            .Replace(", ~3.1GHz", string.Empty)
                            .Replace("ghz", "GHz")
                            .Replace("Ghz", "GHz")
                            .Replace("(not recommended for Intel HD Graphics cards)", ", not recommended for Intel HD Graphics cards")
                            .Replace("()", string.Empty)
                            .Replace("<br>", string.Empty)
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
                            .Replace("<strong>memory:</strong>", string.Empty)
                            .Replace("ram", string.Empty)
                            .Replace("of system", string.Empty)
                            .Replace("<br>", string.Empty)
                            .Trim();
                    ram = ram.Split('/')[ram.Split('/').Length - 1];
                    logger.Debug($"ram: {ram}");
                    if (ram.ToLower().IndexOf("mb") > -1)
                    {
                        requirement.Ram = 1024 * 1024 * long.Parse(ram.ToLower().Replace("mb", string.Empty).Trim());
                    }
                    if (ram.ToLower().IndexOf("gb") > -1)
                    {
                        requirement.Ram = 1024 * 1024 * 1024 * long.Parse(ram.ToLower().Replace("gb", string.Empty).Trim());
                    }
                    requirement.RamUsage = SizeSuffix(requirement.Ram, true);
                }

                //< li >< strong > Graphics:</ strong > GeForce GT 440(1024 MB) or equivalent / Radeon HD 6450(512 MB) or equivalent / Iris Pro Graphics 5200(1792 MB) < br ></ li >
                if (ElementRequirement.InnerHtml.IndexOf("<strong>Graphics") > -1)
                {
                    string gpu = ElementRequirement.InnerHtml
                            .Replace("\t", " ")
                            .Replace("<strong>Graphics:</strong>", string.Empty)
                            .Replace("ATI or NVidia card w/ 1024 MB RAM (NVIDIA GeForce GTX 260 or ATI HD 4890)", "NVIDIA GeForce GTX 260 or ATI HD 4890")
                            .Replace("Video card must be 128 MB or more and should be a DirectX 9-compatible with support for Pixel Shader 2.0b (", string.Empty)
                            .Replace("- *NOT* an Express graphics card).", string.Empty)
                            .Replace("DirectX 11 class GPU with 1GB VRAM (", string.Empty)
                            //.Replace(")<br>", string.Empty)
                            .Replace("/320M 512MB VRAM", string.Empty)
                            .Replace(" 512MB VRAM (Intel integrated GPUs are not supported!)", " / Intel integrated GPUs are not supported!")
                            .Replace("(not recommended for Intel HD Graphics cards)", ", not recommended for Intel HD Graphics cards")
                            .Replace("or similar (no support for onboard cards)", string.Empty)
                            .Replace("level Graphics Card (requires support for SSE)", string.Empty)
                            .Replace("- Integrated graphics and very low budget cards might not work.", string.Empty)
                            .Replace("Shader Model 3.0", string.Empty)
                            .Replace("shader model 3.0", string.Empty)
                            .Replace(" compatible", string.Empty)
                            .Replace("Any", string.Empty)
                            .Replace("any", string.Empty)

                            .Replace("ATI or NVidia card", "Card")
                            .Replace("w/", "with")
                            .Replace("Graphics: ", string.Empty)
                            .Replace(" equivalent or better", string.Empty)
                            .Replace(" or equivalent.", string.Empty)
                            .Replace("or equivalent.", string.Empty)
                            .Replace(" or equivalent", string.Empty)
                            .Replace(" or better.", string.Empty)
                            .Replace("or better.", string.Empty)
                            .Replace(" or better", string.Empty)
                            .Replace(" or newer", string.Empty)
                            .Replace("or newer", string.Empty)
                            .Replace("or higher", string.Empty)
                            .Replace("or better", string.Empty)
                            .Replace("or greater graphics card", string.Empty)
                            .Replace("or equivalent", string.Empty)
                            .Replace("Mid-range", string.Empty)
                            .Replace(" Memory Minimum", string.Empty)
                            .Replace(" memory minimum", string.Empty)
                            .Replace(" Memory Recommended", string.Empty)
                            .Replace(" memory recommended", string.Empty)
                            .Replace("e.g.", string.Empty)
                            .Replace("Laptop integrated ", string.Empty)
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
                            .Replace(" or more and should be a DirectX 9-compatible with support for Pixel Shader 3.0", string.Empty)
                            .Replace(", or ", string.Empty)
                            .Replace("()", string.Empty)
                            .Replace("<br>", string.Empty)
                            .Replace("  ", " ")
                            .Replace(". Integrated Intel HD Graphics should work but is not supported; problems are generally solved with a driver update.", string.Empty)
                            .Trim();
                    logger.Debug($"gpu: {gpu}");
                    gpu = Regex.Replace(gpu, " - ([0-9]) GB", " ($1 GB)");
                    //gpu = Regex.Replace(gpu, "([0-9])Gb", "($1 GB)");
                    gpu = gpu.Replace(",", "¤").Replace(" or ", "¤").Replace(" OR ", "¤").Replace(" / ", "¤").Replace(" | ", "¤");
                    foreach (string sTemp in gpu.Split('¤'))
                    {
                        if (sTemp.Trim() != string.Empty)
                        {
                            requirement.Gpu.Add(sTemp.Trim());
                        }
                    }
                }
                if (ElementRequirement.InnerHtml.IndexOf("<strong>DirectX") > -1 && ElementRequirement.InnerHtml.IndexOf("8") > -1)
                {
                    requirement.Gpu.Add("DirectX 8");
                }
                if (ElementRequirement.InnerHtml.IndexOf("<strong>DirectX") > -1 && ElementRequirement.InnerHtml.IndexOf("9") > -1)
                {
                    requirement.Gpu.Add("DirectX 9");
                }
                if (ElementRequirement.InnerHtml.IndexOf("<strong>DirectX") > -1 && ElementRequirement.InnerHtml.IndexOf("10") > -1)
                {
                    requirement.Gpu.Add("DirectX 10");
                }
                if (ElementRequirement.InnerHtml.IndexOf("<strong>DirectX") > -1 && ElementRequirement.InnerHtml.IndexOf("11") > -1)
                {
                    requirement.Gpu.Add("DirectX 11");
                }


                //< li >< strong > DirectX:</ strong > Version 10 < br ></ li >
                //< li >< strong > Network:</ strong > Broadband Internet connection<br></ li >

                //< li >< strong > Storage:</ strong > 350 MB available space </ li >
                if (ElementRequirement.InnerHtml.IndexOf("<strong>Storage") > -1 || ElementRequirement.InnerHtml.IndexOf("<strong>Hard Drive") > -1)
                {
                    string storage = ElementRequirement.InnerHtml.ToLower()
                        .Replace("\t", " ")
                        .Replace("<strong>storage:</strong>", string.Empty)
                        .Replace("<strong>hard drive:</strong>", string.Empty)
                        .Replace("available space", string.Empty)
                        .Replace("equivalent or better", string.Empty)
                        .Replace("or equivalent", string.Empty)
                        .Replace("hd space", string.Empty)
                        .Replace("free space", string.Empty)
                        .Replace("free hard drive space", string.Empty)
                        .Replace("<br>", string.Empty)
                        .Trim();
                    logger.Debug($"storage: {storage}");
                    if (storage.IndexOf("mb") > -1)
                    {
                        requirement.Storage = 1024 * 1024 * long.Parse(storage.Replace("mb", string.Empty).Trim());
                    }
                    if (storage.IndexOf("gb") > -1)
                    {
                        requirement.Storage = 1024 * 1024 * 1024 * long.Parse(storage.Replace("gb", string.Empty).Trim());
                    }
                    requirement.StorageUsage = SizeSuffix(requirement.Storage);
                }
            }

            return requirement;
        }
    }
}
