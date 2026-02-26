using CommonPluginsShared.Collections;
using CommonPluginsShared.Models;
using CommonPluginsStores.Models;
using LiteDB;
using Playnite.SDK.Data;

namespace SystemChecker.Models
{
	/// <summary>
	/// Stores system requirements for a single game (minimum and recommended entries).
	/// Extends <see cref="PluginDataBaseGame{T}"/> with requirement-specific accessors.
	/// </summary>
	public class PluginGameRequirements : PluginDataBaseGame<RequirementEntry>
	{
		/// <summary>
		/// Returns <c>true</c> when at least one requirement entry contains data.
		/// Cached until Items changes via <see cref="RefreshCachedValues"/>.
		/// </summary>
		[DontSerialize]
		[BsonIgnore]
		public override bool HasData
		{
			get
			{
				if (!_hasData.HasValue)
				{
					_hasData = Items != null
						&& Items.Count > 0
						&& ((GetMinimum()?.HasData ?? false)
							|| (GetRecommended()?.HasData ?? false));
				}
				return _hasData.Value;
			}
		}

		/// <summary>
		/// Returns the number of requirement entries that actually contain data (0, 1 or 2).
		/// Cached until Items changes via <see cref="RefreshCachedValues"/>.
		/// </summary>
		[DontSerialize]
		[BsonIgnore]
		public override ulong Count
		{
			get
			{
				if (!_count.HasValue)
				{
					ulong count = 0;
					if (GetMinimum()?.HasData ?? false) count++;
					if (GetRecommended()?.HasData ?? false) count++;
					_count = count;
				}
				return _count.Value;
			}
		}

		#region Accessors

		/// <summary>
		/// Returns the minimum requirements entry, or a new empty <see cref="RequirementEntry"/>
		/// when none exists.
		/// </summary>
		public RequirementEntry GetMinimum() => FindRequirementByType(isMinimum: true);

		/// <summary>
		/// Returns the recommended requirements entry, or a new empty <see cref="RequirementEntry"/>
		/// when none exists.
		/// </summary>
		public RequirementEntry GetRecommended() => FindRequirementByType(isMinimum: false);

		#endregion

		#region Private helpers

		/// <summary>
		/// Searches <see cref="PluginDataBaseGame{T}.Items"/> for a <see cref="RequirementEntry"/>
		/// whose <see cref="RequirementEntry.IsMinimum"/> flag matches <paramref name="isMinimum"/>.
		/// </summary>
		private RequirementEntry FindRequirementByType(bool isMinimum)
		{
			return Items?.Find(x => x.IsMinimum == isMinimum) ?? new RequirementEntry();
		}

		#endregion
	}
}