using CommonPluginsShared;
using Playnite.SDK;
using Playnite.SDK.Data;
using SystemChecker.Clients;
using SystemChecker.Models;

namespace SystemChecker.Services
{
	/// <summary>
	/// Base class for hardware comparison logic with integrated caching
	/// </summary>
	public abstract class HardwareChecker
	{
		protected static readonly ILogger Logger = LogManager.GetLogger();

		protected abstract string CheckType { get; }
		protected abstract string ComponentPcName { get; }
		protected abstract string ComponentRequirementName { get; }

		/// <summary>
		/// Performs hardware comparison with caching
		/// </summary>
		public CheckResult IsBetter()
		{
			string cacheKey = CheckData.CreateCacheKey(CheckType, ComponentPcName, ComponentRequirementName);

			CheckData cachedData = CheckDataCache.GetCached(cacheKey);
			if (cachedData != null)
			{
				Common.LogDebug(true, $"{CheckType}.IsBetter - Using cached result for {cacheKey}");
				return cachedData.Result;
			}

			Common.LogDebug(true, $"{CheckType}.IsBetter - Computing result for {ComponentPcName} vs {ComponentRequirementName}");

			CheckResult result = PerformCheck();

			CheckData checkData = new CheckData(result, ComponentPcName, ComponentRequirementName, CheckType);
			CheckDataCache.SetCached(checkData);

			return result;
		}

		/// <summary>
		/// Abstract method to be implemented by derived classes for specific hardware checks
		/// </summary>
		protected abstract CheckResult PerformCheck();

		/// <summary>
		/// Calls benchmark service (expensive operation)
		/// </summary>
		protected bool? CallBenchmark(string pcName, string requirementName, bool isGpu)
		{
			Benchmark benchmark = new Benchmark();
			return isGpu
				? benchmark.IsBetterGpu(pcName, requirementName)
				: benchmark.IsBetterCpu(pcName, requirementName);
		}
	}
}