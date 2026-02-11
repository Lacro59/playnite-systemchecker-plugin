using AngleSharp.Dom;
using AngleSharp.Dom.Html;
using AngleSharp.Parser.Html;
using CommonPluginsShared;
using CommonPluginsShared.Extensions;
using CommonPluginsStores.Models;
using FuzzySharp;
using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using SystemChecker.Models;
using SystemChecker.Services;

namespace SystemChecker.Clients
{
	public class Benchmark
	{
		private static readonly ILogger Logger = LogManager.GetLogger();
		private static readonly SystemCheckerDatabase PluginDatabase = SystemChecker.PluginDatabase;

		private static FileDataTools FileDataTools => new FileDataTools(PluginDatabase.PluginName, "Benchmark");

		#region GPU

		private const string UrlVideoCard = @"https://www.videocardbenchmark.net/gpu_list.php";

		private List<BenchmarkData> _videoCardDataList;
		internal List<BenchmarkData> VideoCardDataList
		{
			get
			{
				if (_videoCardDataList == null)
				{
					string cacheFile = Path.Combine(PluginDatabase.Paths.PluginUserDataPath, "VideoCardDataList.json");
					List<BenchmarkData> data = FileDataTools.LoadData<List<BenchmarkData>>(cacheFile, 43200);

					if (data?.Count > 0)
					{
						Common.LogDebug(true, "GetVideoCardDataFromCache");
						_videoCardDataList = data;
					}
					else
					{
						Common.LogDebug(true, "GetVideoCardDataFromWeb");
						_videoCardDataList = GetData(UrlVideoCard);

						if (_videoCardDataList?.Count > 0)
						{
							FileDataTools.SaveData(cacheFile, _videoCardDataList);
						}
						else
						{
							Logger.Warn("VideoCardDataList is empty");
						}
					}
				}
				return _videoCardDataList;
			}
			set => _videoCardDataList = value;
		}

		public bool? IsBetterGpu(string gpuPC, string gpuCompare)
		{
			return IsBetter(gpuPC, gpuCompare, isGpu: true);
		}

