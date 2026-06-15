namespace SystemChecker.Models
{
	/// <summary>
	/// Helpers for <see cref="CheckStatus"/> aggregation and UI display.
	/// </summary>
	public static class CheckStatusExtensions
	{
		/// <summary>
		/// Returns the icon glyph used by <see cref="Controls.StatusIcon"/>.
		/// </summary>
		public static string ToIcon(this CheckStatus status)
		{
			switch (status)
			{
				case CheckStatus.Pass:
					return "\u2713";
				case CheckStatus.Fail:
					return "\u2717";
				case CheckStatus.Unknown:
					return "\u26a0";
				default:
					return string.Empty;
			}
		}

		/// <summary>
		/// Aggregates component statuses into an overall result.
		/// Any failure fails the whole check; unknowns yield <c>null</c> when no failure exists.
		/// </summary>
		public static bool? ToAllOk(params CheckStatus[] statuses)
		{
			bool hasUnknown = false;

			foreach (CheckStatus status in statuses)
			{
				if (status == CheckStatus.Fail)
				{
					return false;
				}

				if (status == CheckStatus.Unknown)
				{
					hasUnknown = true;
				}
			}

			return hasUnknown ? (bool?)null : true;
		}
	}
}
