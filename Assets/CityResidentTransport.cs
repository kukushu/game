using System;
using System.Linq;

namespace HarborCity
{
    public sealed partial class CityModel
    {
        public void EnableResidentTransport()
        {
            EnsureResidents();
            if(society.transportEnabled) return;
            // Old representative commuters have no one-to-one resident identity.
            traffic.trips.RemoveAll(t=>t.purpose==TripPurpose.Commute && t.residentId==0);
            foreach(var h in society.families) foreach(var p in h.people)
            {p.location=h.home; p.requestedAt=-1; p.departureDay=-1; p.tripId=0; p.atWork=false;}
            society.transportEnabled=true;
        }
        public float ExpectedCommute(Household h,int home,int work)
        {
            float estimate=CommuteMinutes(home,work);
            if(estimate<0 || !society.transportEnabled || h.people==null) return estimate;
            var p=h.people.Find(person=>person.worker);
            // Only reuse observations for this exact home/job and road revision.
            // New alternatives retain a free-flow estimate until actually tried.
            return p!=null && p.lastCommute>=0 && p.observedHome==home && p.observedWork==work && p.observedRevision==roads.revision
                ? Math.Max(estimate,p.lastCommute) : estimate;
        }
        public ResidentActivity ObserveTransport(Household h,CityResident p)
        {
            var a=new ResidentActivity();
            var trip=traffic.trips.Find(t=>t.id==p.tripId && t.residentId==p.id);
            if(trip!=null)
            {
                var from=roads.Node(trip.Current); var to=roads.Node(trip.Next);
                a.located=from!=null && to!=null; a.travelling=true; a.destination=trip.destination;
                if(a.located) {a.x=from.x+(to.x-from.x)*trip.progress; a.z=from.z+(to.z-from.z)*trip.progress;}
                a.state=trip.status==TripStatus.Waiting?"道路中断，等待恢复":trip.blocked>.1f?"通勤排队":trip.returning?"回家途中":"上班途中";
                a.reason="实际车辆 #"+trip.id+" · 已出行 "+((traffic.clock-trip.departedAt)*1440/society.settings.secondsPerDay).ToString("F1")+" 分钟；等待 "+(trip.blocked*1440/society.settings.secondsPerDay).ToString("F1")+" 分钟";
                a.progress=(trip.segment+trip.progress)/Math.Max(1,trip.route.Count-1);
                foreach(int n in trip.route.Skip(trip.segment)) {var node=roads.Node(n); if(node!=null) a.route.Add(node);}
                return a;
            }
            int location=p.atWork?p.location:h.home;
            a.located=h.resident && location>=0 && location<tiles.Length && tiles[location]!=0;
            a.building=a.located?location:-1;
            if(a.located) {a.x=buildings[location].x; a.z=buildings[location].z;}
            float shift=480+p.id%3*30;
            a.state=!h.resident?"城外":p.atWork?(ResidentMinute<shift+480 && p.arrivedDay==day?"工作中":"等待返程"):
                p.worker && h.work>=0 && ResidentMinute>=Math.Max(0,shift-Math.Max(0,ExpectedCommute(h,h.home,h.work))) && p.departureDay!=day?"等待出发":"在家";
            a.destination=a.state=="等待返程"?h.home:a.state=="等待出发"?h.work:-1;
            a.reason=!p.worker?"家庭成员；暂未安排独立就业、上学或购物":
                "今日实际在岗 "+p.workedMinutes.ToString("F1")+" / 480 分钟，已挣 ¥"+p.earnedWages.ToString("F1")+"（日末入账）"+
                (p.lastCommute>=0?"；最近上班耗时 "+p.lastCommute.ToString("F1")+" 分钟，迟到 "+p.lastDelay.ToString("F1")+" 分钟":"；尚无实际到岗记录");
            if(a.state=="等待出发" || a.state=="等待返程") a.reason+="；入口排队或道路不可达，尚未发车";
            return a;
        }
    }

