using Playnite.SDK;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SystemChecker.Models;
using CommonPluginsShared;
using CommonPluginsShared.Caching;
using CommonPluginsShared.IO;
using Playnite.SDK.Models;
using CommonPluginsStores.Models;
using CommonPluginsShared.SystemInfo;

namespace SystemChecker.Services
{
	/// <summary>
	/// Provides system requirement checking functionality with hardware comparison.
	/// Implements automatic persistent caching with per-entry expiration for static hardware checks (OS, CPU, RAM, GPU).
	/// Storage checks are never cached as disk space varies dynamically.
	/// 
	/// Cache is automatically initialized on first use and saved on every new entry.
	/// No manual initialization required - completely autonomous system.
	/// </summary>
	public class SystemApi
	{
		private static readonly ILogger Logger = LogManager.GetLogger();
		private static readonly SystemCheckerDatabase PluginDatabase = SystemChecker.PluginDatabase;
		private static Game GameContext;

		// Compiled regex for better performance
		private static readonly Regex NumberExtractor = new Regex(@"\d+", RegexOptions.Compiled);

		// Legacy OS versions that automatically pass requirements (obsolete)
		private static readonly HashSet<string> OldOsList = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			"95", "98", "XP", "Millenium", "ME", "2000", "Vista"
		};

		#region Cache Infrastructure

		/// <summary>
		/// Represents a single cached check result with metadata.
		/// Stores computation result, creation date, and component identifiers for debugging.
		/// </summary>
		private class CachedCheckResult
		{
			/// <summary>Gets or sets the hardware compatibility check result.</summary>
			public bool Result { get; set; }

			/// <summary>Gets or sets the cache entry creation timestamp.</summary>
			public DateTime DateCreated { get; set; }

			/// <summary>Gets or sets the PC component name (e.g., CPU model, GPU name).</summary>
			public string ComponentPcName { get; set; }

			/// <summary>Gets or sets the requirement specification string.</summary>
			public string ComponentRequirementName { get; set; }

			/// <summary>Gets or sets the check type identifier (OS, CPU, RAM, GPU).</summary>
			public string CheckType { get; set; }

			/// <summary>Initializes a new cache result with current timestamp.</summary>
			public CachedCheckResult()
			{
				DateCreated = DateTime.Now;
			}

			/// <summary>
			/// Initializes a new cache result with full metadata.
			/// </summary>
			/// <param name="result">Hardware compatibility result</param>
			/// <param name="componentPc">PC component identifier</param>
			/// <param name="componentRequirement">Requirement specification</param>
			/// <param name="checkType">Check type (OS/CPU/RAM/GPU)</param>
			public CachedCheckResult(bool result, string componentPc, string componentRequirement, string checkType)
			{
				Result = result;
				ComponentPcName = componentPc;
				ComponentRequirementName = componentRequirement;
				CheckType = checkType;
				DateCreated = DateTime.Now;
			}

			/// <summary>
			/// Checks if this cache entry has expired based on configured TTL.
			/// </summary>
			/// <param name="expirationDays">Maximum age in days before expiration</param>
			/// <returns>True if expired, false if still valid</returns>
			public bool IsExpired(int expirationDays)
			{
				return (DateTime.Now - DateCreated).TotalDays > expirationDays;
			}
		}

		/// <summary>
		/// Serializable container for all hardware check caches.
		/// Includes system fingerprint for hardware change detection.
		/// </summary>
		private class HardwareCacheData
		{
			public Dictionary<string, CachedCheckResult> OsCache { get; set; } = new Dictionary<string, CachedCheckResult>();
			public Dictionary<string, CachedCheckResult> CpuCache { get; set; } = new Dictionary<string, CachedCheckResult>();
			public Dictionary<string, CachedCheckResult> RamCache { get; set; } = new Dictionary<string, CachedCheckResult>();
			public Dictionary<string, CachedCheckResult> GpuCache { get; set; } = new Dictionary<string, CachedCheckResult>();

			/// <summary>
			/// Hardware configuration fingerprint for invalidation on hardware change.
			/// Format: MachineName_OS_CPU_GPU_RAM
			/// </summary>
			public string SystemFingerprint { get; set; }
		}

