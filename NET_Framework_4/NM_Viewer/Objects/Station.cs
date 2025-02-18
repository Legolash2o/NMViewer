using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NM_Viewer.Objects
{
   public class Station
    {
        public long Id { get; set; }
   

        public string Name { get; set; }

        public string TIPLOC { get; set; }

        public byte R { get; set; }

        public byte G { get; set; }

        public byte B { get; set; }
    }
}
