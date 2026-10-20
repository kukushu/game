using System;
using System.Collections.Generic;
namespace HarborCity
{
    public enum LandUse { Empty, Road, Residential, Commercial, Industrial, Power, Water, Park, Bulldoze, Sewage, Landfill, Clinic, ElementarySchool, FireHouse, PoliceStation, HighSchool, University, Cemetery }
    [Serializable] public sealed partial class CityModel
    {
        public const float BuildHalfSize=CityMapState.BuildHalfSize, RoadHalfSize=BuildHalfSize-1.5f;
        [NonSerialized] internal CityEntities entities;
        public CityMapState map=new CityMapState();
        public int money=65000, day=1;
        public CityTradeState trade {get;internal set;}=new CityTradeState();
        public CityConsumptionState consumption {get;internal set;}=new CityConsumptionState();
        [NonSerialized] public int population,jobs,income,upkeep,power,water,demand,happiness=70;
        public static int Cost(LandUse use)
        {
            switch (use)
            {
                case LandUse.Road: return 100;
                case LandUse.Residential: case LandUse.Commercial: case LandUse.Industrial: return 60;
                case LandUse.Power: return 3500;
                case LandUse.Water: return 2500;
                case LandUse.Park: return 500;
                case LandUse.Sewage: return 2500;
                case LandUse.Landfill:return 4000;
                case LandUse.Clinic:return 10000;
                case LandUse.ElementarySchool:return 10000;
                case LandUse.HighSchool:return 16000;
                case LandUse.University:return 24000;
                case LandUse.Cemetery:return 4000;
                case LandUse.FireHouse:return 12000;
                case LandUse.PoliceStation:return 12000;
                case LandUse.Bulldoze: return 40;
                default: return 0;
            }
        }
    }
}
