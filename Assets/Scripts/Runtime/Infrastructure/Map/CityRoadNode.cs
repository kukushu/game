using System;
using System.Collections.Generic;
namespace HarborCity
{
    [Serializable]
    public sealed class RoadNode
    {
        public int id;
        public float x, y, z;
        public JunctionControl control;
        public List<int> stopFrom=new List<int>();
        public RoadNode Copy() => new RoadNode { id = id, x = x, y = y, z = z,control=control,stopFrom=stopFrom==null?new List<int>():new List<int>(stopFrom) };
    }
}
