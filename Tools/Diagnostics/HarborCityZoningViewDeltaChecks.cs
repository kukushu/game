using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.IO;
using UnityEngine;
using HarborCity;

// Native Unity test only. The runner supplies the actual frozen pre-change
// view as HarborCityZoningViewsReference; no reference renderer ships in game.
public static class HarborCityZoningViewDeltaChecks
{
    public static string StableBenchmark {get;private set;}
    sealed class Image
    {
        internal GameObject go;internal Mesh mesh;internal Material material;
        internal Vector3[] vertices,normals;internal int[] indices;
        internal Bounds bounds;internal UnityEngine.Rendering.IndexFormat format;
        internal bool Same(Image b)=>material==b.material&&vertices.SequenceEqual(b.vertices)&&normals.SequenceEqual(b.normals)&&indices.SequenceEqual(b.indices)&&bounds==b.bounds&&format==b.format;
    }
    static List<GameObject> Objects(object view)=>(List<GameObject>)view.GetType().GetField("objects",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(view);
    static List<Image> Capture(object view)=>Objects(view).Select(go=>
    {
        var mesh=go.GetComponent<MeshFilter>().sharedMesh;
        return new Image{go=go,mesh=mesh,material=go.GetComponent<Renderer>().sharedMaterial,vertices=mesh.vertices,normals=mesh.normals,indices=mesh.triangles,bounds=mesh.bounds,format=mesh.indexFormat};
    }).ToList();
    public static IEnumerator CompareCity(object view,IReadOnlyList<ZoneCell> cells,Func<float,float,float> height,Material[] palette,string output)
    {
        var groups=cells.Where(c=>c.occupiedBuilding<0).GroupBy(c=>((int)Math.Floor(c.pose.x/64),(int)Math.Floor(c.pose.z/64),c.use==LandUse.Residential?1:c.use==LandUse.Commercial?2:c.use==LandUse.Industrial?3:0)).ToArray();
        var actual=Objects(view);if(actual.Count!=groups.Length)throw new Exception("Real city chunk count differs");
        int triangles=0,vertices=0;
        for(int i=0;i<groups.Length;i++)
        {
            // Release each reference chunk on the following frame: never retain
            // a second whole city's meshes, nor include assertions in edit time.
            using(var old=new HarborCityZoningViewsReference(palette))
            {
                old.Sync(groups[i].ToList(),height);var expected=Capture(old).Single();var mesh=actual[i].GetComponent<MeshFilter>().sharedMesh;
                if(actual[i].name!=expected.go.name||!mesh.vertices.SequenceEqual(expected.vertices)||!mesh.normals.SequenceEqual(expected.normals)||!mesh.triangles.SequenceEqual(expected.indices)||mesh.bounds!=expected.bounds||mesh.indexFormat!=expected.format||actual[i].GetComponent<Renderer>().sharedMaterial!=expected.material||!actual[i].GetComponent<Renderer>().enabled)throw new Exception("Real city ordered mesh differs at "+i);
                vertices+=mesh.vertexCount;triangles+=expected.indices.Length/3;
            }
            yield return null;
        }
        File.WriteAllText(Path.Combine(output,"view-equivalence.txt"),"PASS frozen full-group order, vertices, indices, normals, bounds, index format, materials and visibility: "+groups.Length+" chunks / "+vertices+" vertices / "+triangles+" triangles. Verification outside performance window.\n");
    }
    static string Input(List<ZoneCell> cells)=>string.Join(";",cells.Select(c=>c.key+":"+c.use+":"+c.occupiedBuilding+":"+c.available+":"+string.Join(",",new[]{c.pose.x,c.pose.z,c.pose.yaw,c.pose.width,c.pose.depth,c.pose.groundY}.Select(BitConverter.SingleToInt32Bits))));
    public static void Run(Material[] palette,Action<bool,string> check)
    {
        using(var view=new HarborCityZoningViews(palette))
        using(var legacy=new HarborCityZoningViewsReference(palette))
        {
            var cells=Enumerable.Range(0,120).Select(i=>new ZoneCell{key="cell:"+i,use=(LandUse)(i%4),occupiedBuilding=i%17==0?7:-1,available=i%5!=0,pose=new ResidentialBuilding{x=(i%12-6)*15.5f,z=(i/12-5)*15.5f,width=1.46f,depth=1.46f,yaw=i%3*23,groundY=2}}).ToList();
            Func<float,float,float> height=(x,z)=>2+(float)Math.Sin(x*.03)*.3f+z*.002f;
            Action<string,bool> verify=(label,invalidate)=>
            {
                var input=Input(cells);var prior=Capture(view).ToDictionary(i=>i.go.name);
                if(invalidate){view.InvalidateSurface();legacy.InvalidateSurface();}view.Sync(cells,height);legacy.Sync(cells,height);
                // A cold frozen Sync is the original full ordered GroupBy result,
                // independent of its old same-count index cache.
                using(var old=new HarborCityZoningViewsReference(palette))
                {
                    old.Sync(cells,height);var expected=Capture(old);var actual=Capture(view);
                    var cached=Capture(legacy);
                    check(view.PlotCount==old.PlotCount&&actual.Count==expected.Count,label+": plots/chunks");
                    check(cached.Count==expected.Count&&legacy.PlotCount==old.PlotCount,label+": old cached/full counts");
                    for(int i=0;i<expected.Count;i++)
                    {
                        check(actual[i].go.name==expected[i].go.name,label+": first-appearance chunk order");
                        check(actual[i].Same(expected[i]),label+": exact ordered vertices/indices/normals/material/bounds");
                        check(cached[i].go.name==actual[i].go.name&&cached[i].Same(actual[i]),label+": stateful frozen view differs");
                        check(actual[i].go.activeSelf&&actual[i].go.GetComponent<Renderer>().enabled,label+": visible");
                        if(!invalidate&&prior.TryGetValue(actual[i].go.name,out var before)&&before.Same(actual[i]))
                            check(ReferenceEquals(before.go,actual[i].go)&&ReferenceEquals(before.mesh,actual[i].mesh),label+": unchanged chunk rebuilt");
                    }
                }
                check(Input(cells)==input,label+": authoritative cell/order mutated");
            };
            verify("initial",false);verify("unchanged",false);
            var saved=cells[32];cells.RemoveAt(32);verify("middle deletion shifts all later indices",false);
            cells.Insert(3,saved);verify("insertion in different order",false);
            cells.RemoveRange(cells.Count-7,7);verify("tail truncation",false);
            cells.Reverse();verify("reverse all",false);
            cells[0].occupiedBuilding=5;verify("occupied hides",false);
            cells[0].occupiedBuilding=-2;verify("project reservation remains visible",false);
            cells[1].use=LandUse.Industrial;cells[2].pose.x+=64;verify("use and boundary move",false);
            // Removing the first/last member and whole chunks must update exact
            // grouping order without moving or dropping the other plots.
            cells.RemoveAll(c=>c.pose.x<0);verify("delete complete chunks",false);
            cells.Add(new ZoneCell{key=cells[0].key,use=LandUse.Commercial,pose=new ResidentialBuilding{x=-64,z=64,width=1.46f,depth=1.46f,groundY=2}});verify("new chunk and duplicate input key",false);
            var random=new System.Random(20261025);
            for(int step=0;step<80;step++)
            {
                int i=random.Next(cells.Count);switch(step%5)
                {
                    case 0:var item=cells[i];cells.RemoveAt(i);cells.Insert(random.Next(cells.Count+1),item);break;
                    case 1:cells[i].occupiedBuilding=cells[i].occupiedBuilding>=0?-1:20;break;
                    case 2:cells[i].use=(LandUse)random.Next(5);break;
                    case 3:cells[i].pose.x+=random.Next(2)==0?-64:64;break;
                    default:cells.Insert(i,new ZoneCell{key="added:"+step,use=LandUse.Residential,pose=new ResidentialBuilding{x=step*3-120,z=10,width=1.46f,depth=1.46f,groundY=2}});if(step%3==0)cells.RemoveAt(random.Next(cells.Count));break;
                }
                verify("random "+step,false);
            }
            verify("surface invalidation",true);
            using(var faultView=new HarborCityZoningViews(palette))
            {
                var faultCells=new[]{16f,144f}.Select(x=>new ZoneCell{use=LandUse.Residential,pose=new ResidentialBuilding{x=x,z=16,width=1.46f,depth=1.46f,groundY=2}}).ToList();
                faultView.Sync(faultCells,height);var beforeFailure=Capture(faultView);bool failed=false,second=false;
                // The first chunk is fully built before sampling the second.
                try{faultView.Sync(faultCells,(x,z)=>{if(x>64){second=true;throw new InvalidOperationException("second chunk failure");}return 2;});}catch(InvalidOperationException){failed=true;}
                check(failed&&second,"partial failure not reached");var afterFailure=Capture(faultView);
                check(beforeFailure.Count==afterFailure.Count,"partial failure changed chunk count");
                for(int i=0;i<beforeFailure.Count;i++)check(ReferenceEquals(beforeFailure[i].mesh,afterFailure[i].mesh)&&afterFailure[i].go.GetComponent<Renderer>().enabled,"partial failure published/hid candidate");
                faultView.Sync(faultCells,height);check(faultView.RebuiltChunks==0,"failed build modified committed comparison snapshot");
            }
            verify("retry after failure",false);
            cells.Clear();verify("clear",false);cells.Add(saved);verify("reuse cleared view",false);
            view.Sync(null,height);check(view.RendererCount==0&&view.PlotCount==0,"null clear");
            view.Dispose();view.Sync(cells,null);check(view.PlotCount==1,"reuse disposed owner");
        }
        // Bounded CPU regression check for the old same-count fast path. Tiny
        // quads keep this display-only fixture small; this is not city FPS.
        using(var old=new HarborCityZoningViewsReference(palette))
        using(var current=new HarborCityZoningViews(palette))
        {
            var cells=Enumerable.Range(0,256787).Select(i=>new ZoneCell{key="bench:"+i,use=LandUse.Residential,pose=new ResidentialBuilding{x=i%500-250,z=i/500-250,width=.1f,depth=.1f,groundY=2}}).ToList();
            old.Sync(cells,null);current.Sync(cells,null);
            var rows=new List<string>{"case,sample,old_ms,new_ms,old_rebuilt,new_rebuilt"};
            foreach(string label in new[]{"unchanged","occupancy","count-change"})
            for(int sample=0;sample<3;sample++)
            {
                if(label=="occupancy")cells[1000].occupiedBuilding=sample%2==0?42:-1;
                if(label=="count-change"){if(sample%2==0)cells.RemoveAt(3000);else cells.Insert(3000,new ZoneCell{use=LandUse.Residential,pose=new ResidentialBuilding{x=-200,z=-200,width=.1f,depth=.1f,groundY=2}});}
                double a=0,b=0;var timer=new System.Diagnostics.Stopwatch();
                for(int pass=0;pass<2;pass++)
                {
                    bool runOld=(pass+sample)%2==0;timer.Restart();if(runOld)old.Sync(cells,null);else current.Sync(cells,null);timer.Stop();if(runOld)a=timer.Elapsed.TotalMilliseconds;else b=timer.Elapsed.TotalMilliseconds;
                }
                check(old.PlotCount==current.PlotCount&&old.RendererCount==current.RendererCount&&old.RebuiltChunks==current.RebuiltChunks,"Stable-path benchmark changed display counts");
                rows.Add(string.Join(",",label,sample,a.ToString("R",System.Globalization.CultureInfo.InvariantCulture),b.ToString("R",System.Globalization.CultureInfo.InvariantCulture),old.RebuiltChunks,current.RebuiltChunks));
            }
            StableBenchmark=string.Join("\n",rows)+"\n";
        }
    }
}
