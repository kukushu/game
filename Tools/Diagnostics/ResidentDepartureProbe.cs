// Test-only observer, copied into a disposable Temp assembly by the preparer.
namespace HarborCity.RoadCore
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    internal static class ResidentDepartureProbe
    {
        internal static bool Enabled;
        internal const long TargetTick=13941;
        internal struct Row {internal long tick,searches;internal int origin,destination;internal TransportPurpose purpose;internal TransportRejection reason;internal bool accepted,blocked;internal double ms;}
        static readonly List<Row> rows=new List<Row>();
        internal static void Record(long tick,int origin,int destination,TransportPurpose purpose,TransportRejection reason,bool accepted,bool blocked,long searches,long elapsed)
        {
            if(rows.Count>=200000)throw new InvalidOperationException("Bounded departure observation overflow");
            rows.Add(new Row{tick=tick,origin=origin,destination=destination,purpose=purpose,reason=reason,accepted=accepted,blocked=blocked,searches=searches,ms=elapsed*1000.0/System.Diagnostics.Stopwatch.Frequency});
        }
        internal static void Write(string directory)
        {
            if(rows.Count==0)throw new InvalidOperationException("Requested departure tick was not observed");
            File.WriteAllLines(Path.Combine(directory,"departure-attempts.csv"),new[]{"tick,origin,destination,purpose,result,all_reachable_sources_blocked,point_searches,prepare_ms"}.Concat(rows.Select(r=>string.Join(",",r.tick,r.origin,r.destination,r.purpose,r.accepted?"Accepted":r.reason.ToString(),r.blocked,r.searches,r.ms.ToString("R",CultureInfo.InvariantCulture)))));
            File.WriteAllLines(Path.Combine(directory,"departure-summary.csv"),new[]{"purpose,result,all_reachable_sources_blocked,attempts,point_searches,prepare_ms"}.Concat(rows.GroupBy(r=>new{r.purpose,r.reason,r.accepted,r.blocked}).OrderBy(g=>g.Key.purpose).ThenBy(g=>g.Key.reason).ThenBy(g=>g.Key.blocked).Select(g=>string.Join(",",g.Key.purpose,g.Key.accepted?"Accepted":g.Key.reason.ToString(),g.Key.blocked,g.Count(),g.Sum(r=>r.searches),g.Sum(r=>r.ms).ToString("R",CultureInfo.InvariantCulture)))));
            if(rows.Any(r=>r.accepted&&r.blocked))throw new InvalidOperationException("Occupied-source shadow contradicts accepted departure");
        }
    }
}
