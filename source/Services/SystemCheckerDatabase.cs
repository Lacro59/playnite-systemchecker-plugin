using CommonPluginsShared;
using CommonPluginsShared.Collections;
using CommonPluginsShared.SystemInfo;
using CommonPluginsShared.Utilities;
using CommonPluginsStores.Models;
using CommonPluginsStores.Steam;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SystemChecker.Clients;
using SystemChecker.Models;

namespace SystemChecker.Services
{
    public class SystemCheckerDatabase : PluginDatabaseObject<SystemCheckerSettingsViewModel, RequirementsCollection, PluginGameRequirements, RequirementEntry>
    {
        public SystemConfigurationManager SystemConfigurationManager;

        private PCGamingWikiRequirements PCGamingWikiRequirements { get; set; }
        private SteamRequirements SteamRequirements { get; set; }

        public SystemCheckerDatabase(SystemCheckerSettingsViewModel pluginSettings, string pluginUserDataPath) : base(pluginSettings, "SystemChecker", pluginUserDataPath)
        {
            TagBefore = "[SC]";
            WindowPluginService = new WindowPluginService(PluginName, this);
        }

        protected override void LoadMoreData()
        {
            try
            {
                Logger.Info("LoadMoreData started");

                PCGamingWikiRequirements = new PCGamingWikiRequirements();
                SteamRequirements = new SteamRequirements();

                SystemConfigurationManager = new SystemConfigurationManager(Path.Combine(Paths.PluginUserDataPath, "Configurations.json"));
                _database.PC = SystemConfigurationManager.GetSystemConfiguration();

                Logger.Info($"LoadMoreData completed");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error in LoadMoreData");
                throw; // Re-throw to make LoadDatabase fail
            }
        }


        public override PluginGameRequirements Get(Guid id, bool onlyCache = false, bool force = false)
        {
            PluginGameRequirements pluginGameRequirements = base.GetOnlyCache(id);

            // Get from web
            if ((pluginGameRequirements == null && !onlyCache) || force)
            {
                pluginGameRequirements = GetWeb(id);
                AddOrUpdate(pluginGameRequirements);
            }

            if (pluginGameRequirements == null)
            {
                Game game = API.Instance.Database.Games.Get(id);
                if (game != null)
                {
                    pluginGameRequirements = GetDefault(game);
                    AddOrUpdate(pluginGameRequirements);
                }
            }

            return pluginGameRequirements;
        }

        public override PluginGameRequirements GetDefault(Game game)
        {
            PluginGameRequirements pluginGameRequirements = base.GetDefault(game);
            pluginGameRequirements.Items = new List<RequirementEntry> { new RequirementEntry { IsMinimum = true }, new RequirementEntry() };

            return pluginGameRequirements;
        }

        public override PluginGameRequirements GetWeb(Guid id)
        {
            Game game = API.Instance.Database.Games.Get(id);
            PluginGameRequirements pluginGameRequirements = GetDefault(game);

            try
            {
                string sourceName = PlayniteTools.GetSourceName(game);

                // Search datas
                Logger.Info($"Try find with PCGamingWikiRequirements for {game.Name}");
                pluginGameRequirements = PCGamingWikiRequirements.GetRequirements(game);

                if (!PCGamingWikiRequirements.IsFind())
                {
                    Logger.Info($"Try find with SteamRequirements for {game.Name}");
                    switch (sourceName.ToLower())
                    {
                        case "steam":
                            pluginGameRequirements = SteamRequirements.GetRequirements(game);
                            break;

                        default:
                            SteamApi steamApi = new SteamApi(PluginName, CommonPluginsShared.PlayniteTools.ExternalPlugin.SystemChecker);
                            uint steamID = steamApi.GetAppId(game);
                            if (steamID != 0)
                            {
                                pluginGameRequirements = SteamRequirements.GetRequirements(game, steamID);
                            }
                            break;
                    }
                }

                pluginGameRequirements = NormalizeRecommended(pluginGameRequirements);
                pluginGameRequirements = PurgeGraphicsCardData(pluginGameRequirements);
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, "SystemChecker");
            }

            return pluginGameRequirements;
        }

        private PluginGameRequirements NormalizeRecommended(PluginGameRequirements pluginGameRequirements)
        {
            RequirementEntry Minimum = pluginGameRequirements.GetMinimum();
            RequirementEntry Recommended = pluginGameRequirements.GetRecommended();

            if (Minimum.HasData && Recommended.HasData)
            {
                if (Recommended.Os.Count == 0)
                {
                    Recommended.Os = Minimum.Os;
                }
                if (Recommended.Cpu.Count == 0)
                {
                    Recommended.Cpu = Minimum.Cpu;
                }
                if (Recommended.Gpu.Count == 0)
                {
                    Recommended.Gpu = Minimum.Gpu;
                }
                if (Recommended.Ram == 0)
                {
                    Recommended.Ram = Minimum.Ram;
                }
                if (Recommended.Storage == 0)
                {
                    Recommended.Storage = Minimum.Storage;
                }
            }

            pluginGameRequirements.Items = new List<RequirementEntry> { Minimum, Recommended };
            return pluginGameRequirements;
        }

        #region Tag

