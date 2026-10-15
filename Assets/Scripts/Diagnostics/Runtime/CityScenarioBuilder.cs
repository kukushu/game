using System;
using System.Linq;
using System.Collections.Generic;
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
        public int householdsPerResidentialBuilding=1,lanesPerDirection=1;
        public bool seedRoadNetwork;
        public float diagnosticBuildHalfSize=CityMapState.BuildHalfSize;
        public void Validate()
        {
            if(lanesPerDirection<1||lanesPerDirection>2||householdsPerResidentialBuilding<1||householdsPerResidentialBuilding>8||schema!=1||population<3||population>100000||gridRadius<2||gridRadius>24||streetSpacing<24||streetSpacing*gridRadius>diagnosticBuildHalfSize-24||!CityMapState.DiagnosticFlat(diagnosticBuildHalfSize).Valid()||days<1||days>120||warmupDays<0||warmupDays>=days||openingPublicGrant<0||layout!="grid"||mode!="mature")throw new ArgumentException("Unsupported scenario settings; mature grid and residential density must fit the current build area");
        }
    }
    public static class CityScenarioBuilder
    {
        public static CityCoreIndustrySession Create(CityScenarioSettings settings,GameDesignData design=null,Action<string,CityCoreIndustrySession> progress=null)
        {
            settings.Validate();var initialRoads=settings.seedRoadNetwork?GridNetwork(settings):null;var s=CityCoreIndustrySession.CreateEmpty(design,initialRoads);var city=s.City;if(settings.diagnosticBuildHalfSize!=CityMapState.BuildHalfSize)city.map=CityMapState.DiagnosticFlat(settings.diagnosticBuildHalfSize);var zoning=s.Simulation.Growth.Zoning;zoning.SetSurface(Flat);
            progress?.Invoke("empty",s);
            if(settings.openingPublicGrant>0)s.Simulation.Finance.GrantDevelopmentFunds(settings.openingPublicGrant);
            int r=settings.gridRadius,spacing=settings.streetSpacing;int roadsBuilt=0;
            float border=1-settings.diagnosticBuildHalfSize;
            if(initialRoads==null)
            {
            for(int z=-r;z<=r;z++)for(int x=-r;x<r;x++){Road(s,x*spacing,z*spacing,(x+1)*spacing,z*spacing,settings.lanesPerDirection);if(++roadsBuilt%25==0)progress?.Invoke("roads",s);}
            for(int x=-r;x<=r;x++)for(int z=-r;z<r;z++){Road(s,x*spacing,z*spacing,x*spacing,(z+1)*spacing,settings.lanesPerDirection);if(++roadsBuilt%25==0)progress?.Invoke("roads",s);}
            Road(s,border,0,-r*spacing,0,settings.lanesPerDirection);
            }
            else
            {
                // Mature-fixture authoring only: the exact complete grid is validated
                // once by the normal Network. It is not an interactive build benchmark.
                int price=0;foreach(var road in initialRoads.roads)price=checked(price+(int)Math.Ceiling(new Geometry(road.points).Length*road.lanes*(40.0/3)-1e-6));
                s.Simulation.Finance.CheckSpend(price);s.Simulation.Finance.SpendApproved(price);
            }
            s.Simulation.Trade.ConnectBorder(new Point(border,2,0));
            progress?.Invoke("road-network-complete",s);
            // Connected infrastructure, not all-city supply flags.
            foreach(var kind in new[]{UtilityKind.Electricity,UtilityKind.Water})
            {
                for(int z=-r;z<=r;z++)Line(s,kind,-r*spacing,z*spacing,r*spacing,z*spacing);
                for(int x=-r;x<=r;x++)Line(s,kind,x*spacing,-r*spacing,x*spacing,r*spacing);
            }
            // Wider streets need actual curb-side infrastructure to reach the back zoning rows.
            // Keep the original centre backbone, connect these parallels at every cross street.
            if(settings.lanesPerDirection>1)
                foreach(var kind in new[]{UtilityKind.Electricity,UtilityKind.Water})
                foreach(float offset in new[]{-2.2f,2.2f})
                {
                    for(int z=-r;z<=r;z++)Line(s,kind,-r*spacing,z*spacing+offset,r*spacing,z*spacing+offset);
                    for(int x=-r;x<=r;x++)Line(s,kind,x*spacing+offset,-r*spacing,x*spacing+offset,r*spacing);
                }
            progress?.Invoke("utility-backbones-complete",s);
            int households=(settings.population+2)/3,workers=settings.population-settings.population/3;
            int jobsPerPair=s.Simulation.Design.Employment.SlotsPerLevel*2;
            int pairs=(workers+jobsPerPair-1)/jobsPerPair;
            int wastePlants=Math.Max(2,(settings.population+149)/150);
            // Each connected water/sewage facility also consumes electricity.
            // Preserve the original 500 fixture while sizing larger fixtures for actual loads.
            int capacitySets=Math.Max((settings.population+workers*2+159)/160+1,(settings.population+workers*2+wastePlants*2+155)/156);
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
                    if(use==LandUse.Water||use==LandUse.Sewage)ConnectPipe(s,b,zoning.Entrance(b).Value.point,settings.lanesPerDirection==1);
                    if(s.Entities.Buildings.Count%25==0)progress?.Invoke("facilities",s);return;
                }
                throw new InvalidOperationException("Scenario lacks legal facility frontage: "+use);
            };
            for(int i=0;i<capacitySets;i++){facility(LandUse.Power);facility(LandUse.Water);facility(LandUse.Sewage);}
            for(int i=0;i<wastePlants;i++)facility(LandUse.Landfill);
            Func<LandUse,CityBuilding> building=use=>
            {
                for(;cursor<lots.Length;cursor++)
                {
                    var p=lots[cursor].pose;var b=CityModel.NewBuilding(use);b.x=p.x;b.z=p.z;b.yaw=p.yaw;
                    if(b is ResidentialBuilding apartment&&settings.householdsPerResidentialBuilding>1){apartment.housing=HousingKind.Apartment;apartment.width=2.9f;apartment.depth=5.9f;}
                    if(!zoning.CanPlace(b))continue;var access=zoning.Entrance(b);if(!access.HasValue)continue;
                    b.id=city.nextBuildingId;
                    if(b is ResidentialBuilding home){home.housingUnits=settings.householdsPerResidentialBuilding;home.askingRent=0;}
                    if(b is IndustrialBuilding f){f.factory.capital=s.Simulation.Design.Production.InitialFactoryCash;f.factory.cash=f.factory.capital;}
                    if(b is CommercialBuilding shop){shop.Account.Capital=s.Simulation.Design.Retail.InitialShopCash;shop.Account.Cash=shop.Account.Capital;}
                    s.Transport.CheckNewAccess(b.id,access.Value.drivable,access.Value.distance);s.Entities.AddBuilding(b);s.Transport.RegisterAccess(b.id,access.Value.drivable,access.Value.distance);cursor++;if(s.Entities.Buildings.Count%25==0)progress?.Invoke("business-and-housing",s);return b;
                }
                throw new InvalidOperationException("Scenario lacks legal building frontage; requested population not silently reduced");
            };
            for(int i=0;i<pairs;i++){building(LandUse.Industrial);building(LandUse.Commercial);}
            int remaining=settings.population;CityBuilding residence=null;
            for(int i=0;i<households;i++)
            {
                if(i%settings.householdsPerResidentialBuilding==0)residence=building(LandUse.Residential);
                var home=residence;var family=new Household{id=city.society.nextId,home=home.id,unit=i%settings.householdsPerResidentialBuilding,resident=true,savings=700,openingSavings=700,rent=0};s.Entities.AddFamily(family);
                int count=Math.Min(3,remaining);for(int k=0;k<count;k++){int id=city.society.nextCitizenId;s.Entities.AddResident(family.id,new CityResident{id=id,name="Scenario "+id,age=k==2?10:30,canWork=k<2,skill=2,location=home.id});}remaining-=count;if(i%100==0)progress?.Invoke("households",s);
            }
            s.Simulation.Utilities.Invalidate();s.Simulation.Utilities.Refresh();s.Simulation.Population.UpdateStatistics();
            foreach(var b in s.Entities.Buildings)if(!city.Supply(b.id).Complete)throw new InvalidOperationException("Scenario initial supply incomplete at building "+b.id+" "+b.Use+" ("+b.x+","+b.z+") "+city.Supply(b.id).Problem+" power="+city.power+" water="+city.water+" drain="+city.SewageCapacity);
            if(s.Entities.Members.Count!=settings.population||s.Entities.Members.Any(m=>m.Person.atWork||m.Person.tripId!=0))throw new InvalidOperationException("Scenario invented activity or wrong population");
            progress?.Invoke("validate",s);s.Validate();progress?.Invoke("complete",s);return s;
        }
        // Fixture-only pipe planning: reuse the already connected backbone/branch instead
        // of drawing an overlapping second segment. The normal planner remains authority.
        static void ConnectPipe(CityCoreIndustrySession s,CityBuilding b,Point access,bool preserveOriginal)
        {
            var net=s.City.utilities.water;var from=new RoadNode{x=b.x,z=b.z};
            double dx=access.x-b.x,dz=access.z-b.z,length=Math.Sqrt(dx*dx+dz*dz);
            var original=new RoadNode{x=(float)(access.x+dx/length),z=(float)(access.z+dz/length)};
            if(preserveOriginal&&net.Plan(from,original,out _,s.City.map.buildHalfSize)!=null){Line(s,UtilityKind.Water,b.x,b.z,original.x,original.z);return;}
            RoadNode closest=null;float distance=float.MaxValue;
            foreach(var edge in net.edges){var a=net.Node(edge.a);var z=net.Node(edge.b);var point=CityRoads.Lerp(a,z,CityRoads.Projection(from,a,z));float d=CityRoads.Length(from,point);if(d<distance){distance=d;closest=point;}}
            if(closest==null)throw new InvalidOperationException("Scenario has no existing pipe to connect");
            if(distance<.4f){s.Simulation.Utilities.Invalidate();s.Simulation.Utilities.Refresh();if(s.City.Supply(b.id).Complete)return;throw new InvalidOperationException("Existing pipe near facility does not provide actual complete supply");}
            Line(s,UtilityKind.Water,b.x,b.z,closest.x,closest.z);
        }
        static NetworkData GridNetwork(CityScenarioSettings settings)
        {
            var data=new NetworkData();var nodes=new Dictionary<(int,int),int>();int r=settings.gridRadius,spacing=settings.streetSpacing;
            int Junction(int x,int z){if(nodes.TryGetValue((x,z),out int id))return id;id=data.nextJunction++;nodes.Add((x,z),id);data.junctions.Add(new Junction{id=id,position=new Point(x,2,z)});return id;}
            void Add(int ax,int az,int bx,int bz){data.roads.Add(new PhysicalRoad{id=data.nextRoad++,from=Junction(ax,az),to=Junction(bx,bz),lanes=settings.lanesPerDirection,points=new[]{new Point(ax,2,az),new Point(bx,2,bz)}});}
            // Every geometric intersection is an explicit shared grid vertex.
            for(int z=-r;z<=r;z++)for(int x=-r;x<r;x++)Add(x*spacing,z*spacing,(x+1)*spacing,z*spacing);
            for(int x=-r;x<=r;x++)for(int z=-r;z<r;z++)Add(x*spacing,z*spacing,x*spacing,(z+1)*spacing);
            float border=1-settings.diagnosticBuildHalfSize;if(border!=(int)border)throw new ArgumentException("Seeded grid boundary must be integral");Add((int)border,0,-r*spacing,0);return data;
        }
        public static float Flat(float x,float z)=>2;
        static void Road(CityCoreIndustrySession s,float ax,float az,float bx,float bz,int lanes=1)
        {var plan=s.Transport.PlanConnectedRoad(new[]{new Point(ax,2,az),new Point(bx,2,bz)},snap:.1,lanes:lanes);if(!s.Construction.Commit(plan,out _))throw new InvalidOperationException("Scenario road commit failed");}
        static void Line(CityCoreIndustrySession s,UtilityKind kind,float ax,float az,float bx,float bz)=>s.Simulation.Utilities.Build(kind,new Point(ax,2,az),new Point(bx,2,bz));
        static void Shuffle<T>(T[] a,int seed){uint value=unchecked((uint)seed);for(int i=a.Length-1;i>0;i--){value=unchecked(value*1664525+1013904223);int j=(int)(value%(uint)(i+1));T b=a[i];a[i]=a[j];a[j]=b;}}
    }
}
