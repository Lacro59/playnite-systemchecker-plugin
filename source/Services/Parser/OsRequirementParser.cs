using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SystemChecker.Services.Parser
{
	/// <summary>
	/// Normalises raw OS requirement strings into comparable Windows version tokens.
	/// Unrecognised or non-Windows values are discarded.
	/// </summary>
	public static class OsRequirementParser
	{
		private static readonly Regex FeatureUpdatePattern = new Regex(@"\b(\d{2})h(\d)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
		private static readonly Regex BuildNumberPattern = new Regex(@"\((\d{5})(?:\.\d+)?\)", RegexOptions.Compiled);
		private static readonly Regex MajorVersionPattern = new Regex(@"\b(11|10|8|7)\b", RegexOptions.Compiled);
		private static readonly Regex OsLabelPrefixRegex = new Regex(
			@"^(?i)(?:os\s*[*]?\s*:|microsoft\s+)?(?:\d{2,3}\s*-?\s*bit\s+)?",
			RegexOptions.Compiled);

		/// <summary>
		/// Attempts to normalise <paramref name="raw"/> into a canonical token (e.g. <c>11</c>, <c>10</c>, <c>xp</c>).
		/// </summary>
		public static bool TryNormalize(string raw, out string normalized)
		{
			normalized = null;

			if (string.IsNullOrWhiteSpace(raw))
			{
				return false;
			}

			string value = PrepareOsToken(raw);

			if (IsAmbiguousOsToken(value))
			{
				return false;
			}

			value = value.ToLowerInvariant();

			if (IsNonWindowsPlatform(value))
			{
				return false;
			}

			int? buildNumber = null;
			Match buildMatch = BuildNumberPattern.Match(value);
			if (buildMatch.Success && int.TryParse(buildMatch.Groups[1].Value, out int parsedBuild))
			{
				buildNumber = parsedBuild;
			}

			string withoutParens = Regex.Replace(value, @"\([^)]*\)", " ").Trim();

			Match featureUpdate = FeatureUpdatePattern.Match(withoutParens);
			if (featureUpdate.Success && int.TryParse(featureUpdate.Groups[1].Value, out int featureYear))
			{
				normalized = MapFeatureUpdateToVersion(featureYear);
				return normalized != null;
			}

			if (Regex.IsMatch(withoutParens, @"\b8\.1\b"))
			{
				normalized = "8.1";
				return true;
			}

			if (ContainsNamedVersion(withoutParens, "xp"))
			{
				normalized = "xp";
				return true;
			}

			if (ContainsNamedVersion(withoutParens, "vista"))
			{
				normalized = "vista";
				return true;
			}

			if (ContainsNamedVersion(withoutParens, "millenium") || ContainsNamedVersion(withoutParens, "me"))
			{
				normalized = "me";
				return true;
			}

			if (Regex.IsMatch(withoutParens, @"\b2000\b"))
			{
				normalized = "2000";
				return true;
			}

			if (Regex.IsMatch(withoutParens, @"\b98\b"))
			{
				normalized = "98";
				return true;
			}

			if (Regex.IsMatch(withoutParens, @"\b95\b"))
			{
				normalized = "95";
				return true;
			}

			Match majorVersion = MajorVersionPattern.Match(withoutParens);
			if (majorVersion.Success)
			{
				normalized = majorVersion.Groups[1].Value;
				return true;
			}

			if (buildNumber.HasValue)
			{
				normalized = MapBuildToVersion(buildNumber.Value);
				return normalized != null;
			}

			return false;
		}

		/// <summary>
		/// Keeps only recognisable Windows versions, normalised and deduplicated.
		/// </summary>
		public static List<string> FilterList(List<string> osList)
		{
			if (osList == null || osList.Count == 0)
			{
				return osList ?? new List<string>();
			}

			var result = new List<string>();
			var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			foreach (string raw in osList)
			{
				foreach (string part in raw.Split('/', ',', ';'))
				{
					if (TryNormalize(part, out string normalized) && seen.Add(normalized))
					{
						result.Add(normalized);
					}
				}
			}

			return result;
		}

		private static string PrepareOsToken(string raw)
		{
			string value = raw.Trim().TrimEnd('.').TrimEnd('+');
			value = Regex.Replace(value, @"\?+", string.Empty);
			value = Regex.Replace(value, @"(?i)\s*\(\s*64\s*-?\s*bit\s*\)", string.Empty);
			value = Regex.Replace(value, @"(?i)\bSP1\b", string.Empty);
			value = OsLabelPrefixRegex.Replace(value, string.Empty).Trim();
			value = Regex.Replace(value, @"(?i)^windows\s+", string.Empty).Trim();
			value = Regex.Replace(value, @"(?i)^window\s+", string.Empty).Trim();
			if (string.Equals(value, "window", StringComparison.OrdinalIgnoreCase))
			{
				return string.Empty;
			}
			return value;
		}

		private static bool IsAmbiguousOsToken(string value)
		{
			string lower = value.Trim().ToLowerInvariant();

			return lower == "64-bit"
				|| lower == "64 bit"
				|| lower == "32-bit"
				|| lower == "32 bit"
				|| Regex.IsMatch(lower, @"^\d{2,3}-?bit$");
		}

		private static bool IsNonWindowsPlatform(string value)
		{
			return value.Contains("macos")
				|| value.Contains("mac os")
				|| value.Contains("osx")
				|| value.Contains("linux")
				|| value.Contains("ubuntu")
				|| value.Contains("steamos")
				|| value.Contains("debian")
				|| value.Contains("fedora");
		}

		private static bool ContainsNamedVersion(string value, string name)
		{
			return Regex.IsMatch(value, $@"\b{Regex.Escape(name)}\b", RegexOptions.IgnoreCase);
		}

		private static string MapFeatureUpdateToVersion(int featureYear)
		{
			if (featureYear >= 23)
			{
				return "11";
			}

			if (featureYear >= 22)
			{
				return "10";
			}

			if (featureYear >= 15)
			{
				return "10";
			}

			return null;
		}

		private static string MapBuildToVersion(int build)
		{
			if (build >= 22000)
			{
				return "11";
			}

			if (build >= 10240)
			{
				return "10";
			}

			if (build >= 9600)
			{
				return "8.1";
			}

			if (build >= 9200)
			{
				return "8";
			}

			if (build >= 7600)
			{
				return "7";
			}

			return null;
		}
	}
}
