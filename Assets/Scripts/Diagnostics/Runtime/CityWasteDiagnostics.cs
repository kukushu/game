using System;
using System.Collections.Generic;
using System.Linq;
using HarborCity.RoadCore;

namespace HarborCity
{
    // Optional, disposable measurement. Never read by scheduling or saved as facts.
    public sealed class WasteStationDiagnostic
    {
        public int station,peakFleet,peakStored;
        public long dispatchChecks,fleetFull,storageFull,unavailable,noCandidates,allCandidatesRejected,admissionAttempts,dispatched,pickups,returns,collected,delivered,processed,returnContinuationRejected,returnStorageBlocked,partialLegs;
        public double runningSeconds,emptyRunningSeconds,outboundSeconds,returnSeconds,outboundDistance,returnDistance,outboundFreeflowSeconds,returnFreeflowSeconds,outboundExcessSeconds,returnExcessSeconds,maxLegSeconds;
        internal readonly long[] rejected=new long[8];
        public Dictionary<string,long> AdmissionRejections
        {get{var result=new Dictionary<string,long>();foreach(TransportRejection reason in Enum.GetValues(typeof(TransportRejection)))result.Add(reason.ToString(),rejected[(int)reason]);return result;}}
        internal WasteStationDiagnostic Copy()=>(WasteStationDiagnostic)MemberwiseClone();
    }
    public sealed class CityWasteDiagnostics
    {
        struct Leg {internal int station;internal bool returning;internal double distance,freeflow;}
        readonly Dictionary<int,Leg> legs=new Dictionary<int,Leg>();
        readonly Dictionary<int,WasteStationDiagnostic> stations=new Dictionary<int,WasteStationDiagnostic>();
        public int PeakActive {get;private set;}
        WasteStationDiagnostic Station(int id){if(!stations.TryGetValue(id,out var s))stations.Add(id,s=new WasteStationDiagnostic{station=id});return s;}
        public WasteStationDiagnostic[] Snapshot()=>stations.OrderBy(p=>p.Key).Select(p=>p.Value.Copy()).ToArray();
        internal void Plant(int id,int stored,bool running,double seconds)
        {var s=Station(id);s.peakStored=Math.Max(s.peakStored,stored);if(running){s.runningSeconds+=seconds;if(stored==0)s.emptyRunningSeconds+=seconds;}}
        internal void Process(int id,int amount)=>Station(id).processed+=amount;
        internal WasteStationDiagnostic Dispatch(int id)=>Station(id);
        internal void Start(World world,WasteMission mission,Vehicle vehicle,int fleet,int active)
        {
            var s=Station(mission.station);s.peakFleet=Math.Max(s.peakFleet,fleet);PeakActive=Math.Max(PeakActive,active);
            var plan=vehicle.Plan;double length=0,freeflow=0;
            for(int i=0;i<plan.Segments.Count;i++)
            {
                var d=world.Network.Get(plan.Segments[i]);double start=i==0?vehicle.distance:d.StartDistance,end=i==plan.Segments.Count-1&&plan.EndDistance>=0?plan.EndDistance:d.EndDistance;
                double distance=end-start;length+=distance;freeflow+=distance/d.Speed;
            }
            legs.Add(mission.task,new Leg{station=mission.station,returning=mission.phase==WastePhase.Return,distance=length,freeflow=freeflow});
        }
        internal void Complete(int station,TripTask task,double now,int payload,bool returning)
        {
            var s=Station(station);double seconds=now-task.departedAt;s.maxLegSeconds=Math.Max(s.maxLegSeconds,seconds);
            if(returning){s.returns++;s.delivered+=payload;s.returnSeconds+=seconds;}else{s.pickups++;s.collected+=payload;s.outboundSeconds+=seconds;}
            if(!legs.TryGetValue(task.id,out var leg)){s.partialLegs++;return;}
            if(leg.station!=station||leg.returning!=returning)throw new InvalidOperationException("Waste diagnostic leg mismatch");
            legs.Remove(task.id);double excess=Math.Max(0,seconds-leg.freeflow);
            if(returning){s.returnDistance+=leg.distance;s.returnFreeflowSeconds+=leg.freeflow;s.returnExcessSeconds+=excess;}
            else{s.outboundDistance+=leg.distance;s.outboundFreeflowSeconds+=leg.freeflow;s.outboundExcessSeconds+=excess;}
        }
    }
}
