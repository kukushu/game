using System;
namespace HarborCity
{
    public enum RoadStructure { Ground, Elevated, Tunnel }
    public sealed partial class CityRoads
    {
        // Intersect each actual sloping deck; oblique terrain hits miss tunnels/bridges.
        public RoadEdge PickVisibleRay(float ox,float oy,float oz,float dx,float dy,float dz,bool underground)
        {
            RoadEdge best=null;double nearest=double.PositiveInfinity;
            foreach(var edge in edges)
            {
                if((edge.structure==RoadStructure.Tunnel)!=underground)continue;
                var a=Node(edge.a);var b=Node(edge.b);
                double vx=b.x-a.x,vz=b.z-a.z,length2=vx*vx+vz*vz;
                if(length2<.000001)continue;
                double slope=(b.y-a.y)/length2;
                double denominator=dy-slope*(dx*vx+dz*vz);
                if(Math.Abs(denominator)<.000001)continue;
                double t=(a.y+.09-oy+slope*((ox-a.x)*vx+(oz-a.z)*vz))/denominator;
                if(t<0 || t>=nearest)continue;
                double x=ox+t*dx,z=oz+t*dz;
                double progress=Math.Clamp(((x-a.x)*vx+(z-a.z)*vz)/length2,0,1);
                double ex=x-a.x-progress*vx,ez=z-a.z-progress*vz;
                double radius=CityRoadTypes.Width(edge.kind)/2+.2;
                if(ex*ex+ez*ez>radius*radius)continue;
                best=edge;nearest=t;
            }
            return best;
        }
        // Prototype world scale, not verified original-game metre values.
        public const float VerticalClearance=3;
        // Construction tool fallback, not a replacement for ordinary ground
        // roads: only terrain rejection can request an endpoint-height span.
        public RoadPlan PlanTerrainSpan(CityModel city,RoadNode from,RoadNode to,Func<float,float,float> height,bool oneWay=false,RoadKind kind=RoadKind.Standard,bool engineered=false,RoadNode control=null)
        {
            RoadPlan Make(bool span)=>control==null?Plan(city,from,to,height,oneWay,kind,span):PlanCurve(city,from,control,to,height,oneWay,kind,span);
            var ground=Make(engineered);
            if(engineered || ground.Valid || !ground.terrainBlocked)return ground;
            // Wet endpoints need a deliberate bridge ramp or tunnel entrance.
            if(height(from.x,from.z)<=.15f || height(to.x,to.z)<=.15f)return ground;
            var span=Make(true);span.automaticSpan=true;
            if(!span.Valid && span.terrainBlocked && span.grade<=.35f)
            {
                var raised=RaisedWaterSpan(city,span,height,oneWay,kind);
                if(raised!=null)return raised;
            }
            return span;
        }
        RoadPlan RaisedWaterSpan(CityModel city,RoadPlan source,Func<float,float,float> height,bool oneWay,RoadKind kind)
        {
            var points=new System.Collections.Generic.List<RoadNode>();bool water=false;
            for(int k=1;k<source.points.Count;k++)
            {
                var a=source.points[k-1];var b=source.points[k];int samples=Math.Max(1,(int)Math.Ceiling(Length(a,b)/2));
                for(int i=k==1?0:1;i<=samples;i++)points.Add(Lerp(a,b,(float)i/samples));
            }
            var heights=points.ConvertAll(p=>p.y);
            for(int i=0;i<points.Count-1;i++)
            {
                // Protect the entire sample edge when even a small part crosses
                // water. Both ends then meet the existing minimum deck height.
                int samples=Math.Max(1,(int)Math.Ceiling(Length(points[i],points[i+1])/.5f));
                for(int s=0;s<=samples;s++)
                {var p=Lerp(points[i],points[i+1],(float)s/samples);if(height(p.x,p.z)>.15f)continue;water=true;heights[i]=Math.Max(heights[i],1.5f);heights[i+1]=Math.Max(heights[i+1],1.5f);break;}
            }
            if(!water)return null;
            // Minimal upward envelope with 30% approach grades, leaving some
            // margin beneath the existing 35% construction limit.
            for(int i=1;i<points.Count;i++)heights[i]=Math.Max(heights[i],heights[i-1]-.30f*Length(points[i-1],points[i]));
            for(int i=points.Count-2;i>=0;i--)heights[i]=Math.Max(heights[i],heights[i+1]-.30f*Length(points[i],points[i+1]));
            if(heights[0]>source.start.y+.0001f || heights[heights.Count-1]>source.end.y+.0001f)return null;
            var result=new RoadPlan{start=source.start.Copy(),end=source.end.Copy(),revision=revision,stroke=nextStroke,engineered=true,automaticSpan=true};
            var copy=Copy();
            for(int i=0;i<points.Count;i++){points[i].y=heights[i];result.points.Add(points[i]);}
            for(int i=1;i<points.Count;i++)
            {
                copy.nextStroke=result.stroke;
                var part=copy.PlanSegment(city,points[i-1],points[i],height,.4f,oneWay,kind,true);
                result.length+=part.length;result.cost+=part.cost;result.grade=Math.Max(result.grade,part.grade);
                if(!part.Valid){result.error=part.error;result.terrainBlocked=part.terrainBlocked;return result;}
                result.splits.AddRange(part.splits);copy=part.network;
            }
            result.network=copy;copy.nextStroke=result.stroke+1;
            if(city.money<result.cost)result.error="资金不足";
            return result;
        }
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
