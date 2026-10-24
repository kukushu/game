using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
namespace HarborCity.RoadCore
{
    // Experimental observation only; disabled without an explicit diagnostics caller.
    public static class DepotRoutingTrace
    {
        public static Action<int, RoadAccess, RoadAccess, bool> QueryObserver;
        public static bool Enabled; public static int Depot; public static string Direction = "other";
        public sealed class Sample : IDisposable
        {
            public string kind, direction; public int depot, expanded; readonly long start = Stopwatch.GetTimestamp(), bytes = GC.GetAllocatedBytesForCurrentThread(); public double ms; public long allocated;
            internal Sample(string k)
            {
                kind = k;
                direction = Direction;
                depot = Depot;
            }
            public void Dispose()
            {
                ms = (Stopwatch.GetTimestamp() - start) * 1000.0 / Stopwatch.Frequency;
                allocated = CityCoreProfile.AllocationAvailable ? GC.GetAllocatedBytesForCurrentThread() - bytes : -1;
                Rows.Add(this);
            }
        }
        public static readonly List<Sample> Rows = new List<Sample>();
        public static Sample Start(string kind) => Enabled ? new Sample(kind) : null;
        public static void Reset()
        {
            Rows.Clear();
            Depot = 0;
            Direction = "other";
        }
        public static void Write(string path)
        {
            using (var w = new StreamWriter(path))
            {
                w.WriteLine("kind,depot,direction,expanded,ms,gc_bytes");
                foreach (var r in Rows)
                    w.WriteLine(r.kind + "," + r.depot + "," + r.direction + "," + r.expanded + "," + r.ms.ToString("R", CultureInfo.InvariantCulture) + "," + r.allocated);
            }
        }
    }
}
