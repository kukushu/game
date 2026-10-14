using System;
using System.Collections.Generic;
using HarborCity.RoadCore;
namespace HarborCity
{
    // One indexed entry per shop, not a factory x shop matrix. Candidate lists
    // are reusable scratch space; inventory/demand remain live building facts.
    internal sealed class CityFreightCandidates
    {
        readonly CityEntities entities;readonly World world;
        sealed class Entry {internal CommercialBuilding shop;internal RoadAccess access;internal int order;}
        sealed class StationOrder:IComparer<Entry>
        {internal static readonly StationOrder Instance=new StationOrder();public int Compare(Entry a,Entry b){int c=a.access.distance.CompareTo(b.access.distance);return c!=0?c:a.order.CompareTo(b.order);}}
        sealed class OriginalOrder:IComparer<Entry>
        {internal static readonly OriginalOrder Instance=new OriginalOrder();public int Compare(Entry a,Entry b)=>a.order.CompareTo(b.order);}
        readonly Dictionary<long,List<Entry>> lanes=new Dictionary<long,List<Entry>>(CityRoadKeyComparer.Instance);
        readonly List<Entry> scratch=new List<Entry>();
        Network network;int revision=-1;long accessRevision=-1;
        internal CityFreightCandidates(CityEntities entities,World world){this.entities=entities;this.world=world;}
        void Refresh()
        {
            if(ReferenceEquals(network,world.Network)&&revision==entities.Revision&&accessRevision==world.AccessRevision)return;
            lanes.Clear();network=world.Network;revision=entities.Revision;accessRevision=world.AccessRevision;
            for(int i=0;i<entities.Shops.Count;i++)
            {
                var shop=entities.Shops[i];var access=world.GetAccess(shop.id);
                if(access==null||!access.valid||(access.use&AccessUse.Freight)==0)continue;
                if(!lanes.TryGetValue(access.lane,out var bucket))lanes.Add(access.lane,bucket=new List<Entry>());
                bucket.Add(new Entry{shop=shop,access=access,order=i});
            }
            foreach(var bucket in lanes.Values)bucket.Sort(StationOrder.Instance);
        }
        internal void Collect(RoadAccess source,List<CommercialBuilding> output)
        {
            Refresh();scratch.Clear();output.Clear();if(source==null||!source.valid||(source.use&AccessUse.Freight)==0)return;
            foreach(var pair in lanes)
            {
                var bucket=pair.Value;int first=0;
                if(pair.Key==source.lane)
                {
                    // Probe a strictly upstream station to detect an actual cycle.
                    // At distance zero all stations are downstream already.
                    bool cycle=network.HasLaneCycle(source.lane);
                    if(!cycle){int lo=0,hi=bucket.Count;while(lo<hi){int mid=(lo+hi)/2;if(bucket[mid].access.distance<source.distance)lo=mid+1;else hi=mid;}first=lo;}
                }
                else if(!network.CanReach(source,bucket[0].access))continue;
                for(int i=first;i<bucket.Count;i++)scratch.Add(bucket[i]);
            }
            scratch.Sort(OriginalOrder.Instance);foreach(var entry in scratch)output.Add(entry.shop);
        }
    }
}