    public sealed partial class CityTraffic
    {
        Household TripFamily(TrafficTrip t) => city.society?.families.Find(h=>h.id==t.householdId);
        bool ValidateResidentTrip(TrafficTrip t)
        {
            var h=TripFamily(t); var p=h?.people.Find(person=>person.id==t.residentId);
            if(h==null || p==null || !h.resident)
            {if(p!=null) {p.tripId=0; p.atWork=false;} State.trips.Remove(t); return false;}
            if(!Endpoint(t.destination) || (t.returning && t.destination!=h.home))
            {
                // A removed workplace sends the resident home via the surviving route graph.
                t.destination=h.home; t.home=h.home; t.returning=true;
                if(!Endpoint(t.destination)) {Wait(t); return false;}
            }
            // The origin building may be demolished while a car is already on the road.
            t.home=h.home;
            if(!Endpoint(t.home)) {Wait(t); return false;}
            return true;
        }
        void ArriveResident(TrafficTrip t)
        {
            var h=TripFamily(t); var p=h?.people.Find(person=>person.id==t.residentId);
            if(p!=null)
            {
                p.tripId=0; p.location=t.destination; p.accessNode=t.Current; p.atWork=!t.returning;
                if(!t.returning)
                {
                    p.arrivedDay=city.day;
                    p.lastCommute=Math.Max(0,(State.clock-t.departedAt)*1440/city.society.settings.secondsPerDay);
                    p.lastDelay=Math.Max(0,(city.day-p.departureDay)*1440+city.ResidentMinute-(480+p.id%3*30));
                    p.observedHome=t.origin; p.observedWork=t.destination; p.observedRevision=city.roads.revision;
                }
            }
            State.completed++; State.trips.Remove(t);
        }
        void AdvanceResidents()
        {
            var s=city.society;
            float minute=city.ResidentMinute, minutes=Step*1440/s.settings.secondsPerDay;
            foreach(var h in s.families)
            {
                if(!h.resident || h.people==null) continue;
                foreach(var p in h.people)
                {
                    if(!p.worker) continue;
                    float shift=480+p.id%3*30, end=shift+480;
                    if(p.atWork && p.tripId==0 && p.arrivedDay==city.day && city.JobCapacity(p.location)>0)
                    {
                        float worked=Math.Max(0,Math.Min(minute+minutes,end)-Math.Max(minute,shift));
                        p.workedMinutes+=worked; p.earnedWages+=worked/480*city.Wage(p.location);
                    }
                    if(p.tripId>0 || State.clock<p.retryAt) continue;
                    bool returning=p.atWork && (minute>=end || p.arrivedDay<city.day || city.JobCapacity(p.location)==0);
                    float estimate=city.ExpectedCommute(h,h.home,h.work);
                    bool departing=!p.atWork && p.departureDay!=city.day && estimate>=0 && minute>=Math.Max(0,shift-estimate) && minute<end;
                    if(!returning && !departing) continue;
                    if(p.requestedAt<0) p.requestedAt=State.clock;
                    p.retryAt=State.clock+.25f;
                    var trip=Dispatch(returning?p.location:h.home,returning?h.home:h.work,TripPurpose.Commute,p.id);
                    // If the workplace was demolished after arrival, depart from its
                    // recorded road access instead of inventing a new position.
                    if(trip==null && returning && !Endpoint(p.location) && Endpoint(h.home) && Road(p.accessNode))
                    {
                        var route=Search(new System.Collections.Generic.List<int>{p.accessNode},Access(h.home));
                        if(route.Count>0 && CanEnter(route,null) && State.trips.Count<TaskCapacity)
                        {trip=new TrafficTrip{id=State.nextId++,origin=p.location,destination=h.home,purpose=TripPurpose.Commute,residentId=p.id,route=route}; State.trips.Add(trip);}
                    }
                    if(trip==null) continue;
                    trip.departedAt=p.requestedAt; p.requestedAt=-1;
                    trip.householdId=h.id; trip.home=h.home; trip.returning=returning;
                    p.tripId=trip.id; if(!returning) p.departureDay=city.day;
                }
            }
            // Traffic and the household clock advance on the same fixed step, including multi-day fast forward.
            s.dayElapsed+=Step;
            if(s.dayElapsed+.00001f>=s.settings.secondsPerDay)
            {s.dayElapsed=Math.Max(0,s.dayElapsed-s.settings.secondsPerDay); city.Tick();}
        }
    }
}
