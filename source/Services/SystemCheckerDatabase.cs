using CommonPluginsShared;
using CommonPluginsShared.Collections;
using CommonPluginsStores.Models;
using CommonPluginsStores.Steam;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SystemChecker.Clients;
using SystemChecker.Models;

namespace SystemChecker.Services
{
    public class SystemCheckerDatabase : PluginDatabaseObject<SystemCheckerSettingsViewModel, RequirementsCollection, PluginGameRequirements, RequirementEntry>
    {
        public LocalSystem LocalSystem;

        private PCGamingWikiRequirements PCGamingWikiRequirements { get; set; }
        private SteamRequirements SteamRequirements { get; set; }

        public SystemCheckerDatabase(SystemCheckerSettingsViewModel pluginSettings, string pluginUserDataPath) : base(pluginSettings, "SystemChecker", pluginUserDataPath)
        {
            TagBefore = "[SC]";
		}

		protected override void LoadMoreData()
		{
			try
			{
				Logger.Info("LoadMoreData started");

				PCGamingWikiRequirements = new PCGamingWikiRequirements();
				SteamRequirements = new SteamRequirements();

				LocalSystem = new LocalSystem(Path.Combine(Paths.PluginUserDataPath, "Configurations.json"));
				_database.PC = LocalSystem.GetSystemConfiguration();

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

                pluginGameRequirements = NormalizeRecommanded(pluginGameRequirements);
                pluginGameRequirements = PurgeGraphicsCardData(pluginGameRequirements);
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, "SystemChecker");
            }

            return pluginGameRequirements;
        }

        private PluginGameRequirements NormalizeRecommanded(PluginGameRequirements pluginGameRequirements)
        {
            RequirementEntry Minimum = pluginGameRequirements.GetMinimum();
            RequirementEntry Recommanded = pluginGameRequirements.GetRecommanded();

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
                }
                if (Recommanded.Storage == 0)
                {
                    Recommanded.Storage = Minimum.Storage;
                }
            }

            pluginGameRequirements.Items = new List<RequirementEntry> { Minimum, Recommanded };
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
                    RequirementEntry systemRecommanded = item.GetRecommanded();

                    CheckSystem CheckMinimum = SystemApi.CheckConfig(game, systemMinimum, systemConfiguration, game.IsInstalled);
                    CheckSystem CheckRecommanded = SystemApi.CheckConfig(game, systemRecommanded, systemConfiguration, game.IsInstalled);

                    if (!(bool)CheckMinimum.AllOk && !(bool)CheckRecommanded.AllOk)
                    {
                        return;
                    }

                    Guid? tagId = null;
                    // Minimum
                    if ((bool)CheckMinimum.AllOk)
                    {
                        tagId = CheckTagExist($"{ResourceProvider.GetString("LOCSystemCheckerConfigMinimum")}");
                    }
                    // Recommanded
                    if ((bool)CheckRecommanded.AllOk)
                    {
                        tagId = CheckTagExist($"{ResourceProvider.GetString("LOCSystemCheckerConfigRecommanded")}");
                    }

                    if (tagId != null)
                    {
                        if (game.TagIds != null)
                        {
                            game.TagIds.Add((Guid)tagId);
                        }
                        else
                        {
                            game.TagIds = new List<Guid> { (Guid)tagId };
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
                if (game.TagIds != null)
                {
                    game.TagIds.Add((Guid)AddNoDataTag());
                }
                else
                {
                    game.TagIds = new List<Guid> { (Guid)AddNoDataTag() };
                }
            }

            API.Instance.MainView.UIDispatcher?.Invoke(() =>
            {
                API.Instance.Database.Games.Update(game);
                game.OnPropertyChanged();
            });
        }

        #endregion

        public override void SetThemesResources(Game game)
        {
            PluginGameRequirements pluginGameRequirements = Get(game, true);

            if (pluginGameRequirements == null)
            {
                PluginSettings.Settings.HasData = false;
                PluginSettings.Settings.IsMinimumOK = false;
                PluginSettings.Settings.IsRecommandedOK = false;
                PluginSettings.Settings.IsAllOK = false;
                PluginSettings.Settings.RecommandedStorage = string.Empty;

                return;
            }

            SystemConfiguration systemConfiguration = Database.PC;
            RequirementEntry systemMinimum = pluginGameRequirements.GetMinimum();
            RequirementEntry systemRecommanded = pluginGameRequirements.GetRecommanded();

            CheckSystem checkMinimum = SystemApi.CheckConfig(game, systemMinimum, systemConfiguration, game.IsInstalled);
            CheckSystem checkRecommanded = SystemApi.CheckConfig(game, systemRecommanded, systemConfiguration, game.IsInstalled);


            PluginSettings.Settings.HasData = pluginGameRequirements.HasData;
            PluginSettings.Settings.IsMinimumOK = false;
            PluginSettings.Settings.IsRecommandedOK = false;
            PluginSettings.Settings.IsAllOK = false;

            if (systemMinimum.HasData)
            {
                PluginSettings.Settings.IsMinimumOK = checkMinimum.AllOk ?? false;
                PluginSettings.Settings.IsAllOK = checkMinimum.AllOk ?? false;

                PluginSettings.Settings.RecommandedStorage = systemMinimum.Storage != 0 ? Tools.SizeSuffix(systemMinimum.Storage) : string.Empty;
            }

            if (systemRecommanded.HasData && (checkRecommanded.AllOk ?? false))
            {
                PluginSettings.Settings.IsRecommandedOK = checkRecommanded.AllOk ?? false;
                PluginSettings.Settings.IsAllOK = checkRecommanded.AllOk ?? false;

                PluginSettings.Settings.RecommandedStorage = systemRecommanded.Storage != 0 ? Tools.SizeSuffix(systemRecommanded.Storage) : string.Empty;
            }
        }

        public PluginGameRequirements PurgeGraphicsCardData(PluginGameRequirements pluginGameRequirements)
        {
            RequirementEntry Minimum = pluginGameRequirements.GetMinimum();
            RequirementEntry Recommanded = pluginGameRequirements.GetRecommanded();

            if (Minimum.HasData && Minimum.Gpu.Count > 1 && Minimum.Gpu.FindAll(x => Gpu.CallIsNvidia(x) || Gpu.CallIsAmd(x) || Gpu.CallIsIntel(x)).Count > 0)
            {
                Minimum.Gpu = Minimum.Gpu.FindAll(x => Gpu.CallIsNvidia(x) || Gpu.CallIsAmd(x) || Gpu.CallIsIntel(x)).ToList();
            }

            if (Recommanded.HasData && Recommanded.Gpu.Count > 1 && Recommanded.Gpu.FindAll(x => Gpu.CallIsNvidia(x) || Gpu.CallIsAmd(x) || Gpu.CallIsIntel(x)).Count > 0)
            {
                Recommanded.Gpu = Recommanded.Gpu.FindAll(x => Gpu.CallIsNvidia(x) || Gpu.CallIsAmd(x) || Gpu.CallIsIntel(x)).ToList();
            }

            pluginGameRequirements.Items = new List<RequirementEntry> { Minimum, Recommanded };
            return pluginGameRequirements;
        }
    }
}