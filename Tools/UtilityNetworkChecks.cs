using System;
using System.Linq;
using HarborCity;

public static class UtilityNetworkChecks
{
    static int checks;
    static RoadNode P(float x,float z)=>new RoadNode{x=x,z=z};
    static void Check(bool ok,string why){if(!ok)throw new Exception("Utility graph: "+why);checks++;}
    public static void Run()
    {
        var graph=new CityUtilityNetwork();var plan=graph.Plan(P(-20,0),P(20,0),out string error);
        Check(plan!=null && plan.Valid() && graph.edges.Count==0,"Utility preview is an atomic independent graph: "+error);
        var crossed=plan.Plan(P(0,-10),P(0,10),out error);
        Check(crossed!=null && crossed.Valid() && crossed.edges.Count==4 && crossed.nodes.Count==5,"Crossing pipes split both lines into a real junction");
        Check(crossed.Components().Values.Distinct().Count()==1 && plan.edges.Count==1,"Connectivity follows pipe junction and leaves preview source unchanged");
        var branch=crossed.Plan(crossed.Snap(10,.2f),P(10,15),out error);
        Check(branch!=null && branch.Valid() && branch.Components().Values.Distinct().Count()==1,"A branch snapping midway creates a real connected node");
        Check(crossed.Plan(P(-10,0),P(10,0),out error)==null && crossed.edges.Count==4,"Overlapping construction is rejected without mutation");
        Check(graph.Plan(P(0,0),P(.1f,0),out error)==null && graph.Plan(P(0,0),P(100,0),out error)==null && graph.Plan(P(float.NaN,0),P(10,0),out error)==null,"Short, outside and nonfinite segments rejected");
        var separate=branch.Plan(P(30,30),P(40,30),out error);Check(separate.Components().Values.Distinct().Count()==2,"Unconnected islands do not share imaginary supply");
        var serializer=new System.Web.Script.Serialization.JavaScriptSerializer();var copy=serializer.Deserialize<CityUtilityNetwork>(serializer.Serialize(separate));
        Check(copy.Valid() && copy.Components().Values.Distinct().Count()==2 && copy.nextNode==separate.nextNode,"Utility geometry and connectivity survive serialization");
        var edge=copy.Pick(35,30);Check(edge!=null && copy.Remove(edge.id) && copy.Valid() && copy.Components().Values.Distinct().Count()==1,"Demolition removes connectivity and ignores leftover orphan nodes");
        Console.WriteLine("PASS: "+checks+" independent utility network checks");
    }
}
