using PluginCommon.Collections;
using System.Collections.Generic;

namespace SystemChecker.Models
{
    public class GameRequierements :  PluginDataBaseGame<Requirement>
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

        public string Link { get; set; } = string.Empty;


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
