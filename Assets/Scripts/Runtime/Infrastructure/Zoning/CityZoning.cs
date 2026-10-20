using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace HarborCity
{
    [Serializable] public sealed class ZoneDesignation
    {
        public int id;
        public string key;
        public LandUse use;
    }
    [Serializable] public sealed class CityZoningState
    {
        // Prototype world scale. Four rows follow CS1; metres per unit remain unverified.
        public const float CellSize=1.5f;
        public const int Depth=4;
        public const string MapId="harbor-terrain-v1";
        public int nextId=1;
        public string mapId=MapId;
        public float cellSize=CellSize;
        public List<ZoneDesignation> designations=new List<ZoneDesignation>();
        [NonSerialized] public int revision;
        public static bool ZoneUse(LandUse use)=>use==LandUse.Residential || use==LandUse.Commercial || use==LandUse.Industrial;
        public bool Valid(float buildExtent=CityModel.BuildHalfSize)
        {
            if(mapId!=MapId || cellSize!=CellSize || nextId<1 || designations==null || designations.Count>400000) return false;
            var ids=new HashSet<int>();var keys=new HashSet<string>();
            return designations.All(d=>d!=null && d.id>0 && d.id<nextId && ids.Add(d.id)
                && ZoneUse(d.use) && ZoneCell.ValidKey(d.key,buildExtent) && keys.Add(d.key));
        }
    }
    public sealed class ZoneCell
    {
        public string key,block;
        public int column,row,side,occupiedBuilding=-1;
        public bool available;
        public LandUse use;
        public CityBuilding pose;
        public static string Key(CityBuilding b)
        {
#if CITY_CORE_ATTRIBUTION
            using var keyObservation=HarborCity.RoadCore.CityCoreProfile.Measure(HarborCity.RoadCore.CityCoreProfile.Part.ZoningCellKey);
#endif
            var values=new[]{Math.Round(b.x,3),Math.Round(b.z,3),Math.Round(b.yaw,3)%360};
            return string.Join(":",values.Select(v=>(v==0?0:v).ToString("F3",CultureInfo.InvariantCulture)));
        }
        public static bool ValidKey(string key,float buildExtent=CityModel.BuildHalfSize)
        {
            if(key==null || key.Length>64) return false;
            var parts=key.Split(':');if(parts.Length!=3) return false;
            var values=new float[3];
            for(int i=0;i<3;i++) if(!float.TryParse(parts[i],NumberStyles.Float,CultureInfo.InvariantCulture,out values[i]) || float.IsNaN(values[i]) || float.IsInfinity(values[i])) return false;
            return Math.Abs(values[0])<=buildExtent && Math.Abs(values[1])<=buildExtent && values[2]>=0 && values[2]<360;
        }
    }
    public sealed partial class CityModel
    {
        public CityZoningState zoning=new CityZoningState();
    }
}
