using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Dom.Html;
using AngleSharp.Parser.Html;
using CommonPluginsShared;
using CommonPluginsShared.Extensions;
using FuzzySharp;
using Playnite.SDK;
using Playnite.SDK.Data;
using SystemChecker.Models;
using SystemChecker.Services;

namespace SystemChecker.Clients
{
    public class Benchmark
    {
        internal static ILogger logger => LogManager.GetLogger();
        private readonly SystemCheckerDatabase PluginDatabase = SystemChecker.PluginDatabase;


        #region GPU
        private const string urlVideoCard = @"https://www.videocardbenchmark.net/gpu_list.php";

        protected List<BenchmarkData> _VideoCardDataList;
        internal List<BenchmarkData> VideoCardDataList
        {
            get
            {
                string dataPath = Path.Combine(PluginDatabase.Paths.PluginUserDataPath, "VideoCardDataList.json");
                if (_VideoCardDataList == null)
                {
                    // From cache if exists & not expired
                    if (File.Exists(dataPath) && File.GetLastWriteTime(dataPath).AddDays(30) > DateTime.Now)
                    {
                        Common.LogDebug(true, "GetVideoCardDataFromCache");
                        VideoCardDataList = Serialization.FromJsonFile<List<BenchmarkData>>(dataPath);
                    }
                    // From web
                    else
                    {
                        Common.LogDebug(true, "GetVideoCardDataFromWeb");
                        VideoCardDataList = GetData(urlVideoCard);

                        // Write file for cache
                        if (VideoCardDataList?.Count > 0)
                        {
                            File.WriteAllText(dataPath, Serialization.ToJson(VideoCardDataList), Encoding.UTF8);
                        }
                        else
                        {
                            logger.Warn($"VideoCardDataList is empty");
                        }
                    }
                }
                return _VideoCardDataList;
            }

            set => _VideoCardDataList = value;
        }


        public bool? IsBetterGpu(string gpuPC, string gpuCompare)
        {
            return IsBetter(gpuPC, gpuCompare, true);
        }


        public string PrepareGpuName(string gpu)
        {
            bool isNvidia = Gpu.CallIsNvidia(gpu);

            gpu = gpu.Replace("nvidia", string.Empty, StringComparison.InvariantCultureIgnoreCase)
                .Replace("amd", string.Empty, StringComparison.InvariantCultureIgnoreCase)
                .Replace("series", string.Empty, StringComparison.InvariantCultureIgnoreCase)
                .Replace("graphics card", string.Empty, StringComparison.InvariantCultureIgnoreCase)
                .Replace("graphics", string.Empty, StringComparison.InvariantCultureIgnoreCase)
                .Replace("®", string.Empty, StringComparison.InvariantCultureIgnoreCase)
                .Replace("(", string.Empty, StringComparison.InvariantCultureIgnoreCase)
                .Replace(")", string.Empty, StringComparison.InvariantCultureIgnoreCase)
                .Trim();

            gpu = Regex.Replace(gpu, @";[ ]?\d+[ ]?(MB)?(GB)?", string.Empty, RegexOptions.IgnoreCase).Trim();

            int.TryParse(Regex.Match(gpu, @"\d+").Value, out int val);

            if (isNvidia)
            {
                if (gpu.ToLower().IndexOf("geforce") == -1)
                {
                    gpu = "geforce " + gpu;
                }

                if (gpu.ToLower().IndexOf("gts") > -1)
                {
                    gpu = gpu.ToLower().Replace("gts", string.Empty).Replace("geforce", "geforce gts").Trim();
                }
                if (gpu.ToLower().IndexOf("gtx") > -1)
                {
                    gpu = gpu.ToLower().Replace("gtx", string.Empty).Replace("geforce", "geforce gtx").Trim();
                }
                if (gpu.ToLower().IndexOf("rtx") > -1 && gpu.ToLower().IndexOf("geforce") == -1)
                {
                    gpu = gpu.ToLower().Replace("rtx", string.Empty).Replace("geforce", "geforce rtx").Trim();
                }

                if (Regex.IsMatch(gpu, @"\d* gt", RegexOptions.IgnoreCase) && gpu.ToLower().IndexOf("gtx") == -1 && gpu.ToLower().IndexOf("gts") == -1)
                {
                    gpu = gpu.ToLower().Replace("gt", string.Empty).Replace("geforce", "geforce gt").Trim();
                }
                
                int.TryParse(val.ToString().Substring(1), out int val2);
                if (Regex.IsMatch(gpu, @"geforce \d+", RegexOptions.IgnoreCase)
                    && (!Regex.IsMatch(gpu, @"\d+m", RegexOptions.IgnoreCase) || val2 >= 50)
                    && (!Regex.IsMatch(gpu, @"\d+a", RegexOptions.IgnoreCase) || val2 >= 50)
                    && val != 510
                    && gpu.ToLower().IndexOf("rtx") == -1
                    && gpu.ToLower().IndexOf("gtx") == -1
                    && gpu.ToLower().IndexOf("gt") == -1
                    && gpu.ToLower().IndexOf("gts") == -1)
                {
                    if (val == 150 || val == 160 || val == 240 || val == 250 || val == 350 || val == 360 || val == 450)
                    {
                        gpu = gpu.ToLower().Replace("geforce", "geforce gts");
                    }
                    else if (val < 2000)
                    {
                        gpu = gpu.ToLower().Replace("geforce", "geforce gtx");
                    }
                    else
                    {
                        gpu = gpu.ToLower().Replace("geforce", "geforce rtx");
                    }
                }

                gpu = gpu.ToLower().Replace("ti", " ti");
            }
            else
            {
                gpu = Regex.Replace(gpu, @"HD\d+", $"hd {val}", RegexOptions.IgnoreCase).Trim();
            }

            return gpu.Replace("  ", " ");
        }
        #endregion


