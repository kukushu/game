using System;
using System.Linq;
using HarborCity.RoadCore;
namespace HarborCity
{
public static class CityIntegratedScaleFixture
{
    public static CityCoreIndustrySession Create(int population)
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
}
