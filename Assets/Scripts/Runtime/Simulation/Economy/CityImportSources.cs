using System;
using System.Collections.Generic;
using HarborCity.RoadCore;

namespace HarborCity
{
    // One phase-local candidate per legal import, not a factory x port route table.
    // Stocks, money, tasks and source occupancy remain owned by their domains.
    internal sealed class CityImportSources
    {
        struct Candidate {internal int id;internal Point position;internal double bound;}
        sealed class ByBound:IComparer<Candidate>
        {internal static readonly ByBound Instance=new ByBound();public int Compare(Candidate a,Candidate b){int c=a.bound.CompareTo(b.bound);return c!=0?c:a.id.CompareTo(b.id);}}
        readonly World world;
        readonly List<Candidate> available=new List<Candidate>(),ordered=new List<Candidate>();
        readonly HashSet<int> rejected=new HashSet<int>();
        Network network;double maximumSpeed;
        internal CityImportSources(World world){this.world=world;}
        internal void Begin(IReadOnlyList<CityOutsideConnection> ports,Dictionary<int,int> trucks,int capacity)
        {
            if(!ReferenceEquals(network,world.Network)){network=world.Network;maximumSpeed=0;foreach(var d in network.Drivables)maximumSpeed=Math.Max(maximumSpeed,d.Speed);}
            available.Clear();
            foreach(var port in ports)
            {
                trucks.TryGetValue(port.id,out int count);var access=world.GetAccess(port.id);
                if(!port.active||port.flow!=OutsideFlow.Import||count>=capacity||access==null||!access.valid||(access.use&AccessUse.Freight)==0||world.FreightSourceBlocked(port.id))continue;
                available.Add(new Candidate{id=port.id,position=network.Get(access.lane).Geometry.Sample(access.distance)});
            }
        }
        internal void ForFactory(){rejected.Clear();}
        internal void Reject(int id){rejected.Add(id);}
        internal void Accepted(int id,Dictionary<int,int> trucks,int capacity)
        {
            // A successful spawn changes source occupancy for every destination.
            // Never keep quoting that blocked source for the rest of this phase.
            for(int i=available.Count-1;i>=0;i--)if(available[i].id==id&&(trucks[id]>=capacity||world.FreightSourceBlocked(id)))available.RemoveAt(i);
        }
        internal int Choose(int factory)
        {
            if(available.Count==0)return 0;var access=world.GetAccess(factory);
            if(access==null||!access.valid)return 0;
            var position=network.Get(access.lane).Geometry.Sample(access.distance);ordered.Clear();
            foreach(var candidate in available)
            {
                if(rejected.Contains(candidate.id))continue;var entry=candidate;
                entry.bound=maximumSpeed>0?Math.Max(0,Point.Distance(entry.position,position)/maximumSpeed-1e-6):0;ordered.Add(entry);
            }
            ordered.Sort(ByBound.Instance);double best=double.PositiveInfinity;int chosen=0;
            foreach(var candidate in ordered)
            {
                // Triangle inequality is only a safe pruning bound, never a route.
                // Equality still queries so the stable endpoint-ID tie can win.
                if(candidate.bound>best)break;
                double seconds=world.FreightTravelSeconds(candidate.id,factory);
                if(double.IsInfinity(seconds))continue;
                if(seconds<best||seconds==best&&(chosen==0||candidate.id<chosen)){best=seconds;chosen=candidate.id;}
            }
            return chosen;
        }
    }
}
