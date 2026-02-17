using System.Collections.Generic;

namespace SystemChecker.Models
{
	/// <summary>
	/// Encapsulates parsed search parameters including flags, filters, and cleaned search text.
	/// </summary>
	public class SearchParameters
	{
		/// <summary>
		/// Gets or sets a value indicating whether to filter by minimum requirements (-min flag).
		/// </summary>
		public bool HasMin { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether to filter by recommended requirements (-rec flag).
		/// </summary>
		public bool HasRec { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether to filter by any requirements (-any flag).
		/// </summary>
		public bool HasAny { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether to filter by not played games (-np flag).
		/// </summary>
		public bool HasNp { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether to filter by favorite games only (-fav flag).
		/// </summary>
		public bool HasFav { get; set; }

		/// <summary>
		/// Gets or sets the list of store names to filter by (-stores= parameter).
		/// </summary>
		public List<string> Stores { get; set; } = new List<string>();

		/// <summary>
		/// Gets or sets the list of completion statuses to filter by (-status= parameter).
		/// </summary>
		public List<string> Status { get; set; } = new List<string>();

		/// <summary>
		/// Gets or sets the cleaned search term with all flags and parameters removed.
		/// Used for matching game names.
		/// </summary>
		public string CleanSearchTerm { get; set; } = string.Empty;
	}
}