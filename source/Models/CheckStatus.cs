namespace SystemChecker.Models
{
	/// <summary>
	/// Outcome of a single hardware requirement comparison.
	/// </summary>
	public enum CheckStatus
	{
		/// <summary>The system meets or exceeds the requirement.</summary>
		Pass = 0,

		/// <summary>The system does not meet the requirement.</summary>
		Fail = 1,

		/// <summary>The comparison could not be performed reliably.</summary>
		Unknown = 2
	}
}
