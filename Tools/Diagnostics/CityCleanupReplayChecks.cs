using System;
using System.IO;
using System.Linq;
using HarborCity;
using HarborCity.RoadCore;

// Same host/input for before/after cleanup. All checkpoints are complete binary
// city saves; inspection and cold restore run outside the committed fixed steps.
static class CityCleanupReplayChecks
{
    static string output;
    static void Save(CityCoreIndustrySession s,string name){s.Validate();File.WriteAllBytes(Path.Combine(output,name+".hcity"),CityCoreSave.Write(s));Console.WriteLine(name+": residents="+s.Entities.Members.Count+" vehicles="+s.Transport.ActiveCount+" jobs="+s.City.society.jobEntities.Count+" clock="+s.Transport.Clock);}
    static void Road(CityCoreIndustrySession s,params Point[] points){var p=s.Transport.PlanConnectedRoad(points);s.Construction.CheckRoadPlacement(p);if(!s.Construction.Commit(p,out _))throw new Exception("Replay road commit failed");}
    static void Main(string[] args)
    {
        output=args[1];Directory.CreateDirectory(output);
        var s=CityCoreSave.Read(File.ReadAllBytes(args[0]));s.Simulation.Growth.Zoning.SetSurface((x,z)=>2);Save(s,"scale-initial");
        for(int i=1;i<=240;i++){s.Advance(.05);if(i%80==0)Save(s,"scale-"+i);}
        var restored=CityCoreSave.Read(CityCoreSave.Write(s));restored.Simulation.Growth.Zoning.SetSurface((x,z)=>2);
        for(int i=0;i<40;i++){s.Advance(.05);restored.Advance(.05);}if(!CityCoreSave.Write(s).SequenceEqual(CityCoreSave.Write(restored)))throw new Exception("Scale cold continuation differs");Save(s,"scale-restored-continued");
        s=CityNewTownTemplate.Create();Save(s,"town-initial");
        Road(s,new Point(-40,2,-32),new Point(40,2,-32));Road(s,new Point(40,2,-32),new Point(40,2,32));Road(s,new Point(40,2,32),new Point(-40,2,32));Road(s,new Point(-40,2,32),new Point(-40,2,-32));Road(s,CityNewTownTemplate.Connection,new Point(-40,2,0));
        var zoning=s.Simulation.Growth.Zoning;var cells=zoning.Cells().Where(c=>c.available&&Math.Abs(c.pose.x)<48&&Math.Abs(c.pose.z)<42).ToArray();
        foreach(var use in new[]{LandUse.Residential,LandUse.Commercial,LandUse.Industrial})zoning.Paint(cells.Where(c=>(c.pose.x<0?LandUse.Residential:c.pose.z<0?LandUse.Industrial:LandUse.Commercial)==use),use);
        Save(s,"town-zoned");for(int i=0;i<2600;i++)s.Advance(.05);Save(s,"town-grown");
        if(s.Entities.Members.Count==0||!s.Entities.Buildings.Any(b=>b.Use==LandUse.Residential)||!s.Entities.Buildings.Any(b=>b.Use==LandUse.Industrial)||!s.Entities.Buildings.Any(b=>b.Use==LandUse.Commercial))throw new Exception("Normal construction/growth/migration did not run");
        restored=CityCoreSave.Read(CityCoreSave.Write(s));restored.Simulation.Growth.Zoning.SetSurface((x,z)=>2);restored.Simulation.Finance.UnlimitedFunds=s.Simulation.Finance.UnlimitedFunds;
        for(int i=0;i<80;i++){s.Advance(.05);restored.Advance(.05);}if(!CityCoreSave.Write(s).SequenceEqual(CityCoreSave.Write(restored)))throw new Exception("New-town cold continuation differs");Save(s,"town-restored-continued");
        Console.WriteLine("PASS complete scale/new-town checkpoints, fixed steps, construction, zoning, growth, migration and cold restore");
    }
}
