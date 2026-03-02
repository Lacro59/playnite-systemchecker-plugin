using System;
using SystemChecker.Models;

namespace SystemChecker.Services
{
	/// <summary>
	/// Encapsulates check result with component identifiers for caching
	/// </summary>
	public class CheckData
	{
		public CheckResult Result { get; set; }
		public string ComponentPcName { get; set; }
		public string ComponentRequirementName { get; set; }
		public string CheckType { get; set; }
		public DateTime CreatedAt { get; set; }

		public CheckData()
		{
			CreatedAt = DateTime.Now;
		}

		public CheckData(CheckResult result, string componentPcName, string componentRequirementName, string checkType)
		{
			Result = result;
			ComponentPcName = componentPcName;
			ComponentRequirementName = componentRequirementName;
			CheckType = checkType;
			CreatedAt = DateTime.Now;
		}

		/// <summary>
		/// Generates unique cache key for this check
		/// </summary>
		public string GetCacheKey()
		{
			return $"{CheckType}|{ComponentPcName}|{ComponentRequirementName}";
		}

		/// <summary>
		/// Creates cache key from components
		/// </summary>
		public static string CreateCacheKey(string checkType, string componentPcName, string componentRequirementName)
		{
			return $"{checkType}|{componentPcName}|{componentRequirementName}";
		}

		/// <summary>
		/// Checks if cached data has expired (5 days)
		/// </summary>
		public bool IsExpired(int expirationDays = 5)
		{
			return DateTime.Now > CreatedAt.AddDays(expirationDays);
		}
	}
}