using Newtonsoft.Json;
using Playnite.SDK;
using Playnite.SDK.Models;
using PluginCommon;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Management;
using System.Text.RegularExpressions;
using SystemChecker.Models;
using System.Diagnostics;

namespace SystemChecker.Clients
{
    class SystemApi
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private static IResourceProvider resources = new ResourceProvider();
        private IPlayniteAPI _PlayniteApi;

        private string _PluginUserDataPath { get; set; }
        private string PluginDirectory { get; set; }
        private string FilePlugin { get; set; }

        private SystemConfiguration systemConfiguration = new SystemConfiguration();
        private GameRequierements gameRequierements = new GameRequierements();


        public SystemApi(string PluginUserDataPath, IPlayniteAPI PlayniteApi)
        {
            _PlayniteApi = PlayniteApi;
            _PluginUserDataPath = PluginUserDataPath;
            PluginDirectory = PluginUserDataPath + "\\SystemChecker\\";
            FilePlugin = PluginDirectory + "\\pc.json";
        }

        public SystemConfiguration GetInfo()
        {
            systemConfiguration = new SystemConfiguration();
            List<SystemDisk> Disks = GetInfoDisks();

            if (!Directory.Exists(PluginDirectory))
            {
                Directory.CreateDirectory(PluginDirectory);
            }

            if (File.Exists(FilePlugin))
            {
                try
                {
                    string JsonStringData = File.ReadAllText(FilePlugin);
                    systemConfiguration =  JsonConvert.DeserializeObject<SystemConfiguration>(JsonStringData);
                    systemConfiguration.Disks = Disks;
                    return systemConfiguration;
                }
                catch(Exception ex)
                {
                    Common.LogError(ex, "SystemChecker", $"Failed to load item from {FilePlugin}");
                }
            }

            string Name = Environment.MachineName;
            string Os = string.Empty;
            string Cpu = string.Empty;
            uint CpuMaxClockSpeed = 0;
            string GpuName = string.Empty;
            long GpuRam = 0;
            uint CurrentHorizontalResolution = 0;
            uint CurrentVerticalResolution = 0;
            long Ram = 0;


            try
            {
                ManagementObjectSearcher myOperativeSystemObject = new ManagementObjectSearcher("select * from Win32_OperatingSystem");
                foreach (ManagementObject obj in myOperativeSystemObject.Get())
                {
                    Os = (string)obj["Caption"];
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "SystemChecker", "Error on Win32_OperatingSystem");
            }


            try
            {
                ManagementObjectSearcher myProcessorObject = new ManagementObjectSearcher("select * from Win32_Processor");
                foreach (ManagementObject obj in myProcessorObject.Get())
                {
                    Cpu = (string)obj["Name"];
                    CpuMaxClockSpeed = (uint)obj["MaxClockSpeed"];
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "SystemChecker", "Error on Win32_Processor");
            }


            try
            {
                ManagementObjectSearcher myVideoObject = new ManagementObjectSearcher("select * from Win32_VideoController");
                foreach (ManagementObject obj in myVideoObject.Get())
                {
                    GpuName = (string)obj["Name"];
                    GpuRam = (long)Convert.ToDouble(obj["AdapterRAM"]);
                    CurrentHorizontalResolution = (uint)obj["CurrentHorizontalResolution"];
                    CurrentVerticalResolution = (uint)obj["CurrentVerticalResolution"];

                    if (Gpu.CallIsNvidia(GpuName))
                    {
                        break;
                    }
                    if (Gpu.CallIsAmd(GpuName))
                    {
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "SystemChecker", "Error on Win32_VideoController");
            }


            try
            {
                ManagementObjectSearcher myComputerSystemObject = new ManagementObjectSearcher("select * from Win32_ComputerSystem");
                foreach (ManagementObject obj in myComputerSystemObject.Get())
                {
                    double TempRam = Math.Ceiling(Convert.ToDouble(obj["TotalPhysicalMemory"]) / 1024 / 1024 / 1024);
                    Ram = (long)(TempRam * 1024 * 1024 * 1024);
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "SystemChecker", "Error on Win32_ComputerSystem");
            }



            systemConfiguration.Name = Name;
            systemConfiguration.Os = Os;
            systemConfiguration.Cpu = Cpu;
            systemConfiguration.CpuMaxClockSpeed = CpuMaxClockSpeed;
            systemConfiguration.GpuName = GpuName;
            systemConfiguration.GpuRam = GpuRam;
            systemConfiguration.CurrentHorizontalResolution = CurrentHorizontalResolution;
            systemConfiguration.CurrentVerticalResolution = CurrentVerticalResolution;
            systemConfiguration.Ram = Ram;
            systemConfiguration.RamUsage = RequierementMetadata.SizeSuffix(Ram, true);
            systemConfiguration.Disks = Disks;


            File.WriteAllText(FilePlugin, JsonConvert.SerializeObject(systemConfiguration));
            return systemConfiguration;
        }

        private List<SystemDisk> GetInfoDisks()
        {
            List<SystemDisk> Disks = new List<SystemDisk>();
            DriveInfo[] allDrives = DriveInfo.GetDrives();
            foreach (DriveInfo d in allDrives)
            {
                string VolumeLabel = string.Empty;
                try
                {
                    VolumeLabel = d.VolumeLabel;
                }
                catch (Exception ex)
                {
                    logger.Warn($"SystemChecker - Error on VolumeLabel - {ex.Message.Trim()}");
                }

                string Name = string.Empty;
                try
                {
                    Name = d.Name;
                }
                catch (Exception ex)
                {
                    logger.Warn($"SystemChecker - Error on Name - {ex.Message.Trim()}");
                }

                long FreeSpace = 0;
                try
                {
                    FreeSpace = d.TotalFreeSpace;
                }
                catch (Exception ex)
                {
                    logger.Warn($"SystemChecker - Error on TotalFreeSpace - {ex.Message.Trim()}");
                }

                string FreeSpaceUsage = string.Empty;
                try
                {
                    FreeSpaceUsage = RequierementMetadata.SizeSuffix(d.TotalFreeSpace);
                }
                catch (Exception ex)
                {
                    logger.Warn($"SystemChecker - Error on FreeSpaceUsage - {ex.Message.Trim()}");
                }

                Disks.Add(new SystemDisk
                {
                    Name = VolumeLabel,
                    Drive = Name,
                    FreeSpace = FreeSpace,
                    FreeSpaceUsage = FreeSpaceUsage
                });
            }
            return Disks;
        }


        public GameRequierements GetGameRequierements(Game game, bool force = false)
        {
            gameRequierements = new GameRequierements();
            string FileGameRequierements = PluginDirectory + "\\" + game.Id.ToString() + ".json";
            string SourceName = string.Empty;

            try
            {
                SourceName = PlayniteTools.GetSourceName(game, _PlayniteApi);

                if (File.Exists(FileGameRequierements))
                {
                    if (force)
                    {
                        logger.Info($"SystemChecker - Delete cache for {game.Name}");
                        try
                        {
                            File.Delete(FileGameRequierements);
                        }
                        catch (Exception ex)
                        {
                            Common.LogError(ex, "SystemChecker", $"Error on delete file: {FileGameRequierements}");
                        }
                    }
                    else
                    {
                        logger.Info($"SystemChecker - Find from cache for {game.Name}");
                        return JsonConvert.DeserializeObject<GameRequierements>(File.ReadAllText(FileGameRequierements));
                    }
                }

                // Search datas
                logger.Info($"SystemChecker - Try find with PCGamingWikiRequierements for {game.Name}");
                PCGamingWikiRequierements pCGamingWikiRequierements = new PCGamingWikiRequierements(game, _PluginUserDataPath, _PlayniteApi);
                gameRequierements = pCGamingWikiRequierements.GetRequirements();

                if (!pCGamingWikiRequierements.IsFind())
                {
                    logger.Info($"SystemChecker - Try find with SteamRequierements for {game.Name}");
                    SteamRequierements steamRequierements;
                    switch (SourceName.ToLower())
                    {
                        case "steam":
                            steamRequierements = new SteamRequierements(game);
                            gameRequierements = steamRequierements.GetRequirements();
                            gameRequierements.Link = "https://store.steampowered.com/app/" + game.GameId;
                            break;
                        default:
                            SteamApi steamApi = new SteamApi(_PluginUserDataPath);
                            int SteamID = steamApi.GetSteamId(game.Name);
                            if (SteamID != 0)
                            {
                                steamRequierements = new SteamRequierements(game, (uint)SteamID);
                                gameRequierements = steamRequierements.GetRequirements();
                                gameRequierements.Link = "https://store.steampowered.com/app/" + SteamID;
                            }
                            break;
                    }
                }


                NormalizeRecommanded();


                // Save datas
                File.WriteAllText(FileGameRequierements, JsonConvert.SerializeObject(gameRequierements));
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "SystemChecker", $"Error on GetGameRequierements()");
            }

            return gameRequierements;
        }