		// In-memory thread-safe caches with automatic TTL management
		private static readonly SmartCache<CachedCheckResult> _osCache = new SmartCache<CachedCheckResult>();
		private static readonly SmartCache<CachedCheckResult> _cpuCache = new SmartCache<CachedCheckResult>();
		private static readonly SmartCache<CachedCheckResult> _ramCache = new SmartCache<CachedCheckResult>();
		private static readonly SmartCache<CachedCheckResult> _gpuCache = new SmartCache<CachedCheckResult>();

		// Parallel tracking dictionaries for disk persistence (SmartCache doesn't expose internal data)
		private static readonly Dictionary<string, CachedCheckResult> _osCacheTracking = new Dictionary<string, CachedCheckResult>();
		private static readonly Dictionary<string, CachedCheckResult> _cpuCacheTracking = new Dictionary<string, CachedCheckResult>();
		private static readonly Dictionary<string, CachedCheckResult> _ramCacheTracking = new Dictionary<string, CachedCheckResult>();
		private static readonly Dictionary<string, CachedCheckResult> _gpuCacheTracking = new Dictionary<string, CachedCheckResult>();
		private static readonly object _trackingLock = new object();

		private static FileDataService _fileDataService;
		private static string _cacheFilePath;
		private static string _currentSystemFingerprint;
		private static int _cacheExpirationDays = 30;
		private static bool _cacheLoaded = false;
		private static bool _isSaving = false;
		private static readonly object _initializationLock = new object();

		/// <summary>
		/// Ensures cache system is initialized on first use (lazy initialization).
		/// Thread-safe with double-check locking pattern.
		/// Automatically registers cleanup on process exit.
		/// </summary>
		private static void EnsureInitialized()
		{
			if (_cacheLoaded)
			{
				return;
			}

			lock (_initializationLock)
			{
				if (_cacheLoaded)
				{
					return;
				}

				try
				{
					// Initialize services and paths
					_fileDataService = new FileDataService(PluginDatabase.PluginName, "SystemApi");
					_cacheFilePath = Path.Combine(PluginDatabase.Paths.PluginCachePath, "SystemCheck.json");
					_currentSystemFingerprint = GenerateSystemFingerprint();

					// Load existing cache from disk
					LoadCacheFromDisk();

					// Register automatic save on process exit
					AppDomain.CurrentDomain.ProcessExit += OnProcessExit;

					_cacheLoaded = true;
					Common.LogDebug(true, "SystemApi: Cache system initialized successfully");
				}
				catch (Exception ex)
				{
					Common.LogError(ex, false, "SystemApi: Failed to initialize cache system");
				}
			}
		}

		/// <summary>
		/// Event handler for process exit - ensures cache is persisted before shutdown.
		/// Performs synchronous save to guarantee data persistence.
		/// </summary>
		private static void OnProcessExit(object sender, EventArgs e)
		{
			try
			{
				if (_cacheLoaded)
				{
					// Force synchronous save on exit
					SaveCacheToDiskSync();
					Common.LogDebug(true, "SystemApi: Cache saved on process exit");
				}
			}
			catch (Exception ex)
			{
				Common.LogError(ex, false, "SystemApi: Error saving cache on exit");
			}
		}

		/// <summary>
		/// Generates a unique fingerprint based on current hardware configuration.
		/// Used to detect hardware changes and invalidate stale cache entries.
		/// </summary>
		/// <returns>Fingerprint string combining machine name, OS, CPU, GPU, and RAM</returns>
		private static string GenerateSystemFingerprint()
		{
			try
			{
				SystemConfiguration config = PluginDatabase.Database.PC;
				if (config == null)
				{
					return "unknown";
				}

				// Combine key hardware identifiers
				return $"{config.Name}_{config.Os}_{config.Cpu}_{config.GpuName}_{config.RamUsage}";
			}
			catch
			{
				return "unknown";
			}
		}

