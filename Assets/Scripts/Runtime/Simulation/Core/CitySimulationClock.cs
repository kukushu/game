using System;
namespace HarborCity.RoadCore
{
    [Serializable] public sealed class SimulationClockData {public long tick;public double pendingSeconds;}
    public readonly struct TimeContext
    {
        public readonly long Tick;
        public double Start=>Tick*SimulationClock.StepSeconds;
        public double End=>(Tick+1)*SimulationClock.StepSeconds;
        public double Duration=>SimulationClock.StepSeconds;
        internal TimeContext(long tick){Tick=tick;}
    }
    public sealed class SimulationClock
    {
        public const double StepSeconds=.05;
        long tick;double pending;
        public long Tick=>tick;public double Seconds=>tick*StepSeconds;public double PendingSeconds=>pending;
        public bool HasStep=>pending+1e-12>=StepSeconds;
        public TimeContext Context=>new TimeContext(tick);
        public SimulationClock(SimulationClockData state=null)
        {if(state==null)return;if(state.tick<0||state.tick>1000000000000L||state.pendingSeconds<0||double.IsNaN(state.pendingSeconds)||double.IsInfinity(state.pendingSeconds))throw new ArgumentException("Invalid simulation clock");tick=state.tick;pending=state.pendingSeconds;}
        public SimulationClockData Snapshot()=>new SimulationClockData{tick=tick,pendingSeconds=pending};
        internal SimulationClock Copy()=>new SimulationClock(Snapshot());
        internal void Request(double seconds)
        {if(seconds<0||double.IsNaN(seconds)||double.IsInfinity(seconds)||double.IsInfinity(pending+seconds))throw new ArgumentException("Invalid simulation time request");pending+=seconds;}
        internal void Commit(bool consumeDebt)
        {if(consumeDebt&&!HasStep)throw new InvalidOperationException("No fixed-step debt");if(tick>=1000000000000L)throw new InvalidOperationException("Clock exhausted");tick++;if(consumeDebt)pending=Math.Max(0,pending-StepSeconds);}
        internal void ConsumeDebt(){pending=Math.Max(0,pending-StepSeconds);}
    }
}
