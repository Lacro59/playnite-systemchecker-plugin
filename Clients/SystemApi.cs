using Newtonsoft.Json;
using Playnite.SDK;
using Playnite.SDK.Models;
using PluginCommon;
using System;
using System.Collections.Generic;
using System.IO;
using System.Management;
using System.Text.RegularExpressions;
using System.Threading;
using SystemChecker.Models;

namespace SystemChecker.Clients
{
    // https://ourcodeworld.com/articles/read/294/how-to-retrieve-basic-and-advanced-hardware-and-software-information-gpu-hard-drive-processor-os-printers-in-winforms-with-c-sharp
    class SystemApi
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private IPlayniteAPI PlayniteApi;

        private readonly string[] SizeSuffixes = { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
        private string PluginUserDataPath { get; set; }
        private string PluginDirectory { get; set; }
        private string FilePlugin { get; set; }

        private SystemConfiguration systemConfiguration = new SystemConfiguration();
        private GameRequierements gameRequierements = new GameRequierements();


        private string SizeSuffix(Int64 value)
        {
            if (value < 0) { return "-" + SizeSuffix(-value); }
            if (value == 0) { return "0.0 bytes"; }

            int mag = (int)Math.Log(value, 1024);
            decimal adjustedSize = (decimal)value / (1L << (mag * 10));

            return string.Format("{0:n1} {1}", adjustedSize, SizeSuffixes[mag]);
        }


        public SystemApi(string PluginUserDataPath, IPlayniteAPI PlayniteApi)
        {
            this.PlayniteApi = PlayniteApi;
            this.PluginUserDataPath = PluginUserDataPath;
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
            string Os = "";
            string Cpu = "";
            uint CpuMaxClockSpeed = 0;
            string GpuName = "";
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
                    Ram = (long)Convert.ToDouble(obj["TotalPhysicalMemory"]);
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
            systemConfiguration.RamUsage = SizeSuffix(Ram);
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
                string VolumeLabel = "";
                try
                {
                    VolumeLabel = d.VolumeLabel;
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, "SystemChecker", "Error on VolumeLabel");
                }

                string Name = "";
                try
                {
                    Name = d.Name;
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, "SystemChecker", "Error on Name");
                }

