using CommonPluginsShared;
using CommonPluginsShared.Plugins;
using CommonPluginsStores.Models;
using Playnite.SDK;
using System.Collections.Generic;
using System.Linq;
using SystemChecker.Models;

namespace SystemChecker.Services
{
	/// <summary>
	/// Exports SystemChecker requirement data to CSV with separate minimum and recommended columns per game.
	/// </summary>
	public class SystemCheckerExportCsv : PluginExportCsv<PluginGameRequirements>
	{
		private const string FieldGameName = "GameName";
		private const string FieldGameId = "GameId";
		private const string FieldSource = "Source";
		private const string FieldDataDate = "DataDate";
		private const string FieldSourceLink = "SourceLink";
		private const string FieldMinOs = "MinOs";
		private const string FieldMinCpu = "MinCpu";
		private const string FieldMinGpu = "MinGpu";
		private const string FieldMinRam = "MinRam";
		private const string FieldMinStorage = "MinStorage";
		private const string FieldRecOs = "RecOs";
		private const string FieldRecCpu = "RecCpu";
		private const string FieldRecGpu = "RecGpu";
		private const string FieldRecRam = "RecRam";
		private const string FieldRecStorage = "RecStorage";

		/// <inheritdoc />
		protected override Dictionary<string, string> GetHeader()
		{
			string minimum = ResourceProvider.GetString("LOCSystemCheckerConfigMinimum");
			string recommended = ResourceProvider.GetString("LOCSystemCheckerConfigRecommended");
			string os = ResourceProvider.GetString("LOCSystemCheckerOS");
			string cpu = ResourceProvider.GetString("LOCSystemCheckerCpu");
			string gpu = ResourceProvider.GetString("LOCSystemCheckerGpu");
			string ram = ResourceProvider.GetString("LOCSystemCheckerRam");
			string storage = ResourceProvider.GetString("LOCSystemCheckerDisk");

			return new Dictionary<string, string>
			{
				{ FieldGameName, ResourceProvider.GetString("LOCGameNameTitle") },
				{ FieldGameId, ResourceProvider.GetString("LOCGameId") },
				{ FieldSource, ResourceProvider.GetString("LOCCommonGameSource") },
				{ FieldDataDate, ResourceProvider.GetString("LOCCommonDateData") },
				{ FieldSourceLink, ResourceProvider.GetString("LOCURLLabel") },
				{ FieldMinOs, minimum + " - " + os },
				{ FieldMinCpu, minimum + " - " + cpu },
				{ FieldMinGpu, minimum + " - " + gpu },
				{ FieldMinRam, minimum + " - " + ram },
				{ FieldMinStorage, minimum + " - " + storage },
				{ FieldRecOs, recommended + " - " + os },
				{ FieldRecCpu, recommended + " - " + cpu },
				{ FieldRecGpu, recommended + " - " + gpu },
				{ FieldRecRam, recommended + " - " + ram },
				{ FieldRecStorage, recommended + " - " + storage }
			};
		}

		/// <inheritdoc />
		protected override IEnumerable<Dictionary<string, string>> GetRows(PluginGameRequirements item)
		{
			if (item == null)
			{
				yield break;
			}

			RequirementEntry minimum = item.GetMinimum();
			RequirementEntry recommended = item.GetRecommended();

			yield return new Dictionary<string, string>
			{
				{ FieldGameName, item.Game?.Name ?? string.Empty },
				{ FieldGameId, item.Id.ToString() },
				{ FieldSource, PlayniteTools.GetSourceName(item.Id) },
				{ FieldDataDate, FormatCsvUtcDateTime(item.DateLastRefresh) },
				{ FieldSourceLink, FormatSourceLink(item) },
				{ FieldMinOs, FormatList(minimum?.Os) },
				{ FieldMinCpu, FormatList(minimum?.Cpu) },
				{ FieldMinGpu, FormatList(minimum?.Gpu) },
				{ FieldMinRam, FormatRam(minimum) },
				{ FieldMinStorage, FormatStorage(minimum) },
				{ FieldRecOs, FormatList(recommended?.Os) },
				{ FieldRecCpu, FormatList(recommended?.Cpu) },
				{ FieldRecGpu, FormatList(recommended?.Gpu) },
				{ FieldRecRam, FormatRam(recommended) },
				{ FieldRecStorage, FormatStorage(recommended) }
			};
		}

		private static string FormatList(IList<string> values)
		{
			if (values == null || values.Count == 0)
			{
				return string.Empty;
			}

			return string.Join(" | ", values.Where(value => !string.IsNullOrWhiteSpace(value)));
		}

		private static string FormatRam(RequirementEntry entry)
		{
			if (entry == null)
			{
				return string.Empty;
			}

			if (entry.Ram > 0)
			{
				return entry.RamUsage;
			}

			return entry.RamSource ?? string.Empty;
		}

		private static string FormatStorage(RequirementEntry entry)
		{
			if (entry == null)
			{
				return string.Empty;
			}

			if (entry.Storage > 0)
			{
				return entry.StorageUsage;
			}

			return entry.StorageSource ?? string.Empty;
		}

		private static string FormatSourceLink(PluginGameRequirements item)
		{
			if (item?.SourcesLink == null)
			{
				return string.Empty;
			}

			if (!string.IsNullOrEmpty(item.SourcesLink.Url))
			{
				return item.SourcesLink.Url;
			}

			return item.SourcesLink.Name ?? string.Empty;
		}
	}
}
