using CommonPluginsShared.Collections;
using CommonPluginsShared.Models;
using CommonPluginsStores.Models;
using Playnite.SDK.Data;
using System.Collections.Generic;

namespace SystemChecker.Models
{
    public class PluginGameRequirements : PluginDataBaseGame<RequirementEntry>
    {
        [DontSerialize]
        public override bool HasData => (Items?.Count > 0) && ((Items.Find(x => x.IsMinimum)?.HasData ?? false) || (Items.Find(x => !x.IsMinimum)?.HasData ?? false));

        public SourceLink SourcesLink { get; set; }


        public RequirementEntry GetMinimum()
        {
            RequirementEntry Minimum = (Items?.Find(x => x.IsMinimum)) ?? new RequirementEntry();
            return Minimum;
        }

        public RequirementEntry GetRecommended()
        {
            RequirementEntry Recommended = (Items?.Find(x => x.IsMinimum == false)) ?? new RequirementEntry();
            return Recommended;
        }
    }
}