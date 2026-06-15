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
using SystemChecker.Services.Parser;

namespace SystemChecker.Services
{
	public class SystemCheckerDatabase : PluginDatabaseObject<SystemCheckerSettings, PluginGameRequirements, RequirementEntry>
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

		public SystemCheckerDatabase(SystemCheckerSettings pluginSettings, string pluginUserDataPath)
			: base(pluginSettings, "SystemChecker", pluginUserDataPath)
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

				_steamApi = new SteamApi(PluginName, PlayniteTools.ExternalPlugin.SystemChecker);
				_pcGamingWikiRequirements = new PCGamingWikiRequirements(_steamApi);
				_steamRequirements = new SteamRequirements(_steamApi);

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

			return NormalizeRequirements(cached);
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
			string exclusionReason = PlayniteTools.GetLibraryFilterExclusionReason(game, PluginSettings);
			if (exclusionReason != null)
			{
				PlayniteTools.LogLibraryFilterExclusion("SystemChecker.GetWeb", game, exclusionReason);
				return GetDefault(game);
			}

			PluginGameRequirements requirements = GetDefault(game);

			try
			{
				uint steamAppId = 0;
				bool steamAppIdLookupAttempted = false;

				Logger.Info($"GetWeb — trying PCGamingWiki for \"{game.Name}\".");
				requirements = _pcGamingWikiRequirements.GetRequirements(game, ref steamAppId, ref steamAppIdLookupAttempted);

				if (!_pcGamingWikiRequirements.IsFind())
				{
					requirements = FetchFromSteam(game, ref steamAppId, steamAppIdLookupAttempted);
				}

				requirements = NormalizeRequirements(requirements);
			}
			catch (Exception ex)
			{
				Common.LogError(ex, false, true, "SystemChecker");
			}

