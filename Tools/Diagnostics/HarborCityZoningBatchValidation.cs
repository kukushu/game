using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using HarborCity;

// Copied only into the disposable validation project, alongside the actual
// frozen pre-change SurfaceMesh source. Not a product renderer fallback.
public sealed class HarborCityZoningBatchValidation:MonoBehaviour
{
    static int checks;
    static void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}
    static int Bits(float f)=>BitConverter.SingleToInt32Bits(f);
    static int Compare(Vector3 a,Vector3 b){int c=Bits(a.x).CompareTo(Bits(b.x));if(c!=0)return c;c=Bits(a.y).CompareTo(Bits(b.y));return c!=0?c:Bits(a.z).CompareTo(Bits(b.z));}
    struct Triangle:IComparable<Triangle>
    {
        internal Vector3 a,b,c;internal int tint;
        internal Triangle(Vector3 a,Vector3 b,Vector3 c,int tint)
        {this.tint=tint;if(Compare(b,a)<0&&Compare(b,c)<=0){this.a=b;this.b=c;this.c=a;}else if(Compare(c,a)<0&&Compare(c,b)<0){this.a=c;this.b=a;this.c=b;}else{this.a=a;this.b=b;this.c=c;}}
        public int CompareTo(Triangle other){int n=tint.CompareTo(other.tint);if(n!=0)return n;n=Compare(a,other.a);if(n!=0)return n;n=Compare(b,other.b);return n!=0?n:Compare(c,other.c);}
    }
    static List<GameObject> Objects(HarborCityZoningViews view)
    {var field=typeof(HarborCityZoningViews).GetField("objects",BindingFlags.NonPublic|BindingFlags.Instance);if(field==null)throw new Exception("Update cold test for changed view ownership");return (List<GameObject>)field.GetValue(view);}
    static void Add(List<Triangle> result,Mesh mesh,int tint)
    {var v=mesh.vertices;var t=mesh.triangles;for(int i=0;i<t.Length;i+=3)result.Add(new Triangle(v[t[i]],v[t[i+1]],v[t[i+2]],tint));}
    static int Use(ZoneCell c)=>c.use==LandUse.Residential?1:c.use==LandUse.Commercial?2:c.use==LandUse.Industrial?3:0;
    static void Validate(HarborCityZoningViews view,List<ZoneCell> cells,Func<float,float,float> height,Material[] palette)
    {
        var expected=new List<Triangle>();int count=0,vertices=0;
        foreach(var cell in cells)if(cell.occupiedBuilding<0)
        {var reference=HarborCitySurfaceMeshReference.Plot(cell.pose,height);vertices+=reference.vertexCount;Add(expected,reference,Use(cell));Destroy(reference);count++;}
        var actual=new List<Triangle>();int actualVertices=0;
        foreach(var go in Objects(view))
        {
            Check(go.transform.position==Vector3.zero&&go.transform.rotation==Quaternion.identity&&go.transform.localScale==Vector3.one,"World-space mesh transform changed");
            Check(go.GetComponent<Renderer>().enabled,"Hidden zoning is not batching");
            int tint=Array.IndexOf(palette,go.GetComponent<Renderer>().sharedMaterial);Check(tint>=0,"Zoning colour changed");
            var mesh=go.GetComponent<MeshFilter>().sharedMesh;actualVertices+=mesh.vertexCount;Add(actual,mesh,tint);
        }
        Check(view.PlotCount==count,"Plots dropped or occupied-building rule changed");Check(vertices==actualVertices,"Vertices were simplified/dropped");Check(expected.Count==actual.Count,"Triangles changed");
        expected.Sort();actual.Sort();for(int i=0;i<expected.Count;i++)Check(expected[i].CompareTo(actual[i])==0,"Exact triangle position, winding or colour changed at "+i);
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Launch()
    {var args=Environment.GetCommandLineArgs();int flag=Array.IndexOf(args,"-harbor-zone-view-check");if(flag>=0)new GameObject("Zoning view checks").AddComponent<HarborCityZoningBatchValidation>().StartCoroutine(Run(args[flag+1]));}
    static IEnumerator Run(string output)
    {
        yield return null;Directory.CreateDirectory(output);HarborCityZoningViews view=null;Material[] palette=null;
        try
        {
            var shader=Resources.Load<Shader>("RoadCore");palette=new[]{new Material(shader),new Material(shader),new Material(shader),new Material(shader)};view=new HarborCityZoningViews(palette);
            var cells=new List<ZoneCell>();var uses=new[]{LandUse.Empty,LandUse.Residential,LandUse.Commercial,LandUse.Industrial,LandUse.Power};
            for(int i=0;i<600;i++)cells.Add(new ZoneCell{key="test:"+i,use=uses[i%uses.Length],occupiedBuilding=i%13==0?7:-1,available=i%7!=0,pose=new ResidentialBuilding{x=(i%30-15)*3.3f,z=(i/30-10)*3.3f,yaw=i%4*23.5f,width=i%9==0?5.12f:2.96f,depth=2.96f,groundY=i%3*.1f+2}});
            foreach(Func<float,float,float> height in new Func<float,float,float>[]{null,(x,z)=>2,(x,z)=>2+(float)Math.Sin(x*.03)*.5f+z*.002f})
            {
                view.Sync(cells,height);Validate(view,cells,height,palette);Check(view.RendererCount<view.PlotCount,"No actual spatial batching");
                var retained=Objects(view).ToArray();view.Sync(cells,height);Check(view.RebuiltChunks==0&&retained.SequenceEqual(Objects(view)),"Unchanged projection rebuilt chunks");
                // Same-count/same-surface updates exercise the sparse display path.
                cells[4].occupiedBuilding=cells[4].occupiedBuilding<0?4:-1;cells[5].use=cells[5].use==LandUse.Industrial?LandUse.Residential:LandUse.Industrial;
                view.Sync(cells,height);Validate(view,cells,height,palette);Check(view.RebuiltChunks<=4,"Local edit rebuilt remote chunks");
                cells[6].pose.x+=128;view.Sync(cells,height);Validate(view,cells,height,palette);
            }
            // Painting, occupation and spatial movement must rebuild the same data.
            foreach(var cell in cells.Take(30)){cell.use=LandUse.Industrial;cell.occupiedBuilding=-1;cell.pose.x-=128;}
            view.Sync(cells,null);Validate(view,cells,null,palette);
            var before=Objects(view).ToArray();int beforeCount=view.PlotCount;bool failed=false;
            try{view.Sync(cells,(x,z)=>throw new InvalidOperationException("intentional sampler failure"));}catch(InvalidOperationException){failed=true;}
            Check(failed&&view.PlotCount==beforeCount&&before.SequenceEqual(Objects(view)),"Failed display rebuild discarded current geometry");
            // Synthetic display-only stress: preserve >65535 vertices in one chunk.
            var dense=Enumerable.Range(0,1400).Select(i=>new ZoneCell{key="dense:"+i,use=LandUse.Residential,occupiedBuilding=-1,pose=new ResidentialBuilding{width=2.96f,depth=2.96f,groundY=2}}).ToList();
            view.Sync(dense,null);Validate(view,dense,null,palette);Check(Objects(view).Single().GetComponent<MeshFilter>().sharedMesh.indexFormat==UnityEngine.Rendering.IndexFormat.UInt32,"Large batch truncated 16-bit indices");
            view.Sync(new List<ZoneCell>(),null);Check(view.PlotCount==0&&view.RendererCount==0,"Cleared city retained zoning");view.Dispose();foreach(var material in palette)Check(material!=null,"View destroyed borrowed material");
            HarborCityZoningViewDeltaChecks.Run(palette,Check);
            File.WriteAllText(Path.Combine(output,"stable-updates.csv"),HarborCityZoningViewDeltaChecks.StableBenchmark);
            File.WriteAllText(Path.Combine(output,"Result.txt"),"PASS "+checks+" frozen-source triangle and ordered old-view delta checks: flat/slope/rotated/unassigned/occupied/reserved/paint/move/insert/delete/reorder/clear/failure and >65535 vertices. No simulation or pixel/FPS claim.\n");
        }
        catch(Exception error){File.WriteAllText(Path.Combine(output,"Result.txt"),error.ToString());Application.Quit(1);yield break;}
        finally{view?.Dispose();if(palette!=null)foreach(var m in palette)Destroy(m);}
        yield return null;Application.Quit(0);
    }
}
