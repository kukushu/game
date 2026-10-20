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
        public const int OutletsPerSide=5;
        public static float OutletPosition(int index)
        {if(index<0||index>=OutletsPerSide)throw new ArgumentOutOfRangeException(nameof(index));return (index-2)*240;}
        public static CityCoreIndustrySession Create(GameDesignData design=null)
        {
            var session=CityCoreIndustrySession.CreateEmpty(design);
            session.City.map=CityMapState.DiagnosticFlat(CityMapState.GameplayBuildHalfSize);
            session.City.utilities.roadConduits=true;
            session.Simulation.Finance.UnlimitedFunds=true;
            var zoning=session.Simulation.Growth.Zoning;zoning.SetSurface((x,z)=>2);
            // Connected perimeter roads; facilities sit outside them, leaving
            // the central map free. Five spaced outlets on each side have gaps.
            BuildRoad(session,new Point(-660,2,660),new Point(-660,2,-660));
            BuildRoad(session,new Point(-660,2,-660),new Point(660,2,-660));
            for(int i=0;i<OutletsPerSide;i++)
            {
                float position=OutletPosition(i);
                BuildRoad(session,new Point(-660,2,position),new Point(-636,2,position));
                BuildRoad(session,new Point(position,2,-660),new Point(position,2,-636));
            }
            var services=new[]{LandUse.Power,LandUse.Water,LandUse.Sewage,LandUse.Landfill};
            for(int side=0;side<2;side++)
            {
                float along=-640;
                for(int i=0;i<192;along+=6)
                {
                    bool gap=false;for(int outlet=0;outlet<OutletsPerSide;outlet++)if(Math.Abs(along-OutletPosition(outlet))<15){gap=true;break;}
                    if(gap)continue;
                    var use=services[i%services.Length];
                    float across=use==LandUse.Landfill?-667.5f:-666;
                    session.Construction.PlaceFacility(use,side==0?across:along,side==0?along:across,side==0?270:180,zoning);i++;
                }
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
