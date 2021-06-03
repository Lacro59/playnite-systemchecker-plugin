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
using System.Windows;
using System.Windows.Threading;
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
            List<SystemDisk> Disks = LocalSystem.GetInfoDisks();

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


            systemConfiguration = LocalSystem.GetPcInfo();


            File.WriteAllText(FilePlugin, JsonConvert.SerializeObject(systemConfiguration));
            return systemConfiguration;
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

            CheckSystem CheckMinimum = CheckMinimum = SystemApi.CheckConfig(systemMinimum, systemConfiguration, game.IsInstalled);
            CheckSystem CheckRecommanded = SystemApi.CheckConfig(systemRecommanded, systemConfiguration, game.IsInstalled);


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


        protected override void GetPluginTags()
        {
            try
            {
                // Get tags in playnite database
                PluginTags = new List<Tag>();
                foreach (Tag tag in PlayniteApi.Database.Tags)
                {
                    if (tag.Name?.IndexOf("[SC] ") > -1 && tag.Name?.IndexOf("<!LOC") == -1)
                    {
                        PluginTags.Add(tag);
                    }
                }

                // Add missing tags
                if (PluginTags.Count < 2)
                {
                    if (PluginTags.Find(x => x.Name == $"[SC] {resources.GetString("LOCSystemCheckerConfigMinimum")}") == null)
                    {
                        PlayniteApi.Database.Tags.Add(new Tag { Name = $"[SC] {resources.GetString("LOCSystemCheckerConfigMinimum")}" });
                    }
                    if (PluginTags.Find(x => x.Name == $"[SC] {resources.GetString("LOCSystemCheckerConfigRecommanded")}") == null)
                    {
                        PlayniteApi.Database.Tags.Add(new Tag { Name = $"[SC] {resources.GetString("LOCSystemCheckerConfigRecommanded")}" });
                    }

                    foreach (Tag tag in PlayniteApi.Database.Tags)
                    {
                        if (tag.Name.IndexOf("[SC] ") > -1 && tag.Name.IndexOf("<!LOC") == -1)
                        {
                            PluginTags.Add(tag);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Common.LogError(ex, false);
            }
        }

        public override void AddTag(Game game, bool noUpdate = false)
        {
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
                        TagId = (PluginTags.Find(x => x.Name == $"[SC] {resources.GetString("LOCSystemCheckerConfigMinimum")}")).Id;
                    }

                    // Recommanded
                    if ((bool)CheckMinimum.AllOk)
                    {
                        TagId = (PluginTags.Find(x => x.Name == $"[SC] {resources.GetString("LOCSystemCheckerConfigRecommanded")}")).Id;
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
    }
}
