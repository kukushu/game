using System;
using System.Linq;
using HarborCity.RoadCore;

namespace HarborCity
{
    // A reproducible city built from empty through the production commands.
    // No pre-created people, jobs, stock or atWork flags. A caller supplies the
    // real map profile in Unity; headless checks explicitly use flat terrain.
    public static class CityGrowingGridFixture
    {
        public static CityCoreIndustrySession Create(Func<float,float,float> surface=null,GameDesignData design=null,bool commerce=true)
        {
            surface=surface??((x,z)=>2);
            var session=CityCoreIndustrySession.CreateEmpty(design);
            var zoning=session.Simulation.Growth.Zoning;zoning.SetSurface(surface);
            Build(session,surface,-167,0,-55,0,2);
            Build(session,surface,-135,-25,-55,-25,1);
            Build(session,surface,-135,25,-55,25,1);
            foreach(float x in new[]{-135f,-95f,-55f})Build(session,surface,x,-25,x,25,1);
            session.Simulation.Trade.ConnectBorder(new Point(-167,surface(-167,0),0));
            foreach(var use in new[]{LandUse.Power,LandUse.Water,LandUse.Sewage})
            {
                bool placed=false;
                foreach(var cell in zoning.Cells().Where(c=>c.row==1&&c.available).OrderBy(c=>Math.Abs(c.pose.x+95)+Math.Abs(c.pose.z)).ToArray())
                {
                    var lot=CityModel.NewBuilding(use);lot.x=cell.pose.x;lot.z=cell.pose.z;lot.yaw=cell.pose.yaw;
                    if(!zoning.CanPlace(lot)||!zoning.Entrance(lot).HasValue)continue;
                    session.Construction.PlaceFacility(use,lot.x,lot.z,lot.yaw,zoning);placed=true;break;
                }
                if(!placed)throw new InvalidOperationException("Grid cannot place service "+use);
            }
            foreach(var kind in new[]{UtilityKind.Electricity,UtilityKind.Water})
            {
                Line(session,kind,-165,0,-50,0);Line(session,kind,-140,-25,-50,-25);Line(session,kind,-140,25,-50,25);
                foreach(float x in new[]{-135f,-95f,-55f})Line(session,kind,x,-30,x,30);
            }
            foreach(var facility in session.City.buildings.Where(b=>b.Use==LandUse.Water||b.Use==LandUse.Sewage))
            {
                // Services were chosen near the centre street; join that actual
                // pipe component, not a hidden all-city supply flag.
                if(Math.Abs(facility.z)>.08)Line(session,UtilityKind.Water,facility.x,0,facility.x,facility.z);
            }
            foreach(var use in new[]{LandUse.Residential,LandUse.Commercial,LandUse.Industrial})
            {
                var selection=zoning.Cells().Where(c=>c.available&&(c.pose.z>3?(c.pose.x>=-95?LandUse.Residential:LandUse.Empty):c.pose.z<-3?(c.pose.x<-95?(commerce&&c.pose.x>=-135?LandUse.Commercial:LandUse.Empty):LandUse.Industrial):LandUse.Empty)==use).ToArray();
                zoning.Paint(selection,use);
            }
            session.Simulation.Utilities.Refresh();session.Validate();return session;
        }
        public static int PlaceWasteFacility(CityCoreIndustrySession session)
        {
            var zoning=session.Simulation.Growth.Zoning;
            foreach(var cell in zoning.Cells().Where(c=>c.available).OrderBy(c=>Math.Abs(c.pose.x+95)+Math.Abs(c.pose.z)).ToArray())
            {
                var lot=CityModel.NewBuilding(LandUse.Landfill);lot.x=cell.pose.x;lot.z=cell.pose.z;lot.yaw=cell.pose.yaw;
                if(!zoning.CanPlace(lot)||!zoning.Entrance(lot).HasValue)continue;
                int id=session.Construction.PlaceFacility(LandUse.Landfill,lot.x,lot.z,lot.yaw,zoning);session.Simulation.Utilities.Refresh();
                if(!session.City.Supply(id).Complete)throw new InvalidOperationException("Waste fixture facility lacks actual utilities");return id;
            }
            throw new InvalidOperationException("No legal waste facility lot in fixture");
        }
        static void Build(CityCoreIndustrySession s,Func<float,float,float> height,float ax,float az,float bx,float bz,int lanes)
        {
            var points=new Point[9];for(int i=0;i<points.Length;i++){float t=i/8f,x=ax+(bx-ax)*t,z=az+(bz-az)*t;points[i]=new Point(x,height(x,z),z);}
            var plan=s.Transport.PlanConnectedRoad(points,lanes:lanes);
            if(!s.Construction.Commit(plan,out _))throw new InvalidOperationException("Grid road plan became stale");
        }
        static void Line(CityCoreIndustrySession s,UtilityKind kind,float ax,float az,float bx,float bz)
        =>s.Simulation.Utilities.Build(kind,new Point(ax,0,az),new Point(bx,0,bz));
    }
}
