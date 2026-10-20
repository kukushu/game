using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace HarborCity
{
    [Serializable] public sealed class CityLogEvent
    {
        public int schema=1;
        public long seq,tick;
        public string session, utc, type, level, message;
        public int day, householdId=-1, citizenId=-1, jobId=-1, buildingId=-1, tripId=-1, roadRevision;
        public float minute, simulationSeconds;
        [NonSerialized] public object data;
    }
    [Serializable] public sealed class CityLogDetail
    {
        public int origin=-1, destination=-1, previous=-1, amount, count;
        public float minutes, earned, x, z;
        public string reason;
        public List<int> ids;
        public List<HouseholdOption> options;
        public HouseholdOption currentOption, selectedOption;
    }
    public sealed partial class CityModel
    {
        [NonSerialized] public Action<CityLogEvent> logSink;
        public void Trace(string type,string message,object data=null,int household=-1,int citizen=-1,int job=-1,int building=-1,int trip=-1,string level="info")
        {
            if(logSink==null) return;
            var entry=new CityLogEvent {type=type,message=message,data=data,householdId=household,citizenId=citizen,
                jobId=job,buildingId=building,tripId=trip,level=level,day=day,minute=society==null?0:ResidentMinute,
                tick=entities?.Simulation?.Tick??0,simulationSeconds=(float)(entities?.Simulation?.Seconds??0),roadRevision=entities?.RoadRevision??0};
            // Observability must never break a simulation transaction.
            try {logSink?.Invoke(entry);} catch { }
        }
    }
}
