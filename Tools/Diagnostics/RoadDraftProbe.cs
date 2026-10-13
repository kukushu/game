using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;
using HarborCity;

public static class RoadDraftProbe
{
    public static void Main(string[] args)
    {
        var json=new JavaScriptSerializer{MaxJsonLength=int.MaxValue};var rows=new List<string>();
        int valid=0,invalid=0,splits=0;
        for(int i=0;i<36;i++)
        {
            var city=TestCity.Empty();city.money=10000000;
            if(!city.CommitRoad(city.roads.Plan(city,city.roads.Node(CityRoads.Entrance),TestCity.P(60,1.5f),TestCity.Flat)))throw new Exception("Fixture road");
            int factory=TestCity.Build(city,-40,7,LandUse.Industrial),shop=TestCity.Build(city,45,7,LandUse.Commercial);
            city.Inventory(factory).Stock=8;city.society.settings.applicantsPerDay=0;city.traffic.dispatchTimer=-10000;
            var sim=new CityTraffic(city);var trip=sim.Dispatch(factory,shop,TripPurpose.Delivery);sim.Advance(.1f);
            string before=TestCity.Snapshot(city);
            var a=TestCity.P(-25+i%3,-20);var b=TestCity.P(25+i%5,20);
            var control=TestCity.P(-25+i%6*10,-20+i%4*8);
            RoadKind kind=i%3==0?RoadKind.FourLane:i%3==1?RoadKind.Standard:RoadKind.Ramp;
            bool elevated=i%6==0;
            if(elevated){a.y=b.y=control.y=7;}
            Func<float,float,float> height=i%7==0?(Func<float,float,float>)((x,z)=>Math.Abs(z)>10?0:1):TestCity.Flat;
            var plan=city.roads.PlanCurve(city,a,control,b,height,kind==RoadKind.Ramp,kind,elevated);
            if(before!=TestCity.Snapshot(city))throw new Exception("Live city modified on preview/failure");
            string preview=json.Serialize(new{plan.error,plan.length,plan.grade,plan.cost,plan.stroke,plan.revision,plan.points,plan.splits,plan.network});
            if(plan.Valid)
            {
                valid++;splits+=plan.splits.Count;
                if(!city.CommitRoad(plan) || !city.traffic.trips.Contains(trip) || trip.cargo!=8)throw new Exception("Atomic commit lost trip/cargo");
                city.traffic.dispatchTimer=0;city=TestCity.RoundTrip(city);sim=new CityTraffic(city);sim.Advance(4);
                city.traffic.dispatchTimer=0;if(!city.Valid())throw new Exception("Loaded city after curve invalid");
            }
            else invalid++;
            using(var sha=SHA256.Create())rows.Add(i+","+BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(preview+"\n"+TestCity.Snapshot(city)))).Replace("-",""));
        }
        if(valid==0 || invalid==0 || splits==0)throw new Exception("Fixture must cover successful curves, rejected curves and actual road splits");
        File.WriteAllLines(args[0],rows);Console.WriteLine("PASS: "+valid+" committed/loaded curves, "+invalid+" rejected previews, "+splits+" actual crossing splits; live city never modified by preview.");
    }
}