        public override void AddTag(Game game)
        {
            PluginGameRequirements item = Get(game, true);
            if (item.HasData)
            {
                try
                {
                    SystemConfiguration systemConfiguration = Database.PC;
                    RequirementEntry systemMinimum = item.GetMinimum();
                    RequirementEntry systemRecommended = item.GetRecommended();

                    CheckSystem checkMinimum = SystemApi.CheckConfig(game, systemMinimum, systemConfiguration, game.IsInstalled);
                    CheckSystem checkRecommended = SystemApi.CheckConfig(game, systemRecommended, systemConfiguration, game.IsInstalled);

                    if (!(checkMinimum.AllOk ?? false) && !(checkRecommended.AllOk ?? false))
                    {
                        return;
                    }

                    Guid? tagId = null;
                    // Minimum
                    if (checkMinimum.AllOk ?? false)
                    {
                        tagId = CheckTagExist($"{ResourceProvider.GetString("LOCSystemCheckerConfigMinimum")}");
                    }
                    // Recommended
                    if (checkRecommended.AllOk ?? false)
                    {
                        tagId = CheckTagExist($"{ResourceProvider.GetString("LOCSystemCheckerConfigRecommended")}");
                    }

                    if (tagId != null)
                    {
                        if (game.TagIds == null)
                        {
                            game.TagIds = new List<Guid> { tagId.Value };
                        }
                        else if (!game.TagIds.Contains(tagId.Value))
                        {
                            game.TagIds.Add(tagId.Value);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, $"Tag insert error with {game.Name}", true, PluginName, string.Format(ResourceProvider.GetString("LOCCommonNotificationTagError"), game.Name));
                    return;
                }
            }
            else if (TagMissing)
            {
                Guid? noDataTagId = AddNoDataTag();
                if (noDataTagId != null)
                {
                    if (game.TagIds == null)
                    {
                        game.TagIds = new List<Guid> { noDataTagId.Value };
                    }
                    else if (!game.TagIds.Contains(noDataTagId.Value))
                    {
                        game.TagIds.Add(noDataTagId.Value);
                    }
                }
            }

            API.Instance.Database.Games.BeginBufferUpdate();
            try
            {
                API.Instance.MainView.UIDispatcher?.Invoke(() =>
                {
                    API.Instance.Database.Games.Update(game);
                    game.OnPropertyChanged();
                });
            }
            finally
            {
                API.Instance.Database.Games.EndBufferUpdate();
            }
        }

        #endregion

        public override void SetThemesResources(Game game)
        {
            PluginGameRequirements pluginGameRequirements = Get(game, true);

            if (pluginGameRequirements == null)
            {
                PluginSettings.Settings.HasData = false;
                PluginSettings.Settings.IsMinimumOK = false;
                PluginSettings.Settings.IsRecommendedOK = false;
                PluginSettings.Settings.IsAllOK = false;
                PluginSettings.Settings.RecommendedStorage = string.Empty;

                return;
            }

            SystemConfiguration systemConfiguration = Database.PC;
            RequirementEntry systemMinimum = pluginGameRequirements.GetMinimum();
            RequirementEntry systemRecommended = pluginGameRequirements.GetRecommended();

            CheckSystem checkMinimum = SystemApi.CheckConfig(game, systemMinimum, systemConfiguration, game.IsInstalled);
            CheckSystem checkRecommended = SystemApi.CheckConfig(game, systemRecommended, systemConfiguration, game.IsInstalled);


            PluginSettings.Settings.HasData = pluginGameRequirements.HasData;
            PluginSettings.Settings.IsMinimumOK = false;
            PluginSettings.Settings.IsRecommendedOK = false;
            PluginSettings.Settings.IsAllOK = false;

            if (systemMinimum.HasData)
            {
                PluginSettings.Settings.IsMinimumOK = checkMinimum.AllOk ?? false;
                PluginSettings.Settings.IsAllOK = checkMinimum.AllOk ?? false;

                PluginSettings.Settings.RecommendedStorage = systemMinimum.Storage != 0 ? UtilityTools.SizeSuffix(systemMinimum.Storage) : string.Empty;
            }

            if (systemRecommended.HasData && (checkRecommended.AllOk ?? false))
            {
                PluginSettings.Settings.IsRecommendedOK = checkRecommended.AllOk ?? false;
                PluginSettings.Settings.IsAllOK = checkRecommended.AllOk ?? false;

                PluginSettings.Settings.RecommendedStorage = systemRecommended.Storage != 0 ? UtilityTools.SizeSuffix(systemRecommended.Storage) : string.Empty;
            }
        }

        public PluginGameRequirements PurgeGraphicsCardData(PluginGameRequirements pluginGameRequirements)
        {
            RequirementEntry Minimum = pluginGameRequirements.GetMinimum();
            RequirementEntry Recommended = pluginGameRequirements.GetRecommended();

            if (Minimum.HasData && Minimum.Gpu.Count > 1 && Minimum.Gpu.FindAll(x => Gpu.CallIsNvidia(x) || Gpu.CallIsAmd(x) || Gpu.CallIsIntel(x)).Count > 0)
            {
                Minimum.Gpu = Minimum.Gpu.FindAll(x => Gpu.CallIsNvidia(x) || Gpu.CallIsAmd(x) || Gpu.CallIsIntel(x)).ToList();
            }

            if (Recommended.HasData && Recommended.Gpu.Count > 1 && Recommended.Gpu.FindAll(x => Gpu.CallIsNvidia(x) || Gpu.CallIsAmd(x) || Gpu.CallIsIntel(x)).Count > 0)
            {
                Recommended.Gpu = Recommended.Gpu.FindAll(x => Gpu.CallIsNvidia(x) || Gpu.CallIsAmd(x) || Gpu.CallIsIntel(x)).ToList();
            }

            pluginGameRequirements.Items = new List<RequirementEntry> { Minimum, Recommended };
            return pluginGameRequirements;
        }
    }
}