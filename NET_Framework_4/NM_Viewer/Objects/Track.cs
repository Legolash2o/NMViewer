using System;
using System.Collections.Generic;

namespace NM_Viewer.Objects
{
    public class Track
    {
        #region CONSTRUCTOR
        public Track()
        {

        }
        #endregion

        #region PUBLIC PROPERTIES
        public long Id { get; set; }

        public List<Node> Nodes { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public int TrackCategory { get; set; }

        public bool Directed { get; set; }


        #endregion
    }
}