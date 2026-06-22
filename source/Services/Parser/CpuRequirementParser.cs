using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

using SystemChecker.Services;

namespace SystemChecker.Services.Parser
{
	/// <summary>
	/// Parses and normalises CPU requirement strings from store pages.
	/// </summary>
	public static class CpuRequirementParser
	{
		private static readonly Regex IntelTypeRegex = new Regex(@"i[0-9]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		private static readonly Regex IntelModelNumberRegex = new Regex(@"\b\d{3,4}\b", RegexOptions.Compiled);
		private static readonly Regex IntegerClockRegex = new Regex(
			@"(\d+(?:\.\d+)?)\s*(ghz|mhz)\b",
			RegexOptions.Compiled | RegexOptions.IgnoreCase);
		private static readonly Regex ClockGhzRegex = new Regex(@"[0-9]*[.][0-9]*[ GHz]*", RegexOptions.Compiled);
		private static readonly Regex ClockGhzRegex2 = new Regex(@"[0-9]*[.][0-9]*[GHz]*", RegexOptions.Compiled);
		private static readonly Regex ClockMhzRegex = new Regex(@"[0-9]*[.][0-9]*[ MHz]*", RegexOptions.Compiled);
		private static readonly Regex ClockMhzRegex2 = new Regex(@"[0-9]*[.][0-9]*[MHz]*", RegexOptions.Compiled);
		private static readonly Regex CoreCountSuffixRegex = new Regex(
			@"\b(single|dual|quad|hexa|octa|eight|six|twelve)-?\s*core\b",
			RegexOptions.Compiled | RegexOptions.IgnoreCase);
		private static readonly Regex ParentheticalAlternativeRegex = new Regex(
			@"\((AMD|Intel|FX|Athlon|Ryzen|Pentium|Celeron).+?\)",
			RegexOptions.IgnoreCase | RegexOptions.Compiled);
		private static readonly Regex CjkRegex = new Regex(@"[\p{IsCJKUnifiedIdeographs}]+", RegexOptions.Compiled);

		/// <summary>
		/// Expands combined alternatives, discards junk entries, and deduplicates.
		/// </summary>
		public static List<string> ExpandCpuList(List<string> cpuList)
		{
			if (cpuList == null || cpuList.Count == 0)
			{
				return cpuList ?? new List<string>();
			}

			var result = new List<string>();
			var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			foreach (string raw in cpuList)
			{
				foreach (string alternative in SplitAlternatives(raw))
				{
					if (IsDiscardable(alternative))
					{
						continue;
					}

					string token = NormalizeName(alternative);
					if (!string.IsNullOrWhiteSpace(token) && seen.Add(token))
					{
						result.Add(token);
					}
				}
			}

			return result;
		}

		/// <summary>
		/// Normalises a raw WMI or store requirement CPU name.
		/// </summary>
		public static string NormalizeName(string cpuName)
		{
			if (string.IsNullOrWhiteSpace(cpuName))
			{
				return cpuName;
			}

			string normalized = cpuName
				.Replace("(R)", string.Empty)
				.Replace("(r)", string.Empty)
				.Replace("(TM)", string.Empty)
				.Replace("(tm)", string.Empty)
				.Replace("®", string.Empty)
				.Replace("™", string.Empty);

			normalized = CoreCountSuffixRegex.Replace(normalized, string.Empty);
			normalized = Regex.Replace(normalized, @"\s+w/\s+.+$", string.Empty, RegexOptions.IgnoreCase);
			normalized = Regex.Replace(normalized, @"(?i)\s+or equivalent\.?$", string.Empty);
			normalized = ExpandShortIntelModel(normalized);

			return Regex.Replace(normalized, @"\s+", " ").Trim();
		}

		private static string ExpandShortIntelModel(string cpuName)
		{
			if (string.IsNullOrWhiteSpace(cpuName))
			{
				return cpuName;
			}

			if (Regex.IsMatch(cpuName, @"(?i)\bCore i[3579]\d*\b") && cpuName.IndexOf("Intel", StringComparison.OrdinalIgnoreCase) < 0)
			{
				return "Intel " + cpuName.Trim();
			}

			Match shortIntel = Regex.Match(cpuName, @"(?i)^i([3579])(?:-\d+)?$");
			if (shortIntel.Success)
			{
				return $"Intel Core i{shortIntel.Groups[1].Value}";
			}

			Match bareSeries = Regex.Match(cpuName, @"(?i)^i([3579])$");
			if (bareSeries.Success)
			{
				return $"Intel Core i{bareSeries.Groups[1].Value}";
			}

			cpuName = Regex.Replace(cpuName, @"(?i)\bIntel\s+Core\s+I([3579])\s+(\d{4})\b", "Intel Core i$1-$2");
			cpuName = Regex.Replace(cpuName, @"(?i)\bIntel\s+i([3579])\s+(\d{4})\b", "Intel Core i$1-$2");
			cpuName = Regex.Replace(cpuName, @"(?i)\bCore\s+I([3579])\s+(\d{4})\b", "Core i$1-$2");
			cpuName = Regex.Replace(cpuName, @"(?i)\bAmd\s+", "AMD ");

			return cpuName;
		}

		/// <summary>
		/// Builds a <see cref="CpuObject"/> from a raw CPU name or requirement string.
		/// </summary>
		public static CpuObject Parse(string raw)
		{
			string cpuName = NormalizeName(raw);
			bool isIntel = IsIntel(cpuName);
			bool isAmd = IsAmd(cpuName);

			return new CpuObject
			{
				Name = cpuName,
				IsIntel = isIntel,
				IsAmd = isAmd,
				IsNamedModel = IsBenchmarkableModel(cpuName, isIntel, isAmd),
				Clock = ExtractClock(cpuName),
			};
		}

		public static bool IsIntel(string cpuName)
		{
			return cpuName.IndexOf("intel", StringComparison.OrdinalIgnoreCase) > -1
				|| IntelTypeRegex.IsMatch(cpuName);
		}

		public static bool IsAmd(string cpuName)
		{
			return cpuName.IndexOf("amd", StringComparison.OrdinalIgnoreCase) > -1
				|| cpuName.IndexOf("ryzen", StringComparison.OrdinalIgnoreCase) > -1
				|| Regex.IsMatch(cpuName, @"\bfx[\s-]?\d", RegexOptions.IgnoreCase);
		}

		public static bool IsDiscardable(string raw)
		{
			if (string.IsNullOrWhiteSpace(raw))
			{
				return true;
			}

			string value = raw.Trim();

			if (CjkRegex.IsMatch(value))
			{
				return !IsIntel(value)
					&& !IsAmd(value)
					&& !Regex.IsMatch(value, @"\d+\s*(ghz|mhz)\b", RegexOptions.IgnoreCase);
			}

			string lower = value.ToLowerInvariant();

			return lower == "any"
				|| lower == "anything"
				|| lower == "n/a"
				|| lower.Contains("not specified")
				|| lower.Contains("something idk");
		}

		private static bool IsBenchmarkableModel(string cpuName, bool isIntel, bool isAmd)
		{
			string lower = cpuName.ToLowerInvariant();

			if (isAmd)
			{
				return lower.IndexOf("ryzen", StringComparison.Ordinal) >= 0
					|| lower.IndexOf(" fx", StringComparison.Ordinal) >= 0
					|| lower.IndexOf("fx-", StringComparison.Ordinal) >= 0
					|| lower.IndexOf("athlon", StringComparison.Ordinal) >= 0
					|| lower.IndexOf("epyc", StringComparison.Ordinal) >= 0
					|| lower.IndexOf("threadripper", StringComparison.Ordinal) >= 0;
			}

			if (isIntel)
			{
				if (lower.IndexOf("pentium", StringComparison.Ordinal) >= 0
					|| lower.IndexOf("celeron", StringComparison.Ordinal) >= 0
					|| lower.IndexOf("atom", StringComparison.Ordinal) >= 0)
				{
					return false;
				}

				if (IntelTypeRegex.IsMatch(cpuName)
					|| lower.IndexOf("xeon", StringComparison.Ordinal) >= 0)
				{
					return IntelModelNumberRegex.IsMatch(cpuName);
				}

				return IntelModelNumberRegex.IsMatch(cpuName);
			}

			return false;
		}

		private static double ExtractClock(string cpuName)
		{
			double clock = 0;

			Match integerClock = IntegerClockRegex.Match(cpuName);
			if (integerClock.Success
				&& double.TryParse(
					integerClock.Groups[1].Value.Replace(",", "."),
					NumberStyles.Float,
					CultureInfo.InvariantCulture,
					out clock))
			{
				if (integerClock.Groups[2].Value.IndexOf("m", StringComparison.OrdinalIgnoreCase) >= 0)
				{
					clock /= 1000;
				}

				return clock;
			}

			Match clockMatch = ClockGhzRegex.Match(cpuName);
			if (clockMatch.Success)
			{
				ParseClock(clockMatch.Value.Replace("GHz", string.Empty), ref clock);
			}

			if (clock == 0)
			{
				clockMatch = ClockGhzRegex2.Match(cpuName);
				if (clockMatch.Success)
				{
					ParseClock(clockMatch.Value.Replace("GHz", string.Empty), ref clock);
				}
			}

			if (clock == 0)
			{
				clockMatch = ClockMhzRegex.Match(cpuName);
				if (clockMatch.Success && ParseClock(clockMatch.Value.Replace("MHz", string.Empty), ref clock) && clock != 0)
				{
					clock /= 1000;
				}
			}

			if (clock == 0)
			{
				clockMatch = ClockMhzRegex2.Match(cpuName);
				if (clockMatch.Success && ParseClock(clockMatch.Value.Replace("MHz", string.Empty), ref clock) && clock != 0)
				{
					clock /= 1000;
				}
			}

			return clock;
		}

		private static bool ParseClock(string clockStr, ref double clock)
		{
			clockStr = clockStr
				.Replace(".", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator)
				.Replace(",", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator)
				.Trim();

			return double.TryParse(clockStr, out clock);
		}

		private static IEnumerable<string> SplitAlternatives(string raw)
		{
			if (string.IsNullOrWhiteSpace(raw))
			{
				yield break;
			}

			string value = raw.Trim();
			bool split = false;

			foreach (string part in value.Split('|', '/', ';'))
			{
				string segment = part.Trim();
				if (segment.Length == 0)
				{
					continue;
				}

				foreach (string expanded in SplitParentheticalAlternative(segment))
				{
					split = true;
					yield return expanded;
				}
			}

			if (!split)
			{
				foreach (string expanded in SplitParentheticalAlternative(value))
				{
					yield return expanded;
				}
			}
		}

		private static IEnumerable<string> SplitParentheticalAlternative(string value)
		{
			Match match = ParentheticalAlternativeRegex.Match(value);
			if (!match.Success)
			{
				yield return value;
				yield break;
			}

			string primary = value.Remove(match.Index, match.Length).Trim();
			string secondary = match.Value.Trim('(', ')').Trim();

			if (!string.IsNullOrWhiteSpace(primary))
			{
				yield return primary;
			}

			if (!string.IsNullOrWhiteSpace(secondary))
			{
				yield return secondary;
			}
		}
	}
}
