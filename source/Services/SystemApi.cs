using Playnite.SDK;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Diagnostics;
using SystemChecker.Models;
using CommonPluginsShared;
using Playnite.SDK.Models;
using CommonPluginsStores.Models;

namespace SystemChecker.Services
{
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

		public static CheckSystem CheckConfig(Game game, RequirementEntry requirementEntry, SystemConfiguration systemConfiguration, bool IsInstalled)
		{
#if DEBUG
			Stopwatch globalTimer = Stopwatch.StartNew();
#endif
			GameContext = game;
			Common.LogDebug(true, $"CheckConfig() for {game.Name}");

			if (requirementEntry == null || systemConfiguration == null)
			{
				Common.LogDebug(true, "CheckConfig() with null requirement and/or systemConfiguration");
				return new CheckSystem();
			}

#if DEBUG
			bool isCheckOs = MeasureExecution("CheckOS", () => CheckOS(systemConfiguration.Os, requirementEntry.Os));
			bool isCheckCpu = MeasureExecution("CheckCpu", () => CheckCpu(systemConfiguration, requirementEntry.Cpu));
			bool isCheckRam = MeasureExecution("CheckRam", () => CheckRam(systemConfiguration.Ram, systemConfiguration.RamUsage, requirementEntry.Ram, requirementEntry.RamUsage));
			bool isCheckGpu = MeasureExecution("CheckGpu", () => CheckGpu(systemConfiguration, requirementEntry.Gpu));
			bool isCheckStorage = MeasureExecution("CheckStorage", () => IsInstalled || CheckStorage(systemConfiguration.Disks, requirementEntry.Storage));

			globalTimer.Stop();
			Common.LogDebug(true, $"CheckConfig() total execution time: {globalTimer.ElapsedMilliseconds}ms");
#else
            bool isCheckOs = CheckOS(systemConfiguration.Os, requirementEntry.Os);
            bool isCheckCpu = CheckCpu(systemConfiguration, requirementEntry.Cpu);
            bool isCheckRam = CheckRam(systemConfiguration.Ram, systemConfiguration.RamUsage, requirementEntry.Ram, requirementEntry.RamUsage);
            bool isCheckGpu = CheckGpu(systemConfiguration, requirementEntry.Gpu);
            bool isCheckStorage = IsInstalled || CheckStorage(systemConfiguration.Disks, requirementEntry.Storage);
#endif

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

#if DEBUG
		private static T MeasureExecution<T>(string methodName, Func<T> action)
		{
			Stopwatch timer = Stopwatch.StartNew();
			T result = action();
			timer.Stop();
			Common.LogDebug(true, $"{methodName} executed in {timer.ElapsedMilliseconds}ms ({timer.ElapsedTicks} ticks)");
			return result;
		}
#endif

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

		private static bool CheckRam(double systemRam, string systemRamUsage, double requirementRam, string requirementRamUsage)
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

		private static void LogError(Exception ex, string methodName)
		{
			string message = string.Format(ResourceProvider.GetString("LOCSystemCheckerTryRefresh"), GameContext?.Name);
			Common.LogError(ex, false, message, true, PluginDatabase.PluginName, message);
		}
	}
}