using CommonPluginsShared;
using CommonPluginsShared.Collections;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using SystemChecker.Clients;
using SystemChecker.Models;
using CommonPluginsStores;
using System.Diagnostics;
using CommonPluginsStores.Steam;

namespace SystemChecker.Services
{
    public class SystemCheckerDatabase : PluginDatabaseObject<SystemCheckerSettingsViewModel, RequierementsCollection, GameRequierements, Requirement>
    {
        public LocalSystem LocalSystem;

        private PCGamingWikiRequierements PCGamingWikiRequierements { get; set; }
        private SteamRequierements SteamRequierements { get; set; }

        public SystemCheckerDatabase(SystemCheckerSettingsViewModel PluginSettings, string PluginUserDataPath) : base(PluginSettings, "SystemChecker", PluginUserDataPath)
        {
            TagBefore = "[SC]";

            PCGamingWikiRequierements = new PCGamingWikiRequierements();
            SteamRequierements = new SteamRequierements();
        }


        protected override bool LoadDatabase()
        {
            try
            {
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();

                Database = new RequierementsCollection(Paths.PluginDatabasePath);
                Database.SetGameInfo<Requirement>();

                LocalSystem = new LocalSystem(Path.Combine(Paths.PluginUserDataPath, $"Configurations.json"));
                Database.PC = LocalSystem.GetSystemConfiguration();

                stopWatch.Stop();
                TimeSpan ts = stopWatch.Elapsed;
                Logger.Info($"LoadDatabase with {Database.Count} items - {string.Format("{0:00}:{1:00}.{2:00}", ts.Minutes, ts.Seconds, ts.Milliseconds / 10)}");
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, PluginName);
                return false;
            }

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
                Game game = API.Instance.Database.Games.Get(Id);
                if (game != null)
                {
                    gameRequierements = GetDefault(game);
                    AddOrUpdate(gameRequierements);
                }
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
            Game game = API.Instance.Database.Games.Get(Id);
            GameRequierements gameRequierements = GetDefault(game);

            try
            {
                string SourceName = CommonPluginsShared.PlayniteTools.GetSourceName(game);

                // Search datas
                Logger.Info($"Try find with PCGamingWikiRequierements for {game.Name}");
                gameRequierements = PCGamingWikiRequierements.GetRequirements(game);

                if (!PCGamingWikiRequierements.IsFind())
                {
                    Logger.Info($"Try find with SteamRequierements for {game.Name}");
                    switch (SourceName.ToLower())
                    {
                        case "steam":
                            gameRequierements = SteamRequierements.GetRequirements(game);
                            break;

                        default:
                            SteamApi steamApi = new SteamApi(PluginName);
                            uint steamID = steamApi.GetAppId(game.Name);
                            if (steamID != 0)
                            {
                                gameRequierements = SteamRequierements.GetRequirements(game, steamID);
                            }
                            break;
                    }
                }

                gameRequierements = NormalizeRecommanded(gameRequierements);
                gameRequierements = PurgeGraphicsCardData(gameRequierements);
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false, true, "SystemChecker");
            }

            return gameRequierements;
        }


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


        #region Tag
        public override void AddTag(Game game)
        {
            GameRequierements item = Get(game, true);
            if (item.HasData)
            {
                try
                {
                    SystemConfiguration systemConfiguration = Database.PC;
                    Requirement systemMinimum = item.GetMinimum();
                    Requirement systemRecommanded = item.GetRecommanded();

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
            GameRequierements gameRequierements = Get(game, true);

            if (gameRequierements == null)
            {
                PluginSettings.Settings.HasData = false;
                PluginSettings.Settings.IsMinimumOK = false;
                PluginSettings.Settings.IsRecommandedOK = false;
                PluginSettings.Settings.IsAllOK = false;
                PluginSettings.Settings.RecommandedStorage = string.Empty;

                return;
            }

            SystemConfiguration systemConfiguration = Database.PC;
            Requirement systemMinimum = gameRequierements.GetMinimum();
            Requirement systemRecommanded = gameRequierements.GetRecommanded();

            CheckSystem CheckMinimum = SystemApi.CheckConfig(game, systemMinimum, systemConfiguration, game.IsInstalled);
            CheckSystem CheckRecommanded = SystemApi.CheckConfig(game, systemRecommanded, systemConfiguration, game.IsInstalled);


            PluginSettings.Settings.HasData = gameRequierements.HasData;
            PluginSettings.Settings.IsMinimumOK = false;
            PluginSettings.Settings.IsRecommandedOK = false;
            PluginSettings.Settings.IsAllOK = false;

            if (systemMinimum.HasData)
            {
                PluginSettings.Settings.IsMinimumOK = CheckMinimum.AllOk ?? false;
                PluginSettings.Settings.IsAllOK = CheckMinimum.AllOk ?? false;

                PluginSettings.Settings.RecommandedStorage = systemMinimum.Storage != 0 ? Tools.SizeSuffix(systemMinimum.Storage) : string.Empty;
            }

            if (systemRecommanded.HasData && (CheckRecommanded.AllOk ?? false))
            {
                PluginSettings.Settings.IsRecommandedOK = CheckRecommanded.AllOk ?? false;
                PluginSettings.Settings.IsAllOK = CheckRecommanded.AllOk ?? false;

                PluginSettings.Settings.RecommandedStorage = systemRecommanded.Storage != 0 ? Tools.SizeSuffix(systemRecommanded.Storage) : string.Empty;
            }
        }


        public GameRequierements PurgeGraphicsCardData(GameRequierements gameRequierements)
        {
            Requirement Minimum = gameRequierements.GetMinimum();
            Requirement Recommanded = gameRequierements.GetRecommanded();

            if (Minimum.HasData && Minimum.Gpu.Count > 1 && Minimum.Gpu.FindAll(x => Gpu.CallIsNvidia(x) || Gpu.CallIsAmd(x) || Gpu.CallIsIntel(x)).Count > 0)
            {
                Minimum.Gpu = Minimum.Gpu.FindAll(x => Gpu.CallIsNvidia(x) || Gpu.CallIsAmd(x) || Gpu.CallIsIntel(x)).ToList();
            }

            if (Recommanded.HasData && Recommanded.Gpu.Count > 1 && Recommanded.Gpu.FindAll(x => Gpu.CallIsNvidia(x) || Gpu.CallIsAmd(x) || Gpu.CallIsIntel(x)).Count > 0)
            {
                Recommanded.Gpu = Recommanded.Gpu.FindAll(x => Gpu.CallIsNvidia(x) || Gpu.CallIsAmd(x) || Gpu.CallIsIntel(x)).ToList();
            }

            gameRequierements.Items = new List<Requirement> { Minimum, Recommanded };
            return gameRequierements;
        }
    }
}
