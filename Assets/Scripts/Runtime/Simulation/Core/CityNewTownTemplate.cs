using System;
using HarborCity.RoadCore;

namespace HarborCity
{
    // New-game recipe, never a saved city or an upgrade of an existing session.
    // Build privately through the same construction/finance rules as the player.
    public static class CityNewTownTemplate
    {
        public const int FacilitiesPerService=24;
        public static Point Connection=>new Point(-76,2,80);
        public static CityCoreIndustrySession Create(GameDesignData design=null)
        {
            var session=CityCoreIndustrySession.CreateEmpty(design);
            session.City.map=CityMapState.DiagnosticFlat(CityMapState.BuildHalfSize);
            session.City.utilities.roadConduits=true;
            session.Simulation.Finance.UnlimitedFunds=true;
            var zoning=session.Simulation.Growth.Zoning;zoning.SetSurface((x,z)=>2);
            BuildRoad(session,Connection,new Point(-76,2,140));
            var services=new[]{LandUse.Power,LandUse.Water,LandUse.Sewage};
            for(int row=0;row<services.Length;row++)BuildRoad(session,new Point(-152,2,100+20*row),new Point(-76,2,100+20*row));
            for(int row=0;row<services.Length;row++)
                for(int column=0;column<FacilitiesPerService/2;column++)
                    foreach(int side in new[]{-1,1})
                        session.Construction.PlaceFacility(services[row],-146+5*column,100+20*row+side*4.5f,side>0?0:180,zoning);
            foreach(float z in new[]{90f,110f})session.Construction.PlaceFacility(LandUse.Landfill,-69.5f,z,90,zoning);
            session.Simulation.Utilities.Refresh();session.Validate();
            return session;
        }
        static void BuildRoad(CityCoreIndustrySession session,Point from,Point to)
        {
            var plan=session.Transport.PlanConnectedRoad(new[]{from,to});
            session.Construction.CheckRoadPlacement(plan);
            if(!session.Construction.Commit(plan,out _))throw new InvalidOperationException("New-town infrastructure road could not be committed");
        }
    }
}
