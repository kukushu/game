using System;
using HarborCity.RoadCore;
namespace HarborCity
{
    // Utility networks are separate physical infrastructure, not a second road
    // traffic graph. Reuse the existing component/capacity rules and facts.
    public sealed class CityCoreUtilities
    {
        readonly CityModel city;readonly CityEntities entities;readonly World world;
        int entityVersion=-1,roadVersion=-1,powerVersion=-1,waterVersion=-1;
        CityUtilityNetwork power,water;
        bool dirty=true;
        public CityCoreUtilities(CityModel city,CityEntities entities,World world){this.city=city;this.entities=entities;this.world=world;}
        public void Invalidate(){dirty=true;}
        public void Refresh()
        {
            if(!city.development.enabled)return;
            if(!dirty&&entityVersion==entities.Revision&&roadVersion==world.NetworkGeneration&&ReferenceEquals(power,city.utilities.electricity)&&ReferenceEquals(water,city.utilities.water)&&powerVersion==power.revision&&waterVersion==water.revision)return;
            city.RecalculateUtilities();dirty=false;entityVersion=entities.Revision;roadVersion=world.NetworkGeneration;power=city.utilities.electricity;water=city.utilities.water;powerVersion=power.revision;waterVersion=water.revision;
        }
        public int Build(UtilityKind kind,Point from,Point to)
        {
            entities.StructuralBoundary();var network=city.UtilityNetwork(kind);var candidate=network.Plan(new RoadNode{x=(float)from.x,z=(float)from.z},new RoadNode{x=(float)to.x,z=(float)to.z},out string error);if(candidate==null)throw new InvalidOperationException(error);
            double quoted=Math.Ceiling(Point.Distance(new Point(from.x,0,from.z),new Point(to.x,0,to.z))*(kind==UtilityKind.Electricity?15:10));if(quoted>int.MaxValue||city.money<quoted)throw new InvalidOperationException("管线建设资金不足");
            if(kind==UtilityKind.Electricity)city.utilities.electricity=candidate;else city.utilities.water=candidate;int paid=(int)quoted;city.money-=paid;Refresh();return paid;
        }
        public bool Remove(UtilityKind kind,int edge)
        {entities.StructuralBoundary();if(city.money<40||!city.UtilityNetwork(kind).Remove(edge))return false;city.money-=40;Refresh();return true;}
    }
}
