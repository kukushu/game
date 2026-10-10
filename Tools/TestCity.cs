using System;
using System.Linq;
using System.Web.Script.Serialization;
using HarborCity;

public static class TestCity
{
    public static float Flat(float x,float z)=>1;
    public static RoadNode P(float x,float z)=>new RoadNode{x=x,z=z,y=1};
    public static CityModel Empty()
    {var c=CityModel.Create();c.development.enabled=false;c.SetEntranceHeight(Flat);return c;}
    public static int Build(CityModel c,float x,float z,LandUse use,HousingKind housing=HousingKind.Apartment)
    {
        var preview=c.RoadsidePreview(x,z,use,housing,out string error);
        if(preview==null) throw new Exception(error);
        int id=c.PlaceBuilding(preview,use,Flat,out error);
        if(id<0) throw new Exception("Fixture: "+error+" at "+x+","+z);
        return id;
    }
    public static CityModel Create()
    {
        var c=Empty(); c.nextBuildingId=101;
        if(!c.CommitRoad(c.roads.Plan(c,c.roads.Node(CityRoads.Entrance),P(48,1.5f),Flat))) throw new Exception("Fixture road");
        foreach(var x in new[]{-42.137f,-30.137f,-18.137f,-6.137f,6.137f,18.137f}) {Build(c,x,7,LandUse.Residential);c.nextBuildingId+=7;}
        Build(c,-46,-5,LandUse.Power);Build(c,-38,-5,LandUse.Water);
        Build(c,-25,-5,LandUse.Commercial);Build(c,25,-5,LandUse.Commercial);
        Build(c,-12,-5,LandUse.Industrial);Build(c,12,-5,LandUse.Industrial);
        foreach(var home in c.buildings.OfType<ResidentialBuilding>())
        {
            int id=c.society.nextId++;
            var h=new Household {id=id,home=home.id,unit=0,resident=true,rent=home.askingRent,leaseEnd=31,nextReview=2+id%7,savings=1000,
                spacePreference=.5f,privacyPreference=.5f,timePreference=.5f,savingPreference=.5f,noiseSensitivity=.5f,pollutionSensitivity=.5f,trafficSensitivity=.5f};
            for(int n=0;n<3;n++) h.people.Add(new CityResident {id=id*10+n,householdId=id,name="测试居民"+(id*10+n),age=n==2?10:30,canWork=n<2,skill=n<2?2:0,location=home.id});
            c.society.families.Add(h);
        }
        c.society.nextCitizenId=c.Citizens.Max(p=>p.id)+1;
        foreach(var p in c.Citizens.Where(p=>p.canWork))
        {var job=c.society.jobEntities.FirstOrDefault(j=>j.occupiedCitizenId<0 && j.requiredSkill<=p.skill);if(job!=null)c.AssignJob(p,job.id);}
        c.Recalculate();if(!c.Valid())throw new Exception("Fixture invalid");return c;
    }
    public static CityModel RoundTrip(CityModel c)
    {var json=new JavaScriptSerializer {MaxJsonLength=int.MaxValue};return json.Deserialize<CitySaveData>(json.Serialize(c.ToSaveData())).ToCity();}
    public static string Snapshot(CityModel c)=>new JavaScriptSerializer {MaxJsonLength=int.MaxValue}.Serialize(c.ToSaveData());
}
