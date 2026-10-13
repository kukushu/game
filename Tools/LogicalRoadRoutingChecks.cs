using System;
using System.Collections.Generic;
using System.Linq;
using HarborCity;

public static class LogicalRoadRoutingChecks
{
    static int checks,fallbacks;
    static void Compare(CityRoads roads,List<int> starts,List<int> goals,bool walking)
    {
        var expected=roads.CalculateLegacyPath(starts,goals,walking);var actual=CityLogicalRoadRouter.Search(roads,starts,goals,walking);
        if(actual==null)fallbacks++;
        if(actual!=null && !expected.SequenceEqual(actual))throw new Exception("Logical route differs: starts="+string.Join(",",starts)+" goals="+string.Join(",",goals)+" walking="+walking+" old="+string.Join(",",expected)+" new="+(actual==null?"fallback":string.Join(",",actual)));
        if(!roads.FindPath(starts,goals,walking).SequenceEqual(expected))throw new Exception("Default routing/cached result differs from original oracle");
        checks++;
    }
    public static void Run()
    {
        var random=new Random(773);
        for(int fixture=0;fixture<18;fixture++)
        {
            var roads=new CityRoads{nextStroke=2};var grid=new int[7,5];
            for(int x=0;x<7;x++)for(int z=0;z<5;z++)
            {
                int id=x==0 && z==0?CityRoads.Entrance:roads.nextNode;roads.nextNode+=17;grid[x,z]=id;
                roads.nodes.Add(new RoadNode{id=id,x=x*3,z=z*3,y=fixture<6?1:1+random.Next(3)*.2f});
            }
            for(int x=0;x<7;x++)for(int z=0;z<5;z++)
            {
                // Only whole horizontal streets / odd columns: geometry chains remain.
                if(x<6)Add(grid[x,z],grid[x+1,z]);
                if(z<4 && x%3==0)Add(grid[x,z],grid[x,z+1]);
            }
            roads.Changed();if(!roads.Valid())throw new Exception("Routing grid fixture invalid");
            var nodes=roads.nodes.Select(n=>n.id).ToArray();
            foreach(bool walking in new[]{false,true})for(int pair=0;pair<200;pair++)
            {
                var starts=new List<int>{nodes[random.Next(nodes.Length)]};var goals=new List<int>{nodes[random.Next(nodes.Length)]};
                if(pair%9==0){starts.Add(nodes[random.Next(nodes.Length)]);goals.Add(nodes[random.Next(nodes.Length)]);}
                Compare(roads,starts,goals,walking);
            }
            var json=new System.Web.Script.Serialization.JavaScriptSerializer{MaxJsonLength=int.MaxValue};
            var saved=json.Deserialize<CityRoads>(json.Serialize(roads));
            Compare(saved,new List<int>{nodes[3]},new List<int>{nodes[25]},false);
            roads.edges.Reverse();roads.Changed();Compare(roads,new List<int>{nodes[3]},new List<int>{nodes[25]},false);
            roads.edges[0].oneWay=!roads.edges[0].oneWay;roads.edges[0].kind=RoadKind.Standard;roads.Changed();Compare(roads,new List<int>{nodes[3]},new List<int>{nodes[25]},false);
            roads.Remove(new List<int>{roads.edges[1].id});Compare(roads,new List<int>{nodes[3]},new List<int>{nodes[25]},false);
            void Add(int a,int b)
            {
                RoadKind kind=fixture<6?RoadKind.Standard:random.Next(3)==0?RoadKind.FourLane:RoadKind.Standard;
                bool oneWay=kind==RoadKind.Standard && fixture>=12 && random.Next(3)==0;
                roads.edges.Add(new RoadEdge{id=roads.nextEdge++,a=a,b=b,stroke=1,kind=kind,oneWay=oneWay});
            }
        }
        // Original sampled loop and internal building endpoints remain legal search cuts.
        var loop=new CityRoads{nextStroke=2};
        for(int i=0;i<120;i++)loop.nodes.Add(new RoadNode{id=i==0?CityRoads.Entrance:loop.nextNode++,x=(float)Math.Cos(i*Math.PI/60)*30,z=(float)Math.Sin(i*Math.PI/60)*30,y=1});
        for(int i=0;i<120;i++)loop.edges.Add(new RoadEdge{id=loop.nextEdge++,a=loop.nodes[i].id,b=loop.nodes[(i+1)%120].id,stroke=1});
        loop.Changed();foreach(bool walking in new[]{false,true})for(int i=0;i<120;i++)Compare(loop,new List<int>{loop.nodes[i].id},new List<int>{loop.nodes[(i+37)%120].id},walking);
        var bad=loop.Copy();bad.nodes[1].x=bad.nodes[0].x;bad.nodes[1].y=bad.nodes[0].y;bad.nodes[1].z=bad.nodes[0].z;bad.Changed();if(bad.Valid())throw new Exception("Zero length raw save accepted");
        bad=loop.Copy();bad.nodes[1].stopFrom=null;bad.Changed();if(bad.Valid())throw new Exception("Null stop list accepted");
        if(fallbacks>=checks)throw new Exception("All routing searches fell back");Console.WriteLine("PASS: "+checks+" paths, conservative tie fallbacks="+fallbacks+" exact logical/legacy route sequences including ties, multiple/internal endpoints, direction, order, edits, loops and save");
    }
}
