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
	public class SystemCheckerDatabase : PluginDatabaseObject<SystemCheckerSettingsViewModel, PluginGameRequirements, RequirementEntry>
	{
		/// <summary>
		/// Exposes the active system configuration manager for external consumers
		/// (e.g. the settings UI or diagnostic views).
		/// </summary>
		public SystemConfigurationManager SystemConfigurationManager { get; private set; }

		public SystemConfiguration PC { get; private set; }

		private PCGamingWikiRequirements _pcGamingWikiRequirements;
		private SteamRequirements _steamRequirements;
		private SteamApi _steamApi;

		public SystemCheckerDatabase(SystemCheckerSettingsViewModel pluginSettingsViewModel, string pluginUserDataPath)
			: base(pluginSettingsViewModel, "SystemChecker", pluginUserDataPath)
		{
			TagBefore = "[SC]";
			PluginWindows = new SystemCheckWindows(PluginName, this);
		}

		#region Initialisation

		/// <inheritdoc/>
		protected override void LoadMoreData()
		{
			try
			{
				Logger.Info("LoadMoreData started.");

				_pcGamingWikiRequirements = new PCGamingWikiRequirements();
				_steamRequirements = new SteamRequirements();
				_steamApi = new SteamApi(PluginName, PlayniteTools.ExternalPlugin.SystemChecker);

				SystemConfigurationManager = new SystemConfigurationManager(
					Path.Combine(Paths.PluginUserDataPath, "Configurations.json"));

				PC = SystemConfigurationManager.GetSystemConfiguration();

				Logger.Info("LoadMoreData completed.");
			}
			catch (Exception ex)
			{
				Logger.Error(ex, "Error in LoadMoreData.");
				throw; // Propagate so LoadDatabase returns false.
			}
		}

		#endregion

		#region Data retrieval

		/// <inheritdoc/>
		public override PluginGameRequirements Get(Guid id, bool onlyCache = false, bool force = false)
		{
			PluginGameRequirements cached = base.GetOnlyCache(id);

			if ((cached == null && !onlyCache) || force)
			{
				cached = GetWeb(id);
				AddOrUpdate(cached);
			}

			if (cached == null)
			{
				Game game = API.Instance.Database.Games.Get(id);
				if (game != null)
				{
					cached = GetDefault(game);
					AddOrUpdate(cached);
				}
			}

			return cached;
		}

		/// <inheritdoc/>
		public override PluginGameRequirements GetDefault(Game game)
		{
			PluginGameRequirements requirements = base.GetDefault(game);
			// Two entries: index 0 = minimum, index 1 = recommended.
			requirements.Items = new List<RequirementEntry>
			{
				new RequirementEntry { IsMinimum = true },
				new RequirementEntry()
			};
			return requirements;
		}

		/// <inheritdoc/>
		public override PluginGameRequirements GetWeb(Guid id)
		{
			Game game = API.Instance.Database.Games.Get(id);
			PluginGameRequirements requirements = GetDefault(game);

			try
			{
				Logger.Info($"GetWeb — trying PCGamingWiki for \"{game.Name}\".");
				requirements = _pcGamingWikiRequirements.GetRequirements(game);

				if (!_pcGamingWikiRequirements.IsFind())
				{
					requirements = FetchFromSteam(game);
				}

				requirements = NormalizeRecommended(requirements);
				requirements = PurgeGraphicsCardData(requirements);
			}
			catch (Exception ex)
			{
				Common.LogError(ex, false, true, "SystemChecker");
			}

			return requirements;
		}

		/// <summary>
		/// Attempts to fetch requirements from the Steam source.
		/// For Steam games, uses the Steam app ID directly; for others, resolves the ID via the Steam API.
		/// </summary>
		private PluginGameRequirements FetchFromSteam(Game game)
		{
			string sourceName = PlayniteTools.GetSourceName(game);
			Logger.Info($"GetWeb — trying Steam for \"{game.Name}\" (source: {sourceName}).");

			if (string.Equals(sourceName, "steam", StringComparison.OrdinalIgnoreCase))
			{
				return _steamRequirements.GetRequirements(game);
			}

			uint steamId = _steamApi.GetAppId(game);

			return steamId != 0
				? _steamRequirements.GetRequirements(game, steamId)
				: GetDefault(game);
		}

		#endregion

		#region Data normalisation

		/// <summary>
		/// Copies minimum fields into the recommended entry wherever recommended fields are absent,
		/// ensuring the recommended entry is always at least as complete as minimum.
		/// </summary>
		private static PluginGameRequirements NormalizeRecommended(PluginGameRequirements requirements)
		{
			RequirementEntry minimum = requirements.GetMinimum();
			RequirementEntry recommended = requirements.GetRecommended();

			if (!minimum.HasData || !recommended.HasData)
			{
				return requirements;
			}

			if (recommended.Os.Count == 0) { recommended.Os = minimum.Os; }
			if (recommended.Cpu.Count == 0) { recommended.Cpu = minimum.Cpu; }
			if (recommended.Gpu.Count == 0) { recommended.Gpu = minimum.Gpu; }
			if (recommended.Ram == 0) { recommended.Ram = minimum.Ram; }
			if (recommended.Storage == 0) { recommended.Storage = minimum.Storage; }

			requirements.Items = new List<RequirementEntry> { minimum, recommended };
			return requirements;
		}

		/// <summary>
		/// Removes GPU entries that cannot be attributed to a known vendor (Nvidia / AMD / Intel)
		/// when at least one vendor-attributed entry exists. Generic or unrecognised entries are dropped.
		/// </summary>
		public PluginGameRequirements PurgeGraphicsCardData(PluginGameRequirements requirements)
		{
			RequirementEntry minimum = requirements.GetMinimum();
			RequirementEntry recommended = requirements.GetRecommended();

			FilterGpuList(minimum);
			FilterGpuList(recommended);

			requirements.Items = new List<RequirementEntry> { minimum, recommended };
			return requirements;
		}

		/// <summary>
		/// Filters <paramref name="entry"/>'s GPU list to known-vendor cards only,
		/// but only when the list has more than one entry and at least one known-vendor card exists.
		/// </summary>
		private static void FilterGpuList(RequirementEntry entry)
		{
			if (!entry.HasData || entry.Gpu.Count <= 1)
			{
				return;
			}

			List<string> knownVendor = entry.Gpu.FindAll(IsKnownGpuVendor);
			if (knownVendor.Count > 0)
			{
				entry.Gpu = knownVendor;
			}
		}

		/// <summary>
		/// Returns <c>true</c> if <paramref name="gpuName"/> can be attributed to Nvidia, AMD, or Intel.
		/// </summary>
		private static bool IsKnownGpuVendor(string gpuName)
		{
			return Gpu.CallIsNvidia(gpuName) || Gpu.CallIsAmd(gpuName) || Gpu.CallIsIntel(gpuName);
		}

		#endregion

		#region Tag management

		/// <inheritdoc/>
		/// <inheritdoc/>
		protected override bool AppendPluginTag(Game game)
		{
			PluginGameRequirements item = Get(game, true);

			if (item.HasData)
			{
				try
				{
					SystemConfiguration systemConfig = PC;
					CheckSystem checkMinimum = SystemApi.CheckConfig(game, item.GetMinimum(), systemConfig, game.IsInstalled);
					CheckSystem checkRecommended = SystemApi.CheckConfig(game, item.GetRecommended(), systemConfig, game.IsInstalled);

					if (!(checkMinimum.AllOk ?? false) && !(checkRecommended.AllOk ?? false))
						return false;

					Guid? tagId = ResolveSystemTag(checkMinimum, checkRecommended);
					if (tagId != null)
					{
						AppendTagId(game, tagId.Value);
						return true;
					}
				}
				catch (Exception ex)
				{
					Common.LogError(ex, false, $"Tag insert error {game.Name}", true, PluginName,
						string.Format(ResourceProvider.GetString("LOCCommonNotificationTagError"), game.Name));
				}
				return false;
			}

			if (TagMissing)
			{
				Guid? noDataTagId = AddNoDataTag();
				if (noDataTagId != null)
				{
					AppendTagId(game, noDataTagId.Value);
					return true;
				}
			}

			return false;
		}



		/// <summary>
		/// Returns the tag ID that best represents the system's compatibility:
		/// recommended takes priority over minimum.
		/// Returns <c>null</c> when neither check passes (guard already handled by caller).
		/// </summary>
		private Guid? ResolveSystemTag(CheckSystem checkMinimum, CheckSystem checkRecommended)
		{
			if (checkRecommended.AllOk ?? false)
			{
				return CheckTagExist(ResourceProvider.GetString("LOCSystemCheckerConfigRecommended"));
			}

			if (checkMinimum.AllOk ?? false)
			{
				return CheckTagExist(ResourceProvider.GetString("LOCSystemCheckerConfigMinimum"));
			}

			return null;
		}

		#endregion

		#region Theme resources

		/// <inheritdoc/>
		public override void SetThemesResources(Game game)
		{
			PluginGameRequirements requirements = Get(game, true);

			if (requirements == null)
			{
				ResetThemeSettings();
				return;
			}

			SystemConfiguration systemConfig = PC;
			CheckSystem checkMinimum = SystemApi.CheckConfig(game, requirements.GetMinimum(), systemConfig, game.IsInstalled);
			CheckSystem checkRecommended = SystemApi.CheckConfig(game, requirements.GetRecommended(), systemConfig, game.IsInstalled);

			PluginSettings.Settings.HasData = requirements.HasData;
			PluginSettings.Settings.IsMinimumOK = false;
			PluginSettings.Settings.IsRecommendedOK = false;
			PluginSettings.Settings.IsAllOK = false;
			PluginSettings.Settings.RecommendedStorage = string.Empty;

			RequirementEntry minimum = requirements.GetMinimum();
			if (minimum.HasData)
			{
				PluginSettings.Settings.IsMinimumOK = checkMinimum.AllOk ?? false;
				PluginSettings.Settings.IsAllOK = checkMinimum.AllOk ?? false;
				PluginSettings.Settings.RecommendedStorage = FormatStorage(minimum.Storage);
			}

			RequirementEntry recommended = requirements.GetRecommended();
			if (recommended.HasData && (checkRecommended.AllOk ?? false))
			{
				PluginSettings.Settings.IsRecommendedOK = true;
				PluginSettings.Settings.IsAllOK = true;
				PluginSettings.Settings.RecommendedStorage = FormatStorage(recommended.Storage);
			}
		}

		/// <summary>Resets all theme-bound settings properties to their default (no data) state.</summary>
		private void ResetThemeSettings()
		{
			PluginSettings.Settings.HasData = false;
			PluginSettings.Settings.IsMinimumOK = false;
			PluginSettings.Settings.IsRecommendedOK = false;
			PluginSettings.Settings.IsAllOK = false;
			PluginSettings.Settings.RecommendedStorage = string.Empty;
		}

		#endregion

		/// <summary>
		/// Returns a human-readable storage size string, or <see cref="string.Empty"/> when <paramref name="bytes"/> is zero.
		/// </summary>
		private static string FormatStorage(double bytes)
		{
			return bytes != 0 ? UtilityTools.SizeSuffix(bytes) : string.Empty;
		}
	}
}