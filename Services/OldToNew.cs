using CommonPluginsShared;
using Newtonsoft.Json;
using Playnite.SDK;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SystemChecker.Models;

namespace SystemChecker.Services
{
    public class OldToNew
    {
        private ILogger logger = LogManager.GetLogger();

        public bool IsOld = false;

        private string PathActivityDB = "SystemChecker";

        public ConcurrentDictionary<Guid, GameRequierementsOld> Items { get; set; } = new ConcurrentDictionary<Guid, GameRequierementsOld>();


        public OldToNew(string PluginUserDataPath)
        {
            PathActivityDB = Path.Combine(PluginUserDataPath, PathActivityDB);

            if (Directory.Exists(PathActivityDB))
            {
                Directory.Move(PathActivityDB, PathActivityDB + "_old");

                PathActivityDB += "_old";

                LoadOldDB();
                IsOld = true;
            }
        }

        public void LoadOldDB()
        {
            logger.Info($"LoadOldDB()");

            Parallel.ForEach(Directory.EnumerateFiles(PathActivityDB, "*.json"), (objectFile) =>
            {
                string objectFileManual = string.Empty;

                try
                {
                    if (!objectFile.Replace(PathActivityDB, "").Replace(".json", "").Replace("\\", "").ToLower().Contains("pc"))
                    {
                        var JsonStringData = File.ReadAllText(objectFile);

                        Common.LogDebug(true, objectFile.Replace(PathActivityDB, "").Replace(".json", "").Replace("\\", ""));

                        Guid gameId = Guid.Parse(objectFile.Replace(PathActivityDB, "").Replace(".json", "").Replace("\\", ""));

                        GameRequierementsOld gameRequierements = JsonConvert.DeserializeObject<GameRequierementsOld>(JsonStringData);

                        Items.TryAdd(gameId, gameRequierements);
                    }
                }
                catch (Exception ex)
                {
                    Common.LogError(ex, false, $"Failed to load item from {objectFile} or {objectFileManual}");
                }
            });

            logger.Info($"Find {Items.Count} items");
        }

        public void ConvertDB(IPlayniteAPI PlayniteApi)
        {
            GlobalProgressOptions globalProgressOptions = new GlobalProgressOptions(
                "SystemChecker - Database migration",
                false
            );
            globalProgressOptions.IsIndeterminate = true;

            PlayniteApi.Dialogs.ActivateGlobalProgress((activateGlobalProgress) =>
            {
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();

                logger.Info($"ConvertDB()");

                int Converted = 0;

                foreach (var item in Items)
                {
                    try
                    {
                        if (PlayniteApi.Database.Games.Get(item.Key) != null)
                        {
                            GameRequierements gameRequierements = SystemChecker.PluginDatabase.Get(item.Key, true);

                            Requirement Minimum = item.Value.Minimum;
                            Minimum.IsMinimum = true;

                            Requirement Recommanded = item.Value.Recommanded;

                            gameRequierements.Items = new List<Requirement> { Minimum, Recommanded };

                            Thread.Sleep(10);
                            SystemChecker.PluginDatabase.Update(gameRequierements);
                            Converted++;
                        }
                        else
                        {
                            logger.Warn($"Game is deleted - {item.Key.ToString()}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Common.LogError(ex, false, $"Failed to load ConvertDB from {item.Key.ToString()}");
                    }
                }

                logger.Info($"Converted {Converted} / {Items.Count}");

                stopWatch.Stop();
                TimeSpan ts = stopWatch.Elapsed;
                logger.Info($"Migration - {String.Format("{0:00}:{1:00}.{2:00}", ts.Minutes, ts.Seconds, ts.Milliseconds / 10)}");
            }, globalProgressOptions);

            IsOld = false;
        }
    }

    public class GameRequierementsOld
    {
        public Requirement Minimum { get; set; }
        public Requirement Recommanded { get; set; }
        public string Link { get; set; } = string.Empty;
    }
}
