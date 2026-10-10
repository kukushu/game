using System;
using System.Linq;

namespace HarborCity
{
    [Serializable] public sealed class LandfillBuilding : CityBuilding
    {
        public override LandUse Use=>LandUse.Landfill;
        // Provisional scaled units; exact original capacities/rates remain unverified.
        public const int Capacity=3200,Trucks=3,Payload=8;
        public int stored;
        public bool emptying;
    }
    [Serializable] public sealed class CityWasteState
    {
        public bool enabled=true;
        public long generated,collected,transferred,lost;
        public bool Valid()=>generated>=0 && collected>=0 && transferred>=0 && lost>=0;
    }
    public sealed partial class CityModel
    {
        public CityWasteState waste=new CityWasteState();
        public const int GarbageWarning=32;
        public long GarbageInBuildings=>buildings.Sum(b=>(long)b.garbage);
        public long LandfillWaste=>buildings.OfType<LandfillBuilding>().Sum(b=>(long)b.stored);
        public long GarbageInTransit=>traffic.trips.Where(t=>t.cargoKind==CargoKind.Waste).Sum(t=>(long)t.cargo);
        public long WasteBalanceError=>waste.generated-(GarbageInBuildings+LandfillWaste+GarbageInTransit+waste.lost);
        public int LandfillIncoming(int id)=>traffic.trips.Where(t=>t.cargoKind==CargoKind.Waste &&
            (t.purpose==TripPurpose.GarbagePickup && t.home==id || t.purpose==TripPurpose.GarbageTransfer && !t.returning && t.destination==id)).Sum(t=>t.cargo>0?t.cargo:LandfillBuilding.Payload);
        public bool SetLandfillEmptying(int id,bool value)
        {
            var b=GetBuilding(id) as LandfillBuilding;if(b==null)return false;b.emptying=value;
            Trace("garbage.emptying",value?"填埋场开始清空；须实际转运":"填埋场恢复接收垃圾",b,building:id);return true;
        }
        void GenerateGarbage()
        {
            if(!waste.enabled)return;
            foreach(var b in buildings.Where(b=>!b.abandoned && CityZoningState.ZoneUse(b.Use)))
            {
                int amount=b is ResidentialBuilding?society.families.Where(h=>h.resident && h.home==b.id).Sum(h=>LivingMembers(h)):
                    (int)Math.Floor(Citizens.Where(p=>Workplace(p)==b.id).Sum(p=>(double)p.workedMinutes)/240);
                amount=Math.Min(amount,100000-b.garbage);b.garbage+=amount;waste.generated+=amount;
                if(amount>0)Trace("garbage.generated","实际住户/劳动活动产生建筑垃圾（速率待校准）",new CityLogDetail{amount=amount,count=b.garbage},building:b.id);
            }
        }
    }
}
