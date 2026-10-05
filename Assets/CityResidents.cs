using System;
using System.Collections.Generic;

namespace HarborCity
{
    [Serializable] public sealed class CityResident
    {
        public int id, age;
        public string name;
        public bool canWork;
        public int householdId, skill, jobId=-1, wagePaid;
        public int tripId, departureDay=-1, arrivedDay=-1, location=-1, observedHome=-1, observedWork=-1, observedRevision=-1;
        public bool atWork;
        public double factoryWageCredit;
        public int totalFactoryWagesPaid;
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
        void InitializeResidents(Household h,int count)
        {
            if(h.people==null) h.people=new List<CityResident>();
            while(h.people.Count<count)
            {
                int slot=h.people.Count, id=h.id*10+slot;
                int age=slot==0?25+h.id%30:slot==1?24+h.id%30:6+(h.id+slot)%12;
                h.people.Add(new CityResident {id=id,householdId=h.id, name="赵钱孙李周吴郑王陈林"[h.id%10].ToString()+
                    new[]{"安","宁","晨","悦","远","禾","清","乐"}[id%8]+new[]{"然","明","雅","辰","舟"}[(id/8)%5],
                    age=age,canWork=age>=18,skill=(h.id+slot)%3});
            }
        }
        public float ResidentMinute => society.dayElapsed/society.settings.secondsPerDay*1440;
        public ResidentActivity ObserveResident(Household h,CityResident person)=>ObserveTransport(h,person);
    }
}
