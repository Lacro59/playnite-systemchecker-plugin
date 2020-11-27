using Playnite.SDK;
using PluginCommon.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
