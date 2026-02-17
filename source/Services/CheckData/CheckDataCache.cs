using CommonPluginsShared;
using CommonPluginsShared.IO;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SystemChecker.Services
{
	/// <summary>
	/// Persistent cache for hardware checks using FileDataTools with expiration management
	/// </summary>
	public class CheckDataCache
	{
		private static readonly ILogger _logger = LogManager.GetLogger();
		private static readonly SystemCheckerDatabase PluginDatabase = SystemChecker.PluginDatabase;
		private static readonly FileDataService _fileDataService = new FileDataService(PluginDatabase.PluginName, "CheckData");
		private static readonly object _cacheLock = new object();
		private static Dictionary<string, CheckData> _cache;
		private static readonly string _cacheFilePath;
		private static DateTime _lastCleanup = DateTime.Now;

		private const int ExpirationDays = 5;
		private const int CleanupIntervalHours = 24;

		static CheckDataCache()
		{
			_cacheFilePath = Path.Combine(PluginDatabase.Paths.PluginUserDataPath, "CheckCache.json");
			LoadCache();
			CleanExpiredEntries();
		}

		/// <summary>
		/// Loads cache from disk and removes expired entries
		/// </summary>
		private static void LoadCache()
		{
			lock (_cacheLock)
			{
				try
				{
					var loadedCache = _fileDataService.LoadData<Dictionary<string, CheckData>>(_cacheFilePath, -1);

					if (loadedCache != null)
					{
						int originalCount = loadedCache.Count;
						_cache = loadedCache
							.Where(kvp => kvp.Value != null && !kvp.Value.IsExpired(ExpirationDays))
							.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

						int expiredCount = originalCount - _cache.Count;
						if (expiredCount > 0)
						{
							_logger.Info($"Removed {expiredCount} expired entries from cache");
							SaveCache();
						}

						_logger.Info($"Loaded {_cache.Count} cached check results");
					}
					else
					{
						_cache = new Dictionary<string, CheckData>();
					}
				}
				catch (Exception ex)
				{
					_logger.Error(ex, "Failed to load check cache");
					_cache = new Dictionary<string, CheckData>();
				}
			}
		}

		/// <summary>
		/// Saves cache to disk
		/// </summary>
		private static void SaveCache()
		{
			try
			{
				_fileDataService.SaveData(_cacheFilePath, _cache);
			}
			catch (Exception ex)
			{
				_logger.Error(ex, "Failed to save check cache");
			}
		}

		/// <summary>
		/// Gets cached result if available and not expired
		/// </summary>
		public static CheckData GetCached(string cacheKey)
		{
			lock (_cacheLock)
			{
				PeriodicCleanup();

				if (_cache.TryGetValue(cacheKey, out CheckData cached))
				{
					if (cached.IsExpired(ExpirationDays))
					{
						_cache.Remove(cacheKey);
						SaveCache();
						_logger.Debug($"Cache entry expired and removed: {cacheKey}");
						return null;
					}

					return cached;
				}

				return null;
			}
		}

		/// <summary>
		/// Stores result in cache with current timestamp
		/// </summary>
		public static void SetCached(CheckData checkData)
		{
			if (checkData == null || string.IsNullOrEmpty(checkData.GetCacheKey()))
			{
				return;
			}

			lock (_cacheLock)
			{
				_cache[checkData.GetCacheKey()] = checkData;
				SaveCache();
			}
		}

		/// <summary>
		/// Clears entire cache
		/// </summary>
		public static void ClearCache()
		{
			lock (_cacheLock)
			{
				_cache.Clear();
				SaveCache();
				_logger.Info("Cache cleared");
			}
		}

		/// <summary>
		/// Manually triggers cleanup of expired entries
		/// </summary>
		public static void CleanExpiredEntries()
		{
			lock (_cacheLock)
			{
				int originalCount = _cache.Count;
				var validEntries = _cache
					.Where(kvp => !kvp.Value.IsExpired(ExpirationDays))
					.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

				int removedCount = originalCount - validEntries.Count;

				if (removedCount > 0)
				{
					_cache = validEntries;
					SaveCache();
					_logger.Info($"Cleaned {removedCount} expired entries from cache");
				}

				_lastCleanup = DateTime.Now;
			}
		}

		/// <summary>
		/// Performs periodic cleanup every 24 hours
		/// </summary>
		private static void PeriodicCleanup()
		{
			if (DateTime.Now > _lastCleanup.AddHours(CleanupIntervalHours))
			{
				CleanExpiredEntries();
			}
		}

		/// <summary>
		/// Gets cache statistics
		/// </summary>
		public static CacheStatistics GetStatistics()
		{
			lock (_cacheLock)
			{
				var stats = new CacheStatistics
				{
					TotalEntries = _cache.Count,
					ExpiredEntries = _cache.Count(kvp => kvp.Value.IsExpired(ExpirationDays)),
					GpuEntries = _cache.Count(kvp => kvp.Value.CheckType == "GPU"),
					CpuEntries = _cache.Count(kvp => kvp.Value.CheckType == "CPU"),
					OldestEntry = _cache.Values.Any() ? _cache.Values.Min(c => c.CreatedAt) : DateTime.Now,
					NewestEntry = _cache.Values.Any() ? _cache.Values.Max(c => c.CreatedAt) : DateTime.Now
				};

				return stats;
			}
		}
	}

	/// <summary>
	/// Cache statistics for monitoring
	/// </summary>
	public class CacheStatistics
	{
		public int TotalEntries { get; set; }
		public int ExpiredEntries { get; set; }
		public int GpuEntries { get; set; }
		public int CpuEntries { get; set; }
		public DateTime OldestEntry { get; set; }
		public DateTime NewestEntry { get; set; }
	}
}