                long FreeSpace = 0;
                try
                {
                    FreeSpace = d.TotalFreeSpace;
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, "SystemChecker", "Error on TotalFreeSpace");
                }

                string FreeSpaceUsage = "";
                try
                {
                    FreeSpaceUsage = SizeSuffix(d.TotalFreeSpace);
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, "SystemChecker", "Error on FreeSpaceUsage");
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


        public GameRequierements GetGameRequierements(Game game)
        {
            gameRequierements = new GameRequierements();
            string FileGameRequierements = PluginDirectory + "\\" + game.Id.ToString() + ".json";

            string SourceName = "";
            if (game.SourceId == Guid.Parse("00000000-0000-0000-0000-000000000000"))
            {
                SourceName = "Playnite";
            }
            else
            {
                SourceName = game.Source.Name;
            }

            if (File.Exists(FileGameRequierements))
            {
                logger.Info($"SystemChecker - Find from cache for {game.Name}");
                return JsonConvert.DeserializeObject<GameRequierements>(File.ReadAllText(FileGameRequierements));
            }

            // Search datas
            logger.Info($"SystemChecker - Try find with PCGamingWikiRequierements for {game.Name}");
            PCGamingWikiRequierements pCGamingWikiRequierements = new PCGamingWikiRequierements(game, PluginUserDataPath, PlayniteApi);
            gameRequierements = pCGamingWikiRequierements.GetRequirements();

            if (!pCGamingWikiRequierements.isFind())
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
                        SteamApi steamApi = new SteamApi(PluginUserDataPath);
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

            // Save datas
            File.WriteAllText(FileGameRequierements, JsonConvert.SerializeObject(gameRequierements));

            return gameRequierements;
        }


        public static CheckSystem CheckConfig(Requirement requirement, SystemConfiguration systemConfiguration)
        {
            if (requirement != null)
            {
                bool isCheckOs = CheckOS(systemConfiguration.Os, requirement.Os);
                bool isCheckCpu = CheckCpu(systemConfiguration.Cpu, systemConfiguration.CpuMaxClockSpeed, requirement.Cpu);
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
                    Int32.TryParse(Regex.Replace(systemOs, "[^.0-9]", "").Trim(), out numberOsPc);
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

        private static bool CheckCpu(string systemCpu, uint CpuMaxClockSpeed, List<string> requierementCpu)
        {
            try
            {
                if (requierementCpu.Count > 0)
                {
                    foreach (var cpu in requierementCpu)
                    {
                        // Intel familly
                        if (cpu.ToLower().IndexOf("intel") > -1)
                        {
                            //logger.Debug($"cpu intel - {cpu}");

                            // Old processor
                            if (cpu.ToLower().IndexOf("i3") == -1 & cpu.ToLower().IndexOf("i5") == -1 && cpu.ToLower().IndexOf("i7") == -1 && cpu.ToLower().IndexOf("i9") == -1)
                            {
                                return true;
                            }
                        }

                        // AMD familly
                        if (cpu.ToLower().IndexOf("amd") > -1)
                        {
                            //logger.Debug($"cpu amd - {cpu}");

                            // Old processor
                            if (cpu.ToLower().IndexOf("ryzen") == -1)
                            {
                                return true;
                            }
                        }

                        // Only frequency
                        if ((cpu.ToLower().IndexOf("intel") == -1 || cpu.ToLower().IndexOf("core") == -1) && cpu.ToLower().IndexOf("amd") == -1)
                        {
                            //logger.Debug($"cpu frequency - {cpu}");

                            //Quad-Core CPU 3 GHz (64 Bit)
                            int index = -1;
                            string Clock = cpu.ToLower();
                            logger.Debug($"Clock - {Clock}");

                            // delete end string
                            index = Clock.IndexOf("ghz");
                            if (index > -1)
                            {
                                Clock = Clock.Substring(0, index).Trim();
                            }
                            //logger.Debug($"Clock - {Clock}");

                            // delete start string
                            string ClockTemp = Clock;
                            for (int i = 0; i < Clock.Length; i++)
                            {
                                if (Clock[i] == ' ')
                                {
                                    ClockTemp = Clock.Substring(i).Trim();
                                }
                            }
                            Clock = ClockTemp;
                            //logger.Debug($"Clock - {Clock}");

                            try
                            {
                                char a = Convert.ToChar(Thread.CurrentThread.CurrentCulture.NumberFormat.NumberDecimalSeparator);
                                Clock = Clock.Replace('.', a).Replace(',', a).Replace("+", "").Trim();
                                if (double.Parse(Clock) * 1000 < (CpuMaxClockSpeed * 2))
                                {
                                    return true;
                                }
                            }
                            catch (Exception ex)
                            {
                                Common.LogError(ex, "SystemChecker", $"Error on find clock control - {Clock}");
                            }
                        }

                        // Recent
                        if (CheckCpuBetter(cpu, systemCpu))
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
                Common.LogError(ex, "SystemChecker", $"Error on CheckCpu() with {systemCpu} & {JsonConvert.SerializeObject(requierementCpu)}");
            }

            return false;
        }

        private static bool CheckRam(long systemRam, string systemRamUsage, long requierementRam, string requierementRamUsage)
        {
            if (requierementRam == 0)
            {
                return true;
            }

            try
            {
                double _systemRamUsage = 0;
                double _requierementRamUsage = 0;

                double.TryParse(systemRamUsage.Replace("GB", "").Replace("MB", ""), out _systemRamUsage);
                double.TryParse(requierementRamUsage.Replace("GB", "").Replace("MB", ""), out _requierementRamUsage);

                if (_systemRamUsage != 0 && _requierementRamUsage != 0)
                {
                    return _systemRamUsage >= _requierementRamUsage;
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
                    foreach (var gpu in requierementGpu)
                    {
                        Gpu gpuCheck = new Gpu(systemConfiguration, gpu);
                        if (gpuCheck.IsBetter())
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







        private static bool CheckCpuBetter(string cpuRequirement, string cpuPc)
        {
            bool Result = false;

            try
            {
                cpuRequirement = cpuRequirement.ToLower();
                cpuPc = cpuPc.ToLower();

                int index = 0;

                string CpuRequirementReference = "";
                int CpuRequirementNumber = 0;

                // Intel
                if (cpuRequirement.IndexOf("i3") > -1)
                {
                    CpuRequirementReference = cpuRequirement.Substring(cpuRequirement.IndexOf("i3")).Trim();
                }
                if (cpuRequirement.IndexOf("i5") > -1)
                {
                    CpuRequirementReference = cpuRequirement.Substring(cpuRequirement.IndexOf("i5")).Trim();
                }
                if (cpuRequirement.IndexOf("i7") > -1)
                {
                    CpuRequirementReference = cpuRequirement.Substring(cpuRequirement.IndexOf("i7")).Trim();
                }
                if (cpuRequirement.IndexOf("i9") > -1)
                {
                    CpuRequirementReference = cpuRequirement.Substring(cpuRequirement.IndexOf("i9")).Trim();
                }
                index = CpuRequirementReference.IndexOf(" ");
                if (index > -1)
                {
                    CpuRequirementReference = CpuRequirementReference.Substring(0, index);
                }
                CpuRequirementReference = CpuRequirementReference.Trim();
                int.TryParse(Regex.Replace(CpuRequirementReference.Replace("i3", "").Replace("i5", "").Replace("i7", "").Replace("i9", ""), "[^.0-9]", "").Trim(), out CpuRequirementNumber);
                //logger.Debug($"CpuRequirementReference: {CpuRequirementReference}");
                //CpuRequirementReference = CpuRequirementReference.Substring(0, 2);

                // AMD



                //logger.Debug($"CpuRequirementReference - {CpuRequirementReference}");
                //logger.Debug($"CpuRequirementNumber - {CpuRequirementNumber}");


                string CpuPcReference = "";
                int CpuPcNumber = 0;

                //Intel(R) Core(TM) i5-4590 CPU @ 3.30GHz
                if (cpuPc.IndexOf("intel") > -1)
                {
                    if (cpuPc.IndexOf("i3") > -1)
                    {
                        CpuPcReference = cpuPc.Substring(cpuPc.IndexOf("i3"), (cpuPc.Length - cpuPc.IndexOf("i3"))).Trim();
                    }
                    if (cpuPc.IndexOf("i5") > -1)
                    {
                        CpuPcReference = cpuPc.Substring(cpuPc.IndexOf("i5"), (cpuPc.Length - cpuPc.IndexOf("i5"))).Trim();
                    }
                    if (cpuPc.IndexOf("i7") > -1)
                    {
                        CpuPcReference = cpuPc.Substring(cpuPc.IndexOf("i7"), (cpuPc.Length - cpuPc.IndexOf("i7"))).Trim();
                    }
                    if (cpuPc.IndexOf("i9") > -1)
                    {
                        CpuPcReference = cpuPc.Substring(cpuPc.IndexOf("i9"), (cpuPc.Length - cpuPc.IndexOf("i9"))).Trim();
                    }
                    index = CpuPcReference.IndexOf(" ");
                    if (index > -1)
                    {
                        CpuPcReference = CpuPcReference.Substring(0, index);
                    }
                    CpuPcReference = CpuPcReference.Trim();
                    int.TryParse(Regex.Replace(CpuPcReference.Replace("i3", "").Replace("i5", "").Replace("i7", "").Replace("i9", ""), "[^.0-9]", "").Trim(), out CpuPcNumber);
                    CpuPcReference = CpuPcReference.Substring(0, 2);

                    if (int.Parse(CpuPcReference.Replace("i", "")) == int.Parse(CpuRequirementReference.Replace("i", "")))
                    {
                        if (CpuPcNumber >= CpuRequirementNumber)
                        {
                            Result = true;
                        }
                    }

                    if (int.Parse(CpuPcReference.Replace("i", "")) > int.Parse(CpuRequirementReference.Replace("i", "")))
                    {
                        Result = true;
                    }

                    if (CpuPcReference == "i3" && CpuRequirementReference == "i5")
                    {
                        if (CpuPcNumber >= (CpuRequirementNumber * 2))
                        {
                            Result = true;
                        }
                    }
                    if (CpuPcReference == "i3" && CpuRequirementReference == "i7")
                    {
                        if (CpuPcNumber >= (CpuRequirementNumber * 3))
                        {
                            Result = true;
                        }
                    }
                    if (CpuPcReference == "i5" && CpuRequirementReference == "i7")
                    {
                        if (CpuPcNumber >= (CpuRequirementNumber * 2))
                        {
                            Result = true;
                        }
                    }
                }
                // AMD
                else
                {

                }


                //logger.Debug($"CpuPcReference - {CpuPcReference}");
                //logger.Debug($"CpuPcNumber - {CpuPcNumber}");
            }
            catch (Exception ex)
            {
                Common.LogError(ex, "SystemChecker", $"Error on CheckCpuBetter()");
            }

            return Result;
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
