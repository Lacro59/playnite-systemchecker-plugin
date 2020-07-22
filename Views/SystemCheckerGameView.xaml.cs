using Newtonsoft.Json;
using Playnite.Controls;
using Playnite.SDK;
using Playnite.SDK.Models;
using PluginCommon;
using System.Collections.Generic;
using SystemChecker.Clients;
using SystemChecker.Models;

namespace SystemChecker.Views
{
    /// <summary>
    /// Logique d'interaction pour SystemCheckerGameView.xaml
    /// </summary>
    public partial class SystemCheckerGameView : WindowBase
    {
        private static readonly ILogger logger = LogManager.GetLogger();


        public string LocalOs { get; set; }
        public string LocalCpu { get; set; }
        public string LocalRamUsage { get; set; }
        public string LocalGpu { get; set; }

        public string MinimumOs { get; set; }
        public List<string> MinimumCpu { get; set; }
        public string MinimumRamUsage { get; set; }
        public List<string> MinimumGpu { get; set; }
        public string MinimumStorage { get; set; }
        
        public string RecommandedOs { get; set; }
        public List<string> RecommandedCpu { get; set; }
        public string RecommandedRamUsage { get; set; }
        public List<string> RecommandedGpu { get; set; }
        public string RecommandedStorage { get; set; }

        public string MinimumCheckOs { get; set; }
        public string MinimumCheckCpu { get; set; }
        public string MinimumCheckRam { get; set; }
        public string MinimumCheckGpu { get; set; }
        public string MinimumCheckStorage { get; set; }

        public string RecommandedCheckOs { get; set; }
        public string RecommandedCheckCpu { get; set; }
        public string RecommandedCheckRam { get; set; }
        public string RecommandedCheckGpu { get; set; }
        public string RecommandedCheckStorage { get; set; }

        public SystemCheckerGameView(string PluginUserDataPath, Game GameSelected)
        {
            InitializeComponent();


            // Local
            SystemApi systemApi = new SystemApi(PluginUserDataPath);
            SystemConfiguration systemConfiguration = systemApi.GetInfo();

            LocalOs = systemConfiguration.Os;
            LocalCpu = systemConfiguration.Cpu;
            LocalRamUsage = systemConfiguration.RamUsage;
            LocalGpu = systemConfiguration.GpuName;
            LocalDisks.ItemsSource = systemConfiguration.Disks;


            // Minimum & Recommanded
            GameRequierements gameRequierements = systemApi.GetGameRequierements(GameSelected);

            if (gameRequierements.Minimum != null && gameRequierements.Minimum.Ram != 0)
            {
                MinimumOs = "Windows " + string.Join(" / ", gameRequierements.Minimum.Os);
                MinimumCpu = gameRequierements.Minimum.Cpu;
                MinimumRamUsage = gameRequierements.Minimum.RamUsage;
                MinimumGpu = gameRequierements.Minimum.Gpu;
                MinimumStorage = gameRequierements.Minimum.StorageUsage;
            }
            if (gameRequierements.Recommanded != null && gameRequierements.Recommanded.Ram != 0)
            {
                RecommandedOs = "Windows " + string.Join(" / ", gameRequierements.Recommanded.Os);
                RecommandedCpu = gameRequierements.Recommanded.Cpu;
                RecommandedRamUsage = gameRequierements.Recommanded.RamUsage;
                RecommandedGpu = gameRequierements.Recommanded.Gpu;
                RecommandedStorage = gameRequierements.Recommanded.StorageUsage;
            }


            // Check config
            string IsOk = "";
            string IsKo = "";

            CheckSystem CheckMinimum = SystemApi.CheckConfig(gameRequierements.Minimum, systemConfiguration);
            if (gameRequierements.Minimum != null)
            {
                MinimumCheckOs = IsKo;
                if (CheckMinimum.CheckOs)
                {
                    MinimumCheckOs = IsOk;
                }

                MinimumCheckCpu = IsKo;
                if (CheckMinimum.CheckCpu)
                {
                    MinimumCheckCpu = IsOk;
                }

                MinimumCheckRam = IsKo;
                if (CheckMinimum.CheckRam)
                {
                    MinimumCheckRam = IsOk;
                }

                MinimumCheckGpu = IsKo;
                if (CheckMinimum.CheckGpu)
                {
                    MinimumCheckGpu = IsOk;
                }

                MinimumCheckStorage = IsKo;
                if (CheckMinimum.CheckStorage)
                {
                    MinimumCheckStorage = IsOk;
                }
            }

            CheckSystem CheckRecommanded = SystemApi.CheckConfig(gameRequierements.Recommanded, systemConfiguration);
            if (gameRequierements.Recommanded != null)
            {
                RecommandedCheckOs = IsKo;
                if (CheckRecommanded.CheckOs)
                {
                    RecommandedCheckOs = IsOk;
                }

                RecommandedCheckCpu = IsKo;
                if (CheckRecommanded.CheckCpu)
                {
                    RecommandedCheckCpu = IsOk;
                }

                RecommandedCheckRam = IsKo;
                if (CheckRecommanded.CheckRam)
                {
                    RecommandedCheckRam = IsOk;
                }

                RecommandedCheckGpu = IsKo;
                if (CheckRecommanded.CheckGpu)
                {
                    RecommandedCheckGpu = IsOk;
                }

                RecommandedCheckStorage = IsKo;
                if (CheckRecommanded.CheckStorage)
                {
                    RecommandedCheckStorage = IsOk;
                }
            }

            logger.Debug("CheckMinimum" + JsonConvert.SerializeObject(CheckMinimum));
            logger.Debug("CheckRecommanded" + JsonConvert.SerializeObject(CheckRecommanded));

            DataContext = this;
        }

        private void Grid_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            Tools.DesactivePlayniteWindowControl(this);
        }
    }
}
