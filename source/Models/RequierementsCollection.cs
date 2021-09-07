using Playnite.SDK;
using Playnite.SDK.Models;
using CommonPluginsShared.Collections;
using CommonPluginsPlaynite.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonPluginsShared;

namespace SystemChecker.Models
{
    public class RequierementsCollection : PluginItemCollection<GameRequierements>
    {
        public SystemConfiguration PC { get; set; }

        public RequierementsCollection(string path, GameDatabaseCollection type = GameDatabaseCollection.Uknown) : base(path, type)
        {
        }
    }
}
