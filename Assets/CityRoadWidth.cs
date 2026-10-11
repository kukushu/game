using System;
using System.Collections.Generic;
using System.Linq;
namespace HarborCity
{
    public sealed partial class CityModel
    {
        // The complete carriageway must fit. Shared nodes and centerline crossings
        // are legal junctions; close roads without a connection must not overlap.
        public bool RoadWidthClear(RoadNode a,RoadNode b,RoadKind kind,Func<float,float,float> height,List<int> ignore,out string reason,bool engineered=false)
        {
            reason="";float width=CityRoadTypes.Width(kind),length=CityRoads.Length(a,b);
            if(length<.01f){reason="道路太短。";return false;}
            foreach(var p in new[]{a,b})if(Math.Abs(p.x)+width/2>RoadHalfSize || Math.Abs(p.z)+width/2>RoadHalfSize)
            {reason="道路端部宽度超出建设边界。";return false;}
            float rx=-(b.z-a.z)/length,rz=(b.x-a.x)/length;
            for(int i=0,steps=(int)Math.Ceiling(length/.5f);i<=steps;i++)
            {
                var p=CityRoads.Lerp(a,b,(float)i/steps);float left=0,right=0;
                for(int side=-1;side<=1;side++)
                {
                    float x=p.x+rx*width/2*side,z=p.z+rz*width/2*side;
                    if(Math.Abs(x)>RoadHalfSize || Math.Abs(z)>RoadHalfSize){reason="道路宽度超出建设边界。";return false;}
                    float y=height(x,z);if(!engineered && y<=.15f){reason="拓宽路面进入水面，当前需绕行。";return false;}
                    if(side==-1)left=y;if(side==1)right=y;
                }
                if(!engineered && Math.Abs(left-right)>.95f){reason="拓宽后的横坡过大。";return false;}
            }
            foreach(var lot in buildings)if(CityRoads.BuildingCollision(lot,a,b,width,height,engineered)){reason="道路拓宽侵占建筑 #"+lot.id+"，请先拆除或绕行。";return false;}
            foreach(var p in development.projects)if(CityRoads.BuildingCollision(p.building.ToBuilding(),a,b,width,height,engineered)){reason="道路拓宽侵占正在施工的建筑。";return false;}
            var footprint=new ResidentialBuilding{x=(a.x+b.x)/2,z=(a.z+b.z)/2,yaw=-(float)(Math.Atan2(b.z-a.z,b.x-a.x)*180/Math.PI),width=length+.08f,depth=width+.12f};
            List<(RoadNode point,float radius,HashSet<int> run)> junctions=null;
            foreach(var e in roads.edges)
            {
                if(ignore!=null && ignore.Contains(e.id))continue;
                var c=roads.Node(e.a);var d=roads.Node(e.b);
                if(CityRoads.Separated(a,b,c,d))continue;
                if(CityRoads.Length(a,c)<.08f || CityRoads.Length(a,d)<.08f || CityRoads.Length(b,c)<.08f || CityRoads.Length(b,d)<.08f || Crosses(a,b,c,d))continue;
                if(!footprint.HitsRoad(c,d,CityRoadTypes.Width(e.kind)))continue;
                if(junctions==null)junctions=RoadJunctionFootprints(a,b,width,ignore);
                if(OutsideJunctionOverlap(footprint,e,c,d,junctions))
                {reason="拓宽路面与邻近道路重叠，请扩大间距。";return false;}
            }
            return true;
        }
        static bool Crosses(RoadNode a,RoadNode b,RoadNode c,RoadNode d)
            =>RoadIntersection(a,b,c,d,out _);
        static bool RoadIntersection(RoadNode a,RoadNode b,RoadNode c,RoadNode d,out RoadNode point)
        {
            point=null;
            float dx=b.x-a.x,dz=b.z-a.z,ex=d.x-c.x,ez=d.z-c.z,cross=dx*ez-dz*ex;
            if(Math.Abs(cross)<.0001f)return false;
            float t=((c.x-a.x)*ez-(c.z-a.z)*ex)/cross,u=((c.x-a.x)*dz-(c.z-a.z)*dx)/cross;
            if(t<-.0001f || t>1.0001f || u<-.0001f || u>1.0001f)return false;
            point=CityRoads.Lerp(c,d,Math.Clamp(u,0,1));return true;
        }
        List<(RoadNode point,float radius,HashSet<int> run)> RoadJunctionFootprints(RoadNode a,RoadNode b,float width,List<int> ignore)
        {
            var result=new List<(RoadNode,float,HashSet<int>)>();float length=CityRoads.Length(a,b);
            // An upgrade checks each sampling span separately. Its second span
            // can still overlap a junction mouth even though it does not itself
            // contain the junction center. Use the endpoints of the whole run.
            if(ignore!=null)
            foreach(var changed in roads.edges.Where(e=>ignore.Contains(e.id)))
            foreach(int node in new[]{changed.a,changed.b})
            foreach(var edge in roads.edges.Where(e=>!ignore.Contains(e.id) && (e.a==node || e.b==node)))
            {
                var center=roads.Node(node);var other=roads.Node(edge.a==node?edge.b:edge.a);
                var run=new HashSet<int>(roads.Run(edge.id));
                float oldWidth=roads.edges.Where(e=>run.Contains(e.id)).Max(e=>CityRoadTypes.Width(e.kind));
                float sine=Math.Abs((b.x-a.x)*(other.z-center.z)-(b.z-a.z)*(other.x-center.x))/(length*CityRoads.Length(center,other));
                result.Add((center,(width/2+oldWidth/2+.2f)/(sine<.01f?1:Math.Max(.33f,sine)),run));
            }
            foreach(var edge in roads.edges)
            {
                if(ignore!=null && ignore.Contains(edge.id))continue;
                var c=roads.Node(edge.a);var d=roads.Node(edge.b);RoadNode point;
                if(CityRoads.Length(a,c)<.08f || CityRoads.Length(a,d)<.08f)point=a;
                else if(CityRoads.Length(b,c)<.08f || CityRoads.Length(b,d)<.08f)point=b;
                else if(!RoadIntersection(a,b,c,d,out point))continue;
                var run=new HashSet<int>(roads.Run(edge.id));
                float oldWidth=roads.edges.Where(e=>run.Contains(e.id)).Max(e=>CityRoadTypes.Width(e.kind));
                float sine=Math.Abs((b.x-a.x)*(d.z-c.z)-(b.z-a.z)*(d.x-c.x))/(length*CityRoads.Length(c,d));
                float radius=(width/2+oldWidth/2+.2f)/(sine<.01f?1:Math.Max(.33f,sine));
                result.Add((point,radius,run));
            }
            return result;
        }
        // Graph edges are sampling spans, not separate streets. Only the portion
        // of a connected street inside a genuine junction may overlap. Preserve
        // collision checks outside it, including nearby unconnected roads.
        static bool OutsideJunctionOverlap(CityBuilding footprint,RoadEdge edge,RoadNode a,RoadNode b,List<(RoadNode point,float radius,HashSet<int> run)> junctions)
        {
            var pieces=new List<(float from,float to)>{(0,1)};float dx=b.x-a.x,dz=b.z-a.z,length2=dx*dx+dz*dz;
            foreach(var junction in junctions)
            {
                if(!junction.run.Contains(edge.id))continue;
                float px=a.x-junction.point.x,pz=a.z-junction.point.z,dot=px*dx+pz*dz;
                float discriminant=dot*dot-length2*(px*px+pz*pz-junction.radius*junction.radius);
                if(discriminant<=0)continue;
                float root=(float)Math.Sqrt(discriminant),start=Math.Max(0,(-dot-root)/length2),end=Math.Min(1,(-dot+root)/length2);
                if(start>=end)continue;
                var remaining=new List<(float,float)>();
                foreach(var piece in pieces)
                {
                    if(end<=piece.from || start>=piece.to){remaining.Add(piece);continue;}
                    if(start>piece.from)remaining.Add((piece.from,start));if(end<piece.to)remaining.Add((end,piece.to));
                }
                pieces=remaining;
            }
            return pieces.Any(p=>footprint.HitsRoad(CityRoads.Lerp(a,b,p.from),CityRoads.Lerp(a,b,p.to),CityRoadTypes.Width(edge.kind)));
        }
        void RemapRoadZones(List<ZoneCell> old,Func<float,float,float> height)
        {
            var cells=ZoningCells(height);var current=new HashSet<string>(cells.Select(c=>c.key));
            var oldKeys=new HashSet<string>(old.Select(c=>c.key));
            zoning.designations.RemoveAll(d=>oldKeys.Contains(d.key) && !current.Contains(d.key));zoning.revision++;
            float size=CityZoningState.CellSize;
            var buckets=old.GroupBy(c=>((int)Math.Floor(c.pose.x/size),(int)Math.Floor(c.pose.z/size))).ToDictionary(g=>g.Key,g=>g.ToList());
            // Keep land-use paint attached to overlapping land, not to a former
            // road-width-dependent row number. Existing buildings are never moved.
            foreach(var cell in cells.Where(c=>c.available && c.use==LandUse.Empty))
            {
                var near=new List<ZoneCell>();int bx=(int)Math.Floor(cell.pose.x/size),bz=(int)Math.Floor(cell.pose.z/size);
                for(int x=bx-2;x<=bx+2;x++)for(int z=bz-2;z<=bz+2;z++)if(buckets.TryGetValue((x,z),out var values))near.AddRange(values);
                var source=near.Where(c=>Math.Abs(c.pose.yaw-cell.pose.yaw)<.01f && c.pose.Overlaps(cell.pose))
                    .OrderBy(c=>(c.pose.x-cell.pose.x)*(c.pose.x-cell.pose.x)+(c.pose.z-cell.pose.z)*(c.pose.z-cell.pose.z)).FirstOrDefault();
                if(source!=null && zoning.nextId<int.MaxValue && zoning.designations.Count<400000)
                {zoning.designations.Add(new ZoneDesignation{id=zoning.nextId++,key=cell.key,use=source.use});cell.use=source.use;}
            }
        }
    }
}