        private void NormalizeRecommanded()
        {
            if (gameRequierements.Recommanded.Os.Count > 1 || gameRequierements.Recommanded.Cpu.Count > 1 || gameRequierements.Recommanded.Gpu.Count > 1 
                || gameRequierements.Recommanded.Ram > 0 || gameRequierements.Recommanded.Storage > 0)
            {
                if (gameRequierements.Recommanded.Os.Count == 0)
                {
                    gameRequierements.Recommanded.Os = gameRequierements.Minimum.Os;
                }
                if (gameRequierements.Recommanded.Cpu.Count == 0)
                {
                    gameRequierements.Recommanded.Cpu = gameRequierements.Minimum.Cpu;
                }
                if (gameRequierements.Recommanded.Gpu.Count == 0)
                {
                    gameRequierements.Recommanded.Gpu = gameRequierements.Minimum.Gpu;
                }
                if (gameRequierements.Recommanded.Ram == 0)
                {
                    gameRequierements.Recommanded.Ram = gameRequierements.Minimum.Ram;
                    gameRequierements.Recommanded.RamUsage = gameRequierements.Minimum.RamUsage;
                }
                if (gameRequierements.Recommanded.Storage == 0)
                {
                    gameRequierements.Recommanded.Storage = gameRequierements.Minimum.Storage;
                    gameRequierements.Recommanded.StorageUsage = gameRequierements.Minimum.StorageUsage;
                }
            }
        }


