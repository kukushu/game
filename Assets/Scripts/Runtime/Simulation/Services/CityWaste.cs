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
        public const int ProcessingPerDay=256;
        public float processingRemainder;
    }
    [Serializable] public sealed class CityWasteState
    {
        public bool enabled=true;
        public long generated,collected,transferred,lost,processed;
        public bool Valid()=>generated>=0 && collected>=0 && transferred>=0 && lost>=0 && processed>=0 && processed<=generated;
    }
    public sealed partial class CityModel
    {
        public CityWasteState waste=new CityWasteState();
        public const int GarbageWarning=32;
    }
}
