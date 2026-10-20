using System;
using HarborCity.RoadCore;

namespace HarborCity
{
    // New-game recipe, never a saved city or an upgrade of an existing session.
    // Build privately through the same construction/finance rules as the player.
    public static class CityNewTownTemplate
    {
        public const int FacilitiesPerService=96;
        public const int GarbageFacilities=FacilitiesPerService;
        public const int TotalFacilities=FacilitiesPerService*3+GarbageFacilities;
        public static Point Connection=>new Point(-636,2,0);
        public static Point BottomConnection=>new Point(0,2,-636);
        public static CityCoreIndustrySession Create(GameDesignData design=null)
        {
            var session=CityCoreIndustrySession.CreateEmpty(design);
            session.City.map=CityMapState.DiagnosticFlat(CityMapState.GameplayBuildHalfSize);
            session.City.utilities.roadConduits=true;
            session.Simulation.Finance.UnlimitedFunds=true;
            var zoning=session.Simulation.Growth.Zoning;zoning.SetSurface((x,z)=>2);
            // Connected perimeter roads; facilities sit outside them, leaving
            // the central map free. Both midpoints have a reserved outlet gap.
            BuildRoad(session,new Point(-660,2,660),new Point(-660,2,-660));
            BuildRoad(session,new Point(-660,2,-660),new Point(660,2,-660));
            BuildRoad(session,new Point(-660,2,0),Connection);
            BuildRoad(session,new Point(0,2,-660),BottomConnection);
            var services=new[]{LandUse.Power,LandUse.Water,LandUse.Sewage,LandUse.Landfill};
            for(int side=0;side<2;side++)for(int i=0;i<192;i++)
            {
                float along=-640+6.6f*i+(i>=96?24:0);
                var use=services[i%services.Length];
                float across=use==LandUse.Landfill?-667.5f:-666;
                session.Construction.PlaceFacility(use,side==0?across:along,side==0?along:across,side==0?270:180,zoning);
            }
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
