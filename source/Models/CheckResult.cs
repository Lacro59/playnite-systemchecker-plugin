namespace SystemChecker.Models
{
	/// <summary>
	/// Result of a pairwise hardware comparison.
	/// </summary>
	public class CheckResult
	{
		/// <summary>Comparison outcome.</summary>
		public CheckStatus Status { get; set; }

		/// <summary>
		/// Legacy pass/fail accessor kept for existing comparisons and cache deserialization.
		/// </summary>
		public bool Result
		{
			get { return Status == CheckStatus.Pass; }
			set { Status = value ? CheckStatus.Pass : CheckStatus.Fail; }
		}

		/// <summary>Creates a successful comparison result.</summary>
		public static CheckResult Pass()
		{
			return new CheckResult { Status = CheckStatus.Pass };
		}

		/// <summary>Creates a failed comparison result.</summary>
		public static CheckResult Fail()
		{
			return new CheckResult { Status = CheckStatus.Fail };
		}

		/// <summary>Creates a result when the comparison could not be performed.</summary>
		public static CheckResult Unknown()
		{
			return new CheckResult { Status = CheckStatus.Unknown };
		}
	}
}
