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
	/// <summary>
	/// Handles CPU and GPU benchmark data retrieval, caching, and comparison logic.
	/// </summary>
	public class Benchmark
	{
		#region Constants

		private const string UrlVideoCard = @"https://www.videocardbenchmark.net/gpu_list.php";
		private const string UrlCpu = @"https://www.cpubenchmark.net/cpu_list.php";

		private const int CacheExpirationMinutes = 43200; // 30 days
		private const int FuzzyMatchThreshold = 95;

		private const string VideoCardCacheFile = "VideoCardDataList.json";
		private const string CpuCacheFile = "CpuDataList.json";

		#endregion

		#region Static Fields

		private static readonly ILogger Logger = LogManager.GetLogger();
		private static readonly SystemCheckerDatabase PluginDatabase = SystemChecker.PluginDatabase;
		private static FileDataTools FileDataTools => new FileDataTools(PluginDatabase.PluginName, "Benchmark");

		// Compiled regex patterns for better performance
		private static readonly Regex RegexMemorySize = new Regex(@";[ ]?\d+[ ]?(MB)?(GB)?", RegexOptions.IgnoreCase | RegexOptions.Compiled);
		private static readonly Regex RegexFirstDigit = new Regex(@"\d+", RegexOptions.Compiled);
		private static readonly Regex RegexGtsPattern = new Regex(@"\d* gt", RegexOptions.IgnoreCase | RegexOptions.Compiled);
		private static readonly Regex RegexGeforceDigits = new Regex(@"geforce \d+", RegexOptions.IgnoreCase | RegexOptions.Compiled);
		private static readonly Regex RegexMobileSuffix = new Regex(@"\d+m", RegexOptions.IgnoreCase | RegexOptions.Compiled);
		private static readonly Regex RegexASuffix = new Regex(@"\d+a", RegexOptions.IgnoreCase | RegexOptions.Compiled);
		private static readonly Regex RegexHdPattern = new Regex(@"HD\d+", RegexOptions.IgnoreCase | RegexOptions.Compiled);
		private static readonly Regex RegexClockSpeed = new Regex(@"(@ \d+.\d+)hz", RegexOptions.Compiled);
		private static readonly Regex RegexClockPrimary = new Regex(@"\d[.]\d+[mg]", RegexOptions.Compiled);
		private static readonly Regex RegexClockSecondary = new Regex(@"\d[mg]", RegexOptions.RightToLeft | RegexOptions.Compiled);
		private static readonly Regex RegexIntelCore = new Regex(@"i\d \d+", RegexOptions.Compiled);
		private static readonly Regex RegexIntelCoreReplace = new Regex(@"(i\d) ", RegexOptions.Compiled);
		private static readonly Regex RegexIntelClock = new Regex(@"\d\w? \d[.]\d+[mg]", RegexOptions.Compiled);
		private static readonly Regex RegexIntelClockReplace = new Regex(@"( \d[.]\d+[mg])", RegexOptions.Compiled);

		#endregion

		#region Private Fields

		private List<BenchmarkData> _videoCardDataList;
		private List<BenchmarkData> _cpuDataList;

		#endregion

		#region GPU Properties & Methods

		/// <summary>
		/// Gets or sets the cached GPU benchmark data list.
		/// Data is loaded from cache if available, otherwise fetched from the web.
		/// </summary>
		internal List<BenchmarkData> VideoCardDataList
		{
			get
			{
				if (_videoCardDataList == null)
				{
					_videoCardDataList = LoadOrFetchBenchmarkData(
						VideoCardCacheFile,
						UrlVideoCard,
						"VideoCardDataList",
						"GetVideoCardDataFromCache",
						"GetVideoCardDataFromWeb"
					);
				}
				return _videoCardDataList;
			}
			set => _videoCardDataList = value;
		}

		/// <summary>
		/// Compares two GPUs and determines if the first GPU is better than or equal to the second.
		/// </summary>
		/// <param name="gpuPC">The GPU name from the user's PC.</param>
		/// <param name="gpuCompare">The GPU name to compare against.</param>
		/// <returns>True if gpuPC is better, false if worse, null if comparison failed.</returns>
		public bool? IsBetterGpu(string gpuPC, string gpuCompare)
		{
			return IsBetter(gpuPC, gpuCompare, isGpu: true);
		}

		/// <summary>
		/// Normalizes GPU names by removing brand names, common suffixes, and standardizing formats
		/// to improve fuzzy matching accuracy with benchmark database.
		/// </summary>
		/// <param name="gpu">Raw GPU name to prepare.</param>
		/// <returns>Normalized GPU name.</returns>
		public string PrepareGpuName(string gpu)
		{
			if (string.IsNullOrWhiteSpace(gpu))
			{
				return string.Empty;
			}

			bool isNvidia = Gpu.CallIsNvidia(gpu);

			// Remove common prefixes and suffixes
			gpu = RemoveCommonGpuTerms(gpu);

			// Remove memory size specifications (e.g., "; 4GB")
			gpu = RegexMemorySize.Replace(gpu, string.Empty).Trim();

			// Extract the first numeric value for GPU series detection
			int.TryParse(RegexFirstDigit.Match(gpu).Value, out int primaryNumber);

			if (isNvidia)
			{
				gpu = NormalizeNvidiaGpuName(gpu, primaryNumber);
			}
			else
			{
				gpu = NormalizeAmdGpuName(gpu, primaryNumber);
			}

			return gpu.Replace("  ", " ").Trim();
		}

		/// <summary>
		/// Removes common GPU-related terms and symbols from the GPU name.
		/// </summary>
		private string RemoveCommonGpuTerms(string gpu)
		{
			return gpu.Replace("nvidia", string.Empty, StringComparison.OrdinalIgnoreCase)
				.Replace("amd", string.Empty, StringComparison.OrdinalIgnoreCase)
				.Replace("(r)", string.Empty, StringComparison.OrdinalIgnoreCase)
				.Replace("series", string.Empty, StringComparison.OrdinalIgnoreCase)
				.Replace("graphics card", string.Empty, StringComparison.OrdinalIgnoreCase)
				.Replace("graphics", string.Empty, StringComparison.OrdinalIgnoreCase)
				.Replace("®", string.Empty, StringComparison.OrdinalIgnoreCase)
				.Replace("(", string.Empty, StringComparison.OrdinalIgnoreCase)
				.Replace(")", string.Empty, StringComparison.OrdinalIgnoreCase)
				.Trim();
		}

		/// <summary>
		/// Normalizes NVIDIA GPU names to match benchmark database format.
		/// Handles GeForce series classification (GT, GTS, GTX, RTX) and TI variants.
		/// </summary>
		private string NormalizeNvidiaGpuName(string gpu, int primaryNumber)
		{
			string gpuLower = gpu.ToLower();

			// Ensure "geforce" prefix exists
			if (gpuLower.IndexOf("geforce", StringComparison.Ordinal) == -1)
			{
				gpu = "geforce " + gpu;
				gpuLower = gpu.ToLower();
			}

			// Handle explicit series designations (GTS, GTX, RTX)
			gpu = HandleExplicitNvidiaSeries(gpu, gpuLower);

			// Determine series based on model number for generic "GeForce XXX" names
			gpu = DetermineNvidiaSeriesByNumber(gpu, gpuLower, primaryNumber);

			// Normalize "ti" suffix spacing
			gpu = gpu.ToLower().Replace("ti", " ti");

			return gpu;
		}

		/// <summary>
		/// Handles NVIDIA GPUs with explicit series designations (GTS, GTX, RTX).
		/// </summary>
		private string HandleExplicitNvidiaSeries(string gpu, string gpuLower)
		{
			if (gpuLower.IndexOf("gts", StringComparison.Ordinal) > -1)
			{
				gpu = gpuLower.Replace("gts", string.Empty).Replace("geforce", "geforce gts").Trim();
			}
			else if (gpuLower.IndexOf("gtx", StringComparison.Ordinal) > -1)
			{
				gpu = gpuLower.Replace("gtx", string.Empty).Replace("geforce", "geforce gtx").Trim();
			}
			else if (gpuLower.IndexOf("rtx", StringComparison.Ordinal) > -1 && gpuLower.IndexOf("geforce", StringComparison.Ordinal) == -1)
			{
				gpu = gpuLower.Replace("rtx", string.Empty).Replace("geforce", "geforce rtx").Trim();
			}
			else if (RegexGtsPattern.IsMatch(gpu)
				&& gpuLower.IndexOf("gtx", StringComparison.Ordinal) == -1
				&& gpuLower.IndexOf("gts", StringComparison.Ordinal) == -1)
			{
				gpu = gpuLower.Replace("gt", string.Empty).Replace("geforce", "geforce gt").Trim();
			}

			return gpu;
		}

		/// <summary>
		/// Determines NVIDIA GPU series (GT, GTS, GTX, RTX) based on model number
		/// when no explicit series designation exists.
		/// </summary>
		private string DetermineNvidiaSeriesByNumber(string gpu, string gpuLower, int primaryNumber)
		{
			if (RegexGeforceDigits.IsMatch(gpu)
				&& gpu.IndexOf("rtx", StringComparison.OrdinalIgnoreCase) == -1
				&& gpu.IndexOf("gtx", StringComparison.OrdinalIgnoreCase) == -1
				&& gpu.IndexOf("gt", StringComparison.OrdinalIgnoreCase) == -1
				&& gpu.IndexOf("gts", StringComparison.OrdinalIgnoreCase) == -1)
			{
				// Check if mobile/APU variant with sufficient performance
				int.TryParse(primaryNumber.ToString().Substring(1), out int secondDigit);
				bool isMobileHighEnd = (!RegexMobileSuffix.IsMatch(gpu) || secondDigit >= 50);
				bool isApuHighEnd = (!RegexASuffix.IsMatch(gpu) || secondDigit >= 50);

				if (isMobileHighEnd && isApuHighEnd && primaryNumber != 510)
				{
					// Classify based on model number ranges
					if (primaryNumber == 150 || primaryNumber == 160 || primaryNumber == 240 ||
						primaryNumber == 250 || primaryNumber == 350 || primaryNumber == 360 || primaryNumber == 450)
					{
						gpu = gpuLower.Replace("geforce", "geforce gts");
					}
					else if (primaryNumber < 2000)
					{
						gpu = gpuLower.Replace("geforce", "geforce gtx");
					}
					else
					{
						gpu = gpuLower.Replace("geforce", "geforce rtx");
					}
				}
			}

			return gpu;
		}

		/// <summary>
		/// Normalizes AMD GPU names to match benchmark database format.
		/// </summary>
		private string NormalizeAmdGpuName(string gpu, int primaryNumber)
		{
			// Standardize HD series format (e.g., "HD7970" -> "hd 7970")
			gpu = RegexHdPattern.Replace(gpu, $"hd {primaryNumber}").Trim();
			return gpu;
		}

		#endregion

		#region CPU Properties & Methods

		/// <summary>
		/// Gets or sets the cached CPU benchmark data list.
		/// Data is loaded from cache if available, otherwise fetched from the web.
		/// </summary>
		internal List<BenchmarkData> CpuDataList
		{
			get
			{
				if (_cpuDataList == null)
				{
					_cpuDataList = LoadOrFetchBenchmarkData(
						CpuCacheFile,
						UrlCpu,
						"CpuDataList",
						"GetCpuDataFromCache",
						"GetCpuDataFromWeb"
					);
				}
				return _cpuDataList;
			}
			set => _cpuDataList = value;
		}

		/// <summary>
		/// Compares two CPUs and determines if the first CPU is better than or equal to the second.
		/// </summary>
		/// <param name="cpuPC">The CPU name from the user's PC.</param>
		/// <param name="cpuCompare">The CPU name to compare against.</param>
		/// <returns>True if cpuPC is better, false if worse, null if comparison failed.</returns>
		public bool? IsBetterCpu(string cpuPC, string cpuCompare)
		{
			return IsBetter(cpuPC, cpuCompare, isGpu: false);
		}

		/// <summary>
		/// Normalizes CPU names by removing brand suffixes and standardizing formats
		/// to improve fuzzy matching accuracy with benchmark database.
		/// </summary>
		/// <param name="cpu">Raw CPU name to prepare.</param>
		/// <returns>Normalized CPU name.</returns>
		private string PrepareCpuName(string cpu)
		{
			if (string.IsNullOrWhiteSpace(cpu))
			{
				return string.Empty;
			}

			bool isIntel = Cpu.CallIsIntel(cpu);

			// Remove common CPU-related terms and symbols
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

			// Standardize clock speed format
			cpu = RegexClockSpeed.Replace(cpu, "$1ghz");
			cpu = NormalizeClockSpeed(cpu);

			if (isIntel)
			{
				cpu = NormalizeIntelCpuName(cpu);
			}

			return cpu.Replace("  ", " ").Trim();
		}

		/// <summary>
		/// Normalizes clock speed notation to consistent format.
		/// Ensures proper decimal precision (e.g., "2.4" -> "2.40", "3" -> "3.00").
		/// </summary>
		private string NormalizeClockSpeed(string cpu)
		{
			string clock = RegexClockPrimary.Match(cpu).Value
				.Replace("g", string.Empty)
				.Replace("m", string.Empty)
				.Trim();

			if (string.IsNullOrEmpty(clock))
			{
				clock = RegexClockSecondary.Match(cpu).Value
					.Replace("g", string.Empty)
					.Replace("m", string.Empty)
					.Trim();
			}

			// Pad clock speed to 2 decimal places for consistency
			if (clock.Length == 3) // e.g., "2.4" -> "2.40"
			{
				cpu = cpu.Replace(clock, clock + "0");
			}
			else if (clock.Length == 1) // e.g., "3" -> "3.00"
			{
				cpu = cpu.Replace(" " + clock, " " + clock + ".00");
			}

			return cpu;
		}

		/// <summary>
		/// Normalizes Intel CPU names to match benchmark database format.
		/// Handles Core i-series and Xeon processors with proper prefixing and formatting.
		/// </summary>
		private string NormalizeIntelCpuName(string cpu)
		{
			// Standardize "Intel i" to "Intel Core i"
			if (cpu.IndexOf("intel i", StringComparison.Ordinal) > -1)
			{
				cpu = cpu.Replace("intel i", "intel core i");
			}

			// Ensure "Intel Core" prefix for Core series
			if (cpu.IndexOf("core i", StringComparison.Ordinal) > -1 && cpu.IndexOf("intel core", StringComparison.Ordinal) == -1)
			{
				cpu = cpu.Replace("core i", "intel core i");
			}

			// Add "Intel Core" prefix if neither Core nor Xeon designation exists
			if (cpu.IndexOf("intel core", StringComparison.Ordinal) == -1 && cpu.IndexOf("intel xeon", StringComparison.Ordinal) == -1)
			{
				cpu = "intel core " + cpu;
			}

			// Standardize Core i-series format (e.g., "i7 9700" -> "i7-9700")
			if (RegexIntelCore.IsMatch(cpu))
			{
				cpu = RegexIntelCoreReplace.Replace(cpu, "$1-");
			}

			// Ensure "@" prefix for clock speeds
			if (RegexIntelClock.IsMatch(cpu))
			{
				cpu = RegexIntelClockReplace.Replace(cpu, " @$1");
			}

			return cpu;
		}

		#endregion

		#region Common Methods

		/// <summary>
		/// Loads benchmark data from cache or fetches from web if cache is expired or unavailable.
		/// Implements a unified caching strategy for both CPU and GPU data.
		/// </summary>
		/// <param name="cacheFileName">Name of the cache file.</param>
		/// <param name="sourceUrl">URL to fetch data from if cache is unavailable.</param>
		/// <param name="dataTypeName">Name of the data type for logging purposes.</param>
		/// <param name="cacheLogMessage">Log message for cache hit.</param>
		/// <param name="webLogMessage">Log message for web fetch.</param>
		/// <returns>List of benchmark data or empty list if fetch failed.</returns>
		private List<BenchmarkData> LoadOrFetchBenchmarkData(
			string cacheFileName,
			string sourceUrl,
			string dataTypeName,
			string cacheLogMessage,
			string webLogMessage)
		{
			string cacheFilePath = Path.Combine(PluginDatabase.Paths.PluginUserDataPath, cacheFileName);
			List<BenchmarkData> data = FileDataTools.LoadData<List<BenchmarkData>>(cacheFilePath, CacheExpirationMinutes);

			if (data?.Count > 0)
			{
				Common.LogDebug(true, cacheLogMessage);
				return data;
			}

			Common.LogDebug(true, webLogMessage);
			List<BenchmarkData> fetchedData = GetData(sourceUrl);

			if (fetchedData?.Count > 0)
			{
				FileDataTools.SaveData(cacheFilePath, fetchedData);
				return fetchedData;
			}

			Logger.Warn($"{dataTypeName} is empty");
			return new List<BenchmarkData>();
		}

		/// <summary>
		/// Scrapes benchmark data from the specified URL.
		/// Parses HTML table containing component names, benchmark marks, and rankings.
		/// </summary>
		/// <param name="url">URL of the benchmark page to scrape.</param>
		/// <returns>List of parsed benchmark data.</returns>
		private List<BenchmarkData> GetData(string url)
		{
			List<BenchmarkData> benchmarkDatas = new List<BenchmarkData>();

			try
			{
				string webData = Web.DownloadStringData(url).GetAwaiter().GetResult();
				HtmlParser parser = new HtmlParser();
				IHtmlDocument htmlDocument = parser.Parse(webData);

				foreach (IElement row in htmlDocument.QuerySelectorAll("table#cputable tbody tr"))
				{
					BenchmarkData benchmarkData = ParseBenchmarkRow(row);
					if (benchmarkData != null)
					{
						benchmarkDatas.Add(benchmarkData);
					}
				}
			}
			catch (Exception ex)
			{
				Common.LogError(ex, false, $"Failed to download {url}", true, PluginDatabase.PluginName);
			}

			return benchmarkDatas;
		}

		/// <summary>
		/// Parses a single HTML table row into a BenchmarkData object.
		/// </summary>
		/// <param name="row">HTML table row element.</param>
		/// <returns>Parsed BenchmarkData or null if parsing failed.</returns>
		private BenchmarkData ParseBenchmarkRow(IElement row)
		{
			BenchmarkData benchmarkData = new BenchmarkData
			{
				Id = row.GetAttribute("id")
			};

			int columnIndex = 0;
			foreach (IElement cell in row.QuerySelectorAll("td"))
			{
				switch (columnIndex)
				{
					case 0: // Name column
						IElement anchor = cell.QuerySelector("a");
						if (anchor != null)
						{
							benchmarkData.Name = anchor.InnerHtml;
						}
						break;

					case 1: // Mark column
						float.TryParse(cell.InnerHtml, out float mark);
						benchmarkData.Mark = mark;
						break;

					case 2: // Rank column
						int.TryParse(cell.InnerHtml, out int rank);
						benchmarkData.Rank = rank;
						break;
				}
				columnIndex++;
			}

			return benchmarkData;
		}

		/// <summary>
		/// Core comparison logic for determining if one component is better than another.
		/// Uses fuzzy string matching to find components in the benchmark database,
		/// then compares their benchmark marks or ranks.
		/// </summary>
		/// <param name="pc">Component name from the user's PC.</param>
		/// <param name="compare">Component name to compare against.</param>
		/// <param name="isGpu">True for GPU comparison, false for CPU comparison.</param>
		/// <returns>True if pc is better, false if worse, null if comparison failed.</returns>
		private bool? IsBetter(string pc, string compare, bool isGpu)
		{
			try
			{
				// Get appropriate benchmark data and normalize names
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

				// Find best matches using fuzzy string matching
				BenchmarkData foundPc = FindBestMatch(pc, benchmarkData);
				BenchmarkData foundCompare = FindBestMatch(compare, benchmarkData);

				// Log matched components for debugging
				Common.LogDebug(true, $"Benchmark - {pc} - {foundPc?.Name ?? string.Empty}");
				Common.LogDebug(true, $"Benchmark - {compare} - {foundCompare?.Name ?? string.Empty}");

				// Return null if either component was not found
				if (foundPc == null || foundCompare == null)
				{
					return null;
				}

				// Compare using Mark if available, otherwise use Rank (lower rank is better)
				return foundPc.Mark == 0 || foundCompare.Mark == 0
					? foundPc.Rank <= foundCompare.Rank
					: foundPc.Mark >= foundCompare.Mark;
			}
			catch (Exception ex)
			{
				Common.LogError(ex, false, PluginDatabase.PluginName);
				return null;
			}
		}

		/// <summary>
		/// Finds the best matching benchmark data for a given component name.
		/// Uses fuzzy string matching with a threshold, preferring exact matches when available.
		/// </summary>
		/// <param name="componentName">Normalized component name to search for.</param>
		/// <param name="benchmarkData">List of benchmark data to search through.</param>
		/// <returns>Best matching BenchmarkData or null if no match found above threshold.</returns>
		private BenchmarkData FindBestMatch(string componentName, List<BenchmarkData> benchmarkData)
		{
			if (string.IsNullOrWhiteSpace(componentName) || benchmarkData == null || benchmarkData.Count == 0)
			{
				return null;
			}

			// Calculate fuzzy match scores for all benchmark entries
			var fuzzyMatches = benchmarkData
				.Select(x => new
				{
					MatchPercent = Fuzz.PartialRatio(componentName.ToLower(), x.Name.ToLower()),
					Data = x
				})
				.OrderByDescending(x => x.MatchPercent)
				.ToList();

			// Check if best match meets threshold
			if (fuzzyMatches.Count == 0 || fuzzyMatches.First().MatchPercent < FuzzyMatchThreshold)
			{
				return null;
			}

			// Prefer exact case-insensitive match over fuzzy-first result
			var exactMatch = fuzzyMatches.FirstOrDefault(x => x.Data.Name.IsEqual(componentName));
			return exactMatch != null ? exactMatch.Data : fuzzyMatches.First().Data;
		}

		#endregion
	}
}