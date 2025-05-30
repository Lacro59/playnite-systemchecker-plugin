﻿using CommonPluginsShared.Collections;
using CommonPluginsShared.Models;
using Playnite.SDK.Data;
using System.Collections.Generic;

namespace SystemChecker.Models
{
    public class GameRequierements : PluginDataBaseGame<Requirement>
    {
        private List<Requirement> _items = new List<Requirement>();
        public override List<Requirement> Items { get => _items; set => SetValue(ref _items, value); }

        [DontSerialize]
        public override bool HasData => (Items?.Count > 0) && (Items.Find(x => x.IsMinimum).HasData || Items.Find(x => !x.IsMinimum).HasData);


        public SourceLink SourcesLink { get; set; }


        public Requirement GetMinimum()
        {
            Requirement Minimum = Items?.Find(x => x.IsMinimum);
            if (Minimum == null)
            {
                Minimum = new Requirement();
            }
            return Minimum;
        }

        public Requirement GetRecommanded()
        {
            Requirement Recommanded = Items?.Find(x => x.IsMinimum == false);
            if (Recommanded == null)
            {
                Recommanded = new Requirement();
            }
            return Recommanded;
        }
    }
}
