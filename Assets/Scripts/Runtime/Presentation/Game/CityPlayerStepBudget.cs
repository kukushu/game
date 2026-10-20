using System;
using HarborCity.RoadCore;
namespace HarborCity
{
    // Render-host pacing credit only, not a second simulation clock. Requested
    // time/debt remains exclusively on CitySimulation's saved clock.
    public sealed class CityPlayerStepBudget
    {
        readonly int maximumSteps;double credit;
        public CityPlayerStepBudget(int maximumSteps)
        {if(maximumSteps<1)throw new ArgumentOutOfRangeException(nameof(maximumSteps));this.maximumSteps=maximumSteps;}
        public void Reset(){credit=0;}
        public int Request(double seconds)
        {
            if(seconds<0||double.IsNaN(seconds)||double.IsInfinity(seconds))throw new ArgumentOutOfRangeException(nameof(seconds));
            credit=Math.Min(maximumSteps*SimulationClock.StepSeconds,credit+seconds);
            return Math.Min(maximumSteps,(int)Math.Floor((credit+1e-10)/SimulationClock.StepSeconds));
        }
        public void Complete(int steps)
        {
            if(steps<0||steps>Math.Floor((credit+1e-10)/SimulationClock.StepSeconds))throw new ArgumentOutOfRangeException(nameof(steps));
            credit=Math.Max(0,credit-steps*SimulationClock.StepSeconds);
        }
    }
}
