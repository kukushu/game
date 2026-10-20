using System;
using HarborCity.RoadCore;

namespace HarborCity
{
    // New-game recipe, never a saved city or an upgrade of an existing session.
    // Build privately through the same construction/finance rules as the player.
    public static class CityNewTownTemplate
    {
        public const int FacilitiesPerService=24;
        public const int GarbageFacilities=8;
        public static Point Connection=>new Point(-132,2,0);
        public static CityCoreIndustrySession Create(GameDesignData design=null)
        {
            var session=CityCoreIndustrySession.CreateEmpty(design);
            session.City.map=CityMapState.DiagnosticFlat(CityMapState.GameplayBuildHalfSize);
            session.City.utilities.roadConduits=true;
            session.Simulation.Finance.UnlimitedFunds=true;
            var zoning=session.Simulation.Growth.Zoning;zoning.SetSurface((x,z)=>2);
            BuildRoad(session,new Point(-156,2,-167),new Point(-156,2,164));
            BuildRoad(session,new Point(-156,2,0),Connection);
            var services=new[]{LandUse.Power,LandUse.Water,LandUse.Sewage};
            for(int i=0;i<services.Length*FacilitiesPerService;i++)
            {
                // One narrow column. Reserve a real gap for the central outlet,
                // so joining the city never requires building through a facility.
                float z=156-3.3f*i-(i>=44?24:0);
                session.Construction.PlaceFacility(services[i/FacilitiesPerService],-150,z,90,zoning);
            }
            for(int i=0;i<GarbageFacilities;i++)session.Construction.PlaceFacility(LandUse.Landfill,-148.5f,-113-7*i,90,zoning);
            session.Simulation.Utilities.Refresh();session.Validate();
            return session;
        }
        static void BuildRoad(CityCoreIndustrySession session,Point from,Point to)
        {
            var plan=session.Transport.PlanConnectedRoad(new[]{from,to},lanes:2);
            session.Construction.CheckRoadPlacement(plan);
            if(!session.Construction.Commit(plan,out _))throw new InvalidOperationException("New-town infrastructure road could not be committed");
        }
    }
}