		/// <summary>
		/// Loads persistent cache from disk if available.
		/// Automatically validates hardware fingerprint and filters expired entries.
		/// Silent failure on errors - starts with empty cache if load fails.
		/// </summary>
		private static void LoadCacheFromDisk()
		{
			if (!File.Exists(_cacheFilePath))
			{
				Common.LogDebug(true, "SystemApi: No cache file found, starting with empty cache");
				return;
			}

			HardwareCacheData cacheData = _fileDataService.LoadData<HardwareCacheData>(_cacheFilePath, -1);

			if (cacheData == null)
			{
				Common.LogDebug(true, "SystemApi: Failed to deserialize cache file");
				return;
			}

			// Invalidate entire cache if hardware configuration changed
			if (cacheData.SystemFingerprint != _currentSystemFingerprint)
			{
				Common.LogDebug(true, $"SystemApi: Hardware changed, cache invalidated (old: {cacheData.SystemFingerprint})");
				return;
			}

			// Load valid entries with remaining TTL
			int totalLoaded = 0;
			int expiredCount = 0;

			totalLoaded += LoadCacheDictionary(cacheData.OsCache, _osCache, _osCacheTracking, ref expiredCount);
			totalLoaded += LoadCacheDictionary(cacheData.CpuCache, _cpuCache, _cpuCacheTracking, ref expiredCount);
			totalLoaded += LoadCacheDictionary(cacheData.RamCache, _ramCache, _ramCacheTracking, ref expiredCount);
			totalLoaded += LoadCacheDictionary(cacheData.GpuCache, _gpuCache, _gpuCacheTracking, ref expiredCount);

			Common.LogDebug(true, $"SystemApi: Loaded {totalLoaded} cache entries ({expiredCount} expired entries removed)");
		}

		/// <summary>
		/// Helper method to load and filter a single cache dictionary.
		/// Calculates remaining TTL for each entry and populates both SmartCache and tracking dictionary.
		/// </summary>
		/// <param name="source">Source dictionary from disk</param>
		/// <param name="smartCache">Target SmartCache instance</param>
		/// <param name="tracking">Parallel tracking dictionary for persistence</param>
		/// <param name="expiredCount">Counter for expired entries (incremented by reference)</param>
		/// <returns>Number of entries successfully loaded</returns>
		private static int LoadCacheDictionary(Dictionary<string, CachedCheckResult> source,
											   SmartCache<CachedCheckResult> smartCache,
											   Dictionary<string, CachedCheckResult> tracking,
											   ref int expiredCount)
		{
			int loaded = 0;
			foreach (KeyValuePair<string, CachedCheckResult> kvp in source)
			{
				// Skip expired entries
				if (kvp.Value.IsExpired(_cacheExpirationDays))
				{
					expiredCount++;
					continue;
				}

				// Calculate remaining TTL from creation date
				TimeSpan remainingTtl = kvp.Value.DateCreated.AddDays(_cacheExpirationDays) - DateTime.Now;
				if (remainingTtl.TotalSeconds > 0)
				{
					smartCache.Set(kvp.Key, kvp.Value, remainingTtl);
					tracking[kvp.Key] = kvp.Value;
					loaded++;
				}
			}
			return loaded;
		}

		/// <summary>
		/// Saves current cache state to disk asynchronously (fire-and-forget).
		/// Prevents concurrent saves using a flag.
		/// Called automatically on every new cache entry.
		/// </summary>
		private static void SaveCacheToDisk()
		{
			if (!_cacheLoaded || string.IsNullOrEmpty(_cacheFilePath) || _fileDataService == null)
			{
				return;
			}

			// Prevent concurrent saves
			if (_isSaving)
			{
				return;
			}

			Task.Run(() => SaveCacheToDiskAsync());
		}

		/// <summary>
		/// Synchronous save for process exit handler.
		/// Ensures data is written before application terminates.
		/// </summary>
		private static void SaveCacheToDiskSync()
		{
			if (!_cacheLoaded || string.IsNullOrEmpty(_cacheFilePath) || _fileDataService == null)
			{
				return;
			}

			if (_isSaving)
			{
				return;
			}

			_isSaving = true;

			try
			{
				HardwareCacheData cacheData;

				lock (_trackingLock)
				{
					cacheData = new HardwareCacheData
					{
						OsCache = new Dictionary<string, CachedCheckResult>(_osCacheTracking),
						CpuCache = new Dictionary<string, CachedCheckResult>(_cpuCacheTracking),
						RamCache = new Dictionary<string, CachedCheckResult>(_ramCacheTracking),
						GpuCache = new Dictionary<string, CachedCheckResult>(_gpuCacheTracking),
						SystemFingerprint = _currentSystemFingerprint
					};
				}

				_fileDataService.SaveData(_cacheFilePath, cacheData);

				int totalEntries = cacheData.OsCache.Count + cacheData.CpuCache.Count +
								  cacheData.RamCache.Count + cacheData.GpuCache.Count;
				Common.LogDebug(true, $"SystemApi: Cache saved to disk ({totalEntries} entries)");
			}
			catch (Exception ex)
			{
				Common.LogError(ex, false, "SystemApi: Error saving cache to disk");
			}
			finally
			{
				_isSaving = false;
			}
		}

