using System.Collections.Generic;

namespace NM_Viewer.Objects
{
    public class Node
    {
        #region CONSTRUCTOR
        public Node()
        {
            Edges = new List<Edge>();
            Stations = new List<Station>();
        }
        #endregion

        #region PUBLIC PROPERTIES
        public long Id { get; set; }

        public double X { get; set; }

        public double Y { get; set; }

        public string Name { get; set; }

        public bool Intersection
        {
            get
            {
                if (Edges.Count == 0)
                    return false;

                if (Edges.Count > 1)
                    return true;

                return false;
            }
        }


        public List<Edge> Edges { get; set; }


        public List<Station> Stations { get; set; }
  
        #endregion
    }
}