		public string PrepareGpuName(string gpu)
		{
			bool isNvidia = Gpu.CallIsNvidia(gpu);

			gpu = gpu.Replace("nvidia", string.Empty, StringComparison.InvariantCultureIgnoreCase)
				.Replace("amd", string.Empty, StringComparison.InvariantCultureIgnoreCase)
				.Replace("(r)", string.Empty, StringComparison.InvariantCultureIgnoreCase)
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
					gpu = gpu.ToLower().Replace("gts", string.Empty).Replace("geforce", "geforce gts").Trim();
				if (gpu.ToLower().IndexOf("gtx") > -1)
					gpu = gpu.ToLower().Replace("gtx", string.Empty).Replace("geforce", "geforce gtx").Trim();
				if (gpu.ToLower().IndexOf("rtx") > -1 && gpu.ToLower().IndexOf("geforce") == -1)
					gpu = gpu.ToLower().Replace("rtx", string.Empty).Replace("geforce", "geforce rtx").Trim();

				if (Regex.IsMatch(gpu, @"\d* gt", RegexOptions.IgnoreCase)
					&& gpu.ToLower().IndexOf("gtx") == -1
					&& gpu.ToLower().IndexOf("gts") == -1)
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
						gpu = gpu.ToLower().Replace("geforce", "geforce gts");
					else if (val < 2000)
						gpu = gpu.ToLower().Replace("geforce", "geforce gtx");
					else
						gpu = gpu.ToLower().Replace("geforce", "geforce rtx");
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

		private const string UrlCpu = @"https://www.cpubenchmark.net/cpu_list.php";

		private List<BenchmarkData> _cpuDataList;
		internal List<BenchmarkData> CpuDataList
		{
			get
			{
				if (_cpuDataList == null)
				{
					string cacheFile = Path.Combine(PluginDatabase.Paths.PluginUserDataPath, "CpuDataList.json");
					List<BenchmarkData> data = FileDataTools.LoadData<List<BenchmarkData>>(cacheFile, 43200);

					if (data?.Count > 0)
					{
						Common.LogDebug(true, "GetCpuDataFromCache");
						_cpuDataList = data;
					}
					else
					{
						Common.LogDebug(true, "GetCpuDataFromWeb");
						_cpuDataList = GetData(UrlCpu);

						if (_cpuDataList?.Count > 0)
						{
							FileDataTools.SaveData(cacheFile, _cpuDataList);
						}
						else
						{
							Logger.Warn("CpuDataList is empty");
						}
					}
				}
				return _cpuDataList;
			}
			set => _cpuDataList = value;
		}

		public bool? IsBetterCpu(string cpuPC, string cpuCompare)
		{
			return IsBetter(cpuPC, cpuCompare, isGpu: false);
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

			string clock = Regex.Match(cpu, @"\d[.]\d+[mg]").Value.Replace("g", string.Empty).Replace("m", string.Empty).Trim();
			if (clock.IsNullOrEmpty())
			{
				clock = Regex.Match(cpu, @"\d[mg]", RegexOptions.RightToLeft).Value.Replace("g", string.Empty).Replace("m", string.Empty).Trim();
			}
			if (clock.Length == 3)
				cpu = cpu.Replace(clock, clock + "0");
			if (clock.Length == 1)
				cpu = cpu.Replace(" " + clock, " " + clock + ".00");

			if (isIntel)
			{
				if (cpu.IndexOf("intel i") > -1)
					cpu = cpu.Replace("intel i", "intel core i");
				if (cpu.IndexOf("core i") > -1 && cpu.IndexOf("intel core") == -1)
					cpu = cpu.Replace("core i", "intel core i");
				if (cpu.IndexOf("intel core") == -1 && cpu.IndexOf("intel xeon") == -1)
					cpu = "intel core " + cpu;
				if (Regex.IsMatch(cpu, @"i\d \d+"))
					cpu = Regex.Replace(cpu, @"(i\d) ", "$1-");
				if (Regex.IsMatch(cpu, @"\d\w? \d[.]\d+[mg]"))
					cpu = Regex.Replace(cpu, @"( \d[.]\d+[mg])", " @$1");
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
				IHtmlDocument htmlDocument = parser.Parse(webData);

				foreach (IElement item in htmlDocument.QuerySelectorAll("table#cputable tbody tr"))
				{
					BenchmarkData benchmarkData = new BenchmarkData
					{
						Id = item.GetAttribute("id")
					};

					int idx = 0;
					foreach (IElement td in item.QuerySelectorAll("td"))
					{
						switch (idx)
						{
							case 0:
								benchmarkData.Name = td.QuerySelector("a").InnerHtml;
								idx++;
								break;
							case 1:
								float.TryParse(td.InnerHtml, out float mark);
								benchmarkData.Mark = mark;
								idx++;
								break;
							case 2:
								int.TryParse(td.InnerHtml, out int rank);
								benchmarkData.Rank = rank;
								idx++;
								break;
						}
					}

					benchmarkDatas.Add(benchmarkData);
				}
			}
			catch (Exception ex)
			{
				Common.LogError(ex, false, $"Failed to download {url}", true, PluginDatabase.PluginName);
			}

			return benchmarkDatas;
		}

		private bool? IsBetter(string pc, string compare, bool isGpu)
		{
			try
			{
				List<BenchmarkData> benchmarkData;
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

				var fuzzListPc = benchmarkData
					.Select(x => new { MatchPercent = Fuzz.PartialRatio(pc.ToLower(), x.Name.ToLower()), Data = x })
					.OrderByDescending(x => x.MatchPercent)
					.ToList();

				var fuzzListCompare = benchmarkData
					.Select(x => new { MatchPercent = Fuzz.PartialRatio(compare.ToLower(), x.Name.ToLower()), Data = x })
					.OrderByDescending(x => x.MatchPercent)
					.ToList();

				List<BenchmarkData> foundPc = new List<BenchmarkData>();
				List<BenchmarkData> foundCompare = new List<BenchmarkData>();

				if (fuzzListPc.First().MatchPercent >= 95)
				{
					// Prefer exact name match over fuzzy-first
					var exactMatch = fuzzListPc.FirstOrDefault(x => x.Data.Name.IsEqual(pc));
					foundPc = new List<BenchmarkData>
					{
						exactMatch != null ? exactMatch.Data : fuzzListPc.First().Data
					};
				}

				if (fuzzListCompare.First().MatchPercent >= 95)
				{
					var exactMatch = fuzzListCompare.FirstOrDefault(x => x.Data.Name.IsEqual(compare));
					foundCompare = new List<BenchmarkData>
					{
						exactMatch != null ? exactMatch.Data : fuzzListCompare.First().Data
					};
				}

				Common.LogDebug(true, $"Benchmark - {pc} - {(foundPc.Count == 0 ? string.Empty : foundPc[0].Name)}");
				Common.LogDebug(true, $"Benchmark - {compare} - {(foundCompare.Count == 0 ? string.Empty : foundCompare[0].Name)}");

				if (foundPc.Count == 0 || foundCompare.Count == 0)
				{
					return null;
				}

				return foundPc[0].Mark == 0 || foundCompare[0].Mark == 0
					? foundPc[0].Rank <= foundCompare[0].Rank
					: foundPc[0].Mark >= foundCompare[0].Mark;
			}
			catch (Exception ex)
			{
				Common.LogError(ex, false, PluginDatabase.PluginName);
				return null;
			}
		}
	}
}