		/// <summary>
		/// Internal async save implementation.
		/// Snapshots tracking dictionaries under lock, then performs I/O outside lock.
		/// </summary>
		private static async Task SaveCacheToDiskAsync()
		{
			if (_isSaving)
			{
				return;
			}

			_isSaving = true;

			try
			{
				HardwareCacheData cacheData;

				// Snapshot under lock to minimize lock duration
				lock (_trackingLock)
				{
					cacheData = new HardwareCacheData
					{
						OsCache = new Dictionary<string, CachedCheckResult>(_osCacheTracking),
						CpuCache = new Dictionary<string, CachedCheckResult>(_cpuCacheTracking),
						RamCache = new Dictionary<string, CachedCheckResult>(_ramCacheTracking),
						GpuCache = new Dictionary<string, CachedCheckResult>(_gpuCacheTracking),
						SystemFingerprint = _currentSystemFingerprint
					};
				}

				// Perform I/O outside lock
				await _fileDataService.SaveDataAsync(_cacheFilePath, cacheData);

				int totalEntries = cacheData.OsCache.Count + cacheData.CpuCache.Count +
								  cacheData.RamCache.Count + cacheData.GpuCache.Count;
				Common.LogDebug(true, $"SystemApi: Cache saved to disk ({totalEntries} entries)");
			}
			catch (Exception ex)
			{
				Common.LogError(ex, false, "SystemApi: Error saving cache to disk");
			}
			finally
			{
				_isSaving = false;
			}
		}

		/// <summary>
		/// Builds a normalized cache key for OS comparison.
		/// Extracts Windows version number and sorts requirement list alphabetically.
		/// This normalization maximizes cache hits by treating equivalent requirements identically.
		/// 
		/// Example: "Win 7|8|10" and "Win 10|7|8" produce the same key.
		/// </summary>
		/// <param name="systemOs">System OS string (e.g., "Microsoft Windows 11 Famille")</param>
		/// <param name="requirementOs">List of required OS versions</param>
		/// <returns>Normalized cache key</returns>
		private static string BuildOsCacheKey(string systemOs, List<string> requirementOs)
		{
			// Extract numeric version (e.g., "11" from "Windows 11")
			string systemVersion = "unknown";
			Match match = NumberExtractor.Match(systemOs ?? "");
			if (match.Success)
			{
				systemVersion = match.Value;
			}

			// Normalize and sort requirements to avoid order-dependent duplicates
			List<string> sortedRequirements = (requirementOs ?? new List<string>())
				.Select(r => r.Trim().ToLowerInvariant())
				.OrderBy(r => r)
				.ToList();

			string requirementKey = string.Join("|", sortedRequirements);
			return $"{systemVersion}_{requirementKey}";
		}

		/// <summary>
		/// Builds a normalized cache key for CPU comparison.
		/// Sorts requirement list to eliminate order-dependent cache misses.
		/// </summary>
		private static string BuildCpuCacheKey(string systemCpu, List<string> requirementCpu)
		{
			string normalizedCpu = (systemCpu ?? "").Trim();

			List<string> sortedRequirements = (requirementCpu ?? new List<string>())
				.Select(r => r.Trim())
				.OrderBy(r => r)
				.ToList();

			string cpuKey = string.Join("|", sortedRequirements);
			return $"{normalizedCpu}_{cpuKey}";
		}

		/// <summary>
		/// Builds a cache key for RAM comparison.
		/// Format: SystemRAM_SystemUnit_RequiredRAM_RequiredUnit
		/// </summary>
		private static string BuildRamCacheKey(long systemRam, string systemRamUsage,
												double requirementRam, string requirementRamUsage)
		{
			return $"{systemRam}_{systemRamUsage}_{requirementRam}_{requirementRamUsage}";
		}

		/// <summary>
		/// Builds a normalized cache key for GPU comparison.
		/// Sorts requirement list to eliminate order-dependent cache misses.
		/// </summary>
		private static string BuildGpuCacheKey(string systemGpuName, List<string> requirementGpu)
		{
			string normalizedGpu = (systemGpuName ?? "").Trim();

			List<string> sortedRequirements = (requirementGpu ?? new List<string>())
				.Select(r => r.Trim())
				.OrderBy(r => r)
				.ToList();

			string gpuKey = string.Join("|", sortedRequirements);
			return $"{normalizedGpu}_{gpuKey}";
		}

