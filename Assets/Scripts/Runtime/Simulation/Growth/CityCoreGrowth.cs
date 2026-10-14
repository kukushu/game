using System;
using System.Linq;
using HarborCity.RoadCore;
namespace HarborCity
{
    public sealed class CityCoreGrowth
    {
        readonly CityModel city;readonly CityEntities entities;readonly World world;
        public CityCoreZoning Zoning {get;}
        public CityCoreGrowth(CityModel city,CityEntities entities,World world){this.city=city;this.entities=entities;this.world=world;Zoning=new CityCoreZoning(city,entities,world);if(city.development.enabled)RefreshStatistics();}
        void RefreshStatistics(){city.population=entities.Members.Count(m=>m.Family.resident&&!m.Person.dead);city.jobs=city.society.jobEntities.Count;}
        // Called by the city at the healthy step boundary, before actor/business
        // phases. Public structure edits remain forbidden inside physics.
        internal void Step(TimeContext time)
        {
            if(!city.development.enabled||!Zoning.Ready)return;
            double due=city.development.nextGrowth;foreach(var p in city.development.projects)due=Math.Min(due,p.completeAt);if(time.Start<due)return;
            AdvanceGrowth(time);
        }
        // Keep captured LINQ predicates on the genuinely due path; the fixed-step
        // gate must not allocate a display class even when growth is disabled.
        void AdvanceGrowth(TimeContext time)
        {
            foreach(var project in city.development.projects.Where(p=>time.Start>=p.completeAt).ToArray())
            {
                var lot=project.building.ToBuilding();var cells=Zoning.Cells();if(!Zoning.CanPlace(lot,true,project.id)||project.cells.Any(key=>!cells.Any(c=>c.key==key&&c.use==lot.Use))){city.development.projects.Remove(project);city.development.revision++;continue;}
                var access=Zoning.Entrance(lot);if(!access.HasValue){city.development.projects.Remove(project);city.development.revision++;continue;}lot.id=city.nextBuildingId;
                if(lot is ResidentialBuilding home){home.housing=HousingKind.Villa;home.housingUnits=1;home.askingRent=0;}
                if(lot is IndustrialBuilding factory){factory.factory.capital=FactoryState.StartingCash;factory.factory.cash=FactoryState.StartingCash;}
                world.CheckNewAccess(lot.id,access.Value.drivable,access.Value.distance);entities.AddBuilding(lot);world.RegisterAccess(lot.id,access.Value.drivable,access.Value.distance);city.development.projects.Remove(project);city.development.revision++;
            }
            if(time.Start<city.development.nextGrowth)return;city.development.nextGrowth=(float)(time.Start+6);
            RefreshStatistics();
            city.DevelopmentDemand();var candidates=Zoning.Cells();
            foreach(var use in new[]{LandUse.Residential,LandUse.Commercial,LandUse.Industrial})
            {
                int demand=use==LandUse.Residential?city.residentialDemand:use==LandUse.Commercial?city.commercialDemand:city.industrialDemand;if(demand<=0||city.development.projects.Any(p=>p.building.type==use))continue;bool planned=false;
                for(int i=0;i<CityModel.GrowthFootprints.Length&&!planned;i++)
                {
                    var size=CityModel.GrowthFootprints[(city.development.nextProjectId-1+i)%CityModel.GrowthFootprints.Length];
                    foreach(var first in candidates.Where(c=>c.row==0&&c.available&&c.use==use))
                    {
                        var footprint=candidates.Where(c=>c.block==first.block&&c.side==first.side&&c.row<size.depth&&c.column>=first.column&&c.column<first.column+size.width).ToArray();if(footprint.Length!=size.width*size.depth||footprint.Any(c=>!c.available||c.use!=use))continue;
                        var lot=CityModel.NewBuilding(use);lot.x=footprint.Average(c=>c.pose.x);lot.z=footprint.Average(c=>c.pose.z);lot.yaw=first.pose.yaw;lot.width=CityZoningState.CellSize*size.width-.1f;lot.depth=CityZoningState.CellSize*size.depth-.1f;
                        if(footprint.Any(c=>!lot.Contains(c.pose.x,c.pose.z))||candidates.Any(c=>c.pose.Overlaps(lot)&&!footprint.Contains(c))||!Zoning.CanPlace(lot)||!Zoning.Entrance(lot).HasValue)continue;
                        if(lot is ResidentialBuilding home){home.housingUnits=1;home.askingRent=0;}
                        city.development.projects.Add(new DevelopmentProject{id=city.development.nextProjectId++,started=(float)time.Start,completeAt=(float)(time.Start+12),building=BuildingSaveData.FromBuilding(lot),cells=footprint.Select(c=>c.key).ToList()});city.development.revision++;planned=true;break;
                    }
                }
            }
        }
    }
}