        #region CPU
        private const string urlCpu = @"https://www.cpubenchmark.net/cpu_list.php";

        protected List<BenchmarkData> _CpuDataList;
        internal List<BenchmarkData> CpuDataList
        {
            get
            {
                string dataPath = Path.Combine(PluginDatabase.Paths.PluginUserDataPath, "CpuDataList.json");
                if (_CpuDataList == null)
                {
                    // From cache if exists & not expired
                    if (File.Exists(dataPath) && File.GetLastWriteTime(dataPath).AddDays(30) > DateTime.Now)
                    {
                        Common.LogDebug(true, "GetCpuDataFromCache");
                        CpuDataList = Serialization.FromJsonFile<List<BenchmarkData>>(dataPath);
                    }
                    // From web
                    else
                    {
                        Common.LogDebug(true, "GetCpuDataFromWeb");
                        CpuDataList = GetData(urlCpu);

                        // Write file for cache
                        if (CpuDataList?.Count > 0)
                        {
                            File.WriteAllText(dataPath, Serialization.ToJson(CpuDataList), Encoding.UTF8);
                        }
                        else
                        {
                            logger.Warn($"CpuDataList is empty");
                        }
                    }
                }
                return _CpuDataList;
            }

            set => _CpuDataList = value;
        }


        public bool? IsBetterCpu(string gpuPC, string gpuCompare)
        {
            return IsBetter(gpuPC, gpuCompare, false);
        }


        private string PrepareCpuName(string cpu)
        {
            bool isIntel = Cpu.CallIsIntel(cpu);

            cpu = cpu.ToLower()
                .Replace("(r)", string.Empty)
                .Replace("(tm)", string.Empty)
                .Replace("®", string.Empty)
                .Replace("™", string.Empty)
                .Replace("cpu @", "@")
                .Replace(" cpu", string.Empty)
                .Replace(" ghz", "ghz")
                .Replace(" mhz", "mhz")
                .Trim();

            cpu = Regex.Replace(cpu, @"(@ \d+.\d+)hz", "$1ghz");

            string clock = Regex.Match(cpu, @"\d[.]\d+[mg]").Value.ToString().Replace("g", string.Empty).Replace("m", string.Empty).Trim();
            if (clock.IsNullOrEmpty())
            {
                clock = Regex.Match(cpu, @"\d[mg]", RegexOptions.RightToLeft).Value.ToString().Replace("g", string.Empty).Replace("m", string.Empty).Trim();
            }
            if (clock.Length == 3)
            {
                cpu = cpu.Replace(clock, clock + "0");
            }
            if (clock.Length == 1)
            {
                cpu = cpu.Replace(" " + clock, " " + clock + ".00");
            }

            if (isIntel)
            {
                if (cpu.IndexOf("intel i") > -1)
                {
                    cpu = cpu.Replace("intel i", "intel core i");
                }
                if (cpu.IndexOf("core i") > -1 && cpu.IndexOf("intel core") == -1)
                {
                    cpu = cpu.Replace("core i", "intel core i");
                }
                if (cpu.IndexOf("intel core") == -1)
                {
                    cpu = "intel core " + cpu;
                }

                if (Regex.IsMatch(cpu, @"i\d \d+"))
                {
                    cpu = Regex.Replace(cpu, @"(i\d) ", "$1-");
                }

                if (Regex.IsMatch(cpu, @"\d\w? \d[.]\d+[mg]"))
                {
                    cpu = Regex.Replace(cpu, @"( \d[.]\d+[mg])", " @$1");
                }
            }
            else
            {

            }

            return cpu.Replace("  ", " ");
        }
        #endregion


