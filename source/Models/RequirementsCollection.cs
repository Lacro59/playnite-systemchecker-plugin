using Playnite.SDK;
using CommonPluginsShared.Collections;
using CommonPluginsShared.SystemInfo;

namespace SystemChecker.Models
{
    public class RequirementsCollection : PluginItemCollection<PluginGameRequirements>
    {
        public SystemConfiguration PC { get; set; }

        public RequirementsCollection(string path, GameDatabaseCollection type = GameDatabaseCollection.Uknown) : base(path, type)
        {
        }
    }
}