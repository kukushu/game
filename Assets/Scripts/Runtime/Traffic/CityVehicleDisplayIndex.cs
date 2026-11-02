using System;using System.Collections.Generic;
namespace HarborCity.RoadCore
{
    internal readonly struct CityDisplayCell:IEquatable<CityDisplayCell>
    {
        internal readonly int X,Z;internal CityDisplayCell(int x,int z){X=x;Z=z;}
        internal static CityDisplayCell At(Point p)=>new CityDisplayCell((int)Math.Floor(p.x/64),(int)Math.Floor(p.z/64));
        internal CityDisplayCell Parent=>new CityDisplayCell((int)Math.Floor(X/4.0),(int)Math.Floor(Z/4.0));
        public bool Equals(CityDisplayCell b)=>X==b.X&&Z==b.Z;public override bool Equals(object b)=>b is CityDisplayCell c&&Equals(c);public override int GetHashCode()=>unchecked(X*397^Z);
    }
    // Read-side ownership only. Road-distance intervals avoid sampling every
    // moving actor's world pose merely to update its coarse cell membership.
    internal sealed class CityVehicleDisplayIndex:IDisposable
    {
        internal sealed class Record {internal Vehicle Actor;internal CityDisplayCell Cell;internal int Purpose,Version;internal long Shadow;}
        internal sealed class Node
        {
            internal CityDisplayCell Key;internal int Level,Count;internal readonly int[] Purposes=new int[4];
            internal readonly Dictionary<CityDisplayCell,Node> Children=new Dictionary<CityDisplayCell,Node>();
            internal readonly HashSet<int> Ids=new HashSet<int>();
        }
        readonly struct Span {internal readonly double Start,End;internal readonly CityDisplayCell Cell;internal Span(double a,double b,CityDisplayCell c){Start=a;End=b;Cell=c;}}
        sealed class LaneMap
        {
            internal readonly Geometry Geometry;readonly List<Span> spans=new List<Span>();
            internal LaneMap(Geometry g)
            {
                Geometry=g;var cuts=new List<double>();
                for(int i=0;i<g.Count-1;i++){
                    Point a=g[i],b=g[i+1];cuts.Clear();cuts.Add(0);cuts.Add(1);
                    Cut(a.x,b.x,cuts);Cut(a.z,b.z,cuts);cuts.Sort();
                    for(int k=1;k<cuts.Count;k++){double lo=cuts[k-1],hi=cuts[k];if(hi-lo<1e-14)continue;double start=g.At(i)+(g.At(i+1)-g.At(i))*lo,end=g.At(i)+(g.At(i+1)-g.At(i))*hi;var cell=CityDisplayCell.At(Point.Lerp(a,b,(lo+hi)*.5));
                        if(spans.Count>0&&spans[spans.Count-1].Cell.Equals(cell)){var last=spans[spans.Count-1];spans[spans.Count-1]=new Span(last.Start,end,cell);}else spans.Add(new Span(start,end,cell));}
                }
                if(spans.Count==0)spans.Add(new Span(0,g.Length,CityDisplayCell.At(g[0])));
            }
            static void Cut(double a,double b,List<double> cuts){if(a==b)return;int first=(int)Math.Floor(Math.Min(a,b)/64)+1,last=(int)Math.Floor(Math.Max(a,b)/64);for(int k=first;k<=last;k++){double t=(k*64.0-a)/(b-a);if(t>0&&t<1)cuts.Add(t);}}
            internal CityDisplayCell Cell(double distance,ref long sampled)
            {
                if(distance<=0)return CityDisplayCell.At(Geometry[0]);if(distance>=Geometry.Length)return CityDisplayCell.At(Geometry[Geometry.Count-1]);
                int lo=0,hi=spans.Count-1;while(lo<hi){int m=(lo+hi)/2;if(distance<spans[m].End)hi=m;else lo=m+1;}var s=spans[lo];double epsilon=1e-10*(1+Math.Abs(distance));
                if(Math.Abs(distance-s.Start)<=epsilon||Math.Abs(distance-s.End)<=epsilon){sampled++;return CityDisplayCell.At(Geometry.Sample(distance));}return s.Cell;
            }
        }
        readonly World world;readonly CityPresentationJournal journal;Network network;
        internal readonly Dictionary<int,Record> Records=new Dictionary<int,Record>();
        internal readonly Dictionary<CityDisplayCell,Node> Roots=new Dictionary<CityDisplayCell,Node>();
        readonly Dictionary<long,LaneMap> lanes=new Dictionary<long,LaneMap>(CityRoadKeyComparer.Instance);
        readonly Dictionary<long,HashSet<int>> byLane=new Dictionary<long,HashSet<int>>(CityRoadKeyComparer.Instance);
        readonly HashSet<int> affected=new HashSet<int>();readonly List<long> changedLanes=new List<long>();
        internal long Updates,PreciseIndexSamples,Rebuilds,LaneBuilds;internal double MinY=-1,MaxY=2;
        internal CityVehicleDisplayIndex(World world){this.world=world;journal=world.ObserveDisplay();network=world.Network;foreach(var v in world.Vehicles)Update(v.id);journal.Clear();Rebuilds++;}
        internal static int Purpose(TransportPurpose p)=>p==TransportPurpose.Freight?2:p==TransportPurpose.Shopping?1:p==TransportPurpose.Work||p==TransportPurpose.Home?0:3;
        static bool Drawable(Vehicle v)=>v!=null&&v.status!=VehicleStatus.Delivered&&v.status!=VehicleStatus.AwaitingRestart;
        Node Leaf(CityDisplayCell key,bool create)
        {
            var parent=key.Parent;var root=parent.Parent;
            if(!Roots.TryGetValue(root,out var node)){if(!create)return null;Roots.Add(root,node=new Node{Key=root,Level=2});}
            if(!node.Children.TryGetValue(parent,out var middle)){if(!create)return null;node.Children.Add(parent,middle=new Node{Key=parent,Level=1});}
            if(!middle.Children.TryGetValue(key,out var leaf)){if(!create)return null;middle.Children.Add(key,leaf=new Node{Key=key});}return leaf;
        }
        void Count(Record record,int delta)
        {
            var key=record.Cell;var middle=Roots[key.Parent.Parent].Children[key.Parent];var root=Roots[key.Parent.Parent];var leaf=middle.Children[key];
            leaf.Count+=delta;middle.Count+=delta;root.Count+=delta;leaf.Purposes[record.Purpose]+=delta;middle.Purposes[record.Purpose]+=delta;root.Purposes[record.Purpose]+=delta;
            if(delta>0)leaf.Ids.Add(record.Actor.id);else leaf.Ids.Remove(record.Actor.id);
            if(leaf.Count==0)middle.Children.Remove(key);if(middle.Count==0)root.Children.Remove(key.Parent);if(root.Count==0)Roots.Remove(key.Parent.Parent);
        }
        LaneMap Map(long id)
        {
            if(!lanes.TryGetValue(id,out var map)){var g=world.Network.Get(id).Geometry;lanes.Add(id,map=new LaneMap(g));LaneBuilds++;for(int i=0;i<g.Count;i++){MinY=Math.Min(MinY,g[i].y-1);MaxY=Math.Max(MaxY,g[i].y+2);}}return map;
        }
        CityDisplayCell Locate(Vehicle v)
        {
            if(v.Position.IsFrozen){var p=v.strandedPosition;MinY=Math.Min(MinY,p.y-1);MaxY=Math.Max(MaxY,p.y+2);return CityDisplayCell.At(p);}
            var map=Map(v.Current);
            if(v.changeTarget!=0){Map(v.changeTarget);PreciseIndexSamples++;var p=world.Position(v);MinY=Math.Min(MinY,p.y-1);MaxY=Math.Max(MaxY,p.y+2);return CityDisplayCell.At(p);}
            return map.Cell(v.distance,ref PreciseIndexSamples);
        }
        void Link(long lane,int id){if(!byLane.TryGetValue(lane,out var ids))byLane.Add(lane,ids=new HashSet<int>());ids.Add(id);}
        void Unlink(long lane,int id){if(byLane.TryGetValue(lane,out var ids)){ids.Remove(id);if(ids.Count==0)byLane.Remove(lane);}}
        // Store only the indexed lane identity, never a second actor position.
        readonly Dictionary<int,long> indexedLanes=new Dictionary<int,long>();
        void Remove(int id,Record record){Count(record,-1);Unlink(indexedLanes[id],id);if(record.Shadow!=0)Unlink(record.Shadow,id);indexedLanes.Remove(id);Records.Remove(id);}
        void Update(int id)
        {
            Updates++;var v=world.FindVehicle(id);Records.TryGetValue(id,out var prior);
            if(!Drawable(v)){if(prior!=null)Remove(id,prior);return;}
            long shadow=v.Position.IsFrozen?0:v.changeTarget;var cell=Locate(v);int purpose=Purpose(v.Task.purpose);int version=(prior?.Version??0)+1;
            if(prior!=null&&prior.Cell.Equals(cell)&&prior.Purpose==purpose&&prior.Shadow==shadow&&indexedLanes[id]==v.Current){prior.Actor=v;prior.Version=version;return;}
            if(prior!=null)Remove(id,prior);Leaf(cell,true);var record=new Record{Actor=v,Cell=cell,Purpose=purpose,Version=version,Shadow=shadow};Records.Add(id,record);indexedLanes.Add(id,v.Current);
            Link(v.Current,id);if(shadow!=0)Link(shadow,id);Count(record,1);
        }
        static bool SameGeometry(Geometry a,Geometry b)
        {
            if(ReferenceEquals(a,b))return true;if(a.Count!=b.Count||a.Length!=b.Length)return false;
            for(int i=0;i<a.Count;i++)if(a[i].x!=b[i].x||a[i].y!=b[i].y||a[i].z!=b[i].z)return false;return true;
        }
        internal void Sync()
        {
            if(!ReferenceEquals(network,world.Network)){
                affected.Clear();changedLanes.Clear();foreach(var pair in lanes)if(!world.Network.Contains(pair.Key)||!SameGeometry(pair.Value.Geometry,world.Network.Get(pair.Key).Geometry)){changedLanes.Add(pair.Key);if(byLane.TryGetValue(pair.Key,out var ids))foreach(int id in ids)affected.Add(id);}
                foreach(long id in changedLanes)lanes.Remove(id);network=world.Network;
                foreach(int id in journal.Ids)affected.Add(id);foreach(int id in affected)Update(id);
            }else foreach(int id in journal.Ids)Update(id);
            journal.Clear();
        }
        internal void AssertExact()
        {int count=0;foreach(var v in world.Vehicles)if(Drawable(v)){count++;if(!Records.TryGetValue(v.id,out var r)||!r.Cell.Equals(CityDisplayCell.At(world.Position(v)))||r.Purpose!=Purpose(v.Task.purpose))throw new InvalidOperationException("Display spatial membership differs");if(v.status==VehicleStatus.Active&&v.changeTarget!=0&&(!lanes.ContainsKey(v.Current)||!lanes.ContainsKey(v.changeTarget)||!byLane.TryGetValue(v.changeTarget,out var changing)||!changing.Contains(v.id)))throw new InvalidOperationException("Changing actor lost a geometry dependency");}if(count!=Records.Count)throw new InvalidOperationException("Display population differs");int total=0;foreach(var node in Roots.Values)total+=node.Count;if(total!=count)throw new InvalidOperationException("Display summary count differs");}
        public void Dispose(){world.ReleaseDisplay(journal);Records.Clear();Roots.Clear();lanes.Clear();byLane.Clear();indexedLanes.Clear();}
    }
}
