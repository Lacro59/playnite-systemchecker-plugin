using System;
using System.Globalization;
using System.Text.RegularExpressions;
using CommonPluginsStores.Models;

namespace SystemChecker.Services.Parser
{
	/// <summary>
	/// Parses RAM and storage requirement strings into byte values.
	/// Handles HTML fragments, multiple slash-separated sizes, and MB/GB/TB/KB units.
	/// </summary>
	public static class RequirementSizeParser
	{
		private static readonly Regex HtmlTagRegex = new Regex(@"<[^>]*>", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		private static readonly Regex SizeTokenRegex = new Regex(
			@"(\d+(?:[.,]\d+)?)\s*(tb|gb|mb|kb)\b",
			RegexOptions.IgnoreCase | RegexOptions.Compiled);
		private static readonly Regex LabelRegex = new Regex(
			@"(?i)\b(memory|ram|storage|hard\s*drive|hdd|ssd|free\s*disk\s*space|available\s*space|video\s*card)\s*:?\s*",
			RegexOptions.Compiled);

		/// <summary>
		/// Parses a raw size string (plain text or HTML) into bytes. Returns 0 when unrecognised.
		/// </summary>
		public static double ParseToBytes(string raw)
		{
			if (string.IsNullOrWhiteSpace(raw))
			{
				return 0;
			}

			string token = ExtractSizeToken(PrepareText(raw));
			if (token == null)
			{
				return 0;
			}

			Match match = SizeTokenRegex.Match(token);
			if (!match.Success)
			{
				return 0;
			}

			if (!TryParseNumber(match.Groups[1].Value, out double number))
			{
				return 0;
			}

			return UnitToBytes(number, match.Groups[2].Value.ToUpperInvariant());
		}

		/// <summary>
		/// Applies <see cref="ParseToBytes"/> to <paramref name="entry"/> when source fields are set.
		/// Existing byte values are kept when no source is present (backward-compatible cache).
		/// </summary>
		public static void NormalizeEntrySizes(RequirementEntry entry)
		{
			if (entry == null)
			{
				return;
			}

			if (!string.IsNullOrWhiteSpace(entry.RamSource))
			{
				double bytes = ParseToBytes(entry.RamSource);
				if (bytes > 0)
				{
					entry.Ram = bytes;
				}
			}

			if (!string.IsNullOrWhiteSpace(entry.StorageSource))
			{
				double bytes = ParseToBytes(entry.StorageSource);
				if (bytes > 0)
				{
					entry.Storage = bytes;
				}
			}
		}

		private static string PrepareText(string raw)
		{
			string text = HtmlTagRegex.Replace(raw, " ");
			text = text.Replace("\t", " ").Replace("\u00A0", " ");
			text = LabelRegex.Replace(text, string.Empty);
			text = Regex.Replace(text, @"(?i)\b(cpu\s*speed|os|video\s*card)\s*:\s*", string.Empty);
			text = Regex.Replace(text, @"(?i)\bram\s*:\s*", string.Empty);
			text = Regex.Replace(text, @"\.\s*mb\b", " ", RegexOptions.IgnoreCase);
			text = Regex.Replace(text, @"(?i)\bmb\s+(ram|available)\b", " ");
			text = Regex.Replace(text, @"(?i)ram\s+mb\s+ram", " ");
			text = Regex.Replace(text, @"\s+", " ").Trim();
			return text;
		}

		private static string ExtractSizeToken(string text)
		{
			if (text.IndexOf('/') >= 0)
			{
				string[] parts = text.Split('/');
				text = parts[parts.Length - 1].Trim();
			}

			Match match = SizeTokenRegex.Match(text);
			return match.Success ? match.Value.Trim() : null;
		}

		private static bool TryParseNumber(string value, out double number)
		{
			string normalised = value.Replace(',', '.');
			return double.TryParse(
				normalised,
				NumberStyles.Any,
				CultureInfo.InvariantCulture,
				out number);
		}

		private static double UnitToBytes(double number, string unit)
		{
			switch (unit)
			{
				case "TB":
					return number * 1024L * 1024 * 1024 * 1024;
				case "GB":
					return number * 1024L * 1024 * 1024;
				case "MB":
					return number * 1024L * 1024;
				case "KB":
					return number * 1024L;
				default:
					return 0;
			}
		}
	}
}
