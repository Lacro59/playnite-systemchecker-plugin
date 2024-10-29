using AngleSharp.Dom.Html;
using AngleSharp.Parser.Html;
using CommonPlayniteShared.PluginLibrary.SteamLibrary.SteamShared;
using CommonPluginsShared;
using CommonPluginsShared.Models;
using Playnite.SDK.Models;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using SystemChecker.Models;
using SystemChecker.Services;
using CommonPluginsShared.Extensions;
using AngleSharp.Dom;

namespace SystemChecker.Clients
{
    public class SteamRequierements : RequierementMetadata
    {
        private readonly SystemCheckerDatabase PluginDatabase = SystemChecker.PluginDatabase;

        private uint AppId { get; set; }


        public SteamRequierements()
        {

        }

        private string GetSteamData()
        {
            string url = string.Empty;
            try
            {
                url = $"https://store.steampowered.com/api/appdetails?appids={AppId}&l=english";
                return Web.DownloadStringData(url).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, $"Failed to download {url}", true, PluginDatabase.PluginName);
                return string.Empty;
            }
        }


        public override GameRequierements GetRequirements()
        {
            GameRequierements = SystemChecker.PluginDatabase.GetDefault(GameContext);

            Requirement Minimum = new Requirement();
            Requirement Recommanded = new Requirement();

            try
            {
                string data = GetSteamData();
                Dictionary<string, StoreAppDetailsResult> parsedData = Serialization.FromJson<Dictionary<string, StoreAppDetailsResult>>(data);

                if (parsedData[AppId.ToString()].data != null && Serialization.ToJson(parsedData[AppId.ToString()].data.pc_requirements) != "[]")
                {
                    Common.LogDebug(true, Serialization.ToJson(parsedData[AppId.ToString()].data.pc_requirements));

                    dynamic pc_requirements = Serialization.FromJson<dynamic>(Serialization.ToJson(parsedData[AppId.ToString()].data.pc_requirements));

                    if (pc_requirements["minimum"] != null)
                    {
                        Minimum = ParseRequirement((string)pc_requirements["minimum"]);
                    }

                    if (pc_requirements["recommended"] != null)
                    {
                        Recommanded = ParseRequirement((string)pc_requirements["recommended"]);
                    }


                    GameRequierements.SourcesLink = new SourceLink
                    {
                        Name = "Steam",
                        GameName = parsedData[AppId.ToString()].data.name,
                        Url = $"https://store.steampowered.com/app/{AppId}/"
                    };
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginDatabase.PluginName);
            }

            Minimum.IsMinimum = true;
            GameRequierements.Items = new List<Requirement> { Minimum, Recommanded };
            return GameRequierements;
        }

