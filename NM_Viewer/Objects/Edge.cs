using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NM_Viewer.Objects
{
    public class Edge
    {
        #region CONSTRUCTORS
        public Edge()
        {
            Nodes = new List<Node>();
        }
        #endregion

        #region PUBLIC PROPERTIES
        public long Id { get; set; }

        public long Length { get; set; }


        public Node EndNode { get; set; }

        public List<Node> Nodes { get; set; }
      

        #endregion

    }
}
