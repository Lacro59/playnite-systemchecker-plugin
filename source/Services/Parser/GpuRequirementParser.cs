using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

using SystemChecker.Services;

namespace SystemChecker.Services.Parser
{
	/// <summary>
	/// Parses and normalises GPU requirement strings from store pages.
	/// </summary>
	public static class GpuRequirementParser
	{
		private static readonly Regex VramRegex = new Regex(@"vram", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		private static readonly Regex MbRegex = new Regex(@"mb", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		private static readonly Regex GbRegex = new Regex(@"gb", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		private static readonly Regex VramSizeRegex = new Regex(
			@"\(?\s*(\d+(?:[.,]\d+)?)\s*(mb|gb|go)\b",
			RegexOptions.IgnoreCase | RegexOptions.Compiled);
		private static readonly Regex DxRegex = new Regex(@"dx[0-9]*", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		private static readonly Regex NonDigitsRegex = new Regex(@"[^\d]", RegexOptions.Compiled);
		private static readonly Regex NvidiaChipRegex = new Regex(
			@"\b(gt|gts|gtx|rtx)\s*[- ]?\s*\d",
			RegexOptions.Compiled | RegexOptions.IgnoreCase);
		private static readonly Regex IntelGpuChipRegex = new Regex(
			@"\b(?:HD\s*Graphics?\s*\d{0,4}|HD\s*\d{3,4}|HD\d{3,4}|UHD\s*\d{3,4}|Iris)\b",
			RegexOptions.Compiled | RegexOptions.IgnoreCase);
		private static readonly Regex ResolutionOnlyRegex = new Regex(
			@"^\d{3,4}\s*[x×]\s*\d{3,4}\b",
			RegexOptions.Compiled | RegexOptions.IgnoreCase);
		private static readonly Regex AmdChipRegex = new Regex(
			@"\b(?:hd\s+radeon|radeon|r[579]\s*[- ]?\d|rx\s*\d|rx\s*\d{4})\b",
			RegexOptions.Compiled | RegexOptions.IgnoreCase);
		private static readonly Regex TrailingVramSuffixRegex = new Regex(
			@"[\s,]+(\d+(?:[.,]\d+)?)\s*(mb|gb|go|vram)\b.*$",
			RegexOptions.IgnoreCase | RegexOptions.Compiled);
		private static readonly Regex CjkRegex = new Regex(@"[\p{IsCJKUnifiedIdeographs}]+", RegexOptions.Compiled);

		/// <summary>
		/// Filters discardable entries, normalises tokens, and deduplicates.
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
				if (IsDiscardable(raw) || IsDiscardableGeneric(raw))
				{
					continue;
				}

				string token = NormalizeToken(raw);
				if (!string.IsNullOrWhiteSpace(token) && seen.Add(token))
				{
					result.Add(token);
				}
			}

			return result;
		}

		/// <summary>
		/// Normalises a single GPU requirement token for storage or comparison.
		/// </summary>
		public static string NormalizeToken(string raw)
		{
			if (string.IsNullOrWhiteSpace(raw))
			{
				return raw;
			}

			if (DirectXRequirementParser.TryNormalize(raw, out string directX))
			{
				return directX;
			}

			if (TryNormalizeVramToken(raw, out string vram))
			{
				return vram;
			}

			if (TryNormalizeChipToken(raw, out string chip))
			{
				return chip;
			}

			return StripMarketingNoise(raw);
		}

		/// <summary>
		/// Builds a <see cref="GpuObject"/> from a raw GPU name or requirement string.
		/// </summary>
		public static GpuObject Parse(string raw)
		{
			string gpuName = StripMarketingNoise(raw);
			string gpuLower = gpuName.ToLowerInvariant();

			long vram = ExtractVramBytes(raw);
			if (vram == 0)
			{
				vram = ExtractLegacyVramBytes(raw, gpuLower);
			}

			bool isIntegrate = IsIntel(gpuName);
			bool isNvidia = IsNvidia(gpuName);
			bool isAmd = IsAmd(gpuName);
			bool isDx = false;
			bool isOgl = false;
			int dxVersion = 0;
			int oglVersion = 0;

			if (gpuLower.IndexOf("directx", StringComparison.Ordinal) > -1
				|| gpuLower.IndexOf("direct3d", StringComparison.Ordinal) > -1
				|| DxRegex.IsMatch(gpuLower))
			{
				isDx = true;
				if (int.TryParse(NonDigitsRegex.Replace(gpuName, string.Empty).Trim(), out dxVersion) && dxVersion > 50)
				{
					dxVersion = int.Parse(dxVersion.ToString().Substring(0, dxVersion.ToString().Length - 1));
				}
				else if (dxVersion == 0)
				{
					dxVersion = gpuLower.IndexOf("direct3d", StringComparison.Ordinal) > -1 ? 3 : 8;
				}
			}

			if (gpuLower.IndexOf("opengl", StringComparison.Ordinal) > -1
				|| gpuLower.IndexOf("open gl", StringComparison.Ordinal) > -1)
			{
				isOgl = true;
				string temp = gpuLower.Replace("opengl", string.Empty).Replace("open gl", string.Empty).Replace(".0", string.Empty).Trim();
				int.TryParse(temp, out oglVersion);
			}

			return new GpuObject
			{
				Name = gpuName,
				IsIntegrate = isIntegrate,
				IsNvidia = isNvidia,
				IsAmd = isAmd,
				IsNamedModel = isNvidia || isAmd || isIntegrate,
				IsOGL = isOgl,
				IsDx = isDx,
				DxVersion = dxVersion,
				OglVersion = oglVersion,
				Vram = vram,
				ResolutionHorizontal = ExtractResolutionHorizontal(gpuLower),
			};
		}

		public static bool IsNvidia(string gpuName)
		{
			return gpuName.IndexOf("nvidia", StringComparison.OrdinalIgnoreCase) > -1
				|| gpuName.IndexOf("geforce", StringComparison.OrdinalIgnoreCase) > -1
				|| gpuName.IndexOf("gtforce", StringComparison.OrdinalIgnoreCase) > -1
				|| gpuName.IndexOf("gtx", StringComparison.OrdinalIgnoreCase) > -1
				|| gpuName.IndexOf("rtx", StringComparison.OrdinalIgnoreCase) > -1
				|| NvidiaChipRegex.IsMatch(gpuName);
		}

		public static bool IsAmd(string gpuName)
		{
			return gpuName.IndexOf("amd", StringComparison.OrdinalIgnoreCase) > -1
				|| gpuName.IndexOf("ati", StringComparison.OrdinalIgnoreCase) > -1
				|| gpuName.IndexOf("radeon", StringComparison.OrdinalIgnoreCase) > -1
				|| AmdChipRegex.IsMatch(gpuName);
		}

		public static bool IsIntel(string gpuName)
		{
			return gpuName.IndexOf("intel", StringComparison.OrdinalIgnoreCase) > -1
				|| IntelGpuChipRegex.IsMatch(gpuName)
				|| Regex.IsMatch(gpuName, @"\bHD\s+Graphics\b", RegexOptions.IgnoreCase);
		}

		public static bool IsDiscardable(string gpuName)
		{
			if (string.IsNullOrWhiteSpace(gpuName) || RequirementTextSanitizer.IsEmptyOrDecorativeOnly(gpuName))
			{
				return true;
			}

			string lower = gpuName.Trim().ToLowerInvariant();

			return lower == "integrated"
				|| lower == "integrated graphics"
				|| lower == "integrated graphic card"
				|| lower == "integrated gpu"
				|| lower == "anything"
				|| lower == "dedicated"
				|| lower == "dedicated graphics"
				|| lower == "video card"
				|| lower == "graphics card"
				|| lower == "discreet video card"
				|| lower == "discrete video card"
				|| lower == "discreet graphics card"
				|| lower == "discrete graphics card";
		}

		public static bool IsDiscardableGeneric(string gpuName)
		{
			if (string.IsNullOrWhiteSpace(gpuName))
			{
				return false;
			}

			string lower = gpuName.Trim().ToLowerInvariant();

			if (CjkRegex.IsMatch(gpuName))
			{
				return !IsNvidia(gpuName)
					&& !IsAmd(gpuName)
					&& !IsIntel(gpuName)
					&& !DirectXRequirementParser.TryNormalize(gpuName, out _);
			}

			if (lower.Contains("hardware accelerated") && lower.Contains("dedicated"))
			{
				return true;
			}

			return lower.Contains("hardware accelerated graphics")
				|| lower.Contains("dedicated video memory")
				|| lower.Contains("any graphics card")
				|| lower.Contains("any video card")
				|| lower.Contains("any gpu")
				|| lower.Contains("can work on integrated")
				|| lower.Contains("anything capable of running opengl")
				|| lower.Contains("anything capable of running open gl")
				|| lower.Contains("will do")
				|| (lower.Contains("video resolution") && !IsNvidia(gpuName) && !IsAmd(gpuName) && !IsIntel(gpuName))
				|| (lower.Contains("high color mode") && !IsNvidia(gpuName) && !IsAmd(gpuName) && !IsIntel(gpuName))
				|| ResolutionOnlyRegex.IsMatch(lower);
		}

		public static bool IsIntegratedRequirement(string gpuName)
		{
			string lower = gpuName.Trim().ToLowerInvariant();

			return lower.Contains("integrated video")
				|| lower.Contains("integrated graphics")
				|| lower.Contains("integrated gpu")
				|| lower.Contains("onboard graphics")
				|| lower.Contains("on-board graphics");
		}

		public static bool IsLegacyRequirement(string gpuName)
		{
			string lower = gpuName.Trim().ToLowerInvariant();

			return lower.Contains("3dfx")
				|| lower.Contains("voodoo")
				|| lower.Contains("glide");
		}

		public static long ExtractVramBytes(string raw)
		{
			if (!TryParseVramSize(raw, out double size, out string unit))
			{
				return 0;
			}

			if (string.Equals(unit, "GB", StringComparison.OrdinalIgnoreCase)
				|| string.Equals(unit, "GO", StringComparison.OrdinalIgnoreCase))
			{
				return (long)(size * 1024 * 1024);
			}

			return (long)(size * 1024);
		}

		private static int ExtractResolutionHorizontal(string gpuLower)
		{
			Match resolution = Regex.Match(gpuLower, @"\b(\d{3,4})\s*[x×]\s*\d{3,4}\b");
			if (resolution.Success && int.TryParse(resolution.Groups[1].Value, out int width))
			{
				return width;
			}

			if (gpuLower.IndexOf("1280", StringComparison.Ordinal) > -1
				&& (gpuLower.IndexOf("×720", StringComparison.Ordinal) > -1 || gpuLower.IndexOf("× 720", StringComparison.Ordinal) > -1))
			{
				return 1280;
			}

			if (gpuLower.IndexOf("1368×", StringComparison.Ordinal) > -1 || gpuLower.IndexOf("1368 ×", StringComparison.Ordinal) > -1)
			{
				return 1368;
			}

			if (gpuLower.IndexOf("1600×", StringComparison.Ordinal) > -1 || gpuLower.IndexOf("1600 ×", StringComparison.Ordinal) > -1)
			{
				return 1600;
			}

			if (gpuLower.IndexOf("1920×", StringComparison.Ordinal) > -1 || gpuLower.IndexOf("1920 ×", StringComparison.Ordinal) > -1)
			{
				return 1920;
			}

			return 0;
		}

		private static string StripMarketingNoise(string gpuName)
		{
			string value = gpuName
				.Replace("(R)", string.Empty, StringComparison.OrdinalIgnoreCase)
				.Replace("(TM)", string.Empty, StringComparison.OrdinalIgnoreCase)
				.Replace("(tm)", string.Empty, StringComparison.OrdinalIgnoreCase)
				.Replace("Graphics", string.Empty, StringComparison.OrdinalIgnoreCase)
				.Trim();

			value = RequirementTextSanitizer.StripDecorativeCharacters(value);
			value = CjkRegex.Replace(value, string.Empty);
			value = Regex.Replace(value, @"(\d+)\s*go\b", "$1 GB", RegexOptions.IgnoreCase);
			value = Regex.Replace(value, @"\bgtforce\b", "GeForce", RegexOptions.IgnoreCase);
			value = Regex.Replace(value, @"\bsli\b", string.Empty, RegexOptions.IgnoreCase);
			value = TrailingVramSuffixRegex.Replace(value, string.Empty).Trim();
			return Regex.Replace(value, @"\s+", " ").Trim();
		}

		private static bool TryNormalizeChipToken(string raw, out string normalized)
		{
			normalized = null;
			string value = raw.Trim();

			if (NvidiaChipRegex.IsMatch(value) || AmdChipRegex.IsMatch(value))
			{
				normalized = StripMarketingNoise(value);
				if (NvidiaChipRegex.IsMatch(normalized) && normalized.IndexOf("geforce", StringComparison.OrdinalIgnoreCase) < 0
					&& normalized.IndexOf("nvidia", StringComparison.OrdinalIgnoreCase) < 0)
				{
					normalized = $"GeForce {normalized}";
				}

				if (AmdChipRegex.IsMatch(normalized) && normalized.IndexOf("radeon", StringComparison.OrdinalIgnoreCase) < 0
					&& normalized.IndexOf("amd", StringComparison.OrdinalIgnoreCase) < 0)
				{
					normalized = $"Radeon {normalized}";
				}

				return true;
			}

			if (IntelGpuChipRegex.IsMatch(value))
			{
				normalized = StripMarketingNoise(value);
				normalized = normalized.IndexOf("intel", StringComparison.OrdinalIgnoreCase) >= 0
					? normalized
					: $"Intel {normalized}";
				return true;
			}

			return false;
		}

		private static bool TryNormalizeVramToken(string raw, out string normalized)
		{
			normalized = null;

			if (IsNvidia(raw) || IsAmd(raw))
			{
				return false;
			}

			if (!VramSizeRegex.IsMatch(raw) || !TryParseVramSize(raw, out double size, out string unit))
			{
				return false;
			}

			string sizeText = size % 1 == 0
				? ((int)size).ToString(CultureInfo.InvariantCulture)
				: size.ToString("0.#", CultureInfo.InvariantCulture);

			normalized = $"{sizeText} {unit} VRAM";
			return true;
		}

		private static long ExtractLegacyVramBytes(string gpuRequirement, string gpuLower)
		{
			if (gpuLower.IndexOf("vram", StringComparison.Ordinal) <= -1
				|| IsNvidia(gpuRequirement)
				|| IsAmd(gpuRequirement))
			{
				return 0;
			}

			string tempVram = gpuRequirement.Replace(".", CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalSeparator);
			tempVram = VramRegex.Replace(tempVram, string.Empty);

			if (gpuLower.IndexOf("mb", StringComparison.Ordinal) > -1)
			{
				if (double.TryParse(MbRegex.Replace(tempVram, string.Empty).Trim(), out double vram) && vram > 0)
				{
					return (long)(vram * 1024);
				}
			}
			else if (gpuLower.IndexOf("gb", StringComparison.Ordinal) > -1)
			{
				if (double.TryParse(GbRegex.Replace(tempVram, string.Empty).Trim(), out double vram) && vram > 0)
				{
					return (long)(vram * 1024 * 1024);
				}
			}

			return 0;
		}

		private static bool TryParseVramSize(string raw, out double size, out string unit)
		{
			size = 0;
			unit = null;

			Match match = VramSizeRegex.Match(raw);
			if (!match.Success)
			{
				return false;
			}

			if (!double.TryParse(
				match.Groups[1].Value.Replace(",", "."),
				NumberStyles.Float,
				CultureInfo.InvariantCulture,
				out size))
			{
				return false;
			}

			unit = match.Groups[2].Value.ToUpperInvariant();
			if (unit == "GO")
			{
				unit = "GB";
			}
			return size > 0;
		}
	}
}