        public GameRequierements GetRequirements(Game game, uint appId = 0)
        {
            GameContext = game;

            AppId = appId;
            if (appId == 0)
            {
                AppId = uint.Parse(GameContext.GameId);
            }

            return GetRequirements();
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
            foreach (IElement ElementRequirement in HtmlRequirement.QuerySelectorAll("li"))
            {
                Common.LogDebug(true, $"SteamRequierements - {ElementRequirement.InnerHtml}");

                if (ElementRequirement.InnerHtml.Contains("TBD", StringComparison.InvariantCultureIgnoreCase)) 
                {
                    continue;
                }

                //<strong>OS:</strong> Windows XP / 7 / 8 / 8.1 / 10 x32 and x64<br> </ li >
                if (ElementRequirement.InnerHtml.IndexOf("<strong>OS") > -1)
                {
                    string os = Regex.Replace(ElementRequirement.InnerHtml, "<[^>]*>", string.Empty);
                    os = os.ToLower()
                        .Replace("\t", " ")
                        .Replace("os: ", string.Empty)
                        .Replace("os *: ", string.Empty)
                        .Replace("pc/ms-dos 5.0", string.Empty)
                        .Replace("(64-bit required)", string.Empty)
                        .Replace("(32/64-bit)", string.Empty)
                        .Replace("with platform update for  7 ( versions only)", string.Empty)
                        .Replace("(vista+ probably works)", string.Empty)
                        .Replace("win ", string.Empty)
                        .Replace("windows", string.Empty)
                        .Replace("microsoft", string.Empty)
                        .Replace(", 32-bit", string.Empty)
                        .Replace(", 32bit", string.Empty)
                        .Replace(", 64-bit", string.Empty)
                        .Replace(", 64bit", string.Empty)
                        .Replace("®", string.Empty)
                        .Replace("™", string.Empty)
                        .Replace("+", string.Empty)
                        .Replace("and above", string.Empty)
                        .Replace("x32", string.Empty)
                        .Replace("and", string.Empty)
                        .Replace("x64", string.Empty)
                        .Replace("32-bit", string.Empty)
                        .Replace("32bit", string.Empty)
                        .Replace("32 bit", string.Empty)
                        .Replace("64-bit", string.Empty)
                        .Replace("64bit", string.Empty)
                        .Replace("64 bit", string.Empty)
                        .Replace("latest service pack", string.Empty)
                        .Replace("32-bit/64-bit", string.Empty)
                        .Replace("32bit/64bit", string.Empty)
                        .Replace("64-bit operating system required", string.Empty)
                        .Replace("32-bit operating system required", string.Empty)
                        .Replace(" operating system required", string.Empty)
                        .Replace("operating system required", string.Empty)
                        .Replace(" equivalent or better", string.Empty)
                        .Replace(" or equivalent.", string.Empty)
                        .Replace(" or equivalent", string.Empty)
                        .Replace(" or newer", string.Empty)
                        .Replace("or newer", string.Empty)
                        .Replace("or later", string.Empty)
                        .Replace("or higher", string.Empty)
                        .Replace("()", string.Empty)
                        .Trim();

                    if (os.Trim() != "(/)" && !os.Trim().IsNullOrEmpty())
                    {
                        foreach (string sTemp in os.Replace(",", "¤").Replace(" or ", "¤").Replace("/", "¤").Split('¤'))
                        {
                            requirement.Os.Add(sTemp.Trim());
                        }
                    }
                }

                //< li >< strong > Processor:</ strong > Intel Core2 Duo E6320 or equivalent /\t AMD Athlon 64 X2 5000 + (2 * 2.6 GHz) or equivalent<br></ li >
                if (ElementRequirement.InnerHtml.IndexOf("<strong>Processor") > -1)
                {
                    string cpu = ElementRequirement.InnerHtml
                            .Replace("\t", " ")
                            .Replace("<strong>Processor:</strong>", string.Empty)
                            .Replace("<strong>Processor: </strong>", string.Empty)
                            .Replace("&nbsp;", string.Empty)
                            .Replace("GHz, or better)", "GHz)")
                            .Replace("Requires a 64-bit processor and operating system", string.Empty)
                            .Replace("More than a Pentium", string.Empty)
                            .Replace("equivalent or higher processor", string.Empty)
                            .Replace("- Low budget CPUs such as Celeron or Duron needs to be at about twice the CPU speed", string.Empty)
                            .Replace(" equivalent or faster processor", string.Empty)
                            .Replace(" equivalent or better", string.Empty)
                            .Replace("above", string.Empty)
                            .Replace("or similar", string.Empty)
                            .Replace("or faster", string.Empty)
                            .Replace("and up", string.Empty)
                            .Replace("(or higher)", string.Empty)
                            .Replace("or higher", string.Empty)
                            .Replace(" or equivalent.", string.Empty)
                            .Replace(" over", string.Empty)
                            .Replace(" or faster", string.Empty)
                            .Replace(" or better", string.Empty)
                            .Replace(" or equivalent", string.Empty)
                            .Replace(" or Equivalent", string.Empty)
                            .Replace("4 CPUs", string.Empty)
                            .Replace("(3 GHz Pentium® 4 recommended)", string.Empty)
                            .Replace("ghz", "GHz")
                            .Replace("Ghz", "GHz")
                            .Replace("®", string.Empty)
                            .Replace("™", string.Empty)
                            .Replace("or later that's SSE2 capable", string.Empty)
                            .Replace("Processor", string.Empty)
                            .Replace("processor", string.Empty)
                            .Replace("x86-compatible", string.Empty)
                            .Replace("(not recommended for Intel HD Graphics cards)", ", not recommended for Intel HD Graphics cards")
                            .Replace("()", string.Empty)
                            .Replace("<br>", string.Empty)
                            .Replace(", x86", string.Empty)
                            .Replace("Yes", string.Empty)
                            .Replace("GHz+", "GHz")
                            .Trim();

                    cpu = Regex.Replace(cpu, @", ~?(\d+(\.\d+)?)", " $1", RegexOptions.IgnoreCase);
                    cpu = Regex.Replace(cpu, @"(\d+),(\d+ GHz)", "$1.$2", RegexOptions.IgnoreCase);
                    cpu = Regex.Replace(cpu, @"(\d+),(\d+) - (\d+ GHz)", "$3", RegexOptions.IgnoreCase);
                    cpu = Regex.Replace(cpu, @"(\d+)GHz", "$1 GHz", RegexOptions.IgnoreCase);
                    cpu = Regex.Replace(cpu, @"(\d+)k", "$1K", RegexOptions.IgnoreCase);
                    cpu = Regex.Replace(cpu, @"(\d+\.\d+)\+ (GHz)", "$1 $2", RegexOptions.IgnoreCase);

                    cpu = Regex.Replace(cpu, @"(,|\/|\sor\s|\sand\s|\|)", "¤", RegexOptions.IgnoreCase);

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
                            .Replace("<strong>memory: </strong>", string.Empty)
                            .Trim();

                    ram = Regex.Match(ram, @"\d+((.|,)\d+)?[ ]?(mb|gb)").ToString().Trim();
                    ram = ram.Split('/')[ram.Split('/').Length - 1];

                    if (ram.ToLower().IndexOf("mb") > -1)
                    {
                        requirement.Ram = 1024 * 1024 * double.Parse(ram.ToLower()
                            .Replace("mb", string.Empty)
                            .Replace(".", CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalSeparator).Trim());
                    }

                    if (ram.ToLower().IndexOf("gb") > -1)
                    {
                        requirement.Ram = 1024 * 1024 * 1024 * double.Parse(ram.ToLower()
                            .Replace("gb", string.Empty)
                            .Replace(".", CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalSeparator).Trim());
                    }

                    requirement.RamUsage = Tools.SizeSuffix(requirement.Ram, true);
                }

                //< li >< strong > Graphics:</ strong > GeForce GT 440(1024 MB) or equivalent / Radeon HD 6450(512 MB) or equivalent / Iris Pro Graphics 5200(1792 MB) < br ></ li >
                if (ElementRequirement.InnerHtml.IndexOf("<strong>Graphics") > -1)
                {
                    string gpu = Regex.Replace(ElementRequirement.InnerHtml, @"with [(]?\d+[ ]?(MB)?(GB)?[)]? (Memory)?(Video RAM)?", string.Empty, RegexOptions.IgnoreCase);                    
                    gpu = Regex.Replace(gpu, @"\(GTX \d+ or above required for VR\)", string.Empty, RegexOptions.IgnoreCase);
                    gpu = Regex.Replace(gpu, @"DirectX \d class GPU with \dGB VRAM \(", string.Empty, RegexOptions.IgnoreCase);
                    gpu = Regex.Replace(gpu, @"Shader Model \d+(\.\d+)?", string.Empty, RegexOptions.IgnoreCase);
                    gpu = Regex.Replace(gpu, @"card capable of shader \d+(\.\d+)?", string.Empty, RegexOptions.IgnoreCase);
                    gpu = Regex.Replace(gpu, @"DX\d+ Compliant with PS\d+(\.\d+)? support", string.Empty, RegexOptions.IgnoreCase);
                    gpu = Regex.Replace(gpu, @"DX\d+ Compliant", string.Empty, RegexOptions.IgnoreCase);
                    gpu = Regex.Replace(gpu, @"de \d+ GB", string.Empty, RegexOptions.IgnoreCase);
                    gpu = Regex.Replace(gpu, @"GPU (\d+)GB VRAM", "GPU $1 GB VRAM", RegexOptions.IgnoreCase);
                    gpu = Regex.Replace(gpu, @"(,|with)?\s*(\d+)\s*(GB|MB)(\s* system ram)?", " ($2 $3)", RegexOptions.IgnoreCase);
                    gpu = Regex.Replace(gpu, @"\(A minimum of \(\d+ GB\) of VRAM\)", string.Empty, RegexOptions.IgnoreCase);

                    gpu = gpu.Replace("\t", " ")
                            .Replace("<strong>Graphics:</strong>", string.Empty)
                            .Replace("<strong>Graphics: </strong>", string.Empty)
                            .Replace("requires graphics card. minimum GPU needed: \"", string.Empty)
                            .Replace("(Integrated Graphics)", string.Empty)
                            .Replace("\" or similar", string.Empty)
                            .Replace("Graphics card supporting", string.Empty)
                            .Replace("compatible video card (integrated or dedicated with min 512MB memory)", string.Empty)
                            .Replace("capable GPU", string.Empty)
                            .Replace("at least ", string.Empty)
                            .Replace(" capable.", string.Empty)
                            .Replace(" or Higher VRAM Graphics Cards", string.Empty)
                            .Replace("VRAM Graphics Cards", "VRAM")
                            .Replace("hardware driver support required for WebGL acceleration. (AMD Catalyst 10.9, nVidia 358.50)", string.Empty)
                            .Replace("ATI or NVidia card w/ 1024 MB RAM (NVIDIA GeForce GTX 260 or ATI HD 4890)", "NVIDIA GeForce GTX 260 or ATI HD 4890")
                            .Replace("Video card must be 128 MB or more and should be a DirectX 9-compatible with support for Pixel Shader 2.0b (", string.Empty)
                            .Replace("- *NOT* an Express graphics card).", string.Empty)
                            .Replace("2GB (GeForce GTX 970 / amd RX 5500 XT)", "GeForce GTX 970 / AMD RX 5500 XT")
                            .Replace(" - anything capable of running OpenGL 4.0 (eg. ATI Radeon HD 57xx or Nvidia GeForce 400 and higher)", string.Empty)
                            .Replace("(AMD or NVIDIA equivalent)", string.Empty)
                            .Replace("/320M 512MB VRAM", string.Empty)
                            .Replace("/Intel Extreme Graphics 82845, 82865, 82915", string.Empty)
                            .Replace(" 512MB VRAM (Intel integrated GPUs are not supported!)", " / Intel integrated GPUs are not supported!")
                            .Replace("(not recommended for Intel HD Graphics cards)", ", not recommended for Intel HD Graphics cards")
                            .Replace("or similar (no support for onboard cards)", string.Empty)
                            .Replace("level Graphics Card (requires support for SSE)", string.Empty)
                            .Replace("- Integrated graphics and very low budget cards might not work.", string.Empty)
                            .Replace("3D with TnL support and", string.Empty)
                            .Replace(" compatible", string.Empty)
                            .Replace("of addressable memory", string.Empty)
                            .Replace("Any", string.Empty)
                            .Replace("any", string.Empty)
                            .Replace("/Nvidia", " / Nvidia")
                            .Replace("or AMD equivalent", string.Empty)
                            .Replace("(Requires support for SSE)", string.Empty)
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
                            .Replace("8GB Memory 8 GB RAM", "(8 GB)")
                            .Replace(" or more and should be a DirectX 9-compatible with support for Pixel Shader 3.0", string.Empty)
                            .Replace("Yes", string.Empty)
                            .Replace(", or ", string.Empty)
                            .Replace("()", string.Empty)
                            .Replace("<br>", string.Empty)
                            .Replace("®", string.Empty)
                            .Replace("™", string.Empty)
                            .Replace(" Compatible", string.Empty)
                            .Replace("  ", " ")
                            .Replace("(Shared Memory is not recommended)", string.Empty)
                            .Replace(". Integrated Intel HD Graphics should work but is not supported; problems are generally solved with a driver update.", string.Empty)
                            .Trim();

                    string gpuTmp = Regex.Match(gpu.ToLower(), @"\d+((.|,)\d+)?[ ]?(mb|gb)").ToString().Trim();
                    if (!gpuTmp.IsNullOrEmpty() && gpu.ToLower().IndexOf(gpuTmp) == 0)
                    {
                        gpu = gpuTmp;
                    }

                    gpu = Regex.Replace(gpu, @"(gb|mb)(\))?\s*\+", "$1$2", RegexOptions.IgnoreCase);
                    gpu = Regex.Replace(gpu, @" - (\d+) (gb|mb)", " ($1 $2)", RegexOptions.IgnoreCase);
                    gpu = gpu.Replace(",", "¤").Replace(" or ", "¤").Replace(" OR ", "¤").Replace(" / ", "¤").Replace(" /", "¤").Replace(" | ", "¤");
                    foreach (string sTemp in gpu.Split('¤'))
                    {
                        if (sTemp.Trim() != string.Empty)
                        {
                            if (sTemp.ToLower().IndexOf("nvidia") > -1 || sTemp.ToLower().IndexOf("amd") > -1 || sTemp.ToLower().IndexOf("intel") > -1)
                            {
                                requirement.Gpu.Add(Regex.Replace(sTemp, @"\(\d+\s*(mb|gb)\)", string.Empty, RegexOptions.IgnoreCase).Trim());
                            }
                            else if (Regex.IsMatch(sTemp, @"\d+((.|,)\d+)?\s*(mb|gb)", RegexOptions.IgnoreCase) && sTemp.Length < 10)
                            {
                                requirement.Gpu.Add(sTemp.ToUpper().Replace("(", string.Empty).Replace(")", string.Empty).Trim() + " VRAM");
                            }
                            else if (Regex.IsMatch(sTemp, @"\(\d+((.|,)\d+)?\s*(mb|gb)\) vram", RegexOptions.IgnoreCase))
                            {
                                requirement.Gpu.Add(sTemp.ToUpper().Replace("(", string.Empty).Replace(")", string.Empty).Trim());
                            }
                            else if (Regex.IsMatch(sTemp, @"\(\d+((.|,)\d+)?\s*(mb|gb)\) ram", RegexOptions.IgnoreCase))
                            {
                                requirement.Gpu.Add(sTemp.ToUpper().Replace("(", string.Empty).Replace(")", string.Empty).Replace("RAM", "VRAM").Trim());
                            }
                            else if (Regex.IsMatch(sTemp, @"DirectX \d+[.]\d", RegexOptions.IgnoreCase))
                            {
                                requirement.Gpu.Add(Regex.Replace(sTemp, @"[.]\d", string.Empty));
                            }
                            else
                            {
                                requirement.Gpu.Add(sTemp.Trim());
                            }
                        }
                    }
                }
                
                if (ElementRequirement.InnerHtml.IndexOf("<strong>DirectX") > -1 && ElementRequirement.InnerHtml.IndexOf("8") > -1 && requirement.Gpu.Find(x => x.IsEqual("DirectX 8")) == null)
                {
                    requirement.Gpu.Add("DirectX 8");
                }
                if (ElementRequirement.InnerHtml.IndexOf("<strong>DirectX") > -1 && ElementRequirement.InnerHtml.IndexOf("9") > -1 && requirement.Gpu.Find(x => x.IsEqual("DirectX 9")) == null)
                {
                    requirement.Gpu.Add("DirectX 9");
                }
                if (ElementRequirement.InnerHtml.IndexOf("<strong>DirectX") > -1 && ElementRequirement.InnerHtml.IndexOf("10") > -1 && requirement.Gpu.Find(x => x.IsEqual("DirectX 10")) == null)
                {
                    requirement.Gpu.Add("DirectX 10");
                }
                if (ElementRequirement.InnerHtml.IndexOf("<strong>DirectX") > -1 && ElementRequirement.InnerHtml.IndexOf("11") > -1 && requirement.Gpu.Find(x => x.IsEqual("DirectX 11")) == null)
                {
                    requirement.Gpu.Add("DirectX 11");
                }
                if (ElementRequirement.InnerHtml.IndexOf("<strong>DirectX") > -1 && ElementRequirement.InnerHtml.IndexOf("12") > -1 && requirement.Gpu.Find(x => x.IsEqual("DirectX 12")) == null)
                {
                    requirement.Gpu.Add("DirectX 12");
                }

                //< li >< strong > Storage:</ strong > 350 MB available space </ li >
                if (ElementRequirement.InnerHtml.IndexOf("<strong>Storage") > -1 || ElementRequirement.InnerHtml.IndexOf("<strong>Hard Drive") > -1)
                {
                    string storage = ElementRequirement.InnerHtml.ToLower()
                        .Replace("\t", " ")
                        .Replace("<strong>storage:</strong>", string.Empty)
                        .Replace("<strong>storage: </strong>", string.Empty)
                        .Replace("<strong>hard drive:</strong>", string.Empty)
                        .Replace("<strong>hard drive: </strong>", string.Empty)
                        .Trim();

                    storage = Regex.Match(storage, @"\d+((.|,)\d+)?[ ]?(mb|gb)").ToString().Trim();

                    if (storage.IndexOf("mb") > -1)
                    {
                        requirement.Storage = 1024 * 1024 * double.Parse(storage.Replace("mb", string.Empty)
                            .Replace(".", CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalSeparator)
                            .Replace("available hard disk space", string.Empty).Trim());
                    }
                    if (storage.IndexOf("gb") > -1)
                    {
                        requirement.Storage = 1024 * 1024 * 1024 * double.Parse(storage.Replace("gb", string.Empty)
                            .Replace(".", CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalSeparator)
                            .Replace("available hard disk space", string.Empty).Trim());
                    }
                    requirement.StorageUsage = Tools.SizeSuffix(requirement.Storage);
                }
            }

            return requirement;
        }
    }
}