			return requirements;
		}

		/// <summary>
		/// Attempts to fetch requirements from the Steam source.
		/// For Steam games, uses the Steam app ID directly; for others, reuses or resolves the AppId once.
		/// </summary>
		private PluginGameRequirements FetchFromSteam(Game game, ref uint steamAppId, bool steamAppIdLookupAttempted)
		{
			string sourceName = PlayniteTools.GetSourceName(game);
			Logger.Info($"GetWeb — trying Steam for \"{game.Name}\" (source: {sourceName}).");

			if (string.Equals(sourceName, "steam", StringComparison.OrdinalIgnoreCase))
			{
				return _steamRequirements.GetRequirements(game);
			}

			if (steamAppId == 0)
			{
				if (steamAppIdLookupAttempted)
				{
					Common.LogDebug(true,
						$"[SystemChecker] Skipping redundant Steam AppId lookup for \"{game.Name}\" — already unresolved during PCGW.");
					return GetDefault(game);
				}

				steamAppId = _steamApi.ResolveAppId(game);
			}

			return steamAppId != 0
				? _steamRequirements.GetRequirements(game, steamAppId)
				: GetDefault(game);
		}

		#endregion

		#region Data normalisation

		/// <summary>
		/// Applies the full post-import normalisation pipeline (sizes, recommended fill-in, GPU, OS, CPU).
		/// Safe to call on freshly downloaded or cached data.
		/// </summary>
		public PluginGameRequirements NormalizeRequirements(PluginGameRequirements requirements)
		{
			if (requirements == null)
			{
				return null;
			}

			SanitizeRequirementText(requirements);
			requirements = PurgeSizeData(requirements);
			requirements = NormalizeRecommended(requirements);
			requirements = PurgeGraphicsCardData(requirements);
			requirements = PurgeOsData(requirements);
			requirements = PurgeCpuData(requirements);
			return requirements;
		}

		/// <summary>
		/// Strips emoji and other decorative Unicode from all requirement strings and the source link label.
		/// </summary>
		private static void SanitizeRequirementText(PluginGameRequirements requirements)
		{
			if (requirements.SourcesLink != null)
			{
				requirements.SourcesLink.GameName = RequirementTextSanitizer.StripDecorativeCharacters(requirements.SourcesLink.GameName);
				requirements.SourcesLink.Name = RequirementTextSanitizer.StripDecorativeCharacters(requirements.SourcesLink.Name);
			}

			SanitizeRequirementEntry(requirements.GetMinimum());
			SanitizeRequirementEntry(requirements.GetRecommended());
		}

		private static void SanitizeRequirementEntry(RequirementEntry entry)
		{
			if (entry == null)
			{
				return;
			}

			RequirementTextSanitizer.SanitizeStringList(entry.Os);
			RequirementTextSanitizer.SanitizeStringList(entry.Cpu);
			RequirementTextSanitizer.SanitizeStringList(entry.Gpu);

			if (!entry.RamSource.IsNullOrEmpty())
			{
				entry.RamSource = RequirementTextSanitizer.StripDecorativeCharacters(entry.RamSource);
			}

			if (!entry.StorageSource.IsNullOrEmpty())
			{
				entry.StorageSource = RequirementTextSanitizer.StripDecorativeCharacters(entry.StorageSource);
			}
		}

		/// <summary>
		/// Parses <see cref="RequirementEntry.RamSource"/> and <see cref="RequirementEntry.StorageSource"/> into byte values.
		/// </summary>
		public PluginGameRequirements PurgeSizeData(PluginGameRequirements requirements)
		{
			RequirementSizeParser.NormalizeEntrySizes(requirements.GetMinimum());
			RequirementSizeParser.NormalizeEntrySizes(requirements.GetRecommended());

			RequirementEntry minimum = requirements.GetMinimum();
			RequirementEntry recommended = requirements.GetRecommended();
			requirements.Items = new List<RequirementEntry> { minimum, recommended };
			return requirements;
		}

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
		/// Splits combined CPU alternatives and removes unrecognised tokens.
		/// </summary>
		public PluginGameRequirements PurgeCpuData(PluginGameRequirements requirements)
		{
			RequirementEntry minimum = requirements.GetMinimum();
			RequirementEntry recommended = requirements.GetRecommended();

			FilterCpuList(minimum);
			FilterCpuList(recommended);

			requirements.Items = new List<RequirementEntry> { minimum, recommended };
			return requirements;
		}

		private static void FilterCpuList(RequirementEntry entry)
		{
			if (!entry.HasData || entry.Cpu.Count == 0)
			{
				return;
			}

			entry.Cpu = CpuRequirementParser.ExpandCpuList(entry.Cpu);
		}

		/// <summary>
		/// Removes unrecognised OS tokens from minimum and recommended entries.
		/// </summary>
		public PluginGameRequirements PurgeOsData(PluginGameRequirements requirements)
		{
			RequirementEntry minimum = requirements.GetMinimum();
			RequirementEntry recommended = requirements.GetRecommended();

			FilterOsList(minimum);
			FilterOsList(recommended);

			requirements.Items = new List<RequirementEntry> { minimum, recommended };
			return requirements;
		}

		private static void FilterOsList(RequirementEntry entry)
		{
			if (!entry.HasData || entry.Os.Count == 0)
			{
				return;
			}

			entry.Os = OsRequirementParser.FilterList(entry.Os);
		}

		/// <summary>
		/// Filters <paramref name="entry"/>'s GPU list to known-vendor cards only,
		/// but only when the list has more than one entry and at least one known-vendor card exists.
		/// </summary>
		private static void FilterGpuList(RequirementEntry entry)
		{
			if (!entry.HasData || entry.Gpu.Count == 0)
			{
				return;
			}

			entry.Gpu = GpuRequirementParser.NormalizeGpuList(entry.Gpu);

			if (entry.Gpu.Count <= 1)
			{
				return;
			}

			List<string> knownVendor = entry.Gpu.FindAll(IsRetainedGpuToken);
			if (knownVendor.Count > 0)
			{
				entry.Gpu = knownVendor;
			}
		}

		private static bool IsRetainedGpuToken(string gpuName)
		{
			return IsKnownGpuVendor(gpuName) || DirectXRequirementParser.TryNormalize(gpuName, out _);
		}

		/// <summary>
		/// Returns <c>true</c> if <paramref name="gpuName"/> can be attributed to Nvidia, AMD, or Intel.
		/// </summary>
		private static bool IsKnownGpuVendor(string gpuName)
		{
			return GpuRequirementParser.IsNvidia(gpuName) || GpuRequirementParser.IsAmd(gpuName) || GpuRequirementParser.IsIntel(gpuName);
		}

		#endregion

		#region Tag management

		/// <inheritdoc/>
		/// <inheritdoc/>
		protected override bool AppendPluginTag(Game game)
		{
			string exclusionReason = PlayniteTools.GetLibraryFilterExclusionReason(game, PluginSettings);
			if (exclusionReason != null)
			{
				PlayniteTools.LogLibraryFilterExclusion("SystemChecker.AppendPluginTag", game, exclusionReason);
				return false;
			}

			PluginGameRequirements item = Get(game, true);

			if (item.HasData)
			{
				try
				{
					SystemConfiguration systemConfig = PC;
					CheckSystem checkMinimum = SystemApi.CheckConfig(game, item.GetMinimum(), systemConfig, game.IsInstalled);
					CheckSystem checkRecommended = SystemApi.CheckConfig(game, item.GetRecommended(), systemConfig, game.IsInstalled);

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
		/// recommended takes priority over minimum, otherwise below minimum.
		/// May return <c>null</c> if the required tag cannot be found or created.
		/// </summary>
		private Guid? ResolveSystemTag(CheckSystem checkMinimum, CheckSystem checkRecommended)
		{
			if (checkRecommended.AllOk == true)
			{
				return CheckTagExist(ResourceProvider.GetString("LOCSystemCheckerConfigRecommended"));
			}

			if (checkMinimum.AllOk == true)
			{
				return CheckTagExist(ResourceProvider.GetString("LOCSystemCheckerConfigMinimum"));
			}

			if (checkMinimum.AllOk == false || checkRecommended.AllOk == false)
			{
				return CheckTagExist(ResourceProvider.GetString("LOCSystemCheckerConfigBelowMinimum"));
			}

			return null;
		}

		#endregion

		#region Theme resources

		/// <inheritdoc/>
		public override void SetThemesResources(Game game)
		{
			string exclusionReason = PlayniteTools.GetLibraryFilterExclusionReason(game, PluginSettings);
			if (exclusionReason != null)
			{
				PlayniteTools.LogLibraryFilterExclusion("SystemChecker.SetThemesResources", game, exclusionReason);
				ResetThemeSettings();
				return;
			}

			PluginGameRequirements requirements = Get(game, true);

			if (requirements == null)
			{
				ResetThemeSettings();
				return;
			}

			SystemConfiguration systemConfig = PC;
			CheckSystem checkMinimum = SystemApi.CheckConfig(game, requirements.GetMinimum(), systemConfig, game.IsInstalled);
			CheckSystem checkRecommended = SystemApi.CheckConfig(game, requirements.GetRecommended(), systemConfig, game.IsInstalled);

			PluginSettings.HasData = requirements.HasData;
			PluginSettings.IsMinimumOK = false;
			PluginSettings.IsRecommendedOK = false;
			PluginSettings.IsAllOK = false;
			PluginSettings.RecommendedStorage = string.Empty;

			RequirementEntry minimum = requirements.GetMinimum();
			if (minimum.HasData)
			{
				PluginSettings.IsMinimumOK = checkMinimum.AllOk ?? false;
				PluginSettings.IsAllOK = checkMinimum.AllOk ?? false;
				PluginSettings.RecommendedStorage = FormatStorage(minimum.Storage);
			}

			RequirementEntry recommended = requirements.GetRecommended();
			if (recommended.HasData && (checkRecommended.AllOk ?? false))
			{
				PluginSettings.IsRecommendedOK = true;
				PluginSettings.IsAllOK = true;
				PluginSettings.RecommendedStorage = FormatStorage(recommended.Storage);
			}
		}

		/// <summary>Resets all theme-bound settings properties to their default (no data) state.</summary>
		private void ResetThemeSettings()
		{
			PluginSettings.HasData = false;
			PluginSettings.IsMinimumOK = false;
			PluginSettings.IsRecommendedOK = false;
			PluginSettings.IsAllOK = false;
			PluginSettings.RecommendedStorage = string.Empty;
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