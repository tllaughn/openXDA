﻿using GSF.Data.Model;

namespace openXDA.Adapters.Model
{
    public class MeterLocation
    {
        [PrimaryKey(true)]
        public int ID { get; set; }

        public string AssetKey { get; set; }

        public string Name { get; set; }

        public string Alias { get; set; }

        public string ShortName { get; set; }

        public float Latitude { get; set; }

        public float Longitude { get; set; }

        public string Description { get; set; }

    }
}
