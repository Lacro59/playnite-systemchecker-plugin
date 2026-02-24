using CommonPluginsShared.Collections;
using CommonPluginsShared.SystemInfo;
using CommonPluginsStores.Models;
using Playnite.SDK.Models;
using SystemChecker.Models;
using SystemChecker.Services;

namespace SystemChecker.Controls
{
	/// <summary>
	/// Provides shared constants and utility methods used by SystemChecker plugin controls.
	/// This class is not a UI control — it exists solely to avoid logic duplication
	/// between <see cref="PluginButton"/> and <see cref="PluginViewItem"/>.
	/// </summary>
	internal static class PluginControlHelper
	{
		// ── Icon glyphs (font-based) ──────────────────────────────────────────

		/// <summary>All system requirements are met (recommended).</summary>
		public const string IconOk = "\uea50";

		/// <summary>System does not meet minimum requirements.</summary>
		public const string IconKo = "\uea52";

		/// <summary>System meets minimum but not recommended requirements.</summary>
		public const string IconMinimum = "\uea51";

		/// <summary>No requirement data available for this game.</summary>
		public const string IconEmpty = "\uea53";

		// ── Core logic ────────────────────────────────────────────────────────

		/// <summary>
		/// Evaluates the game's system requirements against the current PC configuration
		/// and returns the appropriate icon glyph to display.
		/// Returns <c>null</c> if the evaluation cannot proceed (missing PC config or no data).
		/// </summary>
		/// <param name="game">The game being evaluated.</param>
		/// <param name="pluginGameData">Plugin data holding the requirement entries.</param>
		/// <param name="pluginDatabase">The SystemChecker database instance.</param>
		/// <returns>
		/// The icon glyph string if evaluation succeeded; <c>null</c> if requirements
		/// are unavailable or PC configuration is missing.
		/// </returns>
		public static string ResolveIcon(
			Game game,
			PluginDataBaseGameBase pluginGameData,
			SystemCheckerDatabase pluginDatabase)
		{
			SystemConfiguration systemConfiguration = pluginDatabase.PC;
			if (systemConfiguration == null)
			{
				return null;
			}

			var pluginGameRequirements = (PluginGameRequirements)pluginGameData;

			RequirementEntry systemMinimum = pluginGameRequirements.GetMinimum();
			RequirementEntry systemRecommended = pluginGameRequirements.GetRecommended();

			bool hasMinimum = systemMinimum.HasData;
			bool hasRecommended = systemRecommended.HasData;

			if (!hasMinimum && !hasRecommended)
			{
				return null;
			}

			CheckSystem checkMinimum = hasMinimum
				? SystemApi.CheckConfig(game, systemMinimum, systemConfiguration, game.IsInstalled)
				: null;

			CheckSystem checkRecommended = hasRecommended
				? SystemApi.CheckConfig(game, systemRecommended, systemConfiguration, game.IsInstalled)
				: null;

			return DetermineIcon(hasMinimum, hasRecommended, checkMinimum, checkRecommended);
		}

		/// <summary>
		/// Resolves the appropriate icon glyph based on requirement check results.
		/// </summary>
		/// <param name="hasMinimum">Whether minimum requirement data exists.</param>
		/// <param name="hasRecommended">Whether recommended requirement data exists.</param>
		/// <param name="checkMinimum">Result of the minimum requirements check, or <c>null</c>.</param>
		/// <param name="checkRecommended">Result of the recommended requirements check, or <c>null</c>.</param>
		/// <returns>The icon glyph string to display.</returns>
		public static string DetermineIcon(
			bool hasMinimum,
			bool hasRecommended,
			CheckSystem checkMinimum,
			CheckSystem checkRecommended)
		{
			if (hasMinimum)
			{
				if (!(checkMinimum?.AllOk ?? false))
				{
					return IconKo;
				}

				if (hasRecommended)
				{
					return (checkRecommended?.AllOk ?? false) ? IconOk : IconMinimum;
				}

				return IconOk;
			}

			if (hasRecommended)
			{
				return (checkRecommended?.AllOk ?? false) ? IconOk : IconKo;
			}

			return IconEmpty;
		}
	}
}