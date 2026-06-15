using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SystemChecker.Services.Parser
{
	/// <summary>
	/// Strips decorative Unicode (emoji, pictographs, variation selectors) from requirement text.
	/// </summary>
	public static class RequirementTextSanitizer
	{
		private static readonly Regex SurrogatePairRegex = new Regex(
			@"[\uD800-\uDBFF][\uDC00-\uDFFF]",
			RegexOptions.Compiled);

		private static readonly Regex MiscSymbolsRegex = new Regex(
			@"[\u2600-\u26FF\u2700-\u27BF]",
			RegexOptions.Compiled);

		private static readonly Regex VariationAndJoinerRegex = new Regex(
			@"[\uFE00-\uFE0F\u200D\u2060-\u2064]",
			RegexOptions.Compiled);

		private static readonly Regex ReplacementAndQuestionRegex = new Regex(
			@"[\uFFFD\?]+",
			RegexOptions.Compiled);

		private static readonly Regex MeaningfulContentRegex = new Regex(
			@"[\p{L}\p{N}]",
			RegexOptions.Compiled);

		/// <summary>
		/// Removes emoji, symbol pictographs, variation selectors, and replacement characters.
		/// </summary>
		public static string StripDecorativeCharacters(string text)
		{
			if (string.IsNullOrEmpty(text))
			{
				return text;
			}

			string value = SurrogatePairRegex.Replace(text, string.Empty);
			value = MiscSymbolsRegex.Replace(value, string.Empty);
			value = VariationAndJoinerRegex.Replace(value, string.Empty);
			value = ReplacementAndQuestionRegex.Replace(value, string.Empty);
			return Regex.Replace(value, @"\s+", " ").Trim();
		}

		/// <summary>
		/// Returns <c>true</c> when the text contains at least one letter or digit after stripping decoration.
		/// </summary>
		public static bool HasMeaningfulContent(string text)
		{
			if (string.IsNullOrWhiteSpace(text))
			{
				return false;
			}

			return MeaningfulContentRegex.IsMatch(StripDecorativeCharacters(text));
		}

		/// <summary>
		/// Returns <c>true</c> when the value is empty or contains only decorative characters.
		/// </summary>
		public static bool IsEmptyOrDecorativeOnly(string text)
		{
			return !HasMeaningfulContent(text);
		}

		/// <summary>
		/// Sanitises each list item in place; decorative-only entries are removed.
		/// </summary>
		public static void SanitizeStringList(List<string> list)
		{
			if (list == null || list.Count == 0)
			{
				return;
			}

			for (int i = list.Count - 1; i >= 0; i--)
			{
				string cleaned = StripDecorativeCharacters(list[i]);
				if (IsEmptyOrDecorativeOnly(cleaned))
				{
					list.RemoveAt(i);
				}
				else
				{
					list[i] = cleaned;
				}
			}
		}
	}
}
