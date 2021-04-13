using CommonPluginsShared;
using CommonPluginsShared.Collections;
using Newtonsoft.Json;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;
using SystemChecker.Clients;
using SystemChecker.Models;

namespace SystemChecker.Services
{
    public class SystemCheckerDatabase : PluginDatabaseObject<SystemCheckerSettingsViewModel, RequierementsCollection, GameRequierements>
    {
        private PCGamingWikiRequierements pCGamingWikiRequierements;
        private SteamRequierements steamRequierements;

        public SystemCheckerDatabase(IPlayniteAPI PlayniteApi, SystemCheckerSettingsViewModel PluginSettings, string PluginUserDataPath) : base(PlayniteApi, PluginSettings, "SystemChecker", PluginUserDataPath)
        {
            pCGamingWikiRequierements = new PCGamingWikiRequierements(PlayniteApi, PluginUserDataPath);
            steamRequierements = new SteamRequierements();
        }


        protected override bool LoadDatabase()
        {
            Database = new RequierementsCollection(Paths.PluginDatabasePath);
            Database.SetGameInfo<Requirement>(PlayniteApi);

            Database.PC = GetPcInfo();

            GetPluginTags();

            return true;
        }


        public override GameRequierements Get(Guid Id, bool OnlyCache = false, bool Force = false)
        {
            GameRequierements gameRequierements = base.GetOnlyCache(Id);

            // Get from web
            if ((gameRequierements == null && !OnlyCache) || Force)
            {
                gameRequierements = GetWeb(Id);
                AddOrUpdate(gameRequierements);
            }

            if (gameRequierements == null)
            {
                Game game = PlayniteApi.Database.Games.Get(Id);
                gameRequierements = GetDefault(game);
                AddOrUpdate(gameRequierements);
            }

            return gameRequierements;
        }

        public override GameRequierements GetDefault(Game game)
        {
            GameRequierements gameRequierements = base.GetDefault(game);
            gameRequierements.Items = new List<Requirement> { new Requirement { IsMinimum = true }, new Requirement() };

            return gameRequierements;
        }

        public override GameRequierements GetWeb(Guid Id)
        {
            Game game = PlayniteApi.Database.Games.Get(Id);
            GameRequierements gameRequierements = GetDefault(game);

            string SourceName = string.Empty;

            try
            {
                SourceName = PlayniteTools.GetSourceName(PlayniteApi, game);

                // Search datas
                logger.Info($"Try find with PCGamingWikiRequierements for {game.Name}");
                gameRequierements = pCGamingWikiRequierements.GetRequirements(game);

                if (!pCGamingWikiRequierements.IsFind())
                {
                    logger.Info($"Try find with SteamRequierements for {game.Name}");
                    switch (SourceName.ToLower())
                    {
                        case "steam":
                            gameRequierements = steamRequierements.GetRequirements(game);
                            gameRequierements.Link = "https://store.steampowered.com/app/" + game.GameId;
                            break;

                        default:
                            SteamApi steamApi = new SteamApi(Paths.PluginUserDataPath);
                            int SteamID = steamApi.GetSteamId(game.Name);
                            if (SteamID != 0)
                            {
                                gameRequierements = steamRequierements.GetRequirements(game, (uint)SteamID);
                                gameRequierements.Link = "https://store.steampowered.com/app/" + SteamID;
                            }
                            break;
                    }
                }

                gameRequierements = NormalizeRecommanded(gameRequierements);
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
            }

            return gameRequierements;
        }


        #region System infos
        public SystemConfiguration GetPcInfo()
        {
            string Name = Environment.MachineName;
            string FilePlugin = Path.Combine(Paths.PluginUserDataPath, $"{CommonPluginsPlaynite.Common.Paths.GetSafeFilename(Name)}.json");

            SystemConfiguration systemConfiguration = new SystemConfiguration();
            List<SystemDisk> Disks = GetInfoDisks();

            if (File.Exists(FilePlugin))
            {
                try
                {
                    string JsonStringData = File.ReadAllText(FilePlugin);
                    systemConfiguration = JsonConvert.DeserializeObject<SystemConfiguration>(JsonStringData);
                    systemConfiguration.Disks = Disks;

                    return systemConfiguration;
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, $"Failed to load item from {FilePlugin}");
                }
            }


