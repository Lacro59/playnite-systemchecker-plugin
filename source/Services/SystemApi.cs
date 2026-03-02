using Playnite.SDK;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
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
	/// Cache saves are debounced — rapid successive calls (e.g. list scroll) produce a single disk write.
	/// </summary>
	public class SystemApi
	{
		private static readonly ILogger Logger = LogManager.GetLogger();
		private static readonly SystemCheckerDatabase PluginDatabase = SystemChecker.PluginDatabase;
		private static Game GameContext;

		private static readonly Regex NumberExtractor = new Regex(@"\d+", RegexOptions.Compiled);

		private static readonly HashSet<string> OldOsList = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			"95", "98", "XP", "Millenium", "ME", "2000", "Vista"
		};

		#region Cache Infrastructure

		/// <summary>
		/// Represents a single cached check result with metadata.
		/// </summary>
		private class CachedCheckResult
		{
			public bool Result { get; set; }
			public DateTime DateCreated { get; set; }
			public string ComponentPcName { get; set; }
			public string ComponentRequirementName { get; set; }
			public string CheckType { get; set; }

			public CachedCheckResult()
			{
				DateCreated = DateTime.Now;
			}

			public CachedCheckResult(bool result, string componentPc, string componentRequirement, string checkType)
			{
				Result = result;
				ComponentPcName = componentPc;
				ComponentRequirementName = componentRequirement;
				CheckType = checkType;
				DateCreated = DateTime.Now;
			}

			public bool IsExpired(int expirationDays)
			{
				return (DateTime.Now - DateCreated).TotalDays > expirationDays;
			}
		}

		/// <summary>
		/// Serializable container for all hardware check caches.
		/// </summary>
		private class HardwareCacheData
		{
			public Dictionary<string, CachedCheckResult> OsCache { get; set; }
				= new Dictionary<string, CachedCheckResult>();
			public Dictionary<string, CachedCheckResult> CpuCache { get; set; }
				= new Dictionary<string, CachedCheckResult>();
			public Dictionary<string, CachedCheckResult> RamCache { get; set; }
				= new Dictionary<string, CachedCheckResult>();
			public Dictionary<string, CachedCheckResult> GpuCache { get; set; }
				= new Dictionary<string, CachedCheckResult>();

			/// <summary>Hardware fingerprint for invalidation on hardware change.</summary>
			public string SystemFingerprint { get; set; }
		}

		private static readonly SmartCache<CachedCheckResult> _osCache = new SmartCache<CachedCheckResult>();
		private static readonly SmartCache<CachedCheckResult> _cpuCache = new SmartCache<CachedCheckResult>();
		private static readonly SmartCache<CachedCheckResult> _ramCache = new SmartCache<CachedCheckResult>();
		private static readonly SmartCache<CachedCheckResult> _gpuCache = new SmartCache<CachedCheckResult>();

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
		private static volatile bool _isSaving = false;
		private static readonly object _initializationLock = new object();

		// ── Debounce ──────────────────────────────────────────────────────────────
		private static Timer _cacheSaveDebounceTimer;
		private static readonly object _cacheSaveTimerLock = new object();

		/// <summary>
		/// Milliseconds of inactivity after the last cache write before a disk save is triggered.
		/// Rapid successive calls (e.g. list scroll) reset the timer — only one save fires.
		/// </summary>
		private const int CacheSaveDebounceMs = 2000;

		#endregion

		#region Initialisation

		/// <summary>
		/// Ensures cache system is initialized on first use (lazy, double-check locked).
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
					_fileDataService = new FileDataService(PluginDatabase.PluginName, "SystemApi");
					_cacheFilePath = Path.Combine(PluginDatabase.Paths.PluginCachePath, "SystemCheck.json");
					_currentSystemFingerprint = GenerateSystemFingerprint();

					LoadCacheFromDisk();

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

		private static void OnProcessExit(object sender, EventArgs e)
		{
			try
			{
				// Cancel any pending debounce and flush immediately.
				lock (_cacheSaveTimerLock)
				{
					_cacheSaveDebounceTimer?.Dispose();
					_cacheSaveDebounceTimer = null;
				}

				if (_cacheLoaded)
				{
					SaveCacheToDiskSync();
					Common.LogDebug(true, "SystemApi: Cache saved on process exit");
				}
			}
			catch (Exception ex)
			{
				Common.LogError(ex, false, "SystemApi: Error saving cache on exit");
			}
		}

		private static string GenerateSystemFingerprint()
		{
			try
			{
				SystemConfiguration config = PluginDatabase.PC;
				if (config == null)
				{
					return "unknown";
				}

				return string.Format("{0}_{1}_{2}_{3}_{4}",
					config.Name, config.Os, config.Cpu, config.GpuName, config.RamUsage);
			}
			catch
			{
				return "unknown";
			}
		}

		#endregion

		#region Cache persistence

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

			if (cacheData.SystemFingerprint != _currentSystemFingerprint)
			{
				Common.LogDebug(true, string.Format(
					"SystemApi: Hardware changed, cache invalidated (old: {0})",
					cacheData.SystemFingerprint));
				return;
			}

			int totalLoaded = 0;
			int expiredCount = 0;

			totalLoaded += LoadCacheDictionary(cacheData.OsCache, _osCache, _osCacheTracking, ref expiredCount);
			totalLoaded += LoadCacheDictionary(cacheData.CpuCache, _cpuCache, _cpuCacheTracking, ref expiredCount);
			totalLoaded += LoadCacheDictionary(cacheData.RamCache, _ramCache, _ramCacheTracking, ref expiredCount);
			totalLoaded += LoadCacheDictionary(cacheData.GpuCache, _gpuCache, _gpuCacheTracking, ref expiredCount);

			Common.LogDebug(true, string.Format(
				"SystemApi: Loaded {0} cache entries ({1} expired entries removed)",
				totalLoaded, expiredCount));
		}

		private static int LoadCacheDictionary(
			Dictionary<string, CachedCheckResult> source,
			SmartCache<CachedCheckResult> smartCache,
			Dictionary<string, CachedCheckResult> tracking,
			ref int expiredCount)
		{
			int loaded = 0;
			foreach (KeyValuePair<string, CachedCheckResult> kvp in source)
			{
				if (kvp.Value.IsExpired(_cacheExpirationDays))
				{
					expiredCount++;
					continue;
				}

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
		/// Schedules a debounced async cache save.
		/// Resets the timer on every call — only fires after <see cref="CacheSaveDebounceMs"/>
		/// of inactivity, preventing a disk write per scrolled game.
		/// </summary>
		private static void ScheduleCacheSave()
		{
			if (!_cacheLoaded || string.IsNullOrEmpty(_cacheFilePath) || _fileDataService == null)
			{
				return;
			}

			lock (_cacheSaveTimerLock)
			{
				if (_cacheSaveDebounceTimer == null)
				{
					_cacheSaveDebounceTimer = new Timer(
						_ => Task.Run(async () => await SaveCacheToDiskAsync()),
						null,
						CacheSaveDebounceMs,
						Timeout.Infinite);
				}
				else
				{
					// Reset — extends the wait by CacheSaveDebounceMs from now.
					_cacheSaveDebounceTimer.Change(CacheSaveDebounceMs, Timeout.Infinite);
				}
			}
		}

		/// <summary>
		/// Synchronous save for process exit — guarantees flush before termination.
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
				HardwareCacheData cacheData = SnapshotCacheData();
				_fileDataService.SaveData(_cacheFilePath, cacheData);

				Common.LogDebug(true, string.Format(
					"SystemApi: Cache saved to disk (sync, {0} entries)",
					TotalEntries(cacheData)));
			}
			catch (Exception ex)
			{
				Common.LogError(ex, false, "SystemApi: Error saving cache to disk (sync)");
			}
			finally
			{
				_isSaving = false;
			}
		}

		/// <summary>
		/// Async save invoked by the debounce timer.
		/// Skips if a save is already in progress and reschedules to pick up latest state.
		/// </summary>
		private static async Task SaveCacheToDiskAsync()
		{
			if (_isSaving)
			{
				// Another save is already running — reschedule to capture latest entries.
				ScheduleCacheSave();
				return;
			}

			_isSaving = true;

			try
			{
				HardwareCacheData cacheData = SnapshotCacheData();
				await _fileDataService.SaveDataAsync(_cacheFilePath, cacheData);

				Common.LogDebug(true, string.Format(
					"SystemApi: Cache saved to disk ({0} entries)", TotalEntries(cacheData)));
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

		/// <summary>Snapshots all tracking dictionaries under lock for serialization.</summary>
		private static HardwareCacheData SnapshotCacheData()
		{
			lock (_trackingLock)
			{
				return new HardwareCacheData
				{
					OsCache = new Dictionary<string, CachedCheckResult>(_osCacheTracking),
					CpuCache = new Dictionary<string, CachedCheckResult>(_cpuCacheTracking),
					RamCache = new Dictionary<string, CachedCheckResult>(_ramCacheTracking),
					GpuCache = new Dictionary<string, CachedCheckResult>(_gpuCacheTracking),
					SystemFingerprint = _currentSystemFingerprint
				};
			}
		}

		private static int TotalEntries(HardwareCacheData data)
		{
			return data.OsCache.Count + data.CpuCache.Count
				 + data.RamCache.Count + data.GpuCache.Count;
		}

		/// <inheritdoc cref="ClearHardwareCache"/>
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
			ScheduleCacheSave();

			Common.LogDebug(true, "SystemApi: Hardware check cache cleared");
		}

		#endregion

		#region Cache key builders

		private static string BuildOsCacheKey(string systemOs, List<string> requirementOs)
		{
			string systemVersion = "unknown";
			Match match = NumberExtractor.Match(systemOs ?? "");
			if (match.Success)
			{
				systemVersion = match.Value;
			}

			List<string> sortedRequirements = (requirementOs ?? new List<string>())
				.Select(r => r.Trim().ToLowerInvariant())
				.OrderBy(r => r)
				.ToList();

			return string.Format("{0}_{1}", systemVersion, string.Join("|", sortedRequirements));
		}

		private static string BuildCpuCacheKey(string systemCpu, List<string> requirementCpu)
		{
			List<string> sortedRequirements = (requirementCpu ?? new List<string>())
				.Select(r => r.Trim())
				.OrderBy(r => r)
				.ToList();

			return string.Format("{0}_{1}",
				(systemCpu ?? "").Trim(), string.Join("|", sortedRequirements));
		}

		private static string BuildRamCacheKey(long systemRam, string systemRamUsage,
											   double requirementRam, string requirementRamUsage)
		{
			return string.Format("{0}_{1}_{2}_{3}",
				systemRam, systemRamUsage, requirementRam, requirementRamUsage);
		}

		private static string BuildGpuCacheKey(string systemGpuName, List<string> requirementGpu)
		{
			List<string> sortedRequirements = (requirementGpu ?? new List<string>())
				.Select(r => r.Trim())
				.OrderBy(r => r)
				.ToList();

			return string.Format("{0}_{1}",
				(systemGpuName ?? "").Trim(), string.Join("|", sortedRequirements));
		}

		#endregion

		#region Public API

		/// <summary>
		/// Checks if the system configuration meets game requirements.
		/// Uses debounced persistent caching for static hardware checks (OS, CPU, RAM, GPU).
		/// Storage check is never cached — always reflects current disk space.
		/// </summary>
		public static CheckSystem CheckConfig(Game game, RequirementEntry requirementEntry,
											 SystemConfiguration systemConfiguration, bool IsInstalled)
		{
			EnsureInitialized();

			GameContext = game;
			Common.LogDebug(true, string.Format("CheckConfig() for {0}", game.Name));

			if (requirementEntry == null || systemConfiguration == null)
			{
				Common.LogDebug(true, "CheckConfig() with null requirement and/or systemConfiguration");
				return new CheckSystem();
			}

			bool isCheckOs = CheckOSCached(systemConfiguration.Os, requirementEntry.Os);
			bool isCheckCpu = CheckCpuCached(systemConfiguration, requirementEntry.Cpu);
			bool isCheckRam = CheckRamCached(systemConfiguration.Ram, systemConfiguration.RamUsage,
												 requirementEntry.Ram, requirementEntry.RamUsage);
			bool isCheckGpu = CheckGpuCached(systemConfiguration, requirementEntry.Gpu);
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

		#region Cached check methods

		private static bool CheckOSCached(string systemOs, List<string> requirementOs)
		{
			string cacheKey = BuildOsCacheKey(systemOs, requirementOs);
			bool isNewEntry = false;

			CachedCheckResult cached = _osCache.GetOrSet(cacheKey, () =>
			{
				isNewEntry = true;
				bool result = CheckOS(systemOs, requirementOs);
				string requirementKey = string.Join("|", requirementOs ?? new List<string>());
				CachedCheckResult entry = new CachedCheckResult(result, systemOs, requirementKey, "OS");
				lock (_trackingLock) { _osCacheTracking[cacheKey] = entry; }
				return entry;
			}, TimeSpan.FromDays(_cacheExpirationDays));

			if (isNewEntry) { ScheduleCacheSave(); }
			return cached.Result;
		}

		private static bool CheckCpuCached(SystemConfiguration systemConfiguration, List<string> requirementCpu)
		{
			string cacheKey = BuildCpuCacheKey(systemConfiguration.Cpu, requirementCpu);
			bool isNewEntry = false;

			CachedCheckResult cached = _cpuCache.GetOrSet(cacheKey, () =>
			{
				isNewEntry = true;
				bool result = CheckCpu(systemConfiguration, requirementCpu);
				string requirementKey = string.Join("|", requirementCpu ?? new List<string>());
				CachedCheckResult entry = new CachedCheckResult(result, systemConfiguration.Cpu, requirementKey, "CPU");
				lock (_trackingLock) { _cpuCacheTracking[cacheKey] = entry; }
				return entry;
			}, TimeSpan.FromDays(_cacheExpirationDays));

			if (isNewEntry) { ScheduleCacheSave(); }
			return cached.Result;
		}

		private static bool CheckRamCached(long systemRam, string systemRamUsage,
										   double requirementRam, string requirementRamUsage)
		{
			string cacheKey = BuildRamCacheKey(systemRam, systemRamUsage, requirementRam, requirementRamUsage);
			bool isNewEntry = false;

			CachedCheckResult cached = _ramCache.GetOrSet(cacheKey, () =>
			{
				isNewEntry = true;
				bool result = CheckRam(systemRam, systemRamUsage, requirementRam, requirementRamUsage);
				CachedCheckResult entry = new CachedCheckResult(result,
					string.Format("{0} {1}", systemRam, systemRamUsage),
					string.Format("{0} {1}", requirementRam, requirementRamUsage), "RAM");
				lock (_trackingLock) { _ramCacheTracking[cacheKey] = entry; }
				return entry;
			}, TimeSpan.FromDays(_cacheExpirationDays));

			if (isNewEntry) { ScheduleCacheSave(); }
			return cached.Result;
		}

		private static bool CheckGpuCached(SystemConfiguration systemConfiguration, List<string> requirementGpu)
		{
			string cacheKey = BuildGpuCacheKey(systemConfiguration.GpuName, requirementGpu);
			bool isNewEntry = false;

			CachedCheckResult cached = _gpuCache.GetOrSet(cacheKey, () =>
			{
				isNewEntry = true;
				bool result = CheckGpu(systemConfiguration, requirementGpu);
				string requirementKey = string.Join("|", requirementGpu ?? new List<string>());
				CachedCheckResult entry = new CachedCheckResult(result, systemConfiguration.GpuName, requirementKey, "GPU");
				lock (_trackingLock) { _gpuCacheTracking[cacheKey] = entry; }
				return entry;
			}, TimeSpan.FromDays(_cacheExpirationDays));

			if (isNewEntry) { ScheduleCacheSave(); }
			return cached.Result;
		}

		#endregion

		#region Core check logic

		private static bool CheckOS(string systemOs, List<string> requirementOs)
		{
			if (requirementOs == null || requirementOs.Count == 0)
			{
				return true;
			}

			try
			{
				int numberOsPc = 0;
				bool systemOsParsed = false;

				foreach (string os in requirementOs)
				{
					if (systemOs.IndexOf(os, StringComparison.OrdinalIgnoreCase) >= 0)
					{
						return true;
					}

					if (OldOsList.Any(oldOs => os.IndexOf(oldOs, StringComparison.OrdinalIgnoreCase) >= 0))
					{
						return true;
					}

					if (!systemOsParsed)
					{
						Match systemMatch = NumberExtractor.Match(systemOs);
						if (systemMatch.Success)
						{
							int.TryParse(systemMatch.Value, out numberOsPc);
						}
						systemOsParsed = true;
					}

					if (numberOsPc > 0)
					{
						Match requirementMatch = NumberExtractor.Match(os);
						if (requirementMatch.Success
							&& int.TryParse(requirementMatch.Value, out int numberOsRequirement)
							&& numberOsPc >= numberOsRequirement)
						{
							return true;
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

		private static bool CheckCpu(SystemConfiguration systemConfiguration, List<string> requirementCpu)
		{
			if (requirementCpu == null || requirementCpu.Count == 0)
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

		private static bool CheckGpu(SystemConfiguration systemConfiguration, List<string> requirementGpu)
		{
			if (requirementGpu == null || requirementGpu.Count == 0)
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
						return check.SameConstructor
							|| (!gpuCheck.IsWithNoCard && gpuCheck.CardRequirementIsOld)
							|| i == 0;
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

		#region Error handling

		private static void LogError(Exception ex, string methodName)
		{
			string message = string.Format(
				ResourceProvider.GetString("LOCSystemCheckerTryRefresh"), GameContext?.Name);
			Common.LogError(ex, false, message, true, PluginDatabase.PluginName, message);
		}

		#endregion
	}
}