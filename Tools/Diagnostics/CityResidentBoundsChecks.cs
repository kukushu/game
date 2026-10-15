using System;
using System.IO;
using System.Linq;
using HarborCity;
using HarborCity.RoadCore;

static class CityResidentBoundsChecks
{
    static int checks;
    static void Check(bool ok,string message){checks++;if(!ok)throw new Exception(message);}
    static void Bounds(World w,int[] ids)
    {
        var bounds=new ResidentRouteBounds(w);
        foreach(int a in ids)foreach(int b in ids)
        {
            double exact=w.EstimateResidentSeconds(a,b),lower=bounds.Seconds(a,b);
            Check(lower>=0&&!double.IsNaN(lower),"Invalid lower bound");
            Check(double.IsInfinity(exact)||lower<=exact,"Pruning could discard a better job "+a+" -> "+b);
        }
    }
    static void Main(string[] args)
    {
        var w=new World(new WorldData{externalBusiness=true},new SimulationClock(),Enumerable.Range(1,20));
        var points=new[]{new Point(0,0,0),new Point(60,5,0),new Point(60,0,60),new Point(0,-5,60)};
        var nodes=points.Select(p=>w.AddJunction(p)).ToArray();int endpoint=1;
        for(int i=0;i<4;i++)
        {
            int j=(i+1)%4;var mid=Point.Lerp(points[i],points[j],.5);mid=new Point(mid.x+4,mid.y+2,mid.z+4);
            int road=w.AddRoad(nodes[i],nodes[j],new[]{points[i],mid,points[j]},oneWay:i==2,speed:6+i*4,lanes:2);
            foreach(var lane in w.Network.Drivables.Where(d=>d.RoadId==road&&!d.IsConnection).ToArray())
            {w.RegisterAccess(endpoint++,lane.Id,(lane.StartDistance+lane.EndDistance)/2);}
        }
        Bounds(w,Enumerable.Range(1,endpoint-1).ToArray());Check(new ResidentRouteBounds(w).Seconds(-1,1)==0,"Invalid access must defer to exact eligibility");
        var expired=new ResidentRouteBounds(w);int e=w.AddJunction(new Point(120,0,0));w.AddRoad(nodes[1],e,new[]{points[1],new Point(120,0,0)});
        bool rejected=false;try{expired.Seconds(1,2);}catch(InvalidOperationException){rejected=true;}Check(rejected,"Query scope survived structural edit");Bounds(w,Enumerable.Range(1,endpoint-1).ToArray());
        var city=CityCoreSave.Read(File.ReadAllBytes(args[0]));city.Simulation.Growth.Zoning.SetSurface(CityScenarioBuilder.Flat);
        // Actual first-step jobs and all other persisted facts are compared against
        // the unmodified baseline executable, not a second runtime job implementation.
        city.Advance(.05);city.Validate();File.WriteAllBytes(args[1],CityCoreSave.Write(city));
        Console.WriteLine("PASS "+checks+" admissible-bound checks across mixed speeds, curves, heights, one-way and parallel lanes; first-step city saved");
    }
}
