using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SystemChecker.Services.Parser
{
	/// <summary>
	/// Normalises DirectX requirement strings to a canonical <c>DirectX N</c> token.
	/// </summary>
	public static class DirectXRequirementParser
	{
		private static readonly Regex DirectXVersionRegex = new Regex(
			@"(?:direct\s*x|directx|dx)\s*(\d+)",
			RegexOptions.IgnoreCase | RegexOptions.Compiled);
		private static readonly Regex DxCompactRegex = new Regex(
			@"\bdx(\d+)\b",
			RegexOptions.IgnoreCase | RegexOptions.Compiled);

		private static readonly Regex FeatureLevelRegex = new Regex(
			@"\b(?:fl|feature\s*level)\s*(\d+)",
			RegexOptions.IgnoreCase | RegexOptions.Compiled);

		/// <summary>
		/// Attempts to normalise a DirectX requirement to <c>DirectX N</c> (major version only).
		/// </summary>
		public static bool TryNormalize(string raw, out string normalized)
		{
			normalized = null;

			if (string.IsNullOrWhiteSpace(raw))
			{
				return false;
			}

			string value = raw.Trim();

			if (!LooksLikeDirectXRequirement(value))
			{
				return false;
			}

			Match directXMatch = DirectXVersionRegex.Match(value);
			if (directXMatch.Success && int.TryParse(directXMatch.Groups[1].Value, out int version) && IsSupportedVersion(version))
			{
				normalized = $"DirectX {version}";
				return true;
			}

			Match dxCompactMatch = DxCompactRegex.Match(value);
			if (dxCompactMatch.Success && int.TryParse(dxCompactMatch.Groups[1].Value, out int compactVersion) && IsSupportedVersion(compactVersion))
			{
				normalized = $"DirectX {compactVersion}";
				return true;
			}

			Match featureLevelMatch = FeatureLevelRegex.Match(value);
			if (featureLevelMatch.Success && int.TryParse(featureLevelMatch.Groups[1].Value, out int featureLevel) && IsSupportedVersion(featureLevel))
			{
				normalized = $"DirectX {featureLevel}";
				return true;
			}

			if (value.IndexOf("direct3d", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				normalized = "DirectX 3";
				return true;
			}

			return false;
		}

		/// <summary>
		/// Normalises DirectX tokens in <paramref name="raw"/>; returns the original value for other GPU entries.
		/// </summary>
		public static string NormalizeGpuToken(string raw)
		{
			return TryNormalize(raw, out string normalized) ? normalized : raw;
		}

		/// <summary>
		/// Normalises DirectX tokens and removes duplicates. Unparseable DirectX entries are discarded.
		/// </summary>
		public static List<string> NormalizeGpuList(List<string> gpuList)
		{
			if (gpuList == null || gpuList.Count == 0)
			{
				return gpuList ?? new List<string>();
			}

			var result = new List<string>();
			var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			foreach (string raw in gpuList)
			{
				string token;
				if (LooksLikeDirectXRequirement(raw))
				{
					if (!TryNormalize(raw, out token))
					{
						continue;
					}
				}
				else
				{
					token = raw;
				}

				if (seen.Add(token))
				{
					result.Add(token);
				}
			}

			return result;
		}

		private static bool LooksLikeDirectXRequirement(string value)
		{
			if (GpuRequirementParser.IsNvidia(value) || GpuRequirementParser.IsAmd(value) || GpuRequirementParser.IsIntel(value))
			{
				return false;
			}

			string lower = value.ToLowerInvariant();

			return lower.Contains("directx")
				|| lower.Contains("direct x")
				|| lower.Contains("direct3d")
				|| Regex.IsMatch(lower, @"\bdx\s*\d")
				|| Regex.IsMatch(lower, @"\bdx\d")
				|| FeatureLevelRegex.IsMatch(lower);
		}

		private static bool IsSupportedVersion(int version)
		{
			return version >= 3 && version <= 12;
		}
	}
}
