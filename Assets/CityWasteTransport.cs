using System;
using System.Linq;

namespace HarborCity
{
    public sealed partial class CityTraffic
    {
        public TrafficTrip DispatchWaste(int landfillId,int destination,bool transfer=false)
        {
            var landfill=city.GetBuilding(landfillId) as LandfillBuilding;
            var target=city.GetBuilding(destination);
            if(!city.development.enabled || !city.waste.enabled || landfill==null || target==null || landfillId==destination
                || !city.BuildingAccess(landfillId) || !city.Supply(landfillId).Complete || !city.BuildingAccess(destination)
                || State.trips.Count>=TaskCapacity || State.nextId==int.MaxValue
                || State.trips.Count(t=>t.cargoKind==CargoKind.Waste && t.home==landfillId)>=city.GarbageFleetLimit)return null;
            int cargo=0;
            if(transfer)
            {
                var receiver=target as LandfillBuilding;
                if(!landfill.emptying || landfill.stored==0 || receiver==null || receiver.emptying || !city.Supply(receiver.id).Complete)return null;
                cargo=Math.Min(LandfillBuilding.Payload,Math.Min(landfill.stored,LandfillBuilding.Capacity-receiver.stored-city.LandfillIncoming(receiver.id)));
                if(cargo<=0)return null;
            }
            else if(landfill.emptying || target.garbage<=0 || landfill.stored+city.LandfillIncoming(landfillId)>=LandfillBuilding.Capacity)return null;
            var route=FindRoute(landfillId,destination);if(route.Count==0 || !CanEnter(route,null))return null;
            var trip=new TrafficTrip{id=State.nextId++,origin=landfillId,destination=destination,home=landfillId,cargo=cargo,cargoKind=CargoKind.Waste,
                purpose=transfer?TripPurpose.GarbageTransfer:TripPurpose.GarbagePickup,route=route,departedAt=State.clock};
            if(transfer)landfill.stored-=cargo;
            State.trips.Add(trip);city.Trace("garbage.truck_started",transfer?"垃圾转运车实际出发":"垃圾车出发收集",trip,building:landfillId,trip:trip.id);return trip;
        }
        bool ScheduleWaste()
        {
            if(!city.development.enabled || !city.waste.enabled)return false;
            foreach(var landfill in city.buildings.OfType<LandfillBuilding>())
            {
                if(landfill.emptying)
                {
                    foreach(var target in city.buildings.OfType<LandfillBuilding>().Where(b=>b.id!=landfill.id && !b.emptying))
                        if(DispatchWaste(landfill.id,target.id,true)!=null)return true;
                }
                else foreach(var target in city.buildings.Where(b=>b.garbage>0).OrderByDescending(b=>b.garbage).ThenBy(b=>CityRoads.Length(new RoadNode{x=b.x,z=b.z},new RoadNode{x=landfill.x,z=landfill.z})))
                    if(DispatchWaste(landfill.id,target.id)!=null)return true;
            }
            return false;
        }
        void CollectAlongRoute(TrafficTrip trip)
        {
            if(trip.purpose!=TripPurpose.GarbagePickup || trip.returning || trip.status!=TripStatus.Driving || trip.progress>0 || !Road(trip.Current))return;
            var depot=city.GetBuilding(trip.home) as LandfillBuilding;if(depot==null)return;
            int otherCargo=State.trips.Where(t=>t!=trip && t.cargoKind==CargoKind.Waste && t.home==trip.home).Sum(t=>t.cargo);
            int last=-1;
            foreach(var b in city.buildings.Where(b=>b.garbage>0 && city.AccessBuilding(b.id).Contains(trip.Current)))
            {
                int amount=Math.Min(b.garbage,Math.Min(LandfillBuilding.Payload-trip.cargo,Math.Max(0,LandfillBuilding.Capacity-depot.stored-otherCargo-trip.cargo)));
                if(amount<=0)break;b.garbage-=amount;trip.cargo+=amount;city.waste.collected+=amount;last=b.id;
                city.Trace("garbage.collected","垃圾车经过建筑出入口，沿途实际装载",new CityLogDetail{amount=amount,count=trip.cargo},building:b.id,trip:trip.id);
                trip.delay=.5f;
            }
            if(last>=0 && trip.cargo==LandfillBuilding.Payload)
            {
                int current=trip.Current;var route=Search(new System.Collections.Generic.List<int>{current},Access(trip.home));
                trip.returning=true;trip.origin=last;trip.destination=trip.home;trip.route=route.Count>0?route:new System.Collections.Generic.List<int>{current};
                trip.segment=0;trip.progress=0;State.completed++;
                city.Trace("garbage.full","车辆满载，开始从当前位置实际返库",trip,trip:trip.id);
            }
        }
        void ArriveWaste(TrafficTrip trip)
        {
            if(trip.purpose==TripPurpose.GarbagePickup && !trip.returning)
            {
                var target=city.GetBuilding(trip.destination);var depot=city.GetBuilding(trip.home) as LandfillBuilding;
                if(target==null || depot==null){Fail(trip);return;}
                int otherCargo=State.trips.Where(t=>t!=trip && t.cargoKind==CargoKind.Waste && t.home==trip.home).Sum(t=>t.cargo);
                int amount=Math.Min(LandfillBuilding.Payload,Math.Min(target.garbage,Math.Max(0,LandfillBuilding.Capacity-depot.stored-otherCargo-trip.cargo)));
                amount=Math.Min(amount,LandfillBuilding.Payload-trip.cargo);target.garbage-=amount;trip.cargo+=amount;city.waste.collected+=amount;
                city.Trace("garbage.collected","垃圾车实际到达建筑后装载",trip,building:target.id,trip:trip.id);
            }
            else
            {
                var target=city.GetBuilding(trip.destination) as LandfillBuilding;
                if(target==null){Fail(trip);return;}
                int amount=Math.Min(trip.cargo,LandfillBuilding.Capacity-target.stored);target.stored+=amount;
                city.waste.lost+=trip.cargo-amount;
                if(trip.purpose==TripPurpose.GarbageTransfer && !trip.returning)city.waste.transferred+=amount;
                city.Trace("garbage.unloaded","实际抵达填埋场后卸载",new CityLogDetail{amount=amount,count=target.stored},building:target.id,trip:trip.id);trip.cargo=0;
            }
            if(trip.returning){State.trips.Remove(trip);return;}
            State.completed++;trip.status=TripStatus.Visiting;trip.delay=2;trip.blocked=0;
        }
    }
}