		/// <summary>
		/// Clears all hardware check caches in memory and on disk.
		/// Should be called when PC configuration is updated (hardware upgrade, OS reinstall).
		/// Automatically regenerates system fingerprint.
		/// </summary>
		public static void ClearHardwareCache()
		{
			EnsureInitialized();

			_osCache.Clear();
			_cpuCache.Clear();
			_ramCache.Clear();
			_gpuCache.Clear();

			lock (_trackingLock)
			{
				_osCacheTracking.Clear();
				_cpuCacheTracking.Clear();
				_ramCacheTracking.Clear();
				_gpuCacheTracking.Clear();
			}

			_currentSystemFingerprint = GenerateSystemFingerprint();
			SaveCacheToDisk();

			Common.LogDebug(true, "SystemApi: Hardware check cache cleared");
		}

		#endregion

		#region Public API

		/// <summary>
		/// Checks if the system configuration meets game requirements.
		/// 
		/// Uses automatic persistent caching for expensive checks (OS, CPU, RAM, GPU).
		/// Storage check is never cached - always reflects current disk space.
		/// 
		/// Cache is automatically initialized on first call and saved on every new entry.
		/// No manual setup required.
		/// </summary>
		/// <param name="game">Game to check requirements for</param>
		/// <param name="requirementEntry">Minimum or recommended requirements</param>
		/// <param name="systemConfiguration">Current PC configuration</param>
		/// <param name="IsInstalled">True if game is already installed (skips storage check)</param>
		/// <returns>CheckSystem object with individual component results and overall compatibility</returns>
		public static CheckSystem CheckConfig(Game game, RequirementEntry requirementEntry,
											 SystemConfiguration systemConfiguration, bool IsInstalled)
		{
			// Automatic lazy initialization on first use
			EnsureInitialized();

			GameContext = game;
			Common.LogDebug(true, $"CheckConfig() for {game.Name}");

			if (requirementEntry == null || systemConfiguration == null)
			{
				Common.LogDebug(true, "CheckConfig() with null requirement and/or systemConfiguration");
				return new CheckSystem();
			}

			// Perform cached checks for static hardware components
			bool isCheckOs = CheckOSCached(systemConfiguration.Os, requirementEntry.Os);
			bool isCheckCpu = CheckCpuCached(systemConfiguration, requirementEntry.Cpu);
			bool isCheckRam = CheckRamCached(systemConfiguration.Ram, systemConfiguration.RamUsage,
											requirementEntry.Ram, requirementEntry.RamUsage);
			bool isCheckGpu = CheckGpuCached(systemConfiguration, requirementEntry.Gpu);

			// Storage always recalculated - disk space varies dynamically
			bool isCheckStorage = IsInstalled || CheckStorage(systemConfiguration.Disks, requirementEntry.Storage);

			return new CheckSystem
			{
				CheckOs = isCheckOs,
				CheckCpu = isCheckCpu,
				CheckRam = isCheckRam,
				CheckGpu = isCheckGpu,
				CheckStorage = isCheckStorage,
				AllOk = isCheckOs && isCheckCpu && isCheckRam && isCheckGpu && isCheckStorage
			};
		}

		#endregion

		#region Cached Check Methods

		/// <summary>
		/// Cached wrapper for OS compatibility check.
		/// On cache miss: computes result, stores in cache, saves to disk asynchronously.
		/// On cache hit: returns stored result immediately.
		/// </summary>
		private static bool CheckOSCached(string systemOs, List<string> requirementOs)
		{
			string cacheKey = BuildOsCacheKey(systemOs, requirementOs);
			TimeSpan ttl = TimeSpan.FromDays(_cacheExpirationDays);
			bool isNewEntry = false;

			CachedCheckResult cached = _osCache.GetOrSet(cacheKey, () =>
			{
				isNewEntry = true;
				bool result = CheckOS(systemOs, requirementOs);
				string requirementKey = string.Join("|", requirementOs ?? new List<string>());
				CachedCheckResult newEntry = new CachedCheckResult(result, systemOs, requirementKey, "OS");

				lock (_trackingLock)
				{
					_osCacheTracking[cacheKey] = newEntry;
				}

				return newEntry;
			}, ttl);

			// Automatic save on new entry
			if (isNewEntry)
			{
				SaveCacheToDisk();
			}

			return cached.Result;
		}

