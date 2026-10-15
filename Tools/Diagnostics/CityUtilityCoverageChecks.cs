using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using HarborCity;
static class CityUtilityCoverageChecks
{
    static int checks;
    static void Check(bool b,string why){checks++;if(!b)throw new Exception(why);}
    static float Distance(CityBuilding b,CityUtilityNetwork net,UtilityEdge edge)=>Math.Max(0,CityRoads.Distance(new RoadNode{x=b.x,z=b.z},net.Node(edge.a),net.Node(edge.b))-Math.Min(b.width,b.depth)/2);
    static void Index()
    {
        var net=new CityUtilityNetwork();var random=new Random(7119);
        for(int i=0;i<1200;i++)
        {
            float x=(float)(random.NextDouble()*4096-2048),z=(float)(random.NextDouble()*4096-2048);
            float bx=i%4==0?x:(float)(random.NextDouble()*4096-2048),bz=i%4==1?z:(float)(random.NextDouble()*4096-2048);
            if(i%10==0){x=z=-2048;bx=bz=2048;}
            net.nodes.Add(new RoadNode{id=net.nextNode++,x=x,z=z});net.nodes.Add(new RoadNode{id=net.nextNode++,x=bx,z=bz});net.edges.Add(new UtilityEdge{id=net.nextEdge++,a=net.nextNode-2,b=net.nextNode-1});
        }
        Check(net.Valid(2048),"Index fixture invalid");var index=new CityUtilityCoverage(net);var result=new List<UtilityEdge>();
        for(int i=0;i<1500;i++)
        {
            var building=new ResidentialBuilding{x=(float)(random.NextDouble()*4096-2048),z=(float)(random.NextDouble()*4096-2048),width=(float)(1+random.NextDouble()*7.9),depth=(float)(1+random.NextDouble()*7.9)};
            if(i<300){building.x=(i%33-16)*16;building.z=(i/33-4)*16;}
            float reach=i%3==0?1:6;index.Query(building,reach,result);
            Check(result.Select(e=>e.id).SequenceEqual(result.Select(e=>e.id).Distinct().OrderBy(id=>id)),"Candidates duplicate or change source edge order");
            Check(net.edges.Where(e=>Distance(building,net,e)<=reach).Select(e=>e.id).SequenceEqual(result.Where(e=>Distance(building,net,e)<=reach).Select(e=>e.id)),"Coverage missed exact original distance predicate");
        }
        Console.WriteLine("PASS "+checks+" coverage checks: signed bounds, diagonal/axis/long lines, cell corners, footprint and exact filtered candidates");
    }
    static void Main(string[] args)
    {
        Index();var s=CityCoreSave.Read(File.ReadAllBytes(args[0]));s.Simulation.Growth.Zoning.SetSurface(CityScenarioBuilder.Flat);
        using(var writer=new StreamWriter(args[1]))for(int sample=0;sample<25;sample++)
        {
            if(sample==0){s.Simulation.Utilities.Invalidate();s.Simulation.Utilities.Refresh();}
            else if(sample%5==0)s.Simulation.Utilities.SetBudget(CityServiceKind.Electricity,sample%10==0?150:50);
            else{var kind=sample%2==0?UtilityKind.Water:UtilityKind.Electricity;var net=s.City.UtilityNetwork(kind);if(net.edges.Count>0)s.Simulation.Utilities.Remove(kind,net.edges[sample*37%net.edges.Count].id);}
            s.Validate();writer.WriteLine(sample+","+s.City.power+","+s.City.demand+","+s.City.water+","+s.City.WaterDemand+","+s.City.SewageCapacity);
            foreach(var b in s.Entities.Buildings){var supply=s.City.Supply(b.id);writer.WriteLine(b.id+","+supply.electricity+","+supply.water+","+supply.sewage+","+string.Join(";",supply.pipes.Select(p=>p.id+":"+p.load+":"+p.fresh+":"+p.drain+":"+string.Join("/",p.inactiveDrains))));}
        }
        File.WriteAllBytes(args[2],CityCoreSave.Write(s));Console.WriteLine("PASS dynamic utility removal/budget/derived supply observations; reference comparison is performed by the host");
    }
}
