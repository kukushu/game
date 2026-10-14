using System;

namespace HarborCity.RoadCore
{
    [Flags] public enum AccessUse { Freight=1,Resident=2,Service=4,All=7 }
    public readonly struct AccessRegistration
    {
        public readonly int endpoint;public readonly long lane;public readonly double distance;public readonly AccessUse use;
        public AccessRegistration(int endpoint,long lane,double distance,AccessUse use=AccessUse.All){this.endpoint=endpoint;this.lane=lane;this.distance=distance;this.use=use;}
    }
    public readonly struct AccessBinding
    {
        public readonly int endpoint;public readonly long lane;public readonly double distance;
        public AccessBinding(int endpoint,long lane,double distance){this.endpoint=endpoint;this.lane=lane;this.distance=distance;}
    }
    // A road attachment is a fact owned by the road world. Physical building door
    // coordinates remain in the business building; there is no copied building pose.
    [Serializable] public sealed class RoadAccess
    {
        public int endpoint {get;internal set;}
        public int preferredIndex {get;internal set;}
        public long lane {get;internal set;}
        public double distance {get;internal set;}
        public AccessUse use {get;internal set;}=AccessUse.All;
        public bool valid {get;internal set;}=true;
        internal RoadAccess Copy()=>new RoadAccess{endpoint=endpoint,preferredIndex=preferredIndex,lane=lane,distance=distance,use=use,valid=valid};
    }
}
