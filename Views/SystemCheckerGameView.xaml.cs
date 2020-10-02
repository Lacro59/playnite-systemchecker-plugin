using Newtonsoft.Json;
using Playnite.SDK;
using Playnite.SDK.Models;
using PluginCommon;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SystemChecker.Clients;
using SystemChecker.Models;

namespace SystemChecker.Views
{
    /// <summary>
    /// Logique d'interaction pour SystemCheckerGameView.xaml
    /// </summary>
    public partial class SystemCheckerGameView : Window
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private static IResourceProvider resources = new ResourceProvider();
        private IPlayniteAPI PlayniteApi;


        public string ScSourceName { get; set; }

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

        public SystemCheckerGameView(string PluginUserDataPath, Game GameSelected, IPlayniteAPI PlayniteApi)
        {
            this.PlayniteApi = PlayniteApi;

            InitializeComponent();

            this.PreviewKeyDown += new KeyEventHandler(HandleEsc);

            // Local
            SystemApi systemApi = new SystemApi(PluginUserDataPath, PlayniteApi);
            SystemConfiguration systemConfiguration = systemApi.GetInfo();

            LocalOs = systemConfiguration.Os;
            LocalCpu = systemConfiguration.Cpu;
            LocalRamUsage = systemConfiguration.RamUsage;
            LocalGpu = systemConfiguration.GpuName;
            LocalDisks.ItemsSource = systemConfiguration.Disks;


            // Minimum & Recommanded
            GameRequierements gameRequierements = systemApi.GetGameRequierements(GameSelected);

            if (gameRequierements.Minimum != null && gameRequierements.Minimum.Os.Count != 0)
            {
                MinimumOs = "Windows " + string.Join(" / ", gameRequierements.Minimum.Os);
                MinimumCpu = gameRequierements.Minimum.Cpu;
                MinimumRamUsage = gameRequierements.Minimum.RamUsage;
                MinimumGpu = gameRequierements.Minimum.Gpu;
                MinimumStorage = gameRequierements.Minimum.StorageUsage;
            }
            if (gameRequierements.Recommanded != null && gameRequierements.Recommanded.Os.Count != 0)
            {
                RecommandedOs = "Windows " + string.Join(" / ", gameRequierements.Recommanded.Os);
                RecommandedCpu = gameRequierements.Recommanded.Cpu;
                RecommandedRamUsage = gameRequierements.Recommanded.RamUsage;
                RecommandedGpu = gameRequierements.Recommanded.Gpu;
                RecommandedStorage = gameRequierements.Recommanded.StorageUsage;
            }


            // Check config
            string IsOk = "";
            string IsKo = "";

            CheckSystem CheckMinimum = SystemApi.CheckConfig(gameRequierements.Minimum, systemConfiguration);
            if (gameRequierements.Minimum != null && gameRequierements.Minimum.Os.Count != 0)
            {
                MinimumCheckOs = IsKo;
                if (CheckMinimum.CheckOs)
                {
                    MinimumCheckOs = IsOk;
                }
                if (gameRequierements.Minimum.Os.Count == 0)
                {
                    MinimumCheckOs = string.Empty;
                }

                MinimumCheckCpu = IsKo;
                if (CheckMinimum.CheckCpu)
                {
                    MinimumCheckCpu = IsOk;
                }
                if (gameRequierements.Minimum.Cpu.Count == 0)
                {
                    MinimumCheckCpu = string.Empty;
                }

                MinimumCheckRam = IsKo;
                if (CheckMinimum.CheckRam)
                {
                    MinimumCheckRam = IsOk;
                }
                if (gameRequierements.Minimum.Ram == 0)
                {
                    MinimumCheckRam = string.Empty;
                }

                MinimumCheckGpu = IsKo;
                if (CheckMinimum.CheckGpu)
                {
                    MinimumCheckGpu = IsOk;
                }
                if (gameRequierements.Minimum.Gpu.Count == 0)
                {
                    MinimumCheckGpu = string.Empty;
                }

                MinimumCheckStorage = IsKo;
                if (CheckMinimum.CheckStorage)
                {
                    MinimumCheckStorage = IsOk;
                }
                if (gameRequierements.Minimum.Storage == 0)
                {
                    MinimumCheckStorage = string.Empty;
                }
            }

            CheckSystem CheckRecommanded = SystemApi.CheckConfig(gameRequierements.Recommanded, systemConfiguration);
            if (gameRequierements.Recommanded != null && gameRequierements.Recommanded.Os.Count != 0)
            {
                RecommandedCheckOs = IsKo;
                if (CheckRecommanded.CheckOs)
                {
                    RecommandedCheckOs = IsOk;
                }
                if (gameRequierements.Recommanded.Os.Count == 0)
                {
                    RecommandedCheckOs = string.Empty;
                }

                RecommandedCheckCpu = IsKo;
                if (CheckRecommanded.CheckCpu)
                {
                    RecommandedCheckCpu = IsOk;
                }
                if (gameRequierements.Recommanded.Cpu.Count == 0)
                {
                    RecommandedCheckCpu = string.Empty;
                }

                RecommandedCheckRam = IsKo;
                if (CheckRecommanded.CheckRam)
                {
                    RecommandedCheckRam = IsOk;
                }
                if (gameRequierements.Recommanded.Ram == 0)
                {
                    RecommandedCheckRam = string.Empty;
                }

                RecommandedCheckGpu = IsKo;
                if (CheckRecommanded.CheckGpu)
                {
                    RecommandedCheckGpu = IsOk;
                }
                if (gameRequierements.Recommanded.Gpu.Count == 0)
                {
                    RecommandedCheckGpu = string.Empty;
                }

                RecommandedCheckStorage = IsKo;
                if (CheckRecommanded.CheckStorage)
                {
                    RecommandedCheckStorage = IsOk;
                }
                if (gameRequierements.Recommanded.Storage == 0)
                {
                    RecommandedCheckStorage = string.Empty;
                }
            }

            btLink.Visibility = System.Windows.Visibility.Hidden;
            if (gameRequierements.Minimum != null || gameRequierements.Recommanded != null) 
            {
                btLink.Visibility = System.Windows.Visibility.Visible;
                btLink.Tag = gameRequierements.Link;
            }

#if DEBUG
            logger.Debug("CheckMinimum" + JsonConvert.SerializeObject(CheckMinimum));
            logger.Debug("CheckRecommanded" + JsonConvert.SerializeObject(CheckRecommanded));
#endif

            ScSourceName = resources.GetString("LOCSourceLabel") + ": " + GameSelected.Name;

            DataContext = this;
        }

        private void Grid_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            Tools.DesactivePlayniteWindowControl(this);
        }

        private void HandleEsc(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
        }

        private void Button_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Button bt = (Button)sender;

            if (!((string)bt.Tag).IsNullOrEmpty())
            {
                Process.Start((string)bt.Tag);
            }
        }
    }
}
