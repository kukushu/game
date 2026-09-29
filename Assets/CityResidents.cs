using System;
using System.Collections.Generic;

namespace HarborCity
{
    [Serializable] public sealed class CityResident
    {
        public int id, age;
        public string name;
        public bool worker;
        public int tripId, departureDay=-1, arrivedDay=-1, location=-1, observedHome=-1, observedWork=-1, observedRevision=-1;
        public bool atWork;
        public float earnedWages, workedMinutes, lastCommute=-1, lastDelay, retryAt, requestedAt=-1;
        public int accessNode=-1;
    }
    public sealed class ResidentActivity
    {
        public string state, reason;
        public int building=-1, destination=-1;
        public float x,z, progress;
        public bool located, travelling;
        public List<RoadNode> route=new List<RoadNode>();
    }
    public sealed partial class CityModel
    {
        // Additive v4 migration: existing families and decisions are preserved.
        // IDs depend only on the family and member slot, never on the simulation RNG.
        public void EnsureResidents()
        {
            if(society==null) return;
            foreach(var h in society.families)
            {
                if(h.people==null) h.people=new List<CityResident>();
                while(h.people.Count<h.members)
                {
                    int slot=h.people.Count, id=h.id*10+slot;
                    h.people.Add(new CityResident {id=id, name="赵钱孙李周吴郑王陈林"[h.id%10].ToString()+
                        new[]{"安","宁","晨","悦","远","禾","清","乐"}[id%8]+new[]{"然","明","雅","辰","舟"}[(id/8)%5],
                        age=slot==0?25+h.id%30:slot==1?24+h.id%30:6+(h.id+slot)%12, worker=slot==0});
                }
            }
        }
        public float ResidentMinute => society.dayElapsed/society.settings.secondsPerDay*1440;
        public ResidentActivity ObserveResident(Household h,CityResident person)
        {
            if(society.transportEnabled) return ObserveTransport(h,person);
            var a=new ResidentActivity();
            if(!h.resident || HousingCapacity(h.home)==0)
            {a.state=h.resident?"住所已拆除":"城外"; a.reason="暂无可定位的本城住所"; return a;}
            var home=buildings[h.home]; a.located=true; a.x=home.x; a.z=home.z; a.building=h.home;
            a.state="在家";
            if(!person.worker) {a.reason=person.age<18?"未成年成员；本版尚未模拟上学":"家庭成员；本版每户一名劳动者，暂未安排独立就业"; return a;}
            if(h.work<0) {a.reason="待业，等待家庭下一次工作评估"; return a;}
            float duration=CommuteMinutes(h.home,h.work);
            if(duration<0) {a.reason="工作地点不可达，今日留在家中"; a.destination=h.work; return a;}
            float now=ResidentMinute;
            // A deterministic daily itinerary is the authoritative position, independent of the visual car pool.
            float start=480+person.id%3*30, end=start+480;
            duration=Math.Max(.01f,duration);
            if(duration>start || end+duration>=1440) {a.reason="通勤过长，无法完成当日往返"; return a;}
            if(now<start-duration || now>=end+duration) {a.reason="休息；上班时间 "+((int)start/60).ToString("00")+":"+((int)start%60).ToString("00"); return a;}
            var work=buildings[h.work];
            if(now>=start && now<end)
            {a.state="工作中"; a.reason="按工作日程在岗（工资仍按家庭日结）"; a.building=h.work; a.x=work.x; a.z=work.z; return a;}
            bool returning=now>=end;
            a.state=returning?"回家途中":"上班途中"; a.reason="沿当前可达道路出行；道路改动后重新计算";
            a.travelling=true; a.building=-1; a.destination=returning?h.home:h.work;
            a.progress=Math.Clamp((now-(returning?end:start-duration))/duration,0,1);
            a.route.Add(new RoadNode{x=home.entranceX,z=home.entranceZ});
            foreach(int node in roads.FindPath(AccessBuilding(h.home),AccessBuilding(h.work))) a.route.Add(roads.Node(node));
            a.route.Add(new RoadNode{x=work.entranceX,z=work.entranceZ});
            if(returning) a.route.Reverse();
            float length=0; for(int i=1;i<a.route.Count;i++) length+=CityRoads.Length(a.route[i-1],a.route[i]);
            float remaining=length*a.progress;
            for(int i=1;i<a.route.Count;i++)
            {
                var from=a.route[i-1]; var to=a.route[i]; float segment=CityRoads.Length(from,to);
                if(remaining<=segment || i==a.route.Count-1)
                {float t=segment<=0?0:Math.Clamp(remaining/segment,0,1); a.x=from.x+(to.x-from.x)*t; a.z=from.z+(to.z-from.z)*t; break;}
                remaining-=segment;
            }
            return a;
        }
    }
}
