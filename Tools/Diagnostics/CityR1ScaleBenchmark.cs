using System;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Diagnostics;
using HarborCity;
using HarborCity.RoadCore;

// Shared before/after workload: real registered people/jobs/factories/shops and
// physical commuting/freight. Finite inputs, no service/consumption/full-city claim.
public static class CityR1ScaleBenchmark
{
    public static void Main(string[] args)
    {
        using(var output=new StreamWriter(args[0]))
        {
            output.WriteLine("version,residents,factories,shops,steps,active_min,active_max,mean_ms,p95_ms,p99_ms,allocated_per_step,produced,delivered");
            foreach(int count in new[]{100,1000,3000})
            {
                var session=Create(count);for(int i=0;i<20;i++)session.Advance(.05);
                var times=new double[200];int min=int.MaxValue,max=0;var timer=new Stopwatch();long memory=GC.GetAllocatedBytesForCurrentThread();
                for(int i=0;i<times.Length;i++){timer.Restart();session.Advance(.05);timer.Stop();times[i]=timer.Elapsed.TotalMilliseconds;min=Math.Min(min,session.Transport.ActiveCount);max=Math.Max(max,session.Transport.ActiveCount);}
                memory=GC.GetAllocatedBytesForCurrentThread()-memory;session.Validate();int produced=session.City.buildings.OfType<IndustrialBuilding>().Sum(b=>b.factory.produced);int goods=session.City.buildings.OfType<IndustrialBuilding>().Sum(b=>b.Stock)+session.City.buildings.OfType<CommercialBuilding>().Sum(b=>b.Stock)+session.Transport.Vehicles.Sum(v=>v.cargo);if(produced!=goods)throw new Exception("Goods conservation failed");if(!session.City.Citizens.All(p=>p.atWork&&p.workedMinutes>0))throw new Exception("Fixture workers did not actually commute and work");double mean=times.Average();Array.Sort(times);
                string row=string.Join(",",args[1],count,count/4,count/4,times.Length,min,max,mean.ToString("F6",CultureInfo.InvariantCulture),times[189].ToString("F6",CultureInfo.InvariantCulture),times[197].ToString("F6",CultureInfo.InvariantCulture),memory/times.Length,produced,session.Transport.Delivered);output.WriteLine(row);Console.WriteLine(row);
            }
        }
    }
    static CityCoreIndustrySession Create(int population)
    {
        var city=new CityModel();int groups=population/4;city.development.enabled=false;
        for(int i=0;i<groups;i++)
        {
            int f=i*3+1,s=f+1,h=f+2;float x=i*30;
            var factory=new IndustrialBuilding{id=f,x=x+10,z=-3,entranceX=x+10,entranceZ=-1};factory.factory.capital=50000;factory.factory.raw=24;factory.factory.imported=24;factory.factory.rawCosts=24*FactoryState.RawPrice;factory.factory.cash=50000-factory.factory.rawCosts;
            city.buildings.Add(factory);city.buildings.Add(new CommercialBuilding{id=s,x=x+20,z=-3,entranceX=x+20,entranceZ=-1});city.buildings.Add(new ResidentialBuilding{id=h,x=x+2,z=3,entranceX=x+2,entranceZ=1,housing=HousingKind.Apartment,housingUnits=1});
            var family=new Household{id=i+1,home=h,unit=0,resident=true};for(int k=0;k<4;k++)family.people.Add(new CityResident{id=i*4+k+1,householdId=family.id,name="规模工人",age=30,canWork=true,skill=2,location=h});city.society.families.Add(family);
        }
        city.nextBuildingId=groups*3+1;city.society.nextId=groups+1;city.society.nextCitizenId=population+1;city.SyncJobs();var jobs=city.society.jobEntities.ToLookup(j=>j.buildingId);
        for(int i=0;i<groups;i++){var slots=jobs[i*3+1].ToArray();for(int k=0;k<4;k++)city.AssignJob(city.society.families[i].people[k],slots[k].id);}
        var session=new CityCoreIndustrySession(city);int a=session.Transport.AddJunction(new Point(0,0,0)),b=session.Transport.AddJunction(new Point(groups*30+30,0,0));int road=session.Transport.AddRoad(a,b,new[]{session.Transport.JunctionPosition(a),session.Transport.JunctionPosition(b)},lanes:2);long lane=Network.LaneId(road,false);
        for(int i=0;i<groups;i++){int f=i*3+1;double x=i*30;session.Transport.RegisterAccess(f,lane,x+10);session.Transport.RegisterAccess(f+1,lane,x+20);session.Transport.RegisterAccess(f+2,lane,x+2);}return session;
    }
}
