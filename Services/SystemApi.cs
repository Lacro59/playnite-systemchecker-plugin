using Newtonsoft.Json;
using Playnite.SDK;
using Playnite.SDK.Models;
using PluginCommon;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using SystemChecker.Models;

namespace SystemChecker.Services
{
    class SystemApi
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private static IResourceProvider resources = new ResourceProvider();


        public static CheckSystem CheckConfig(Requirement requirement, SystemConfiguration systemConfiguration)
        {
            if (requirement != null)
            {
                bool isCheckOs = CheckOS(systemConfiguration.Os, requirement.Os);
                bool isCheckCpu = CheckCpu(systemConfiguration, requirement.Cpu);
                bool isCheckRam = CheckRam(systemConfiguration.Ram, systemConfiguration.RamUsage, requirement.Ram, requirement.RamUsage);
                bool isCheckGpu = CheckGpu(systemConfiguration, requirement.Gpu);
                bool isCheckStorage = CheckStorage(systemConfiguration.Disks, requirement.Storage); ;

                bool AllOk = (isCheckOs && isCheckCpu && isCheckRam && isCheckGpu && isCheckStorage);

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
                logger.Warn($"SystemChecker - CheckConfig() with null requirement");
            }

            return new CheckSystem();
        }

        private static bool CheckOS(string systemOs, List<string> requierementOs)
        {
            try
            {
                foreach (string Os in requierementOs)
                {
                    if (systemOs.ToLower().IndexOf("10") > -1)
                    {
                        return true;
                    }

                    if (systemOs.ToLower().IndexOf(Os.ToLower()) > -1)
                    {
                        return true;
                    }

                    int numberOsRequirement = 0;
                    int numberOsPc = 0;
                    Int32.TryParse(Os, out numberOsRequirement);
                    Int32.TryParse(Regex.Replace(systemOs, "[^.0-9]", string.Empty).Trim(), out numberOsPc);
                    if (numberOsRequirement != 0 && numberOsPc != 0 && numberOsPc >= numberOsRequirement)
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "SystemChecker", $"Error on CheckOs() with {systemOs} & {JsonConvert.SerializeObject(requierementOs)}");
            }

            return false;
        }

        private static bool CheckCpu(SystemConfiguration systemConfiguration, List<string> requierementCpu)
        {
            try
            {
                if (requierementCpu.Count > 0)
                {
                    foreach (var cpu in requierementCpu)
                    {
                        Cpu cpuCheck = new Cpu(systemConfiguration, cpu);
                        if (cpuCheck.IsBetter())
                        {
                            return true;
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
                Common.LogError(ex, "SystemChecker", $"Error on CheckGpu() with {systemConfiguration.Cpu} & {JsonConvert.SerializeObject(requierementCpu)}");
            }

            return false;
        }

        private static bool CheckRam(double systemRam, string systemRamUsage, double requierementRam, string requierementRamUsage)
        {
            try
            {
                if (systemRamUsage == requierementRamUsage)
                {
                    return true;
                }

                return systemRam >= requierementRam;
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "SystemChecker", $"Error on CheckRam() with {systemRam} & {requierementRam}");
            }

            return false;
        }

        private static bool CheckGpu(SystemConfiguration systemConfiguration, List<string> requierementGpu)
        {
            try
            {
                if (requierementGpu.Count > 0)
                {
                    for(int i = 0; i < requierementGpu.Count; i++)
                    {
                        var gpu = requierementGpu[i];

                        Gpu gpuCheck = new Gpu(systemConfiguration, gpu);
                        if (gpuCheck.IsBetter())
                        {
                            if (gpuCheck.IsWithNoCard && i > 0)
                            {
                                return false;
                            }
                            else
                            {
                                return true;
                            }
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
                Common.LogError(ex, "SystemChecker", $"Error on CheckGpu() with {systemConfiguration.GpuName} & {JsonConvert.SerializeObject(requierementGpu)}");
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
                    //logger.Debug($"CheckStorage - {Disk.FreeSpace} - {requirement.Storage}");
                    if (Disk.FreeSpace >= Storage)
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "SystemChecker", $"Error on CheckStorage() with {Storage} & {JsonConvert.SerializeObject(systemDisks)}");
            }

            return false;
        }
    }

    public class CheckSystem
    {
        public bool CheckOs { get; set; }
        public bool CheckCpu { get; set; }
        public bool CheckRam { get; set; }
        public bool CheckGpu { get; set; }
        public bool CheckStorage { get; set; }
        public bool? AllOk { get; set; } = null;
    }
}