            string Os = string.Empty;
            string Cpu = string.Empty;
            uint CpuMaxClockSpeed = 0;
            string GpuName = string.Empty;
            long GpuRam = 0;
            uint CurrentHorizontalResolution = 0;
            uint CurrentVerticalResolution = 0;
            long Ram = 0;


            // OS
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
                Common.LogError(ex, false, "Error on Win32_OperatingSystem");
            }

            // CPU
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
                Common.LogError(ex, false, "Error on Win32_Processor");
            }

            // GPU
            try
            {
                ManagementObjectSearcher myVideoObject = new ManagementObjectSearcher("select * from Win32_VideoController");
                foreach (ManagementObject obj in myVideoObject.Get())
                {
                    string GpuNameTemp = (string)obj["Name"];

                    Common.LogDebug(true, $"GpuName: {GpuNameTemp}");

                    if (Gpu.CallIsNvidia(GpuNameTemp))
                    {
                        GpuName = (string)obj["Name"];
                        GpuRam = (long)Convert.ToDouble(obj["AdapterRAM"]);
                        CurrentHorizontalResolution = (uint)obj["CurrentHorizontalResolution"];
                        CurrentVerticalResolution = (uint)obj["CurrentVerticalResolution"];
                        break;
                    }
                    if (Gpu.CallIsAmd(GpuNameTemp))
                    {
                        GpuName = (string)obj["Name"];
                        GpuRam = (long)Convert.ToDouble(obj["AdapterRAM"]);
                        CurrentHorizontalResolution = (uint)obj["CurrentHorizontalResolution"];
                        CurrentVerticalResolution = (uint)obj["CurrentVerticalResolution"];
                        break;
                    }
                    if (Gpu.CallIsIntel(GpuNameTemp))
                    {
                        GpuName = (string)obj["Name"];
                        GpuRam = (long)Convert.ToDouble(obj["AdapterRAM"]);
                        CurrentHorizontalResolution = (uint)obj["CurrentHorizontalResolution"];
                        CurrentVerticalResolution = (uint)obj["CurrentVerticalResolution"];
                    }
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, "Error on Win32_VideoController");
            }

            // RAM
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
                Common.LogError(ex, false, "Error on Win32_ComputerSystem");
            }


            systemConfiguration.Name = Name.Trim();
            systemConfiguration.Os = Os.Trim();
            systemConfiguration.Cpu = Cpu.Trim();
            systemConfiguration.CpuMaxClockSpeed = CpuMaxClockSpeed;
            systemConfiguration.GpuName = GpuName.Trim();
            systemConfiguration.GpuRam = GpuRam;
            systemConfiguration.CurrentHorizontalResolution = CurrentHorizontalResolution;
            systemConfiguration.CurrentVerticalResolution = CurrentVerticalResolution;
            systemConfiguration.Ram = Ram;
            systemConfiguration.RamUsage = Tools.SizeSuffix(Ram, true);
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
                if (d.DriveType == DriveType.Fixed)
                {
                    string VolumeLabel = string.Empty;
                    try
                    {
                        VolumeLabel = d.VolumeLabel;
                    }
                    catch (Exception ex)
                    {
                        logger.Warn($"Error on VolumeLabel - {ex.Message.Trim()}");
                    }

                    string Name = string.Empty;
                    try
                    {
                        Name = d.Name;
                    }
                    catch (Exception ex)
                    {
                        logger.Warn($"Error on Name - {ex.Message.Trim()}");
                    }

                    long FreeSpace = 0;
                    try
                    {
                        FreeSpace = d.TotalFreeSpace;
                    }
                    catch (Exception ex)
                    {
                        logger.Warn($"Error on TotalFreeSpace - {ex.Message.Trim()}");
                    }

                    string FreeSpaceUsage = string.Empty;
                    try
                    {
                        FreeSpaceUsage = Tools.SizeSuffix(d.TotalFreeSpace);
                    }
                    catch (Exception ex)
                    {
                        logger.Warn($"Error on FreeSpaceUsage - {ex.Message.Trim()}");
                    }

                    Disks.Add(new SystemDisk
                    {
                        Name = VolumeLabel,
                        Drive = Name,
                        FreeSpace = FreeSpace,
                        FreeSpaceUsage = FreeSpaceUsage
                    });
                }
            }
            return Disks;
        }

        public void RefreshPcInfo()
        {
            string Name = Environment.MachineName;
            string FilePlugin = Path.Combine(Paths.PluginUserDataPath, $"{CommonPluginsPlaynite.Common.Paths.GetSafeFilename(Name)}.json");

            CommonPluginsPlaynite.Common.FileSystem.DeleteFileSafe(FilePlugin);
            Database.PC = GetPcInfo();
            Database.OnCollectionChanged(null, null);
        }
        #endregion


        private GameRequierements NormalizeRecommanded(GameRequierements gameRequierements)
        {
            Requirement Minimum = gameRequierements.GetMinimum();
            Requirement Recommanded = gameRequierements.GetRecommanded();

            if (Minimum.HasData && Recommanded.HasData)
            {
                if (Recommanded.Os.Count == 0)
                {
                    Recommanded.Os = Minimum.Os;
                }
                if (Recommanded.Cpu.Count == 0)
                {
                    Recommanded.Cpu = Minimum.Cpu;
                }
                if (Recommanded.Gpu.Count == 0)
                {
                    Recommanded.Gpu = Minimum.Gpu;
                }
                if (Recommanded.Ram == 0)
                {
                    Recommanded.Ram = Minimum.Ram;
                    Recommanded.RamUsage = Minimum.RamUsage;
                }
                if (Recommanded.Storage == 0)
                {
                    Recommanded.Storage = Minimum.Storage;
                    Recommanded.StorageUsage = Minimum.StorageUsage;
                }
            }

            gameRequierements.Items = new List<Requirement> { Minimum, Recommanded };

            return gameRequierements;
        }


        public override void SetThemesResources(Game game)
        {
            GameRequierements gameRequierements = Get(game, true);

            SystemConfiguration systemConfiguration = Database.PC;
            Requirement systemMinimum = gameRequierements.GetMinimum();
            Requirement systemRecommanded = gameRequierements.GetRecommanded();

            CheckSystem CheckMinimum = CheckMinimum = SystemApi.CheckConfig(systemMinimum, systemConfiguration);
            CheckSystem CheckRecommanded = SystemApi.CheckConfig(systemRecommanded, systemConfiguration);


            PluginSettings.Settings.HasData = gameRequierements.HasData;
            PluginSettings.Settings.IsMinimumOK = false;
            PluginSettings.Settings.IsRecommandedOK = false;
            PluginSettings.Settings.IsAllOK = false;

            if (systemMinimum.HasData)
            {
                PluginSettings.Settings.IsMinimumOK = (bool)CheckMinimum.AllOk;
                PluginSettings.Settings.IsAllOK = (bool)CheckMinimum.AllOk;
            }

            if (systemRecommanded.HasData && (bool)CheckRecommanded.AllOk)
            {
                PluginSettings.Settings.IsRecommandedOK = (bool)CheckRecommanded.AllOk;
                PluginSettings.Settings.IsAllOK = (bool)CheckRecommanded.AllOk;
            }
        }

        public override void Games_ItemUpdated(object sender, ItemUpdatedEventArgs<Game> e)
        {
            foreach (var GameUpdated in e.UpdatedItems)
            {
                Database.SetGameInfo<Requirement>(PlayniteApi, GameUpdated.NewData.Id);
            }
        }
    }
}
