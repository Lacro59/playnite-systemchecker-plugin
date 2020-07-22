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


        public SystemApi(string PluginUserDataPath)
        {
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


            ManagementObjectSearcher myOperativeSystemObject = new ManagementObjectSearcher("select * from Win32_OperatingSystem");
            foreach (ManagementObject obj in myOperativeSystemObject.Get())
            {
                Os = (string)obj["Caption"];
            }


            ManagementObjectSearcher myProcessorObject = new ManagementObjectSearcher("select * from Win32_Processor");
            foreach (ManagementObject obj in myProcessorObject.Get())
            {
                Cpu = (string)obj["Name"];
                CpuMaxClockSpeed = (uint)obj["MaxClockSpeed"];
            }


            ManagementObjectSearcher myVideoObject = new ManagementObjectSearcher("select * from Win32_VideoController");
            foreach (ManagementObject obj in myVideoObject.Get())
            {
                GpuName = (string)obj["Name"];
                GpuRam = (long)Convert.ToDouble(obj["AdapterRAM"]);
                CurrentHorizontalResolution = (uint)obj["CurrentHorizontalResolution"];
                CurrentVerticalResolution = (uint)obj["CurrentVerticalResolution"];
            }


            ManagementObjectSearcher myComputerSystemObject = new ManagementObjectSearcher("select * from Win32_ComputerSystem");

            foreach (ManagementObject obj in myComputerSystemObject.Get())
            {
                Ram = (long)Convert.ToDouble(obj["TotalPhysicalMemory"]);
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
                Disks.Add(new SystemDisk
                {
                    Name = d.VolumeLabel,
                    Drive = d.Name,
                    FreeSpace = d.TotalFreeSpace,
                    FreeSpaceUsage = SizeSuffix(d.TotalFreeSpace)
                });
            }
            return Disks;
        }


        public GameRequierements GetGameRequierements(Game game)
        {
            gameRequierements = new GameRequierements();
            string FileGameRequierements = PluginDirectory + "\\" + game.Id.ToString() + ".json";

            if (game.SourceId != Guid.Parse("00000000-0000-0000-0000-000000000000"))
            {
                if (File.Exists(FileGameRequierements))
                {
                    return JsonConvert.DeserializeObject<GameRequierements>(File.ReadAllText(FileGameRequierements));
                }


                switch (game.Source.Name.ToLower())
                {
                    case "steam":
                        SteamRequierements steamRequierements = new SteamRequierements(game);
                        gameRequierements = steamRequierements.GetRequirements();
                        break;
                }
            }

            File.WriteAllText(FileGameRequierements, JsonConvert.SerializeObject(gameRequierements));
            return gameRequierements;
        }


        public static CheckSystem CheckConfig(Requirement requirement, SystemConfiguration systemConfiguration)
        {
            if (requirement != null)
            {
                bool CheckOs = false;
                foreach (string Os in requirement.Os)
                {
                    //logger.Debug($"CheckOs - {systemConfiguration.Os} - {Os}");

                    if (systemConfiguration.Os.ToLower().IndexOf("10") > -1)
                    {
                        CheckOs = true;
                        break;
                    }

                    if (systemConfiguration.Os.ToLower().IndexOf(Os.ToLower()) > -1)
                    {
                        CheckOs = true;
                        break;
                    }

                    int numberOsRequirement = 0;
                    int numberOsPc = 0;
                    Int32.TryParse(Os, out numberOsRequirement);
                    Int32.TryParse(Regex.Replace(systemConfiguration.Os, "[^.0-9]", "").Trim(), out numberOsPc);
                    if (numberOsRequirement != 0 && numberOsPc != 0 && numberOsPc >= numberOsRequirement)
                    {
                        CheckOs = true;
                        break;
                    }
                }

                bool CheckCpu = false;
                if (requirement.Cpu.Count > 0)
                {
                    foreach (var cpu in requirement.Cpu)
                    {
                        // Intel familly
                        if (cpu.ToLower().IndexOf("intel") > -1)
                        {
                            //logger.Debug($"cpu intel - {cpu}");

                            // Old processor
                            if (cpu.ToLower().IndexOf("i3") == -1 & cpu.ToLower().IndexOf("i5") == -1 && cpu.ToLower().IndexOf("i7") == -1 && cpu.ToLower().IndexOf("i9") == -1)
                            {
                                CheckCpu = true;
                                break;
                            }
                        }

                        // AMD familly
                        if (cpu.ToLower().IndexOf("amd") > -1)
                        {
                            //logger.Debug($"cpu amd - {cpu}");

                            // Old processor
                            if (cpu.ToLower().IndexOf("ryzen") == -1)
                            {
                                CheckCpu = true;
                                break;
                            }
                        }

                        // Only frequency
                        if ((cpu.ToLower().IndexOf("intel") == -1 || cpu.ToLower().IndexOf("core") == -1) && cpu.ToLower().IndexOf("amd") == -1)
                        {
                            //logger.Debug($"cpu frequency - {cpu}");

                            //Quad-Core CPU 3 GHz (64 Bit)
                            int index = -1;
                            string Clock = cpu.ToLower();
                            //logger.Debug($"Clock - {Clock}");

                            // delete end string
                            index = Clock.IndexOf("ghz");
                            Clock = Clock.Substring(0, index).Trim();
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
                                if (double.Parse(Clock) * 1000 < (systemConfiguration.CpuMaxClockSpeed * 2))
                                {
                                    CheckCpu = true;
                                    break;
                                }
                            }
                            catch (Exception ex)
                            {
                                Common.LogError(ex, "SystemChecker", $"Error on find clock control - {Clock}");
                            }
                        }

                        // Recent
                        CheckCpu = CheckCpuBetter(cpu, systemConfiguration.Cpu);
                        if (CheckCpu)
                        {
                            break;
                        }
                    }
                }
                else
                {
                    CheckCpu = true;
                }

                bool CheckRam = false;
                //logger.Debug($"CheckRam - {systemConfiguration.Ram} - {requirement.Ram}");
                if (systemConfiguration.Ram >= requirement.Ram)
                {
                    CheckRam = true;
                }

                bool CheckGpu = false;
                if (requirement.Gpu.Count > 0)
                {
                    foreach (var gpu in requirement.Gpu)
                    {
                        // Other
                        if (gpu == "Pretty much any 3D graphics card")
                        {
                            CheckGpu = true;
                            break;
                        }

                        // Rezolution
                        if (gpu.ToLower().IndexOf("1280×720") > -1 || gpu.ToLower().IndexOf("1280 × 720") > -1)
                        {
                            if (systemConfiguration.CurrentHorizontalResolution >= 1280)
                            {
                                CheckGpu = true;
                                break;
                            }
                        }
                        if (gpu.ToLower().IndexOf("1368×") > -1 || gpu.ToLower().IndexOf("1368 ×") > -1)
                        {
                            if (systemConfiguration.CurrentHorizontalResolution >= 1280)
                            {
                                CheckGpu = true;
                                break;
                            }
                        }
                        if (gpu.ToLower().IndexOf("1600×") > -1 || gpu.ToLower().IndexOf("1600 ×") > -1)
                        {
                            if (systemConfiguration.CurrentHorizontalResolution >= 1280)
                            {
                                CheckGpu = true;
                                break;
                            }
                        }
                        if (gpu.ToLower().IndexOf("1920×") > -1 || gpu.ToLower().IndexOf("1920 ×") > -1)
                        {
                            if (systemConfiguration.CurrentHorizontalResolution >= 1280)
                            {
                                CheckGpu = true;
                                break;
                            }
                        }

                        // Vram
                        if (gpu.ToLower().IndexOf("vram") > -1 && gpu.ToLower().IndexOf("nvidia") == -1 && gpu.ToLower().IndexOf("amd") == -1 && gpu.ToLower().IndexOf("geforce") == -1 && gpu.ToLower().IndexOf("radeon") == -1)
                        {
                            int vram = 0;
                            int.TryParse(Regex.Replace(gpu, "[^.0-9]", "").Trim(), out vram);
                            if (vram > 0)
                            {
                                if (gpu.ToLower().IndexOf("g") > -1)
                                {
                                    vram = vram * 1024 * 1024;
                                }
                                else
                                {
                                    vram = vram * 1024;
                                }

                                if (systemConfiguration.GpuRam > vram)
                                {
                                    CheckGpu = true;
                                    break;
                                }
                            }
                        }
                        if (gpu.ToLower().IndexOf("mb") > -1 && gpu.ToLower().IndexOf("nvidia") == -1 && gpu.ToLower().IndexOf("amd") == -1 && gpu.ToLower().IndexOf("geforce") == -1 && gpu.ToLower().IndexOf("radeon") == -1)
                        {
                            int vram = 0;
                            int.TryParse(Regex.Replace(gpu, "[^.0-9]", "").Trim(), out vram);
                            if (vram > 0)
                            {
                                vram = vram * 1024;

                                if (systemConfiguration.GpuRam > vram)
                                {
                                    CheckGpu = true;
                                    break;
                                }
                            }
                        }
                        if (gpu.ToLower().IndexOf("gb") > -1 && gpu.ToLower().IndexOf("nvidia") == -1 && gpu.ToLower().IndexOf("amd") == -1 && gpu.ToLower().IndexOf("geforce") == -1 && gpu.ToLower().IndexOf("radeon") == -1)
                        {
                            int vram = 0;
                            int.TryParse(Regex.Replace(gpu, "[^.0-9]", "").Trim(), out vram);
                            if (vram > 0)
                            {
                                vram = vram * 1024 * 1024;

                                if (systemConfiguration.GpuRam > vram)
                                {
                                    CheckGpu = true;
                                    break;
                                }
                            }
                        }

                        if (gpu.ToLower().IndexOf("directx") > -1)
                        {
                            int directx = 0;
                            int.TryParse(Regex.Replace(gpu, "[^.0-9]", "").Trim(), out directx);

                            if (directx < 11)
                            {
                                CheckGpu = true;
                                break;
                            }
                        }

                            CheckGpu = CheckGpuBetter(gpu, systemConfiguration.GpuName);
                        if (CheckGpu)
                        {
                            break;
                        }
                    }
                }
                else
                {
                    CheckGpu = true;
                }

                bool CheckStorage = false;
                foreach (SystemDisk Disk in systemConfiguration.Disks)
                {
                    //logger.Debug($"CheckStorage - {Disk.FreeSpace} - {requirement.Storage}");
                    if (Disk.FreeSpace >= requirement.Storage)
                    {
                        CheckStorage = true;
                        break;
                    }
                }

                bool AllOk = (CheckOs && CheckCpu && CheckRam && CheckGpu && CheckStorage);

                return new CheckSystem
                {
                    CheckOs = CheckOs,
                    CheckCpu = CheckCpu,
                    CheckRam = CheckRam,
                    CheckGpu = CheckGpu,
                    CheckStorage = CheckStorage,
                    AllOk = AllOk
                };
            }

            return new CheckSystem();
        }

        private static bool CheckCpuBetter(string cpuRequirement, string cpuPc)
        {
            bool Result = false;

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
            CpuRequirementReference = CpuRequirementReference.Substring(0, 2);

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

                if (int.Parse(CpuPcReference.Replace("i","")) == int.Parse(CpuRequirementReference.Replace("i", "")))
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


            return Result;
        }

        private static bool CheckGpuBetter(string gpuRequirement, string gpuPc)
        {
            bool Result = false;

            gpuRequirement = gpuRequirement.ToLower();
            gpuPc = gpuPc.ToLower();


            #region Pc
            // Integrate
            bool Integrate = false;
            bool IntegrateUhd = false;
            int IntegrateNumber = 0;

            // Integrate Intel
            if (gpuPc.IndexOf("intel") > -1)
            {
                Integrate = true;
                if (gpuPc.IndexOf("uhd") > -1)
                {
                    IntegrateUhd = true;
                }
                int.TryParse(Regex.Replace(gpuPc, "[^.0-9]", "").Trim(), out IntegrateNumber);
            }

            // Integrate Amd

            //logger.Debug($"Integrate - {Integrate}");
            //logger.Debug($"IntegrateUhd - {IntegrateUhd}");
            //logger.Debug($"IntegrateNumber - {IntegrateNumber}");


            bool IsNvidia = false;
            bool IsAmd = false;
            int GraphicNumber = 0;
            if (!Integrate)
            {
                // Nvidia
                if (gpuPc.IndexOf("nvidia") > -1)
                {
                    IsNvidia = true;
                    int.TryParse(Regex.Replace(gpuPc, "[^.0-9]", "").Trim(), out GraphicNumber);
                }

                // Amd

                if (gpuPc.IndexOf("nvidia") > -1)
                {
                    IsAmd = true;
                    int.TryParse(Regex.Replace(gpuPc, "[^.0-9]", "").Trim(), out GraphicNumber);
                }
            }
            #endregion  

            // Marker 0
            // Integrate Intel
            if (gpuRequirement.IndexOf("intel") > -1)
            {
                if (!Integrate)
                {
                    logger.Debug($"SystemChecker(Marker 0) - gpuRequirement: {gpuRequirement} - gpuPc: {gpuPc}");
                    Result = true;
                }
                else
                {
                    if (IntegrateUhd)
                    {
                        if (gpuRequirement.IndexOf("uhd") == -1)
                        {
                            logger.Debug($"SystemChecker(Marker 0) - gpuRequirement: {gpuRequirement} - gpuPc: {gpuPc} - IntegrateUhd: {IntegrateUhd}");
                            Result = true;
                        }
                        else
                        {
                            int IntegrateNumberRequirement = 0;
                            int.TryParse(Regex.Replace(gpuPc, "[^.0-9]", "").Trim(), out IntegrateNumberRequirement);
                            if (IntegrateNumber >= IntegrateNumberRequirement)
                            {
                                logger.Debug($"SystemChecker(Marker 0) - gpuRequirement: {gpuRequirement} - gpuPc: {gpuPc} - IntegrateUhd: {IntegrateUhd} - IntegrateNumber: {IntegrateNumber} - IntegrateNumberRequirement:  {IntegrateNumberRequirement}");
                                Result = true;
                            }
                        }
                    }
                    else
                    {
                        if (gpuRequirement.IndexOf("uhd") == -1)
                        {
                            int IntegrateNumberRequirement = 0;
                            int.TryParse(Regex.Replace(gpuPc, "[^.0-9]", "").Trim(), out IntegrateNumberRequirement);
                            if (IntegrateNumber >= IntegrateNumberRequirement)
                            {
                                logger.Debug($"SystemChecker(Marker 0) - gpuRequirement: {gpuRequirement} - gpuPc: {gpuPc} - IntegrateUhd: {IntegrateUhd} - IntegrateNumber: {IntegrateNumber} - IntegrateNumberRequirement:  {IntegrateNumberRequirement}");
                                Result = true;
                            }
                        }
                    }
                }
            }

            // Integrate Amd

            // Old Nvidia

            // Old Amd


            // Marker 1
            if (IsNvidia && !Integrate)
            {
                // Nvidia vs Nvidia
                if (gpuRequirement.IndexOf("nvidia") > -1)
                {
                    int GraphicNumberRequierement = 0;
                    int.TryParse(Regex.Replace(gpuPc, "[^.0-9]", "").Trim(), out GraphicNumber);

                    if (GraphicNumber > GraphicNumberRequierement)
                    {
                        logger.Debug($"SystemChecker(Marker 1) - gpuRequirement: {gpuRequirement} - gpuPc: {gpuPc} - GraphicNumber: {GraphicNumber} - GraphicNumberRequierement: {GraphicNumberRequierement}");
                        Result = true;
                    }
                }

                // Nvidia vs Amd
            }

            // Marker 2
            if (IsAmd && !Integrate)
            {
                // Amd vs Amd
                if (gpuRequirement.IndexOf("Amd") > -1)
                {
                    int GraphicNumberRequierement = 0;
                    int.TryParse(Regex.Replace(gpuPc, "[^.0-9]", "").Trim(), out GraphicNumber);

                    if (GraphicNumber > GraphicNumberRequierement)
                    {
                        logger.Debug($"SystemChecker(Marker 2) - gpuRequirement: {gpuRequirement} - gpuPc: {gpuPc} - GraphicNumber: {GraphicNumber} - GraphicNumberRequierement: {GraphicNumberRequierement}");
                        Result = true;
                    }
                }

                // Nvidia vs Amd
            }


            // Marker 3
            // Integrate vs other
            if (Integrate)
            {
                // Nvidia

                // Amd
                if (gpuRequirement.IndexOf("radeon") > -1
                    && gpuRequirement.IndexOf("rx") == -1 && gpuRequirement.IndexOf("r7") == -1 && gpuRequirement.IndexOf("r9") == -1
                    && gpuRequirement.IndexOf("hd") == -1)
                {
                    if (IntegrateUhd)
                    {
                        logger.Debug($"SystemChecker(Marker 3) - gpuRequirement: {gpuRequirement} - gpuPc: {gpuPc}");
                        Result = true;
                    }
                }
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
