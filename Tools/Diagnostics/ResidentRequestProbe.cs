using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.Globalization;
using HarborCity.RoadCore;
namespace HarborCity
{
    // Temp-only observations. No values here participate in dispatch decisions.
    public static class ResidentRequestProbe
    {
        public enum Phase {Gate,Shift,Return,Eligible,Estimate,Observed,Leaving,Dispatch}
        public enum Read {Workplace,ResidentJob,Building,ShiftStart,ShiftMinutes,CanAttend,JobFunded}
        public static bool Active,Detailed,Timing,InAdvance;
        public static int Stride=64;
        static long advances,ordinal,begin,dispatchTicks,dispatchCalls;static int phase;static bool sampled,jobSeen;static int lastJob;
        public static long AdvanceTicks,Visits,Samples;
        static readonly long[] gates=new long[6],phaseTicks=new long[8],phaseCalls=new long[8],samplePhaseCalls=new long[8];
        static readonly long[,] readCalls=new long[8,7],readTicks=new long[8,7],readSamples=new long[8,7];
        static readonly long[] jobs=new long[8],repeatJobs=new long[8],invalidJobs=new long[8],returns=new long[2],eligibility=new long[2];
        static long requests,leavers,returners;
        static string N(double n)=>n.ToString("R",CultureInfo.InvariantCulture);
        public static void Reset(bool detailed,bool timing,int stride)
        {
            Detailed=detailed;Timing=timing;Stride=stride;Active=true;InAdvance=false;advances=ordinal=AdvanceTicks=Visits=Samples=dispatchTicks=dispatchCalls=requests=leavers=returners=0;
            foreach(var a in new Array[]{gates,phaseTicks,phaseCalls,samplePhaseCalls,readCalls,readTicks,readSamples,jobs,repeatJobs,invalidJobs,returns,eligibility})Array.Clear(a,0,a.Length);
        }
        public readonly struct AdvanceScope:IDisposable
        {
            readonly bool enabled;readonly long start;
            public AdvanceScope(bool enabled){this.enabled=enabled;start=enabled?Stopwatch.GetTimestamp():0;if(enabled){InAdvance=true;advances++;ordinal=0;}}
            public void Dispose(){if(!enabled)return;AdvanceTicks+=Stopwatch.GetTimestamp()-start;InAdvance=false;sampled=false;}
        }
        public static AdvanceScope Advance()=>new AdvanceScope(Active);
        public readonly struct PersonScope:IDisposable
        {readonly bool enabled;public PersonScope(bool value){enabled=value;}public void Dispose(){if(enabled)phaseTicks[phase]+=Stopwatch.GetTimestamp()-begin;sampled=false;}}
        public static PersonScope Person()
        {
            if(!Active||!Detailed)return default;Visits++;jobSeen=false;phase=0;phaseCalls[0]++;sampled=Timing&&((ordinal++ + advances)%Stride==0);
            if(sampled){Samples++;samplePhaseCalls[0]++;begin=Stopwatch.GetTimestamp();}return new PersonScope(sampled);
        }
        public static void Next(Phase next)
        {if(!Active||!Detailed)return;if(sampled){long now=Stopwatch.GetTimestamp();phaseTicks[phase]+=now-begin;begin=now;samplePhaseCalls[(int)next]++;}phase=(int)next;phaseCalls[phase]++;}
        public static bool Gate(Household h,CityResident p,World world)
        {
            if(!h.resident){if(Active)gates[0]++;return false;}if(p.dead){if(Active)gates[1]++;return false;}if(p.tripId!=0){if(Active)gates[2]++;return false;}if(world.Clock<p.retryAt){if(Active)gates[3]++;return false;}if(world.Clock<p.visitUntil){if(Active)gates[4]++;return false;}if(Active)gates[5]++;return true;
        }
        public static void Results(bool returning,bool eligible,bool leaving)
        {if(!Active||!Detailed)return;returns[returning?1:0]++;eligibility[eligible?1:0]++;if(leaving)leavers++;if(returning)returners++;if(returning||leaving)requests++;}
        public readonly struct ReadScope:IDisposable
        {
            readonly bool timed;readonly int p,k;readonly long start;
            public ReadScope(Read kind){p=phase;k=(int)kind;timed=Active&&sampled;start=timed?Stopwatch.GetTimestamp():0;if(Active&&Detailed){readCalls[p,k]++;if(timed)readSamples[p,k]++;}}
            public void Dispose(){if(timed)readTicks[p,k]+=Stopwatch.GetTimestamp()-start;}
        }
        public static int Workplace(CityModel city,CityResident p){using var s=new ReadScope(Read.Workplace);return city.Workplace(p);}
        public static CityJob Job(CityModel city,CityResident p){using var s=new ReadScope(Read.ResidentJob);return city.ResidentJob(p);}
        public static CityBuilding Building(CityModel city,int id){using var s=new ReadScope(Read.Building);return city.GetBuilding(id);}
        public static int Shift(EmploymentRules rules,int id,bool commercial){using var s=new ReadScope(Read.ShiftStart);return rules.ShiftStart(id,commercial);}
        public static int Minutes(EmploymentRules rules){using var s=new ReadScope(Read.ShiftMinutes);return rules.ShiftMinutes;}
        public static bool Attend(CityModel city,CityResident p){using var s=new ReadScope(Read.CanAttend);return city.CanAttendWork(p);}
        public static bool Funded(CityModel city,CityJob job){using var s=new ReadScope(Read.JobFunded);return city.JobFunded(job);}
        public static void Lookup(int id)
        {if(!Active||!InAdvance||!Detailed)return;jobs[phase]++;if(id<0)invalidJobs[phase]++;if(jobSeen&&id==lastJob)repeatJobs[phase]++;jobSeen=true;lastJob=id;}
        public readonly struct DispatchScope:IDisposable
        {readonly long start;readonly bool enabled;public DispatchScope(bool value){enabled=value;start=value?Stopwatch.GetTimestamp():0;}public void Dispose(){if(enabled){dispatchCalls++;dispatchTicks+=Stopwatch.GetTimestamp()-start;}}}
        public static DispatchScope Dispatch()=>new DispatchScope(Active);
        public static void Write(string folder)
        {
            Active=false;double ms=1000.0/Stopwatch.Frequency;
            File.WriteAllText(Path.Combine(folder,"resident-total.csv"),"calls,resident_ms,dispatch_calls,dispatch_ms,visits,sampled,stride\n"+string.Join(",",advances,N(AdvanceTicks*ms),dispatchCalls,N(dispatchTicks*ms),Visits,Samples,Stride));
            File.WriteAllLines(Path.Combine(folder,"gates.csv"),new[]{"reason,count"}.Concat(new[]{"nonresident","dead","in_transit","retry_delay","shopping_visit","passed"}.Select((n,i)=>n+","+gates[i])));
            File.WriteAllLines(Path.Combine(folder,"phases.csv"),new[]{"phase,reached,sampled,sampled_ms"}.Concat(Enum.GetValues(typeof(Phase)).Cast<Phase>().Select(p=>string.Join(",",p,phaseCalls[(int)p],samplePhaseCalls[(int)p],N(phaseTicks[(int)p]*ms)))));
            var reads=new List<string>{"phase,read,calls,sampled_calls,sampled_ms"};foreach(Phase p in Enum.GetValues(typeof(Phase)))foreach(Read r in Enum.GetValues(typeof(Read)))reads.Add(string.Join(",",p,r,readCalls[(int)p,(int)r],readSamples[(int)p,(int)r],N(readTicks[(int)p,(int)r]*ms)));File.WriteAllLines(Path.Combine(folder,"reads.csv"),reads);
            File.WriteAllLines(Path.Combine(folder,"job-lookups.csv"),new[]{"phase,calls,repeat_same_id_within_iteration,invalid_id"}.Concat(Enum.GetValues(typeof(Phase)).Cast<Phase>().Select(p=>string.Join(",",p,jobs[(int)p],repeatJobs[(int)p],invalidJobs[(int)p]))));
            File.WriteAllText(Path.Combine(folder,"decisions.csv"),"return_true,return_false,eligible_true,eligible_false,leaving_true,request_candidates\n"+string.Join(",",returns[1],returns[0],eligibility[1],eligibility[0],leavers,requests));
        }
        sealed class Arg {internal CityResident p;internal CityJob j;internal int workplace;internal bool commercial;}
        public static void Bench(CityCoreIndustrySession city,string folder)
        {
            // Frozen end-of-window pure reads, not actual Step/Player performance.
            CityCoreProfile.Enabled=false;Active=false;var args=city.Entities.Members.Where(m=>m.Family.resident&&!m.Person.dead&&m.Person.tripId==0&&city.Transport.Clock>=m.Person.retryAt&&city.Transport.Clock>=m.Person.visitUntil).Select(m=>{var p=m.Person;int w=city.City.Workplace(p);return new Arg{p=p,j=city.City.ResidentJob(p),workplace=w,commercial=city.City.development.enabled&&city.City.GetBuilding(w) is CommercialBuilding};}).ToArray();
            var ops=new Dictionary<string,Func<Arg,int>>{{"control",a=>a.p.id},{"Workplace",a=>city.City.Workplace(a.p)},{"ResidentJob",a=>city.City.ResidentJob(a.p)?.id??-1},{"Building",a=>city.City.GetBuilding(a.workplace)?.id??-1},{"ShiftStart",a=>city.Entities.Employment.ShiftStart(a.p.id,a.commercial)},{"ShiftMinutes",a=>city.Entities.Employment.ShiftMinutes},{"CanAttend",a=>city.City.CanAttendWork(a.p)?1:0},{"JobFunded",a=>city.City.JobFunded(a.j)?1:0}};
            long checksum=0;var lines=new List<string>{"round,read,calls,ms,checksum"};for(int round=-1;round<5;round++)foreach(var op in round%2==0?ops.AsEnumerable():ops.Reverse())
            {long start=Stopwatch.GetTimestamp();for(int repeat=0;repeat<32;repeat++)foreach(var a in args)checksum+=op.Value(a);double elapsed=(Stopwatch.GetTimestamp()-start)*1000.0/Stopwatch.Frequency;lines.Add(string.Join(",",round,op.Key,args.Length*32,N(elapsed),checksum));}File.WriteAllLines(Path.Combine(folder,"read-bench.csv"),lines);
            var loops=new Dictionary<string,Action>{{"enumeration",()=>{foreach(var m in city.Entities.Members)checksum+=m.Person.id;}},{"enumeration_and_gate",()=>{foreach(var m in city.Entities.Members){var p=m.Person;var h=m.Family;if(!h.resident||p.dead||p.tripId!=0||city.Transport.Clock<p.retryAt||city.Transport.Clock<p.visitUntil)continue;checksum++;}}}};
            lines=new List<string>{"round,read,calls,ms,checksum"};for(int round=-1;round<5;round++)foreach(var op in round%2==0?loops.AsEnumerable():loops.Reverse()){long start=Stopwatch.GetTimestamp();for(int repeat=0;repeat<32;repeat++)op.Value();lines.Add(string.Join(",",round,op.Key,city.Entities.Members.Count*32,N((Stopwatch.GetTimestamp()-start)*1000.0/Stopwatch.Frequency),checksum));}File.WriteAllLines(Path.Combine(folder,"loop-bench.csv"),lines);
        }
    }
}
