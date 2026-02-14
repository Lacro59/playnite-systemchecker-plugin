using CommonPluginsShared;
using CommonPluginsStores.Models;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using SystemChecker.Models;
using SystemChecker.Services;

namespace SystemChecker.ViewModels
{
    public class SystemCheckerGameViewModel : ObservableObject
    {
        private SystemCheckerDatabase PluginDatabase => SystemChecker.PluginDatabase;
        private Game _game;

        #region Local System Properties

        private string _localOs;
        public string LocalOs { get => _localOs; set => SetValue(ref _localOs, value); }

        private string _localCpu;
        public string LocalCpu { get => _localCpu; set => SetValue(ref _localCpu, value); }

        private string _localRamUsage;
        public string LocalRamUsage { get => _localRamUsage; set => SetValue(ref _localRamUsage, value); }

        private string _localGpu;
        public string LocalGpu { get => _localGpu; set => SetValue(ref _localGpu, value); }

        private ObservableCollection<SystemDisk> _localDisks;
        public ObservableCollection<SystemDisk> LocalDisks { get => _localDisks; set => SetValue(ref _localDisks, value); }

        #endregion

        #region Minimum Requirements Properties

        private string _minimumOs;
        public string MinimumOs { get => _minimumOs; set => SetValue(ref _minimumOs, value); }

        private ObservableCollection<string> _minimumCpu;
        public ObservableCollection<string> MinimumCpu { get => _minimumCpu; set => SetValue(ref _minimumCpu, value); }

        private string _minimumRamUsage;
        public string MinimumRamUsage { get => _minimumRamUsage; set => SetValue(ref _minimumRamUsage, value); }

        private ObservableCollection<string> _minimumGpu;
        public ObservableCollection<string> MinimumGpu { get => _minimumGpu; set => SetValue(ref _minimumGpu, value); }

        private string _minimumStorage;
        public string MinimumStorage { get => _minimumStorage; set => SetValue(ref _minimumStorage, value); }

        #endregion

        #region Recommended Requirements Properties

        private string _recommandedOs;
        public string RecommandedOs { get => _recommandedOs; set => SetValue(ref _recommandedOs, value); }

        private ObservableCollection<string> _recommandedCpu;
        public ObservableCollection<string> RecommandedCpu { get => _recommandedCpu; set => SetValue(ref _recommandedCpu, value); }

        private string _recommandedRamUsage;
        public string RecommandedRamUsage { get => _recommandedRamUsage; set => SetValue(ref _recommandedRamUsage, value); }

        private ObservableCollection<string> _recommandedGpu;
        public ObservableCollection<string> RecommandedGpu { get => _recommandedGpu; set => SetValue(ref _recommandedGpu, value); }

        private string _recommandedStorage;
        public string RecommandedStorage { get => _recommandedStorage; set => SetValue(ref _recommandedStorage, value); }

        #endregion

        #region Check Status Properties

        private string _minimumCheckOs;
        public string MinimumCheckOs { get => _minimumCheckOs; set => SetValue(ref _minimumCheckOs, value); }

        private string _minimumCheckCpu;
        public string MinimumCheckCpu { get => _minimumCheckCpu; set => SetValue(ref _minimumCheckCpu, value); }

        private string _minimumCheckRam;
        public string MinimumCheckRam { get => _minimumCheckRam; set => SetValue(ref _minimumCheckRam, value); }

        private string _minimumCheckGpu;
        public string MinimumCheckGpu { get => _minimumCheckGpu; set => SetValue(ref _minimumCheckGpu, value); }

        private string _minimumCheckStorage;
        public string MinimumCheckStorage { get => _minimumCheckStorage; set => SetValue(ref _minimumCheckStorage, value); }

        private string _recommandedCheckOs;
        public string RecommandedCheckOs { get => _recommandedCheckOs; set => SetValue(ref _recommandedCheckOs, value); }

        private string _recommandedCheckCpu;
        public string RecommandedCheckCpu { get => _recommandedCheckCpu; set => SetValue(ref _recommandedCheckCpu, value); }

        private string _recommandedCheckRam;
        public string RecommandedCheckRam { get => _recommandedCheckRam; set => SetValue(ref _recommandedCheckRam, value); }

        private string _recommandedCheckGpu;
        public string RecommandedCheckGpu { get => _recommandedCheckGpu; set => SetValue(ref _recommandedCheckGpu, value); }

        private string _recommandedCheckStorage;
        public string RecommandedCheckStorage { get => _recommandedCheckStorage; set => SetValue(ref _recommandedCheckStorage, value); }

        #endregion

        #region Source Properties

        private string _sourceLabel;
        public string SourceLabel { get => _sourceLabel; set => SetValue(ref _sourceLabel, value); }

        private string _sourceUrl;
        public string SourceUrl { get => _sourceUrl; set => SetValue(ref _sourceUrl, value); }

        #endregion

        public SystemCheckerGameViewModel(Game gameSelected)
        {
            _game = gameSelected;

            _localDisks = new ObservableCollection<SystemDisk>();
            _minimumCpu = new ObservableCollection<string>();
            _minimumGpu = new ObservableCollection<string>();
            _recommandedCpu = new ObservableCollection<string>();
            _recommandedGpu = new ObservableCollection<string>();

            LoadGameData();
        }