        private List<BenchmarkData> GetData(string url)
        {
            List<BenchmarkData> benchmarkDatas = new List<BenchmarkData>();
            try
            {
                string webData = Web.DownloadStringData(url).GetAwaiter().GetResult();
                HtmlParser parser = new HtmlParser();
                IHtmlDocument HtmlRequirement = parser.Parse(webData);

                foreach (IElement item in HtmlRequirement.QuerySelectorAll("table#cputable tbody tr"))
                {
                    BenchmarkData benchmarkData = new BenchmarkData();
                    benchmarkData.id = item.GetAttribute("id");

                    int idx = 0;
                    foreach (IElement td in item.QuerySelectorAll("td"))
                    {
                        switch (idx)
                        {
                            case 0:
                                benchmarkData.name = td.QuerySelector("a").InnerHtml;
                                idx++;
                                break;

                            case 1:
                                int.TryParse(td.InnerHtml, out int mark);
                                benchmarkData.mark = mark;
                                idx++;
                                break;

                            case 2:
                                int.TryParse(td.InnerHtml, out int rank);
                                benchmarkData.rank = rank;
                                idx++;
                                break;

                            default:
                                break;
                        }
                    }

                    benchmarkDatas.Add(benchmarkData);
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, $"Failed to download {urlVideoCard}", true, PluginDatabase.PluginName);
            }

            return benchmarkDatas;
        }


        private bool? IsBetter(string pc, string compare, bool isGpu)
        {
            try
            {
                List<BenchmarkData> benchmarkData = new List<BenchmarkData>();
                if (isGpu)
                {
                    benchmarkData = VideoCardDataList;
                    pc = PrepareGpuName(pc);
                    compare = PrepareGpuName(compare);
                }
                else
                {
                    benchmarkData = CpuDataList;
                    pc = PrepareCpuName(pc);
                    compare = PrepareCpuName(compare);
                }

                List<BenchmarkData> foundPC = new List<BenchmarkData>();
                List<BenchmarkData> foundCompare = new List<BenchmarkData>();

                var FuzzListPC = benchmarkData.Select(x => new { MatchPercent = Fuzz.PartialRatio(pc.ToLower(), x.name.ToLower()), Data = x })
                    .OrderByDescending(x => x.MatchPercent)
                    .ToList();
                var FuzzListCompare = benchmarkData.Select(x => new { MatchPercent = Fuzz.PartialRatio(compare.ToLower(), x.name.ToLower()), Data = x })
                    .OrderByDescending(x => x.MatchPercent)
                    .ToList();

                if (FuzzListPC.First().MatchPercent >= 95)
                {
                    dynamic matchPC = null;
                    try
                    {
                        matchPC = FuzzListPC.Where(x => x.Data.name.IsEqual(pc))?.First() ?? null;
                    }
                    catch { }
                    
                    if (matchPC != null)
                    {
                        foundPC = new List<BenchmarkData>() { matchPC.Data };
                    }
                    else
                    {
                        foundPC = new List<BenchmarkData>() { FuzzListPC.First().Data };
                    }
                }
                if (FuzzListCompare.First().MatchPercent >= 95)
                {
                    dynamic matchCompare = null;
                    try
                    {
                        matchCompare = FuzzListCompare.Where(x => x.Data.name.IsEqual(compare))?.First() ?? null;
                    }
                    catch { }

                    if (matchCompare != null)
                    {
                        foundCompare = new List<BenchmarkData>() { matchCompare.Data };
                    }
                    else
                    {
                        foundCompare = new List<BenchmarkData>() { FuzzListCompare.First().Data };
                    }
                }

                Common.LogDebug(true, $"Benchmark - {pc} - {(foundPC?.Count == 0 ? "" : foundPC[0].name)}");
                Common.LogDebug(true, $"Benchmark - {compare} - {(foundCompare?.Count == 0 ? "" : foundCompare[0].name)}");

                if (foundPC?.Count == 0 || foundCompare?.Count == 0)
                {
                    return null;
                }

                return foundPC[0].mark >= foundCompare[0].mark;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, PluginDatabase.PluginName);
                return null;
            }
        }
    }
}