		/// <summary>
		/// Cached wrapper for CPU compatibility check.
		/// Delegates to HardwareChecker which has its own benchmark cache.
		/// </summary>
		private static bool CheckCpuCached(SystemConfiguration systemConfiguration, List<string> requirementCpu)
		{
			string cacheKey = BuildCpuCacheKey(systemConfiguration.Cpu, requirementCpu);
			TimeSpan ttl = TimeSpan.FromDays(_cacheExpirationDays);
			bool isNewEntry = false;

			CachedCheckResult cached = _cpuCache.GetOrSet(cacheKey, () =>
			{
				isNewEntry = true;
				bool result = CheckCpu(systemConfiguration, requirementCpu);
				string requirementKey = string.Join("|", requirementCpu ?? new List<string>());
				CachedCheckResult newEntry = new CachedCheckResult(result, systemConfiguration.Cpu, requirementKey, "CPU");

				lock (_trackingLock)
				{
					_cpuCacheTracking[cacheKey] = newEntry;
				}

				return newEntry;
			}, ttl);

			// Automatic save on new entry
			if (isNewEntry)
			{
				SaveCacheToDisk();
			}

			return cached.Result;
		}

		/// <summary>
		/// Cached wrapper for RAM compatibility check.
		/// Simple comparison - no expensive computation involved.
		/// </summary>
		private static bool CheckRamCached(long systemRam, string systemRamUsage,
										  double requirementRam, string requirementRamUsage)
		{
			string cacheKey = BuildRamCacheKey(systemRam, systemRamUsage, requirementRam, requirementRamUsage);
			TimeSpan ttl = TimeSpan.FromDays(_cacheExpirationDays);
			bool isNewEntry = false;

			CachedCheckResult cached = _ramCache.GetOrSet(cacheKey, () =>
			{
				isNewEntry = true;
				bool result = CheckRam(systemRam, systemRamUsage, requirementRam, requirementRamUsage);
				CachedCheckResult newEntry = new CachedCheckResult(result,
					$"{systemRam} {systemRamUsage}",
					$"{requirementRam} {requirementRamUsage}", "RAM");

				lock (_trackingLock)
				{
					_ramCacheTracking[cacheKey] = newEntry;
				}

				return newEntry;
			}, ttl);

			// Automatic save on new entry
			if (isNewEntry)
			{
				SaveCacheToDisk();
			}

			return cached.Result;
		}

		/// <summary>
		/// Cached wrapper for GPU compatibility check.
		/// Delegates to HardwareChecker which has its own benchmark cache.
		/// </summary>
		private static bool CheckGpuCached(SystemConfiguration systemConfiguration, List<string> requirementGpu)
		{
			string cacheKey = BuildGpuCacheKey(systemConfiguration.GpuName, requirementGpu);
			TimeSpan ttl = TimeSpan.FromDays(_cacheExpirationDays);
			bool isNewEntry = false;

			CachedCheckResult cached = _gpuCache.GetOrSet(cacheKey, () =>
			{
				isNewEntry = true;
				bool result = CheckGpu(systemConfiguration, requirementGpu);
				string requirementKey = string.Join("|", requirementGpu ?? new List<string>());
				CachedCheckResult newEntry = new CachedCheckResult(result, systemConfiguration.GpuName, requirementKey, "GPU");

				lock (_trackingLock)
				{
					_gpuCacheTracking[cacheKey] = newEntry;
				}

				return newEntry;
			}, ttl);

			// Automatic save on new entry
			if (isNewEntry)
			{
				SaveCacheToDisk();
			}

			return cached.Result;
		}

		#endregion

		#region Core Check Logic

