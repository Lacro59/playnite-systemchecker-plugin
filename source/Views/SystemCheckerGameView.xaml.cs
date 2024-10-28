using CommonPluginsShared;
using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Documents;
using SystemChecker.Clients;
using SystemChecker.Models;
using SystemChecker.Services;

namespace SystemChecker.Views
{
    /// <summary>
    /// Logique d'interaction pour SystemCheckerGameView.xaml
    /// </summary>
    public partial class SystemCheckerGameView : UserControl
    {
        private SystemCheckerDatabase PluginDatabase => SystemChecker.PluginDatabase;

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


        public SystemCheckerGameView(Game gameSelected)
        {
            InitializeComponent();

            // Local
            SystemConfiguration systemConfiguration = PluginDatabase.Database.PC;

            LocalOs = systemConfiguration.Os;
            LocalCpu = systemConfiguration.Cpu;
            LocalRamUsage = systemConfiguration.RamUsage;
            LocalGpu = systemConfiguration.GpuName;
            LocalDisks.ItemsSource = systemConfiguration.Disks;


            // Minimum & Recommanded
            GameRequierements gameRequierements = PluginDatabase.Get(gameSelected, true);

            Requirement Minimum = gameRequierements.GetMinimum();
            Requirement Recommanded = gameRequierements.GetRecommanded();

            if (Minimum.HasData)
            {
                if (Minimum.Os.Count > 0)
                {
                    MinimumOs = "Windows " + string.Join(" / ", Minimum.Os);
                }

                MinimumCpu = Minimum.Cpu;
                MinimumRamUsage = Minimum.RamUsage;
                MinimumGpu = Minimum.Gpu;
                MinimumStorage = Minimum.StorageUsage;
            }

            if (Recommanded.HasData)
            {
                if (Recommanded.Os.Count > 0)
                {
                    RecommandedOs = "Windows " + string.Join(" / ", Recommanded.Os);
                }

                RecommandedCpu = Recommanded.Cpu;
                RecommandedRamUsage = Recommanded.RamUsage;
                RecommandedGpu = Recommanded.Gpu;
                RecommandedStorage = Recommanded.StorageUsage;
            }


            // Check config
            string IsOk = "";
            string IsKo = "";

            CheckSystem CheckMinimum = SystemApi.CheckConfig(gameSelected, Minimum, systemConfiguration, gameSelected.IsInstalled);
            if (Minimum.HasData)
            {
                MinimumCheckOs = IsKo;
                if (CheckMinimum.CheckOs)
                {
                    MinimumCheckOs = IsOk;
                }
                if (Minimum.Os.Count == 0)
                {
                    MinimumCheckOs = string.Empty;
                }

                MinimumCheckCpu = IsKo;
                if (CheckMinimum.CheckCpu)
                {
                    MinimumCheckCpu = IsOk;
                }
                if (Minimum.Cpu.Count == 0)
                {
                    MinimumCheckCpu = string.Empty;
                }

                MinimumCheckRam = IsKo;
                if (CheckMinimum.CheckRam)
                {
                    MinimumCheckRam = IsOk;
                }
                if (Minimum.Ram == 0)
                {
                    MinimumCheckRam = string.Empty;
                }

                MinimumCheckGpu = IsKo;
                if (CheckMinimum.CheckGpu)
                {
                    MinimumCheckGpu = IsOk;
                }
                if (Minimum.Gpu.Count == 0)
                {
                    MinimumCheckGpu = string.Empty;
                }

                MinimumCheckStorage = IsKo;
                if (CheckMinimum.CheckStorage)
                {
                    MinimumCheckStorage = IsOk;
                }
                if (Minimum.Storage == 0)
                {
                    MinimumCheckStorage = string.Empty;
                }
            }

            CheckSystem CheckRecommanded = SystemApi.CheckConfig(gameSelected, Recommanded, systemConfiguration, gameSelected.IsInstalled);
            if (Recommanded.HasData)
            {
                RecommandedCheckOs = IsKo;
                if (CheckRecommanded.CheckOs)
                {
                    RecommandedCheckOs = IsOk;
                }
                if (Recommanded.Os.Count == 0)
                {
                    RecommandedCheckOs = string.Empty;
                }

                RecommandedCheckCpu = IsKo;
                if (CheckRecommanded.CheckCpu)
                {
                    RecommandedCheckCpu = IsOk;
                }
                if (Recommanded.Cpu.Count == 0)
                {
                    RecommandedCheckCpu = string.Empty;
                }

                RecommandedCheckRam = IsKo;
                if (CheckRecommanded.CheckRam)
                {
                    RecommandedCheckRam = IsOk;
                }
                if (Recommanded.Ram == 0)
                {
                    RecommandedCheckRam = string.Empty;
                }

                RecommandedCheckGpu = IsKo;
                if (CheckRecommanded.CheckGpu)
                {
                    RecommandedCheckGpu = IsOk;
                }
                if (Recommanded.Gpu.Count == 0)
                {
                    RecommandedCheckGpu = string.Empty;
                }

                RecommandedCheckStorage = IsKo;
                if (CheckRecommanded.CheckStorage)
                {
                    RecommandedCheckStorage = IsOk;
                }
                if (Recommanded.Storage == 0)
                {
                    RecommandedCheckStorage = string.Empty;
                }
            }



            Common.LogDebug(true, $"CheckMinimum" + Serialization.ToJson(CheckMinimum));
            Common.LogDebug(true, $"CheckRecommanded" + Serialization.ToJson(CheckRecommanded));


            if (gameRequierements.SourcesLink != null)
            {
                PART_SourceLabel.Text = gameRequierements.SourcesLink.GameName + " (" + gameRequierements.SourcesLink.Name + ")";
                PART_SourceLink.Tag = gameRequierements.SourcesLink.Url;
            }


            DataContext = this;
        }


        private void PART_SourceLink_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (((Hyperlink)sender).Tag is string)
            {
                if (!((string)((Hyperlink)sender).Tag).IsNullOrEmpty())
                {
                    Process.Start((string)((Hyperlink)sender).Tag);
                }
            }
        }
    }
}