        public void GetDataGetAll()
        {
            GlobalProgressOptions globalProgressOptions = new GlobalProgressOptions(
                resources.GetString("LOCSystemCheckerDataDownload"), 
                true
            );
            globalProgressOptions.IsIndeterminate = false;

            _PlayniteApi.Dialogs.ActivateGlobalProgress((activateGlobalProgress) =>
            {
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();

                var db = _PlayniteApi.Database.Games.Where(x => x.Hidden == false);
                activateGlobalProgress.ProgressMaxValue = (double)db.Count();

                string CancelText = string.Empty;

                foreach (Game game in db)
                {
                    if (activateGlobalProgress.CancelToken.IsCancellationRequested)
                    {
                        CancelText = " canceled";
                        break;
                    }

                    try
                    {
                        if (!PlayniteTools.IsGameEmulated(_PlayniteApi, game))
                        {
                            GetGameRequierements(game);
                        }
                    }
                    catch(Exception ex)
                    {
                        Common.LogError(ex, "SystemChecker", "Error on GetDataGetAll()");
                    }

                    activateGlobalProgress.CurrentProgressValue++;
                }

                stopWatch.Stop();
                TimeSpan ts = stopWatch.Elapsed;
                logger.Warn($"SystemChecker - Task GetDataGetAll(){CancelText} - {String.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10)}");
            }, globalProgressOptions);
        }

        public static void DataDeleteAll(IPlayniteAPI PlayniteApi, string PluginUserDataPath)
        {
            string PluginDirectory = PluginUserDataPath + "\\SystemChecker\\";
            if (Directory.Exists(PluginDirectory))
            {
                try
                {
                    Directory.Delete(PluginDirectory, true);
                    Directory.CreateDirectory(PluginDirectory);

                    PlayniteApi.Dialogs.ShowMessage(resources.GetString("LOCSystemCheckerOkRemove"), "SystemChecker");
                }
                catch
                {
                    PlayniteApi.Dialogs.ShowErrorMessage(resources.GetString("LOCSystemCheckerErrorRemove"), "SystemChecker");
                }
            }
        }


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

        private static bool CheckRam(long systemRam, string systemRamUsage, long requierementRam, string requierementRamUsage)
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

        private static bool CheckStorage(List<SystemDisk> systemDisks, long Storage)
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
