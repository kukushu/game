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
    [Serializable]
    public sealed class RoadEdge
    {
        public int id, a, b, stroke;
        public bool oneWay;
        public RoadKind kind;
        public RoadStructure structure;
        public RoadEdge Copy() => new RoadEdge { id = id, a = a, b = b, stroke = stroke, oneWay=oneWay,kind=kind,structure=structure };
    }
    public sealed class RoadSplit
    {
        public int a, b;
        public List<int> path;
    }
    public sealed class RoadPlan
    {
        public CityRoads network;
        public RoadNode start, end;
        public string error;
        public float length, grade;
        public int cost, stroke, revision;
        public bool engineered,terrainBlocked,automaticSpan;
        public readonly List<RoadNode> points = new List<RoadNode>();
        public readonly List<RoadSplit> splits = new List<RoadSplit>();
        public bool Valid => error == null && network != null;
    }

    [Serializable]
    public sealed partial class CityRoads
    {
        public const float Width = 2.7f;
        public const int Entrance = 648;
        public int nextNode = 1296, nextEdge = 1, nextStroke = 1, revision;
        public List<RoadNode> nodes = new List<RoadNode>();
        public List<RoadEdge> edges = new List<RoadEdge>();
        [NonSerialized] Dictionary<int, RoadNode> lookup;
        [NonSerialized] Dictionary<int, List<int>> adjacency;
        [NonSerialized] HashSet<int> connected;
        [NonSerialized] HashSet<long> vehicleConnections;
        [NonSerialized] Dictionary<long,RoadEdge> roadLookup;
        [NonSerialized] Dictionary<long,int> structureLookup;
        [NonSerialized] Dictionary<(int from,int to,bool walking),int[]> singlePaths;
        [NonSerialized] int pathRevision=-1;
        [NonSerialized] CityRoadTopology logicalTopology;

        // Method rather than a serializable property: the old graph is the only saved truth.
        public CityRoadTopology GetLogicalTopology()=>logicalTopology ?? (logicalTopology=CityRoadTopologyBuilder.Build(this));

        public static float Length(RoadNode a, RoadNode b) => (float)Math.Sqrt((a.x-b.x)*(a.x-b.x)+(a.z-b.z)*(a.z-b.z));
        public static float SpatialLength(RoadNode a,RoadNode b)=>(float)Math.Sqrt(Length(a,b)*Length(a,b)+(a.y-b.y)*(a.y-b.y));
        static float Cross(float ax, float az, float bx, float bz) => ax*bz-az*bx;
        public static RoadNode Lerp(RoadNode a, RoadNode b, float t) => new RoadNode { x=a.x+(b.x-a.x)*t, y=a.y+(b.y-a.y)*t, z=a.z+(b.z-a.z)*t };
        public static float Projection(RoadNode p, RoadNode a, RoadNode b)
        {
            float dx=b.x-a.x, dz=b.z-a.z, d=dx*dx+dz*dz;
            return d < .0001f ? 0 : Math.Clamp(((p.x-a.x)*dx+(p.z-a.z)*dz)/d,0,1);
        }
        public static float Distance(RoadNode p, RoadNode a, RoadNode b)
        {
            float t=Projection(p,a,b),dx=p.x-(a.x+(b.x-a.x)*t),dz=p.z-(a.z+(b.z-a.z)*t);
            return (float)Math.Sqrt(dx*dx+dz*dz);
        }

        void Cache()
        {
            if (lookup != null) return;
            lookup = new Dictionary<int,RoadNode>(); adjacency = new Dictionary<int,List<int>>();
            foreach (var n in nodes) { lookup[n.id]=n; adjacency[n.id]=new List<int>(); }
#if !CITY_ROAD_KEY_HASH_DISABLED
            vehicleConnections=new HashSet<long>(CityRoadKeyComparer.Instance);
            roadLookup=new Dictionary<long,RoadEdge>(CityRoadKeyComparer.Instance);
            structureLookup=new Dictionary<long,int>(CityRoadKeyComparer.Instance);
#else
            vehicleConnections=new HashSet<long>();
            roadLookup=new Dictionary<long,RoadEdge>();
            structureLookup=new Dictionary<long,int>();
#endif
            foreach (var e in edges) { adjacency[e.a].Add(e.b); adjacency[e.b].Add(e.a);long ab=((long)e.a<<32)|(uint)e.b,ba=((long)e.b<<32)|(uint)e.a;roadLookup[ab]=roadLookup[ba]=e;structureLookup.TryGetValue(ab,out int mask);structureLookup[ab]=structureLookup[ba]=mask|(1<<(int)e.structure);vehicleConnections.Add(ab);if(!e.oneWay)vehicleConnections.Add(ba); }
            connected = new HashSet<int>(); var queue = new Queue<int>();
            if (lookup.ContainsKey(Entrance)) { connected.Add(Entrance); queue.Enqueue(Entrance); }
            while (queue.Count > 0)
            {
                int n=queue.Dequeue();
                foreach (int next in adjacency[n]) if (connected.Add(next)) queue.Enqueue(next);
            }
        }
        public void Changed() { revision++; lookup=null; adjacency=null; connected=null; logicalTopology=null; }
        public RoadNode Node(int id) { Cache(); return lookup.TryGetValue(id,out var n) ? n : null; }
        public List<int> Neighbors(int id) { Cache(); return adjacency.TryGetValue(id,out var n) ? n : new List<int>(); }
        public bool Active(int id) => Node(id) != null && Neighbors(id).Count > 0;
        public bool Linked(int a,int b) => Neighbors(a).Contains(b);
        public bool CanTravel(int a,int b,bool walking=false){Cache();return walking?roadLookup.TryGetValue(((long)a<<32)|(uint)b,out var e) && CityRoadTypes.Frontage(e.kind):vehicleConnections.Contains(((long)a<<32)|(uint)b);}
        public RoadKind Kind(int a,int b){Cache();return roadLookup.TryGetValue(((long)a<<32)|(uint)b,out var e)?e.kind:RoadKind.Standard;}
        public float EdgeLength(int a,int b)
        {
#if UNITY_5_3_OR_NEWER && ENABLE_PROFILER && CITY_TRAFFIC_ATTRIBUTION && !CITY_TRAFFIC_PROFILING_DISABLED
            CityTrafficAttribution.Current.edgeLengths++;
#endif
            return Math.Max(.1f,SpatialLength(Node(a),Node(b)));
        }
        public float SpeedFactor(int a,int b){Cache();return roadLookup.TryGetValue(((long)a<<32)|(uint)b,out var e)?CityRoadTypes.SpeedFactor(e.kind):1;}
        public float TravelCost(int a,int b,bool walking=false)=>EdgeLength(a,b)/(walking?1:SpeedFactor(a,b));
        public bool Connected(int id) { Cache(); return connected.Contains(id) && Active(id); }

        
        
        public CityRoads Copy()
        {
            var copy=new CityRoads { nextNode=nextNode,nextEdge=nextEdge,nextStroke=nextStroke,revision=revision };
            foreach(var n in nodes) copy.nodes.Add(n.Copy());
            foreach(var e in edges) copy.edges.Add(e.Copy());
            return copy;
        }
        
        void AddEdge(int a,int b,int stroke,bool oneWay=false,RoadKind kind=RoadKind.Standard,RoadStructure structure=RoadStructure.Ground)
        {
            if(a!=b) edges.Add(new RoadEdge { id=nextEdge++,a=a,b=b,stroke=stroke,oneWay=oneWay,kind=kind,structure=structure });
        }
        int AddNode(RoadNode p)
        {
            foreach(var n in nodes) if(Length(n,p)<.08f && Math.Abs(n.y-p.y)<.15f) return n.id;
            var added=p.Copy(); added.id=nextNode++; nodes.Add(added); lookup=null; return added.id;
        }
        public RoadNode Snap(float x,float z,Func<float,float,float> height,float elevation=0)
        {
            var p=new RoadNode { x=x,z=z,y=height(x,z)+elevation }; RoadNode best=null; float nearest=.85f;
            foreach(var n in nodes) if((Active(n.id) || n.id==Entrance) && Math.Abs(n.y-p.y)<.6f && Length(n,p)<nearest) { best=n; nearest=Length(n,p); }
            if(best!=null) return best.Copy();
            // Include the visible road edge and shoulder, not just its centre line.
            nearest=float.MaxValue;
            foreach(var e in edges)
            {
                var q=Lerp(Node(e.a),Node(e.b),Projection(p,Node(e.a),Node(e.b)));
                if(Math.Abs(q.y-p.y)<.6f && Length(q,p)<CityRoadTypes.Width(e.kind)/2+.45f && Length(q,p)<nearest)
                {
                    nearest=Length(q,p);
                    var a=Node(e.a); var b=Node(e.b);
                    best=Length(q,a)<.4f?a.Copy():Length(q,b)<.4f?b.Copy():q;
                }
            }
            return best ?? p;
        }
        public RoadEdge Pick(float x,float z,float radius=-1,float? level=null)
        {
            var p=new RoadNode{x=x,z=z}; RoadEdge best=null;float nearest=float.MaxValue;
            foreach(var e in edges)
            {
                float d=Distance(p,Node(e.a),Node(e.b));
                var projected=Lerp(Node(e.a),Node(e.b),Projection(p,Node(e.a),Node(e.b)));
                if(level.HasValue && Math.Abs(projected.y-level.Value)>1.6f)continue;
                if(d<(radius<0?CityRoadTypes.Width(e.kind)/2+.45f:radius) && d<nearest) { nearest=d; best=e; }
            }
            return best;
        }

        public RoadPlan Plan(CityModel city,RoadNode from,RoadNode to,Func<float,float,float> height,bool oneWay=false,RoadKind kind=RoadKind.Standard,bool engineered=false)
            => PlanSegment(city,from,to,height,1.5f,oneWay,kind,engineered);
        RoadPlan PlanSegment(CityModel city,RoadNode from,RoadNode to,Func<float,float,float> height,float minimum,bool oneWay=false,RoadKind kind=RoadKind.Standard,bool engineered=false)
        {
            var plan=new RoadPlan { start=from.Copy(),end=to.Copy(),revision=revision,stroke=nextStroke,engineered=engineered };
            plan.points.Add(from.Copy()); plan.points.Add(to.Copy());
            plan.length=Length(from,to); plan.cost=ProfileCost(from,to,height,kind,engineered);
            if(!CityRoadTypes.Valid(kind,oneWay)){plan.error=CityRoadTypes.DirectionError;return plan;}
            float width=CityRoadTypes.Width(kind);
            if(plan.length<minimum) { plan.error="道路太短（至少 "+minimum+" 米）"; return plan; }
            if(Math.Abs(from.x)>CityModel.RoadHalfSize || Math.Abs(from.z)>CityModel.RoadHalfSize || Math.Abs(to.x)>CityModel.RoadHalfSize || Math.Abs(to.z)>CityModel.RoadHalfSize)
            { plan.error="道路超出当前建设边界"; return plan; }
            foreach(var building in city.buildings) if(BuildingCollision(building,from,to,width,height,engineered))
            { plan.error="道路侵占建筑或分区，请先拆除或绕行"; return plan; }
            foreach(var project in city.development.projects) if(BuildingCollision(project.building.ToBuilding(),from,to,width,height,engineered))
            {plan.error="道路侵占正在施工的建筑，请绕行";return plan;}
            float previous=engineered?from.y:height(from.x,from.z); int samples=(int)Math.Ceiling(plan.length/.5f);
            float rx=-(to.z-from.z)/plan.length,rz=(to.x-from.x)/plan.length;
            for(int i=0;i<=samples;i++)
            {
                var p=Lerp(from,to,(float)i/samples); float terrain=height(p.x,p.z),y=engineered?p.y:terrain;
                if(i>0) plan.grade=Math.Max(plan.grade,Math.Abs(y-previous)/(plan.length/samples));
                previous=y;
                float left=height(p.x+rx*width/2,p.z+rz*width/2),right=height(p.x-rx*width/2,p.z-rz*width/2);
                if(!engineered && Math.Min(y,Math.Min(left,right))<=.15f || engineered && terrain<=.15f && y<1.5f && y>terrain-2) { plan.terrainBlocked=true;plan.error="跨水道路需抬高桥面，水下隧道需足够覆土"; return plan; }
                if(!engineered && Math.Abs(left-right)>.95f || plan.grade>.35f) { plan.terrainBlocked=true;plan.error="坡度过大，请沿缓坡绕行"; return plan; }
            }
            if((CityRoadTypes.LimitedAccess(kind) || width>Width || edges.Exists(e=>CityRoadTypes.Width(e.kind)>Width)) && !city.RoadWidthClear(from,to,kind,height,null,out string widthError,engineered)){plan.error=widthError;return plan;}
            var copy=Copy(); var crossings=new List<RoadNode>{from.Copy(),to.Copy()};
            float dx=to.x-from.x,dz=to.z-from.z;
            foreach(var e in edges)
            {
                var a=Node(e.a); var b=Node(e.b); if(Separated(from,to,a,b))continue; float ex=b.x-a.x,ez=b.z-a.z;
                float cross=Cross(dx,dz,ex,ez);
                if(Math.Abs(cross)<.0001f)
                {
                    if(Distance(a,from,to)<(width+CityRoadTypes.Width(e.kind))/2 && Distance(b,from,to)<(width+CityRoadTypes.Width(e.kind))/2
                        && Math.Max(Math.Min(Projection(a,from,to),Projection(b,from,to)),0)<.999f)
                    {
                        float t1=((a.x-from.x)*dx+(a.z-from.z)*dz)/(plan.length*plan.length);
                        float t2=((b.x-from.x)*dx+(b.z-from.z)*dz)/(plan.length*plan.length);
                        if(Math.Min(1,Math.Max(t1,t2))-Math.Max(0,Math.Min(t1,t2))>.01f)
                        { plan.error="与已有道路重叠或间距太小"; return plan; }
                    }
                    continue;
                }
                float t=Cross(a.x-from.x,a.z-from.z,ex,ez)/cross;
                float u=Cross(a.x-from.x,a.z-from.z,dx,dz)/cross;
                if(t<-.0001f || t>1.0001f || u<-.0001f || u>1.0001f) continue;
                var point=Lerp(a,b,Math.Clamp(u,0,1));
                float difference=Math.Abs((engineered?Lerp(from,to,Math.Clamp(t,0,1)).y:height(point.x,point.z))-point.y);
                if(difference>=VerticalClearance)continue;
                if(difference>.15f){plan.error="上下道路净空不足；请升高高架或加深隧道";return plan;}
                if(Length(point,a)<.08f) point=a.Copy(); else if(Length(point,b)<.08f) point=b.Copy();
                crossings.Add(point);
                if(Length(point,a)>.08f && Length(point,b)>.08f)
                {
                    if(Math.Min(Length(point,a),Length(point,b))<.4f) { plan.error="路口过近，请吸附到已有节点"; return plan; }
                    int id=copy.AddNode(point); copy.edges.RemoveAll(edge=>edge.id==e.id);
                    copy.AddEdge(e.a,id,e.stroke,e.oneWay,e.kind,e.structure); copy.AddEdge(id,e.b,e.stroke,e.oneWay,e.kind,e.structure);
                    copy.SplitJunctionApproach(e.a,e.b,id);
                    plan.splits.Add(new RoadSplit { a=e.a,b=e.b,path=new List<int>{e.a,id,e.b} });
                }
            }
            crossings.Sort((a,b)=>Projection(a,from,to).CompareTo(Projection(b,from,to)));
            var distinct=new List<RoadNode>();
            foreach(var n in crossings) if(distinct.Count==0 || Length(n,distinct[distinct.Count-1])>.08f) distinct.Add(n);
            for(int k=0;k<distinct.Count-1;k++)
            {
                var a=distinct[k]; var b=distinct[k+1]; float length=Length(a,b);
                if(length<.4f) { plan.error="路口间距过小"; return plan; }
                int count=Math.Max(1,(int)Math.Ceiling(length/3)),last=copy.AddNode(a);
                for(int i=1;i<=count;i++)
                {
                    var p=Lerp(a,b,(float)i/count); p.y=engineered || i==count ? p.y : height(p.x,p.z);
                    int next=copy.AddNode(p); copy.AddEdge(last,next,plan.stroke,oneWay,kind,engineered?Structure(copy.Node(last),p,height):RoadStructure.Ground); last=next;
                }
            }
            copy.Changed();
            // Reject acute branches that would need a much larger junction footprint.
            foreach(var n in distinct)
            {
                int id=copy.AddNode(n); var neighbors=copy.Neighbors(id);
                for(int a=0;a<neighbors.Count;a++) for(int b=a+1;b<neighbors.Count;b++)
                {
                    var p=copy.Node(neighbors[a]); var q=copy.Node(neighbors[b]);
                    float dot=((p.x-n.x)*(q.x-n.x)+(p.z-n.z)*(q.z-n.z))/(Length(p,n)*Length(q,n));
                    if(dot>.94f) { plan.error="交叉角度太小，请扩大转角"; return plan; }
                }
            }
            copy.nextStroke++; plan.network=copy;
            if(city.money<plan.cost) plan.error="资金不足";
            return plan;
        }

        // Quadratic Bezier is sampled by arc length into the same graph used by
        // straight roads. The whole stroke is previewed on copies and committed once.
        public RoadPlan PlanCurve(CityModel city,RoadNode from,RoadNode control,RoadNode to,Func<float,float,float> height,bool oneWay=false,RoadKind kind=RoadKind.Standard,bool engineered=false)
        {
            var result=new RoadPlan {start=from.Copy(),end=to.Copy(),revision=revision,stroke=nextStroke,engineered=engineered};
            if(!CityRoadTypes.Valid(kind,oneWay)){result.error=CityRoadTypes.DirectionError;return result;}
            var dense=new List<RoadNode>(); var distances=new List<float>(); float total=0;
            int samples=Math.Max(16,(int)Math.Ceiling((Length(from,control)+Length(control,to))/.35f));
            if(samples>2000) {result.error="弯道超出施工范围"; return result;}
            for(int i=0;i<=samples;i++)
            {
                float t=(float)i/samples,u=1-t;
                var p=new RoadNode {x=u*u*from.x+2*u*t*control.x+t*t*to.x,z=u*u*from.z+2*u*t*control.z+t*t*to.z};
                p.y=engineered?from.y+(to.y-from.y)*t:height(p.x,p.z); if(i>0) total+=Length(dense[i-1],p);
                dense.Add(p); distances.Add(total);
            }
            result.length=total; result.cost=CityRoadTypes.ConstructionCost(total,kind);
            result.points.Add(from.Copy());
            int spans=Math.Max(1,(int)Math.Floor(total/2)),index=1;
            for(int i=1;i<spans;i++)
            {
                float d=total*i/spans;
                while(index<distances.Count-1 && distances[index]<d) index++;
                float gap=distances[index]-distances[index-1];
                var p=Lerp(dense[index-1],dense[index],gap>0?(d-distances[index-1])/gap:0);
                if(!engineered)p.y=height(p.x,p.z); result.points.Add(p);
            }
            result.points.Add(to.Copy());
            if(total<1.5f || Length(from,to)<1.5f) {result.error="弯道端点太近（至少 1.5 米）"; return result;}
            // An internal sampling vertex is not a player-selected junction. Move
            // it onto a nearby crossing so sampling cannot create a tiny road stub.
            for(int i=1;i<result.points.Count;i++) foreach(var edge in edges)
            {
                var a=result.points[i-1]; var b=result.points[i]; var p=Node(edge.a); var q=Node(edge.b);
                float dx=b.x-a.x,dz=b.z-a.z,ex=q.x-p.x,ez=q.z-p.z,cross=Cross(dx,dz,ex,ez);
                if(Math.Abs(cross)<.0001f) continue;
                float t=Cross(p.x-a.x,p.z-a.z,ex,ez)/cross,u=Cross(p.x-a.x,p.z-a.z,dx,dz)/cross;
                if(t<0 || t>1 || u<0 || u>1) continue;
                var crossing=Lerp(p,q,u);if(Math.Abs(crossing.y-Lerp(a,b,t).y)>.15f)continue;
                if(i>1 && Length(a,crossing)<.4f) result.points[i-1]=crossing;
                else if(i<result.points.Count-1 && Length(b,crossing)<.4f) result.points[i]=crossing;
            }
            // Charge the geometry that will actually be built, including adjusted
            // junction vertices, rather than its straight chord or control polygon.
            result.length=0;
            for(int i=1;i<result.points.Count;i++) result.length+=Length(result.points[i-1],result.points[i]);
            result.cost=0;for(int i=1;i<result.points.Count;i++)result.cost+=ProfileCost(result.points[i-1],result.points[i],height,kind,engineered);
            // Prevent hairpins with overlapping carriageways and nearly stationary
            // tangents; sharp curves need a larger footprint, not a cosmetic mesh.
            for(int i=0;i<=samples;i++)
            {
                float t=(float)i/samples;
                float dx=2*((1-t)*(control.x-from.x)+t*(to.x-control.x));
                float dz=2*((1-t)*(control.z-from.z)+t*(to.z-control.z));
                float speed=(float)Math.Sqrt(dx*dx+dz*dz),cross=Math.Abs(Cross(dx,dz,2*(to.x-2*control.x+from.x),2*(to.z-2*control.z+from.z)));
                if(speed<.1f || cross>0 && speed*speed*speed/cross<CityRoadTypes.Width(kind)*2)
                {result.error="弯道转弯太急，请扩大弧度或拉远终点"; return result;}
            }
            var copy=Copy();
            for(int i=1;i<result.points.Count;i++)
            {
                // One stroke ID across all spans keeps undo and demolition coherent.
                copy.nextStroke=result.stroke;
                var part=copy.PlanSegment(city,result.points[i-1],result.points[i],height,.4f,oneWay,kind,engineered);
                result.grade=Math.Max(result.grade,part.grade);
                if(part.error!=null) {result.terrainBlocked=part.terrainBlocked;result.error=part.error; return result;}
                result.splits.AddRange(part.splits); copy=part.network;
            }
            copy.nextStroke=result.stroke+1; result.network=copy;
            if(city.money<result.cost) result.error="资金不足";
            return result;
        }

        // Preserve the previous curve's terminal tangent. Handle length remains a
        // provisional drawing rule; it is not claimed to match the original tool.
        public static RoadNode ContinuationControl(RoadNode from,RoadNode previousControl,RoadNode to)
        {
            float dx=from.x-previousControl.x,dz=from.z-previousControl.z;
            float magnitude=(float)Math.Sqrt(dx*dx+dz*dz);
            if(magnitude<.001f)return from.Copy();
            float handle=Length(from,to)/2;
            return new RoadNode{x=from.x+dx/magnitude*handle,z=from.z+dz/magnitude*handle,y=from.y};
        }

        public void ApplySplits(TrafficState traffic,List<RoadSplit> splits)
        {
            if(traffic==null) return;
            foreach(var split in splits) foreach(var trip in traffic.trips)
                for(int i=0;i<trip.route.Count-1;i++)
                {
                    int a=trip.route[i],b=trip.route[i+1];
                    if(!(a==split.a && b==split.b || a==split.b && b==split.a)) continue;
                    var path=new List<int>(split.path); if(a==split.b) path.Reverse();
                    int oldSegment=trip.segment;
                    if(i==oldSegment)
                    {
                        float distance=SpatialLength(Node(a),Node(b))*trip.progress;
                        int offset=0;
                        while(offset<path.Count-2 && distance>=EdgeLength(path[offset],path[offset+1]))
                        { distance-=EdgeLength(path[offset],path[offset+1]); offset++; }
                        trip.segment+=offset; trip.progress=Math.Min(.99999f,distance/EdgeLength(path[offset],path[offset+1]));
                        if(offset>0)trip.previousLane=trip.lane;
                    }
                    else if(i<oldSegment) trip.segment+=path.Count-2;
                    trip.route.InsertRange(i+1,path.GetRange(1,path.Count-2)); i+=path.Count-2;
                }
        }

        public List<int> Run(int edgeId)
        {
            var edge=edges.Find(e=>e.id==edgeId); var result=new List<int>(); if(edge==null) return result;
            result.Add(edge.id);
            foreach(int end in new[]{edge.a,edge.b})
            {
                int current=end,previous=end==edge.a ? edge.b : edge.a;
                while(Neighbors(current).Count==2)
                {
                    int next=Neighbors(current).Find(n=>n!=previous);
                    var a=Node(previous); var b=Node(current); var c=Node(next);
                    float dot=((b.x-a.x)*(c.x-b.x)+(b.z-a.z)*(c.z-b.z))/(Length(a,b)*Length(b,c));
                    var following=edges.Find(e=>e.a==current && e.b==next || e.b==current && e.a==next);
                    if(following==null || result.Contains(following.id)) break;
                    if(edge.stroke!=0 && following.stroke!=edge.stroke || dot<.995f && edge.stroke==0) break;
                    result.Add(following.id); previous=current; current=next;
                }
            }
            return result;
        }
        public void Remove(List<int> ids) { edges.RemoveAll(e=>ids.Contains(e.id)); Changed();foreach(var n in nodes)n.stopFrom.RemoveAll(from=>!Linked(n.id,from)); }
        public List<int> FindPath(List<int> starts,List<int> goals,bool walking=false)
        {
            // Waiting departures/returns repeatedly ask for the same physical
            // route. Cache only topology-derived single-endpoint results.
            // Every caller receives its own mutable list, never the cached array.
            if(starts.Count!=1 || goals.Count!=1)return CalculatePath(starts,goals,walking);
            if(singlePaths==null)singlePaths=new Dictionary<(int,int,bool),int[]>();
            if(pathRevision!=revision){singlePaths.Clear();pathRevision=revision;}
            var key=(starts[0],goals[0],walking);
            if(singlePaths.TryGetValue(key,out var cached))return new List<int>(cached);
            var path=CalculatePath(starts,goals,walking);
            // Bound memory in expanding cities; eviction cannot change routing.
            if(singlePaths.Count>=512)singlePaths.Clear();
            singlePaths[key]=path.ToArray();return path;
        }
        List<int> CalculatePath(List<int> starts,List<int> goals,bool walking)
        {
            var path=new List<int>(); if(starts.Count==0 || goals.Count==0) return path;
            var distance=new Dictionary<int,float>(); var previous=new Dictionary<int,int>();
            var open=new SortedSet<(float cost,int id)>();
            foreach(int s in starts) if(Active(s)) { distance[s]=0; previous[s]=-1; open.Add((0,s)); }
            while(open.Count>0)
            {
                var item=open.Min; open.Remove(item); int n=item.id;
                if(goals.Contains(n)) { for(int p=n;p!=-1;p=previous[p]) path.Add(p); path.Reverse(); return path; }
                foreach(int next in Neighbors(n))
                {
                    if(!CanTravel(n,next,walking))continue;
                    float cost=item.cost+TravelCost(n,next,walking)/3.6f;
                    if(distance.TryGetValue(next,out float old) && old<=cost) continue;
                    if(distance.ContainsKey(next)) open.Remove((old,next));
                    distance[next]=cost; previous[next]=n; open.Add((cost,next));
                }
            }
            return path;
        }
        public bool Valid()
        {
            if(nodes==null || edges==null || nodes.Count>20000 || edges.Count>40000 || nextNode<1296 || nextEdge<1 || nextStroke<1) return false;
            var ids=new HashSet<int>(); var edgeIds=new HashSet<int>(); var pairs=new HashSet<string>();
            foreach(var n in nodes) if(n==null || n.id<0 || n.id>=nextNode || !ids.Add(n.id)
                || float.IsNaN(n.x+n.y+n.z) || float.IsInfinity(n.x+n.y+n.z) || Math.Abs(n.x)>CityModel.BuildHalfSize || Math.Abs(n.z)>CityModel.BuildHalfSize || Math.Abs(n.y)>100) return false;
            foreach(var e in edges) if(e==null || e.id<1 || e.id>=nextEdge || !edgeIds.Add(e.id) || !ids.Contains(e.a) || !ids.Contains(e.b)
                || e.a==e.b || e.stroke<0 || e.stroke>=nextStroke || !CityRoadTypes.Valid(e.kind,e.oneWay) || !Enum.IsDefined(typeof(RoadStructure),e.structure) || !pairs.Add(Math.Min(e.a,e.b)+":"+Math.Max(e.a,e.b))) return false;
            lookup=null;
            foreach(var e in edges) if(EdgeLength(e.a,e.b)<.39f) return false;
            foreach(var n in nodes)if(!Enum.IsDefined(typeof(JunctionControl),n.control) || n.stopFrom==null || new HashSet<int>(n.stopFrom).Count!=n.stopFrom.Count || n.stopFrom.Exists(from=>!Linked(from,n.id)))return false;
            return ids.Contains(Entrance);
        }
    }
}
