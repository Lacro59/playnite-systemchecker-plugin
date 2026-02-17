using CommonPluginsShared.Collections;
using CommonPluginsShared.Models;
using CommonPluginsStores.Models;
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
		/// Returns <c>true</c> when at least one requirement entry (minimum or recommended) contains data.
		/// Not serialised — recomputed on each access.
		/// </summary>
		[DontSerialize]
		public override bool HasData
		{
			get
			{
				if (Items == null || Items.Count == 0)
				{
					return false;
				}

				return (GetMinimum()?.HasData ?? false)
					|| (GetRecommended()?.HasData ?? false);
			}
		}

		/// <summary>Gets or sets the URL from which the requirements data was scraped.</summary>
		public SourceLink SourcesLink { get; set; }

		#region Accessors

		/// <summary>
		/// Returns the minimum requirements entry, or a new empty <see cref="RequirementEntry"/>
		/// when none exists (so callers can always dereference the result safely).
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