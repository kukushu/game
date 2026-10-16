using Unity.Profiling;

namespace HarborCity
{
    // A view-owned, cold projection. These strings/counts never drive employment,
    // payroll, simulation or saves. A new city/structural revision refreshes at once.
    internal sealed class HarborCityEmploymentReadout
    {
        static readonly ProfilerMarker sample=new ProfilerMarker("City.Views.UI.EmploymentReadout");
        readonly CityLocalization localization;
        CityCoreIndustrySession bound;long tick=-1;int revision=-1;double due;string text;
        internal HarborCityEmploymentReadout(CityLocalization localization){this.localization=localization;}
        internal string Read(CityCoreIndustrySession session,double realSeconds)
        {
            bool same=ReferenceEquals(bound,session)&&revision==session.Entities.Revision;
            if(same&&(tick==session.Simulation.Tick||realSeconds<due))return text;
            using(sample.Auto())
            {
                int employed=session.City.Employed,unemployed=session.City.Unemployed;
                text=localization.Get("ui.hud.employment",employed,employed+unemployed);
                bound=session;revision=session.Entities.Revision;tick=session.Simulation.Tick;due=realSeconds+.5;return text;
            }
        }
    }
}
