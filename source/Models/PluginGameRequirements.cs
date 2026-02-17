using CommonPluginsShared.Collections;
using CommonPluginsShared.Models;
using CommonPluginsStores.Models;
using Playnite.SDK.Data;

namespace SystemChecker.Models
{
	/// <summary>
	/// Represents game system requirements data including minimum and recommended specifications.
	/// Extends plugin base functionality with requirement-specific operations.
	/// </summary>
	public class PluginGameRequirements : PluginDataBaseGame<RequirementEntry>
	{
		#region Properties

		/// <summary>
		/// Gets a value indicating whether this instance contains valid requirement data.
		/// Returns true if at least one requirement entry (minimum or recommended) has data.
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

				RequirementEntry minimum = Items.Find(x => x.IsMinimum);
				RequirementEntry recommended = Items.Find(x => !x.IsMinimum);

				return (minimum?.HasData ?? false) || (recommended?.HasData ?? false);
			}
		}

		/// <summary>
		/// Gets or sets the source link containing the origin URL of the requirements data.
		/// </summary>
		public SourceLink SourcesLink { get; set; }

		#endregion

		#region Public Methods

		/// <summary>
		/// Retrieves the minimum system requirements entry for the game.
		/// </summary>
		/// <returns>
		/// The minimum requirements entry if found, otherwise a new empty <see cref="RequirementEntry"/> instance.
		/// </returns>
		public RequirementEntry GetMinimum()
		{
			return FindRequirementByType(isMinimum: true);
		}

		/// <summary>
		/// Retrieves the recommended system requirements entry for the game.
		/// </summary>
		/// <returns>
		/// The recommended requirements entry if found, otherwise a new empty <see cref="RequirementEntry"/> instance.
		/// </returns>
		public RequirementEntry GetRecommended()
		{
			return FindRequirementByType(isMinimum: false);
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Finds a requirement entry by its type (minimum or recommended).
		/// </summary>
		/// <param name="isMinimum">True to find minimum requirements, false for recommended requirements.</param>
		/// <returns>
		/// The matching requirement entry if found, otherwise a new empty <see cref="RequirementEntry"/> instance.
		/// </returns>
		private RequirementEntry FindRequirementByType(bool isMinimum)
		{
			RequirementEntry requirement = Items?.Find(x => x.IsMinimum == isMinimum);
			return requirement ?? new RequirementEntry();
		}

		#endregion
	}
}