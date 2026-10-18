using System;
using System.Collections.Generic;
using System.Linq;
using HarborCity;
using HarborCity.RoadCore;

static class CityZoneSelectionChecks
{
    static int count;
    static void Check(bool value,string why){count++;if(!value)throw new Exception(why);}
    static float Flat(float x,float z)=>2;
    static CityCoreIndustrySession Read(byte[] data){var s=CityCoreSave.Read(data);s.Simulation.Growth.Zoning.SetSurface(Flat);return s;}
    static void Main()
    {
        var initial=CityCoreIndustrySession.CreateEmpty();initial.City.map=CityMapState.DiagnosticFlat(168);initial.Simulation.Growth.Zoning.SetSurface(Flat);
        var grid=new CityRoadGrid(new Point(-32,2,-32),new Point(32,2,32),32,168,Flat);
        var plan=initial.Transport.PlanConnectedRoads(grid.Strokes);initial.Construction.CheckRoadPlacement(plan);Check(initial.Construction.Commit(plan,out _),"Road fixture");
        var snapshot=CityCoreSave.Write(initial);
        foreach(var use in new[]{LandUse.Residential,LandUse.Commercial,LandUse.Industrial,LandUse.Empty})foreach(int sx in new[]{-1,1})foreach(int sz in new[]{-1,1})
        {
            var current=Read(snapshot);var reference=Read(snapshot);float x0=sx<0?20:-20,x1=-x0,z0=sz<0?18:-18,z1=-z0;
            if(use==LandUse.Empty){current.Simulation.Growth.Zoning.Paint(current.Simulation.Growth.Zoning.Cells(),LandUse.Residential);reference.Simulation.Growth.Zoning.Paint(reference.Simulation.Growth.Zoning.Cells(),LandUse.Residential);}
            var before=CityCoreSave.Write(current);var chosen=new List<ZoneCell>();current.Simulation.Growth.Zoning.SelectRectangle(x0,z0,x1,z1,chosen);
            var expected=reference.Simulation.Growth.Zoning.Cells().Where(c=>c.pose.x>=-20&&c.pose.x<=20&&c.pose.z>=-18&&c.pose.z<=18).ToArray();
            Check(chosen.Count>0&&chosen.Select(c=>c.key).SequenceEqual(expected.Select(c=>c.key)),"Direction or rectangle centre selection differs");
            Check(before.SequenceEqual(CityCoreSave.Write(current)),"Rectangle selection changed simulation");
            int changed=current.Simulation.Growth.Zoning.Paint(chosen,use),oldChanged=reference.Simulation.Growth.Zoning.Paint(expected,use);
            Check(changed==oldChanged&&CityCoreSave.Write(current).SequenceEqual(CityCoreSave.Write(reference)),"Rectangle paint differs from original individual-cell Paint");
            Check(current.Simulation.Growth.Zoning.Paint(chosen,use)==0,"Repeated paint changed same uses");
            var restore=Read(CityCoreSave.Write(current));for(int i=0;i<80;i++){current.Advance(.05);restore.Advance(.05);}
            Check(CityCoreSave.Write(current).SequenceEqual(CityCoreSave.Write(restore)),"Rectangle save continuation");
        }
        var cells=initial.Simulation.Growth.Zoning.Cells();var selected=new List<ZoneCell>();
        var edge=cells[0].pose;initial.Simulation.Growth.Zoning.SelectRectangle(edge.x,edge.z,edge.x,edge.z,selected);
        Check(selected.Any(c=>ReferenceEquals(c,cells[0])),"Inclusive rectangle edge lost cell");
        initial.Simulation.Growth.Zoning.SelectRectangle(150,150,160,160,selected);Check(selected.Count==0,"Rectangle selected nonexistent roadside cells");
        Console.WriteLine("PASS "+count+" rectangle direction, exact cell selection, original zoning equivalence, clear/repaint and save checks");
    }
}
