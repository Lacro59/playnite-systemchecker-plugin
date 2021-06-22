using CommonPluginsShared.Collections;
using CommonPluginsShared.Models;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace SystemChecker.Models
{
    public class GameRequierements : PluginDataBaseGame<Requirement>
    {
        private List<Requirement> _Items = new List<Requirement>();
        public override List<Requirement> Items
        {
            get
            {
                return _Items;
            }

            set
            {
                _Items = value;
                OnPropertyChanged();
            }
        }

        [JsonIgnore]
        public override bool HasData
        {
            get
            {
                if (Items.Count > 0)
                {
                    return Items.Find(x => x.IsMinimum).HasData;
                }

                return false;
            }
        }


        public SourceLink SourcesLink { get; set; }


        public Requirement GetMinimum()
        {
            Requirement Minimum = Items.Find(x => x.IsMinimum);

            if (Minimum == null)
            {
                Minimum = new Requirement();
            }

            return Minimum;
        }

        public Requirement GetRecommanded()
        {
            Requirement Recommanded = Items.Find(x => x.IsMinimum == false);

            if (Recommanded == null)
            {
                Recommanded = new Requirement();
            }

            return Recommanded;
        }
    }
}
