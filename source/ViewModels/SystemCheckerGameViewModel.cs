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

        private string _recommendedOs;
        public string RecommendedOs { get => _recommendedOs; set => SetValue(ref _recommendedOs, value); }

        private ObservableCollection<string> _recommendedCpu;
        public ObservableCollection<string> RecommendedCpu { get => _recommendedCpu; set => SetValue(ref _recommendedCpu, value); }

        private string _recommendedRamUsage;
        public string RecommendedRamUsage { get => _recommendedRamUsage; set => SetValue(ref _recommendedRamUsage, value); }

        private ObservableCollection<string> _recommendedGpu;
        public ObservableCollection<string> RecommendedGpu { get => _recommendedGpu; set => SetValue(ref _recommendedGpu, value); }

        private string _recommendedStorage;
        public string RecommendedStorage { get => _recommendedStorage; set => SetValue(ref _recommendedStorage, value); }

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

        private string _recommendedCheckOs;
        public string RecommendedCheckOs { get => _recommendedCheckOs; set => SetValue(ref _recommendedCheckOs, value); }

        private string _recommendedCheckCpu;
        public string RecommendedCheckCpu { get => _recommendedCheckCpu; set => SetValue(ref _recommendedCheckCpu, value); }

        private string _recommendedCheckRam;
        public string RecommendedCheckRam { get => _recommendedCheckRam; set => SetValue(ref _recommendedCheckRam, value); }

        private string _recommendedCheckGpu;
        public string RecommendedCheckGpu { get => _recommendedCheckGpu; set => SetValue(ref _recommendedCheckGpu, value); }

        private string _recommendedCheckStorage;
        public string RecommendedCheckStorage { get => _recommendedCheckStorage; set => SetValue(ref _recommendedCheckStorage, value); }

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
            _recommendedCpu = new ObservableCollection<string>();
            _recommendedGpu = new ObservableCollection<string>();

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
            RequirementEntry recommended = pluginGameRequirements.GetRecommended();

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
            if (recommended.HasData)
            {
                if (recommended.Os.Count > 0)
                {
                    RecommendedOs = "Windows " + string.Join(" / ", recommended.Os);
                }

                if (recommended.Cpu != null && recommended.Cpu.Count > 0)
                {
                    RecommendedCpu.Clear();
                    foreach (var cpu in recommended.Cpu)
                    {
                        RecommendedCpu.Add(cpu);
                    }
                }

                RecommendedRamUsage = recommended.RamUsage;

                if (recommended.Gpu != null && recommended.Gpu.Count > 0)
                {
                    RecommendedGpu.Clear();
                    foreach (var gpu in recommended.Gpu)
                    {
                        RecommendedGpu.Add(gpu);
                    }
                }

                RecommendedStorage = recommended.StorageUsage;
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

            CheckSystem checkRecommended = SystemApi.CheckConfig(_game, recommended, systemConfiguration, _game.IsInstalled);
            if (recommended.HasData)
            {
                RecommendedCheckOs = checkRecommended.CheckOs ? isOk : isKo;
                if (recommended.Os.Count == 0)
                {
                    RecommendedCheckOs = string.Empty;
                }

                RecommendedCheckCpu = checkRecommended.CheckCpu ? isOk : isKo;
                if (recommended.Cpu.Count == 0)
                {
                    RecommendedCheckCpu = string.Empty;
                }

                RecommendedCheckRam = checkRecommended.CheckRam ? isOk : isKo;
                if (recommended.Ram == 0)
                {
                    RecommendedCheckRam = string.Empty;
                }

                RecommendedCheckGpu = checkRecommended.CheckGpu ? isOk : isKo;
                if (recommended.Gpu.Count == 0)
                {
                    RecommendedCheckGpu = string.Empty;
                }

                RecommendedCheckStorage = checkRecommended.CheckStorage ? isOk : isKo;
                if (recommended.Storage == 0)
                {
                    RecommendedCheckStorage = string.Empty;
                }
            }

            // Logging
            Common.LogDebug(true, $"CheckMinimum: {Serialization.ToJson(checkMinimum)}");
            Common.LogDebug(true, $"CheckRecommended: {Serialization.ToJson(checkRecommended)}");

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