        private void LoadGameData()
        {
            if (_game == null)
            {
                return;
            }

            // Load local system configuration
            SystemConfiguration systemConfiguration = PluginDatabase.Database.PC;

            LocalOs = systemConfiguration.Os;
            LocalCpu = systemConfiguration.Cpu;
            LocalRamUsage = systemConfiguration.RamUsage;
            LocalGpu = systemConfiguration.GpuName;

            if (systemConfiguration.Disks != null)
            {
                LocalDisks.Clear();
                foreach (var disk in systemConfiguration.Disks)
                {
                    LocalDisks.Add(disk);
                }
            }

            // Load game requirements
            PluginGameRequirements pluginGameRequirements = PluginDatabase.Get(_game, true);

            RequirementEntry minimum = pluginGameRequirements.GetMinimum();
            RequirementEntry recommanded = pluginGameRequirements.GetRecommanded();

            // Load minimum requirements
            if (minimum.HasData)
            {
                if (minimum.Os.Count > 0)
                {
                    MinimumOs = "Windows " + string.Join(" / ", minimum.Os);
                }

                if (minimum.Cpu != null && minimum.Cpu.Count > 0)
                {
                    MinimumCpu.Clear();
                    foreach (var cpu in minimum.Cpu)
                    {
                        MinimumCpu.Add(cpu);
                    }
                }

                MinimumRamUsage = minimum.RamUsage;

                if (minimum.Gpu != null && minimum.Gpu.Count > 0)
                {
                    MinimumGpu.Clear();
                    foreach (var gpu in minimum.Gpu)
                    {
                        MinimumGpu.Add(gpu);
                    }
                }

                MinimumStorage = minimum.StorageUsage;
            }

            // Load recommended requirements
            if (recommanded.HasData)
            {
                if (recommanded.Os.Count > 0)
                {
                    RecommandedOs = "Windows " + string.Join(" / ", recommanded.Os);
                }

                if (recommanded.Cpu != null && recommanded.Cpu.Count > 0)
                {
                    RecommandedCpu.Clear();
                    foreach (var cpu in recommanded.Cpu)
                    {
                        RecommandedCpu.Add(cpu);
                    }
                }

                RecommandedRamUsage = recommanded.RamUsage;

                if (recommanded.Gpu != null && recommanded.Gpu.Count > 0)
                {
                    RecommandedGpu.Clear();
                    foreach (var gpu in recommanded.Gpu)
                    {
                        RecommandedGpu.Add(gpu);
                    }
                }

                RecommandedStorage = recommanded.StorageUsage;
            }

            // Check system configuration
            string isOk = "✓";
            string isKo = "✗";

            CheckSystem checkMinimum = SystemApi.CheckConfig(_game, minimum, systemConfiguration, _game.IsInstalled);
            if (minimum.HasData)
            {
                MinimumCheckOs = checkMinimum.CheckOs ? isOk : isKo;
                if (minimum.Os.Count == 0)
                {
                    MinimumCheckOs = string.Empty;
                }

                MinimumCheckCpu = checkMinimum.CheckCpu ? isOk : isKo;
                if (minimum.Cpu.Count == 0)
                {
                    MinimumCheckCpu = string.Empty;
                }

                MinimumCheckRam = checkMinimum.CheckRam ? isOk : isKo;
                if (minimum.Ram == 0)
                {
                    MinimumCheckRam = string.Empty;
                }

                MinimumCheckGpu = checkMinimum.CheckGpu ? isOk : isKo;
                if (minimum.Gpu.Count == 0)
                {
                    MinimumCheckGpu = string.Empty;
                }

                MinimumCheckStorage = checkMinimum.CheckStorage ? isOk : isKo;
                if (minimum.Storage == 0)
                {
                    MinimumCheckStorage = string.Empty;
                }
            }

            CheckSystem checkRecommanded = SystemApi.CheckConfig(_game, recommanded, systemConfiguration, _game.IsInstalled);
            if (recommanded.HasData)
            {
                RecommandedCheckOs = checkRecommanded.CheckOs ? isOk : isKo;
                if (recommanded.Os.Count == 0)
                {
                    RecommandedCheckOs = string.Empty;
                }

                RecommandedCheckCpu = checkRecommanded.CheckCpu ? isOk : isKo;
                if (recommanded.Cpu.Count == 0)
                {
                    RecommandedCheckCpu = string.Empty;
                }

                RecommandedCheckRam = checkRecommanded.CheckRam ? isOk : isKo;
                if (recommanded.Ram == 0)
                {
                    RecommandedCheckRam = string.Empty;
                }

                RecommandedCheckGpu = checkRecommanded.CheckGpu ? isOk : isKo;
                if (recommanded.Gpu.Count == 0)
                {
                    RecommandedCheckGpu = string.Empty;
                }

                RecommandedCheckStorage = checkRecommanded.CheckStorage ? isOk : isKo;
                if (recommanded.Storage == 0)
                {
                    RecommandedCheckStorage = string.Empty;
                }
            }

            // Logging
            Common.LogDebug(true, $"CheckMinimum: {Serialization.ToJson(checkMinimum)}");
            Common.LogDebug(true, $"CheckRecommanded: {Serialization.ToJson(checkRecommanded)}");

            // Source link
            if (pluginGameRequirements.SourcesLink != null)
            {
                SourceLabel = pluginGameRequirements.SourcesLink.GameName + " (" + pluginGameRequirements.SourcesLink.Name + ")";
                SourceUrl = pluginGameRequirements.SourcesLink.Url;
            }
        }

        public Game Game
        {
            get { return _game; }
        }
    }
}