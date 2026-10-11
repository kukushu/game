using System;
namespace HarborCity
{
    public enum RoadStructure { Ground, Elevated, Tunnel }
    public sealed partial class CityRoads
    {
        // Prototype world scale, not verified original-game metre values.
        public const float VerticalClearance=3;
        public static float StructureMultiplier(RoadStructure structure)=>structure==RoadStructure.Tunnel?4:structure==RoadStructure.Elevated?2.5f:1;
        public static RoadStructure Structure(RoadNode a,RoadNode b,Func<float,float,float> height)
        {
            var p=Lerp(a,b,.5f);float delta=p.y-height(p.x,p.z);
            if(Math.Abs(delta)<=.3f)
            {
                float first=a.y-height(a.x,a.z),last=b.y-height(b.x,b.z);
                delta=Math.Abs(first)>Math.Abs(last)?first:last;
            }
            return delta>.3f?RoadStructure.Elevated:delta<-.3f?RoadStructure.Tunnel:RoadStructure.Ground;
        }
        public bool Underground(int a,int b)=>edges.Exists(e=>(e.a==a && e.b==b || e.b==a && e.a==b) && e.structure==RoadStructure.Tunnel);
        public bool Ground(int a,int b)=>edges.Exists(e=>(e.a==a && e.b==b || e.b==a && e.a==b) && e.structure==RoadStructure.Ground);
        static int ProfileCost(RoadNode a,RoadNode b,Func<float,float,float> height,RoadKind kind,bool engineered)
        {
            float length=Length(a,b);if(!engineered)return CityRoadTypes.ConstructionCost(length,kind);
            int steps=Math.Max(1,(int)Math.Ceiling(length/3));double amount=0;
            for(int i=0;i<steps;i++)amount+=length/steps/CityRoadTypes.CellLength*CityRoadTypes.CostPerCell(kind)*StructureMultiplier(Structure(Lerp(a,b,(float)i/steps),Lerp(a,b,(float)(i+1)/steps),height));
            return (int)Math.Ceiling(amount);
        }
        // Conservative: above-ground buildings always reserve their footprint;
        // buried roads may pass beneath only with at least one unit of cover.
        public static bool BuildingCollision(CityBuilding building,RoadNode a,RoadNode b,float width,Func<float,float,float> height,bool engineered)
        {
            if(!building.HitsRoad(a,b,width))return false;
            if(!engineered)return true;
            int steps=Math.Max(1,(int)Math.Ceiling(Length(a,b)/.5f));
            for(int i=0;i<=steps;i++)
            {
                var p=Lerp(a,b,(float)i/steps);
                if(building.HitsRoad(p,Lerp(a,b,Math.Min(1,(i+.5f)/steps)),width) && p.y>height(p.x,p.z)-1)return true;
            }
            return false;
        }
        // Broad-phase safe rejection only: no merging roads across vertical layers.
        public static bool Separated(RoadNode a,RoadNode b,RoadNode c,RoadNode d)
        {
            return Math.Min(a.y,b.y)-Math.Max(c.y,d.y)>=VerticalClearance || Math.Min(c.y,d.y)-Math.Max(a.y,b.y)>=VerticalClearance;
        }
    }
}