		/// <summary>
		/// Checks if system OS meets requirement OS list.
		/// Supports:
		/// - Direct string matching (case-insensitive)
		/// - Legacy OS auto-pass (95, 98, XP, Vista)
		/// - Numeric version comparison (Windows 11 >= Windows 10)
		/// </summary>
		private static bool CheckOS(string systemOs, List<string> requirementOs)
		{
			if (requirementOs.Count == 0)
			{
				return true;
			}

			try
			{
				int numberOsPc = 0;
				bool systemOsParsed = false;

				foreach (string os in requirementOs)
				{
					// Direct substring match (case-insensitive)
					if (systemOs.IndexOf(os, StringComparison.OrdinalIgnoreCase) >= 0)
					{
						return true;
					}

					// Auto-pass for obsolete OS requirements
					if (OldOsList.Any(oldOs => os.IndexOf(oldOs, StringComparison.OrdinalIgnoreCase) >= 0))
					{
						return true;
					}

					// Extract system OS version number once
					if (!systemOsParsed)
					{
						Match systemMatch = NumberExtractor.Match(systemOs);
						if (systemMatch.Success)
						{
							int.TryParse(systemMatch.Value, out numberOsPc);
						}
						systemOsParsed = true;
					}

					// Numeric version comparison (e.g., 11 >= 10)
					if (numberOsPc > 0)
					{
						Match requirementMatch = NumberExtractor.Match(os);
						if (requirementMatch.Success && int.TryParse(requirementMatch.Value, out int numberOsRequirement))
						{
							if (numberOsPc >= numberOsRequirement)
							{
								return true;
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				LogError(ex, "CheckOS");
			}

			return false;
		}

		/// <summary>
		/// Checks if system CPU meets any requirement CPU.
		/// Uses Cpu class with integrated benchmark comparison and caching.
		/// Returns true if any requirement is met (OR logic).
		/// </summary>
		private static bool CheckCpu(SystemConfiguration systemConfiguration, List<string> requirementCpu)
		{
			if (requirementCpu.Count == 0)
			{
				return true;
			}

			try
			{
				foreach (string cpu in requirementCpu)
				{
					Cpu cpuCheck = new Cpu(systemConfiguration, cpu);
					CheckResult check = cpuCheck.IsBetter();

					if (check.SameConstructor || check.Result)
					{
						return check.Result;
					}
				}
			}
			catch (Exception ex)
			{
				LogError(ex, "CheckCpu");
			}

			return false;
		}

		/// <summary>
		/// Checks if system RAM meets requirement.
		/// Compares amount and unit (GB vs MB).
		/// Returns true if same unit OR system RAM >= required RAM.
		/// </summary>
		private static bool CheckRam(long systemRam, string systemRamUsage,
									double requirementRam, string requirementRamUsage)
		{
			try
			{
				return systemRamUsage == requirementRamUsage || systemRam >= requirementRam;
			}
			catch (Exception ex)
			{
				LogError(ex, "CheckRam");
				return false;
			}
		}

		/// <summary>
		/// Checks if system GPU meets any requirement GPU.
		/// Uses Gpu class with integrated benchmark comparison and caching.
		/// Implements special logic for obsolete GPU requirements and integrated cards.
		/// </summary>
		private static bool CheckGpu(SystemConfiguration systemConfiguration, List<string> requirementGpu)
		{
			if (requirementGpu.Count == 0)
			{
				return true;
			}

			try
			{
				for (int i = 0; i < requirementGpu.Count; i++)
				{
					Gpu gpuCheck = new Gpu(systemConfiguration, requirementGpu[i]);
					CheckResult check = gpuCheck.IsBetter();

					if (check.Result)
					{
						// Pass conditions: same brand OR old requirement OR first GPU in list
						return check.SameConstructor || (!gpuCheck.IsWithNoCard && gpuCheck.CardRequirementIsOld) || i == 0;
					}

					if (check.SameConstructor)
					{
						return false;
					}
				}
			}
			catch (Exception ex)
			{
				LogError(ex, "CheckGpu");
			}

			return false;
		}

		/// <summary>
		/// Checks if any system disk has sufficient free space.
		/// NEVER CACHED - disk space varies dynamically.
		/// Returns true immediately if any disk meets requirement.
		/// </summary>
		private static bool CheckStorage(List<SystemDisk> systemDisks, double storage)
		{
			if (storage == 0)
			{
				return true;
			}

			try
			{
				foreach (SystemDisk disk in systemDisks)
				{
					if (disk.FreeSpace >= storage)
					{
						return true;
					}
				}
			}
			catch (Exception ex)
			{
				LogError(ex, "CheckStorage");
			}

			return false;
		}

		#endregion

		#region Error Handling

		/// <summary>
		/// Logs errors with user-friendly message suggesting data refresh.
		/// Uses localized string resources when available.
		/// </summary>
		private static void LogError(Exception ex, string methodName)
		{
			string message = string.Format(ResourceProvider.GetString("LOCSystemCheckerTryRefresh"), GameContext?.Name);
			Common.LogError(ex, false, message, true, PluginDatabase.PluginName, message);
		}

		#endregion
	}
}
