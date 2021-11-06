using CommonPluginsShared;
using CommonPluginsShared.Collections;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using SystemChecker.Clients;
using SystemChecker.Models;
using CommonPluginsStores;

namespace SystemChecker.Services
{
    public class SystemCheckerDatabase : PluginDatabaseObject<SystemCheckerSettingsViewModel, RequierementsCollection, GameRequierements>
    {
        public LocalSystem LocalSystem;

        private PCGamingWikiRequierements pCGamingWikiRequierements;
        private SteamRequierements steamRequierements;

        public SystemCheckerDatabase(IPlayniteAPI PlayniteApi, SystemCheckerSettingsViewModel PluginSettings, string PluginUserDataPath) : base(PlayniteApi, PluginSettings, "SystemChecker", PluginUserDataPath)
        {
            TagBefore = "[SC]";

            pCGamingWikiRequierements = new PCGamingWikiRequierements(PlayniteApi, PluginUserDataPath);
            steamRequierements = new SteamRequierements();
        }


        protected override bool LoadDatabase()
        {
            Database = new RequierementsCollection(Paths.PluginDatabasePath);
            Database.SetGameInfo<Requirement>(PlayniteApi);

            LocalSystem = new LocalSystem(Path.Combine(Paths.PluginUserDataPath, $"Configurations.json"));
            Database.PC = LocalSystem.GetSystemConfiguration();

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
            Game game = PlayniteApi.Database.Games.Get(Id);
            GameRequierements gameRequierements = GetDefault(game);

            string SourceName = string.Empty;

            try
            {
                SourceName = CommonPluginsShared.PlayniteTools.GetSourceName(game);

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
                            break;

                        default:
                            SteamApi steamApi = new SteamApi();
                            int SteamID = steamApi.GetSteamId(game.Name);
                            if (SteamID != 0)
                            {
                                gameRequierements = steamRequierements.GetRequirements(game, (uint)SteamID);
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
        public override void AddTag(Game game, bool noUpdate = false)
        {
            GetPluginTags();
            GameRequierements gameRequierements = Get(game, true);

            if (gameRequierements.HasData)
            {
                try
                {
                    SystemConfiguration systemConfiguration = Database.PC;
                    Requirement systemMinimum = gameRequierements.GetMinimum();
                    Requirement systemRecommanded = gameRequierements.GetRecommanded();

                    CheckSystem CheckMinimum = SystemApi.CheckConfig(systemMinimum, systemConfiguration, game.IsInstalled);
                    CheckSystem CheckRecommanded = SystemApi.CheckConfig(systemRecommanded, systemConfiguration, game.IsInstalled);


                    if (!(bool)CheckMinimum.AllOk && !(bool)CheckRecommanded.AllOk)
                    {
                        return;
                    }

                    // Minimum
                    Guid? TagId = null;

                    if ((bool)CheckMinimum.AllOk)
                    {
                        TagId = CheckTagExist($"{resources.GetString("LOCSystemCheckerConfigMinimum")}"); 
                    }

                    // Recommanded
                    if ((bool)CheckRecommanded.AllOk)
                    {
                        TagId = CheckTagExist($"{resources.GetString("LOCSystemCheckerConfigRecommanded")}");
                    }

                    if (TagId != null)
                    {
                        if (game.TagIds != null)
                        {
                            game.TagIds.Add((Guid)TagId);
                        }
                        else
                        {
                            game.TagIds = new List<Guid> { (Guid)TagId };
                        }

                        if (!noUpdate)
                        {
                            Application.Current.Dispatcher?.Invoke(() =>
                            {
                                PlayniteApi.Database.Games.Update(game);
                                game.OnPropertyChanged();
                            }, DispatcherPriority.Send);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, true);
                    logger.Error($"Tag insert error with {game.Name}");
                    PlayniteApi.Notifications.Add(new NotificationMessage(
                        $"{PluginName}-Tag-Errors",
                        $"{PluginName}\r\n" + resources.GetString("LOCCommonNotificationTagError"),
                        NotificationType.Error
                    ));
                }
            }
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

                return;
            }

            SystemConfiguration systemConfiguration = Database.PC;
            Requirement systemMinimum = gameRequierements.GetMinimum();
            Requirement systemRecommanded = gameRequierements.GetRecommanded();

            CheckSystem CheckMinimum = CheckMinimum = SystemApi.CheckConfig(systemMinimum, systemConfiguration, game.IsInstalled);
            CheckSystem CheckRecommanded = SystemApi.CheckConfig(systemRecommanded, systemConfiguration, game.IsInstalled);


            PluginSettings.Settings.HasData = gameRequierements.HasData;
            PluginSettings.Settings.IsMinimumOK = false;
            PluginSettings.Settings.IsRecommandedOK = false;
            PluginSettings.Settings.IsAllOK = false;

            if (systemMinimum.HasData)
            {
                PluginSettings.Settings.IsMinimumOK = CheckMinimum.AllOk ?? false;
                PluginSettings.Settings.IsAllOK = CheckMinimum.AllOk ?? false;
            }

            if (systemRecommanded.HasData && (CheckRecommanded.AllOk ?? false))
            {
                PluginSettings.Settings.IsRecommandedOK = CheckRecommanded.AllOk ?? false;
                PluginSettings.Settings.IsAllOK = CheckRecommanded.AllOk ?? false;
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
