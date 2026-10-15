using System;
using System.Linq;
using HarborCity.RoadCore;

namespace HarborCity
{
    // Test construction only. After creation all activity belongs to the normal
    // CitySimulation. No pre-filled goods, arrivals, wages or onsite labour.
    public sealed class CityScenarioSettings
    {
        public int schema=1,population=500,seed=7919,gridRadius=3,streetSpacing=48,days=30,warmupDays=2;
        public string name="mature-grid-500",mode="mature",layout="grid";
        public int openingPublicGrant=1000000;
        public void Validate()
        {
            if(schema!=1||population<3||population>10000||gridRadius<2||gridRadius>3||streetSpacing<24||streetSpacing*gridRadius>144||days<1||days>120||warmupDays<0||warmupDays>=days||openingPublicGrant<0||layout!="grid"||mode!="mature")throw new ArgumentException("Unsupported scenario settings; stage one supports a bounded mature grid, not unverified larger cities/layouts");
        }
    }
    public static class CityScenarioBuilder
    {
        public static CityCoreIndustrySession Create(CityScenarioSettings settings,GameDesignData design=null)
        {
            settings.Validate();var s=CityCoreIndustrySession.CreateEmpty(design);var city=s.City;var zoning=s.Simulation.Growth.Zoning;zoning.SetSurface(Flat);
            if(settings.openingPublicGrant>0)s.Simulation.Finance.GrantDevelopmentFunds(settings.openingPublicGrant);
            int r=settings.gridRadius,spacing=settings.streetSpacing;
            for(int z=-r;z<=r;z++)for(int x=-r;x<r;x++)Road(s,x*spacing,z*spacing,(x+1)*spacing,z*spacing);
            for(int x=-r;x<=r;x++)for(int z=-r;z<r;z++)Road(s,x*spacing,z*spacing,x*spacing,(z+1)*spacing);
            Road(s,-167,0,-r*spacing,0);s.Simulation.Trade.ConnectBorder(new Point(-167,2,0));
            // Connected infrastructure, not all-city supply flags.
            foreach(var kind in new[]{UtilityKind.Electricity,UtilityKind.Water})
            {
                for(int z=-r;z<=r;z++)Line(s,kind,-r*spacing,z*spacing,r*spacing,z*spacing);
                for(int x=-r;x<=r;x++)Line(s,kind,x*spacing,-r*spacing,x*spacing,r*spacing);
            }
            int households=(settings.population+2)/3,workers=settings.population-settings.population/3;
            int pairs=(workers+7)/8;int capacitySets=(settings.population+workers*2+159)/160+1;
            var lots=zoning.Cells().Where(c=>c.row>=1 && Math.Abs(c.pose.x)<r*spacing && Math.Abs(c.pose.z)<r*spacing).OrderBy(c=>c.key,StringComparer.Ordinal).ToArray();Shuffle(lots,settings.seed);
            int cursor=0;
            Action<LandUse> facility=use=>
            {
                for(;cursor<lots.Length;cursor++)
                {
                    var p=lots[cursor].pose;var b=CityModel.NewBuilding(use);b.x=p.x;b.z=p.z;b.yaw=p.yaw;
                    if(!zoning.CanPlace(b)||!zoning.Entrance(b).HasValue)continue;
                    s.Construction.PlaceFacility(use,b.x,b.z,b.yaw,zoning);cursor++;
                    // A water producer needs the physical pipe at its centre.
                    if(use==LandUse.Water||use==LandUse.Sewage){var access=zoning.Entrance(b).Value;double dx=access.point.x-b.x,dz=access.point.z-b.z,length=Math.Sqrt(dx*dx+dz*dz);Line(s,UtilityKind.Water,b.x,b.z,(float)(access.point.x+dx/length),(float)(access.point.z+dz/length));}return;
                }
                throw new InvalidOperationException("Scenario lacks legal facility frontage: "+use);
            };
            for(int i=0;i<capacitySets;i++){facility(LandUse.Power);facility(LandUse.Water);facility(LandUse.Sewage);}
            for(int i=0;i<Math.Max(2,(settings.population+149)/150);i++)facility(LandUse.Landfill);
            Func<LandUse,CityBuilding> building=use=>
            {
                for(;cursor<lots.Length;cursor++)
                {
                    var p=lots[cursor].pose;var b=CityModel.NewBuilding(use);b.x=p.x;b.z=p.z;b.yaw=p.yaw;
                    if(!zoning.CanPlace(b))continue;var access=zoning.Entrance(b);if(!access.HasValue)continue;
                    b.id=city.nextBuildingId;
                    if(b is ResidentialBuilding home){home.housingUnits=1;home.askingRent=0;}
                    if(b is IndustrialBuilding f){f.factory.capital=s.Simulation.Design.Production.InitialFactoryCash;f.factory.cash=f.factory.capital;}
                    if(b is CommercialBuilding shop){shop.Account.Capital=s.Simulation.Design.Retail.InitialShopCash;shop.Account.Cash=shop.Account.Capital;}
                    s.Transport.CheckNewAccess(b.id,access.Value.drivable,access.Value.distance);s.Entities.AddBuilding(b);s.Transport.RegisterAccess(b.id,access.Value.drivable,access.Value.distance);cursor++;return b;
                }
                throw new InvalidOperationException("Scenario lacks legal building frontage; requested population not silently reduced");
            };
            for(int i=0;i<pairs;i++){building(LandUse.Industrial);building(LandUse.Commercial);}
            int remaining=settings.population;
            for(int i=0;i<households;i++)
            {
                var home=building(LandUse.Residential);var family=new Household{id=city.society.nextId,home=home.id,unit=0,resident=true,savings=700,openingSavings=700,rent=0};s.Entities.AddFamily(family);
                int count=Math.Min(3,remaining);for(int k=0;k<count;k++){int id=city.society.nextCitizenId;s.Entities.AddResident(family.id,new CityResident{id=id,name="Scenario "+id,age=k==2?10:30,canWork=k<2,skill=2,location=home.id});}remaining-=count;
            }
            s.Simulation.Utilities.Invalidate();s.Simulation.Utilities.Refresh();s.Simulation.Population.UpdateStatistics();
            foreach(var b in s.Entities.Buildings)if(!city.Supply(b.id).Complete)throw new InvalidOperationException("Scenario initial supply incomplete at building "+b.id+" "+b.Use+" ("+b.x+","+b.z+") "+city.Supply(b.id).Problem+" power="+city.power+" water="+city.water+" drain="+city.SewageCapacity);
            if(s.Entities.Members.Count!=settings.population||s.Entities.Members.Any(m=>m.Person.atWork||m.Person.tripId!=0))throw new InvalidOperationException("Scenario invented activity or wrong population");
            s.Validate();return s;
        }
        public static float Flat(float x,float z)=>2;
        static void Road(CityCoreIndustrySession s,float ax,float az,float bx,float bz)
        {var plan=s.Transport.PlanConnectedRoad(new[]{new Point(ax,2,az),new Point(bx,2,bz)},snap:.1,lanes:1);if(!s.Construction.Commit(plan,out _))throw new InvalidOperationException("Scenario road commit failed");}
        static void Line(CityCoreIndustrySession s,UtilityKind kind,float ax,float az,float bx,float bz)=>s.Simulation.Utilities.Build(kind,new Point(ax,2,az),new Point(bx,2,bz));
        static void Shuffle<T>(T[] a,int seed){uint value=unchecked((uint)seed);for(int i=a.Length-1;i>0;i--){value=unchecked(value*1664525+1013904223);int j=(int)(value%(uint)(i+1));T b=a[i];a[i]=a[j];a[j]=b;}}
    }
}
