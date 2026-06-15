namespace SystemChecker.Models
{
	/// <summary>
	/// Aggregated system requirement check for a single requirement tier (minimum or recommended).
	/// </summary>
	public class CheckSystem
	{
		/// <summary>Operating system check outcome.</summary>
		public CheckStatus CheckOs { get; set; }

		/// <summary>CPU check outcome.</summary>
		public CheckStatus CheckCpu { get; set; }

		/// <summary>RAM check outcome.</summary>
		public CheckStatus CheckRam { get; set; }

		/// <summary>GPU check outcome.</summary>
		public CheckStatus CheckGpu { get; set; }

		/// <summary>Storage check outcome.</summary>
		public CheckStatus CheckStorage { get; set; }

		/// <summary>
		/// Overall outcome: <c>true</c> if all pass, <c>false</c> if any fail,
		/// <c>null</c> if no failure but at least one component is unknown.
		/// </summary>
		public bool? AllOk { get; set; } = null;
	}
}
