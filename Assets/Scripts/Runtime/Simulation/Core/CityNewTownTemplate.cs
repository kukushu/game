using System;
using HarborCity.RoadCore;

namespace HarborCity
{
    // New-game recipe, never a saved city or an upgrade of an existing session.
    // Build privately through the same construction/finance rules as the player.
    public static class CityNewTownTemplate
    {
        public const int FacilitiesPerService=192;
        public const int CentralGarbageFacilities=192;
        public const int GarbageFacilities=FacilitiesPerService+CentralGarbageFacilities;
        public const int TotalFacilities=FacilitiesPerService*3+GarbageFacilities;
        public static Point Connection=>new Point(-636,2,0);
        public static Point BottomConnection=>new Point(0,2,-636);
        public const int OutletsPerSide=5;
        // West, south, east, north. The centre outlets join the cross directly.
        public static Point Outlet(int side,int index)
        {
            float p=OutletPosition(index);
            switch(side){case 0:return new Point(-636,2,p);case 1:return new Point(p,2,-636);case 2:return new Point(636,2,p);case 3:return new Point(p,2,636);default:throw new ArgumentOutOfRangeException(nameof(side));}
        }
        static bool OutletGap(float along)
        {for(int i=0;i<OutletsPerSide;i++)if(Math.Abs(along-OutletPosition(i))<15)return true;return false;}
        public static float OutletPosition(int index)
        {if(index<0||index>=OutletsPerSide)throw new ArgumentOutOfRangeException(nameof(index));return (index-2)*240;}
        public static CityCoreIndustrySession Create(GameDesignData design=null)
        {
            var session=CityCoreIndustrySession.CreateEmpty(design);
            session.City.map=CityMapState.DiagnosticFlat(CityMapState.GameplayBuildHalfSize);
            session.City.utilities.roadConduits=true;
            session.Simulation.Finance.UnlimitedFunds=true;
            var zoning=session.Simulation.Growth.Zoning;zoning.SetSurface((x,z)=>2);
            // Finish topology before registering any facility entrances.
            BuildRoad(session,new Point(-660,2,660),new Point(-660,2,-660));
            BuildRoad(session,new Point(-660,2,-660),new Point(660,2,-660));
            BuildRoad(session,new Point(660,2,-660),new Point(660,2,660));
            BuildRoad(session,new Point(660,2,660),new Point(-660,2,660));
            BuildRoad(session,new Point(-660,2,0),new Point(660,2,0));
            BuildRoad(session,new Point(0,2,-660),new Point(0,2,660));
            for(int side=0;side<4;side++)for(int i=0;i<OutletsPerSide;i++)
            {
                float position=OutletPosition(i);if(position==0)continue;
                var end=Outlet(side,i);
                var start=side==0?new Point(-660,2,position):side==1?new Point(position,2,-660):side==2?new Point(660,2,position):new Point(position,2,660);
                BuildRoad(session,start,end);
            }
            var services=new[]{LandUse.Power,LandUse.Water,LandUse.Sewage,LandUse.Landfill};
            for(int side=0;side<4;side++)
            {
                float along=-640;
                for(int i=0;i<192;along+=6)
                {
                    if(OutletGap(along))continue;
                    var use=services[i%services.Length];
                    float across=use==LandUse.Landfill?667.5f:666;
                    float x=side==0?-across:side==2?across:along,z=side==1?-across:side==3?across:along;
                    session.Construction.PlaceFacility(use,x,z,side==0?270:side==1?180:side==2?90:0,zoning);i++;
                }
            }
            // Garbage-only rows on both sides of all four central arms. Leave
            // the centre and the +/-240, +/-480 connection corridors clear.
            for(int along=24;along<=624;along+=24)
            {
                if(OutletGap(along))continue;
                foreach(int sign in new[]{-1,1})foreach(int bank in new[]{-1,1})
                {
                    session.Construction.PlaceFacility(LandUse.Landfill,sign*along,bank*7.5f,bank<0?180:0,zoning);
                    session.Construction.PlaceFacility(LandUse.Landfill,bank*7.5f,sign*along,bank<0?270:90,zoning);
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
