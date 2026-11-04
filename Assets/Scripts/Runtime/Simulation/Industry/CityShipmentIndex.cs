using System;
using System.Collections.Generic;
using HarborCity.RoadCore;
namespace HarborCity
{
    // One bounded derived entry per live freight task. Retire uses captured amounts
    // because transport clears the authoritative payload before releasing the task.
    internal sealed class CityShipmentIndex
    {
        struct Entry {internal int from,to,raw;internal GoodsAmounts goods;}
        readonly Dictionary<int,Entry> entries=new Dictionary<int,Entry>();
        readonly Dictionary<int,GoodsAmounts> goods=new Dictionary<int,GoodsAmounts>();
        readonly Dictionary<int,int> raw=new Dictionary<int,int>(),trucks=new Dictionary<int,int>();
        internal int Count=>entries.Count;
        internal CityShipmentIndex(World world)
        {foreach(var task in world.Tasks)Add(task);world.TaskCommitted+=Add;world.TaskRetired+=Remove;}
        static void Add(Dictionary<int,int> map,int key,int delta){map.TryGetValue(key,out int n);n=checked(n+delta);if(n<0)throw new InvalidOperationException("Shipment index underflow");if(n==0)map.Remove(key);else map[key]=n;}
        void MoveGoods(int id,GoodsAmounts value,bool subtract){goods.TryGetValue(id,out var old);var next=subtract?old-value:old+value;if(next.Total==0)goods.Remove(id);else goods[id]=next;}
        void Add(TripTask task)
        {if(task.purpose!=TransportPurpose.Freight||task.state!=TripState.InTransit||task.Shipment==null)return;var e=new Entry{from=task.origin,to=task.destination,raw=task.Shipment.kind==CargoKind.RawMaterial?task.Cargo:0,goods=task.Shipment.Products};entries.Add(task.id,e);MoveGoods(e.to,e.goods,false);Add(raw,e.to,e.raw);Add(trucks,e.from,1);Add(trucks,e.to,1);}
        void Remove(int id){if(!entries.TryGetValue(id,out var e))return;MoveGoods(e.to,e.goods,true);Add(raw,e.to,-e.raw);Add(trucks,e.from,-1);Add(trucks,e.to,-1);entries.Remove(id);}
        internal GoodsAmounts Goods(int id)=>goods.TryGetValue(id,out var n)?n:default;
        internal int Raw(int id)=>raw.TryGetValue(id,out int n)?n:0;
        internal int Trucks(int id)=>trucks.TryGetValue(id,out int n)?n:0;
    }
}
