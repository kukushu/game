using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Web.Script.Serialization;
using HarborCity;
using HarborCity.RoadCore;

// Exact frozen oracle, including near-threshold and degenerate segment cases.
static class CityConnectionConflictChecks
{
    static long checks;static readonly List<object> results=new List<object>();
    static void Check(bool ok,string message){checks++;if(!ok)throw new Exception(message);}
    static void NetworkCheck(Network n,string label)
    {
        var frozen=new FrozenConnectionConflicts(n);var actual=new ConnectionConflicts(n);var reversed=new ConnectionConflicts(n);long pairs=0;
        foreach(var group in n.Drivables.Where(d=>d.IsConnection).GroupBy(d=>d.JunctionId))
        {
            var list=group.ToArray();
            foreach(var a in list.Reverse())foreach(var b in list.Reverse())Check(reversed.Contains(a.Id,b.Id)==frozen.Contains(a.Id,b.Id),label+" cold reversed query order");
            foreach(var a in list)
            {
                Check(actual.HasOther(a.Id)==frozen.HasOther(a.Id),label+" HasOther");
                foreach(var b in list)
                {
                    bool expected=frozen.Contains(a.Id,b.Id);
                    Check(actual.Contains(a.Id,b.Id)==expected,label+" pair");
                    Check(actual.Contains(b.Id,a.Id)==expected,label+" reverse/cached pair");pairs++;
                }
            }
        }
        var all=n.Drivables.Where(d=>d.IsConnection).ToArray();
        foreach(var a in all.Take(12))foreach(var b in all.Reverse().Take(12))Check(actual.Contains(a.Id,b.Id)==frozen.Contains(a.Id,b.Id),label+" cross junction");
        Check(!actual.Contains(long.MaxValue,long.MaxValue)&&!actual.Contains(0,0),label+" absent identity");
        results.Add(new{label,roads=n.RoadCount,connections=all.Length,pairs});Console.WriteLine(label+" pairs="+pairs);
    }
    static Network Radial(int arms,int lanes,int variant)
    {
        var d=new NetworkData{nextJunction=arms+2,nextRoad=arms+1};d.junctions.Add(new Junction{id=1,position=new Point(0,2,0)});
        for(int i=0;i<arms;i++)
        {
            double angle=i*2*Math.PI/arms+.013*variant;var end=new Point(Math.Cos(angle)*40,2+(variant%3==0?i*.7:0),Math.Sin(angle)*40);
            d.junctions.Add(new Junction{id=i+2,position=end});var mid=Point.Lerp(new Point(0,2,0),end,.5);if(variant%2!=0){mid.x+=Math.Sin(angle)*2;mid.z-=Math.Cos(angle)*2;}
            d.roads.Add(new PhysicalRoad{id=i+1,from=1,to=i+2,lanes=i%2==0?lanes:Math.Max(1,lanes-variant%2),direction=variant%4==3&&i==0?RoadDirection.Forward:RoadDirection.Both,speed=12,points=new[]{new Point(0,2,0),mid,end}});
        }
        return new Network(d);
    }
    static void GeometryCheck()
    {
        var flags=BindingFlags.Static|BindingFlags.NonPublic;
        var old=typeof(FrozenConnectionConflicts).GetMethod("Overlap",flags);
        var next=typeof(ConnectionConflicts).GetMethod("Overlap",flags);
        var pathType=typeof(ConnectionConflicts).GetNestedType("ProtectedPath",BindingFlags.NonPublic);
        Func<Geometry,object> wrap=g=>Activator.CreateInstance(pathType,BindingFlags.Instance|BindingFlags.NonPublic,null,new object[]{g},null);
        Action<Geometry,Geometry> check=(a,b)=>Check((bool)old.Invoke(null,new object[]{a,b})==(bool)next.Invoke(null,new[]{wrap(a),wrap(b)}),"Frozen geometry overlap differs");
        foreach(double gap in new[]{0,.8999999999,.9,.9000000001,.9999999999,1.0,1.0000000001,2})
        foreach(double scale in new[]{1.0,1e-9,1000000.0})
        {
            var a=new Geometry(new[]{new Point(0,0,0),new Point(scale,0,0)});
            check(a,new Geometry(new[]{new Point(0,gap,0),new Point(scale,gap,0)}));
            check(a,new Geometry(new[]{new Point(scale/2,gap,-1),new Point(scale/2,gap,1)}));
            check(a,new Geometry(new[]{new Point(scale/2,gap,0),new Point(scale/2,gap,0)}));
        }
        var random=new Random(7919);
        check(new Geometry(new[]{new Point(1e20,0,0),new Point(2,0,0)}),new Geometry(new[]{new Point(0,0,-1),new Point(0,0,1)}));
        for(int round=0;round<2000;round++)
        {
            var a=new Point[5];var b=new Point[5];for(int i=0;i<5;i++){a[i]=new Point(random.NextDouble()*20-10,random.NextDouble()*4,random.NextDouble()*20-10);b[i]=new Point(random.NextDouble()*20-10,random.NextDouble()*4,random.NextDouble()*20-10);}
            if(round%7==0)a[2]=a[1];if(round%11==0)b[4]=a[0];check(new Geometry(a),new Geometry(b));
        }
    }
    static void Main(string[] args)
    {
        Directory.CreateDirectory(args[0]);GeometryCheck();
        foreach(int lanes in new[]{1,2,3,4})foreach(int arms in new[]{2,3,4})foreach(int variant in new[]{0,1,3})NetworkCheck(Radial(arms,lanes,variant),"radial-"+arms+"-"+lanes+"-"+variant);
        foreach(string file in args.Skip(1))
        {
            var s=CityCoreSave.Read(File.ReadAllBytes(file));s.Simulation.Growth.Zoning.SetSurface(CityScenarioBuilder.Flat);byte[] before=CityCoreSave.Write(s);
            NetworkCheck(s.Transport.Network,Path.GetFileName(Path.GetDirectoryName(file)));
            Check(before.SequenceEqual(CityCoreSave.Write(s)),"Conflict queries mutated saved facts");s.Validate();
        }
        File.WriteAllText(Path.Combine(args[0],"conflict-oracle.json"),new JavaScriptSerializer().Serialize(new{checks,results,status="PASS",scope="Exact original geometry/pair oracle; includes all local pairs of supplied cities, not FPS or economic-health acceptance"}));Console.WriteLine("PASS "+checks+" frozen conflict comparisons");
    }
}
