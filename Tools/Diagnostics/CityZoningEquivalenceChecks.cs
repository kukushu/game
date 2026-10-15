using System;
using System.IO;
using System.Linq;
using HarborCity;
using HarborCity.RoadCore;

static class CityZoningEquivalenceChecks
{
    static void Dump(CityCoreIndustrySession s,BinaryWriter output)
    {
        var cells=s.Simulation.Growth.Zoning.Cells().OrderBy(c=>c.key,StringComparer.Ordinal).ToArray();output.Write(cells.Length);
        foreach(var c in cells){output.Write(c.key);output.Write(c.block);output.Write((int)c.use);output.Write(c.available);output.Write(c.occupiedBuilding);output.Write(c.pose.x);output.Write(c.pose.z);output.Write(c.pose.yaw);}
        s.Validate();
    }
    static void Main(string[] args)
    {
        using(var output=new BinaryWriter(File.Create(args[1])))
        {
            var mature=CityCoreSave.Read(File.ReadAllBytes(args[0]));mature.Simulation.Growth.Zoning.SetSurface(CityScenarioBuilder.Flat);Dump(mature,output);
            var growing=CityGrowingGridFixture.Create();Dump(growing,output);for(int i=0;i<4800;i++)growing.Advance(.05);Dump(growing,output);
            var angled=CityCoreIndustrySession.CreateEmpty();angled.Simulation.Growth.Zoning.SetSurface(CityScenarioBuilder.Flat);
            foreach(var points in new[]{new[]{new Point(-70,2,-40),new Point(0,2,20),new Point(70,2,-40)},new[]{new Point(-20,2,-70),new Point(-20,2,70)},new[]{new Point(-70,8,0),new Point(70,8,0)}})
            {var plan=angled.Transport.PlanConnectedRoad(points);if(!angled.Construction.Commit(plan,out _))throw new Exception("Angled/overpass test road rejected");Dump(angled,output);}
        }
        Console.WriteLine("PASS mature/growing/project/angled/overpass zoning snapshots saved for exact Git-reference comparison");
    }
}
