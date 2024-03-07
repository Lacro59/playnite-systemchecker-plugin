using Playnite.SDK;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using SystemChecker.Models;
using CommonPluginsShared;
using Playnite.SDK.Models;

namespace SystemChecker.Services
{
    class SystemApi
    {
        private static ILogger Logger => LogManager.GetLogger();
        private static IResourceProvider ResourceProvider => new ResourceProvider();

        private static Game Game;


        public static CheckSystem CheckConfig(Game game, Requirement requirement, SystemConfiguration systemConfiguration, bool IsInstalled)
        {
            Game = game;
            Common.LogDebug(true, $"CheckConfig() for {game.Name}");

            if (requirement != null && systemConfiguration != null)
            {
                bool isCheckOs = CheckOS(systemConfiguration.Os, requirement.Os);
                bool isCheckCpu = CheckCpu(systemConfiguration, requirement.Cpu);
                bool isCheckRam = CheckRam(systemConfiguration.Ram, systemConfiguration.RamUsage, requirement.Ram, requirement.RamUsage);
                bool isCheckGpu = CheckGpu(systemConfiguration, requirement.Gpu);
                bool isCheckStorage = IsInstalled ? IsInstalled : CheckStorage(systemConfiguration.Disks, requirement.Storage);

                bool AllOk = isCheckOs && isCheckCpu && isCheckRam && isCheckGpu && isCheckStorage;

                return new CheckSystem
                {
                    CheckOs = isCheckOs,
                    CheckCpu = isCheckCpu,
                    CheckRam = isCheckRam,
                    CheckGpu = isCheckGpu,
                    CheckStorage = isCheckStorage,
                    AllOk = AllOk
                };
            }
            else
            {
                Common.LogDebug(true, $"CheckConfig() with null requirement and/or systemConfiguration");
            }

            return new CheckSystem();
        }

        private static bool CheckOS(string systemOs, List<string> requierementOs)
        {
            try
            {
                List<string> oldOS = new List<string> { "95", "98", "XP", "Millenium", "ME", "2000", "Vista" };

                if (requierementOs.Count == 0)
                {
                    return true;
                }

                foreach (string Os in requierementOs)
                {
                    if (systemOs.Contains(Os, StringComparison.InvariantCultureIgnoreCase))
                    {
                        return true;
                    }

                    if (oldOS.Where(x => Os.Contains(x, StringComparison.InvariantCultureIgnoreCase)).Count() > 0)
                    {
                        return true;
                    }

                    _ = int.TryParse(Regex.Matches(Os, @"\d+")?[0]?.Value ?? "", out int numberOsRequirement);
                    _ = int.TryParse(Regex.Matches(systemOs, @"\d+")?[0]?.Value ?? "", out int numberOsPc);
                    if (numberOsRequirement != 0 && numberOsPc != 0 && numberOsPc >= numberOsRequirement)
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                string message = string.Format(ResourceProvider.GetString("LOCSystemCheckerTryRefresh"), Game?.Name);
                Common.LogError(ex, false, message, true, "SystemChecker");
            }

            return false;
        }

        private static bool CheckCpu(SystemConfiguration systemConfiguration, List<string> requierementCpu)
        {
            try
            {
                if (requierementCpu.Count > 0)
                {
                    foreach (string cpu in requierementCpu)
                    {
                        Cpu cpuCheck = new Cpu(systemConfiguration, cpu);
                        CheckResult check = cpuCheck.IsBetter();

                        if (check.Result)
                        {
                            return true;
                        }
                        else if (check.SameConstructor)
                        {
                            return check.Result;
                        }
                    }
                }
                else
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                string message = string.Format(ResourceProvider.GetString("LOCSystemCheckerTryRefresh"), Game?.Name);
                Common.LogError(ex, false, message, true, "SystemChecker");
            }

            return false;
        }

        private static bool CheckRam(double systemRam, string systemRamUsage, double requierementRam, string requierementRamUsage)
        {
            try
            {
                return systemRamUsage == requierementRamUsage || systemRam >= requierementRam;
            }
            catch (Exception ex)
            {
                string message = string.Format(ResourceProvider.GetString("LOCSystemCheckerTryRefresh"), Game?.Name);
                Common.LogError(ex, false, message, true, "SystemChecker");
            }

            return false;
        }

        private static bool CheckGpu(SystemConfiguration systemConfiguration, List<string> requierementGpu)
        {
            try
            {
                if (requierementGpu.Count > 0)
                {
                    for (int i = 0; i < requierementGpu.Count; i++)
                    {
                        string gpu = requierementGpu[i];
                        Gpu gpuCheck = new Gpu(systemConfiguration, gpu);
                        CheckResult check = gpuCheck.IsBetter();

                        if (check.Result)
                        {
                            return check.SameConstructor ? check.Result : (!gpuCheck.IsWithNoCard && gpuCheck.CardRequierementIsOld) || i <= 0;
                        }
                        else if (check.SameConstructor)
                        {
                            return check.Result;
                        }
                    }
                }
                else
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                string message = string.Format(ResourceProvider.GetString("LOCSystemCheckerTryRefresh"), SystemApi.Game?.Name);
                Common.LogError(ex, false, message, true, "SystemChecker");
            }

            return false;
        }

        private static bool CheckStorage(List<SystemDisk> systemDisks, double Storage)
        {
            if (Storage == 0)
            {
                return true;
            }

            try
            {
                foreach (SystemDisk Disk in systemDisks)
                {
                    if (Disk.FreeSpace >= Storage)
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                string message = string.Format(ResourceProvider.GetString("LOCSystemCheckerTryRefresh"), Game?.Name);
                Common.LogError(ex, false, message, true, "SystemChecker");
            }

            return false;
        }
    }
}
