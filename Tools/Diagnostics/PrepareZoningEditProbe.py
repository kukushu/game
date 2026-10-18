"""Instrument only a disposable Unity project. Never changes product sources."""
from pathlib import Path
import re
root=Path.cwd()/'Temp/RoadCoreUnity'
runtime=root/'Assets/Scripts/Runtime'
def instrument(file,signature,label):
    path=runtime/file
    source=path.read_text(encoding='utf-8-sig')
    source,count=re.subn(re.escape(signature)+r'\s*\{',lambda m:m[0]+'using var zoneProbeScope=new global::HarborCity.ZoningEditProbeScope("'+label+'");',source,count=1)
    if count!=1:raise RuntimeError(signature)
    path.write_text(source,encoding='utf-8')
instrument(Path('Infrastructure/Zoning/CityCoreZoning.cs'),'public IReadOnlyList<ZoneCell> Cells()','ZoningCells')
instrument(Path('Presentation/Game/HarborCityRoadCoreGame.cs'),'void RenderZones()','ZoneViews')
instrument(Path('Presentation/Game/HarborCityRoadCoreGame.cs'),'void RenderRoads()','RoadViews')
instrument(Path('Presentation/World/HarborCityZoningViews.cs'),'public void Sync(IReadOnlyList<ZoneCell> cells,Func<float,float,float> height)','ZoningDisplay')
views=runtime/'Presentation/World/HarborCityZoningViews.cs'
view_source=views.read_text(encoding='utf-8')
if 'if(cells!=null)foreach(var cell in cells)' in view_source:
    label='ChunkMembership' if 'Chunk BuildChunk' in view_source else 'MeshGeometry'
    view_source=view_source.replace('if(cells!=null)foreach(var cell in cells)','using(new global::HarborCity.ZoningEditProbeScope("'+label+'"))\n                if(cells!=null)foreach(var cell in cells)')
    view_source=view_source.replace('mesh.SetVertices(b.vertices);mesh.SetTriangles(b.indices,0);mesh.RecalculateNormals();','using(new global::HarborCity.ZoningEditProbeScope("MeshUpload")){mesh.SetVertices(b.vertices);mesh.SetTriangles(b.indices,0);mesh.RecalculateNormals();}')
    view_source=view_source.replace('var go=new GameObject(mesh.name);nextObjects.Add(go);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=materials[key.use];','using(new global::HarborCity.ZoningEditProbeScope("GameObjects")){var go=new GameObject(mesh.name);nextObjects.Add(go);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=materials[key.use];}')
    view_source=view_source.replace('foreach(var plot in plots)HarborCitySurfaceMesh.AppendPlot(plot.Pose(),height,vertices,indices);','using(new global::HarborCity.ZoningEditProbeScope("MeshGeometry"))foreach(var plot in plots)HarborCitySurfaceMesh.AppendPlot(plot.Pose(),height,vertices,indices);')
    view_source=view_source.replace('mesh.SetVertices(vertices);mesh.SetTriangles(indices,0);mesh.RecalculateNormals();','using(new global::HarborCity.ZoningEditProbeScope("MeshUpload")){mesh.SetVertices(vertices);mesh.SetTriangles(indices,0);mesh.RecalculateNormals();}')
    view_source=view_source.replace('var go=new GameObject(mesh.name);chunk.instance=go;go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=materials[key.use];return chunk;','using(new global::HarborCity.ZoningEditProbeScope("GameObjects")){var go=new GameObject(mesh.name);chunk.instance=go;go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=materials[key.use];return chunk;}')
    views.write_text(view_source,encoding='utf-8')
host=runtime/'Presentation/Game/HarborCityRoadCoreGame.cs'
source=host.read_text(encoding='utf-8')
anchor='int flag=Array.IndexOf(args,"-harbor-core-validate");'
assert source.count(anchor)==1
source=source.replace(anchor,'if(Array.IndexOf(args,"-harbor-zone-probe")>=0)StartCoroutine(MeasureZoningEdit(args));\n'+anchor)
probe=r'''
        System.Collections.IEnumerator MeasureZoningEdit(string[] args)
        {
            string output=args[Array.IndexOf(args,"-harbor-zone-probe")+1];Directory.CreateDirectory(output);
            speed=0;ScenarioMeasuring=true;QualitySettings.vSyncCount=0;Application.targetFrameRate=60;
            var recorder=gameObject.AddComponent<ZoningEditFrameProbe>();
            for(int i=0;i<5;i++)yield return null;
            double h=session.City.map.buildHalfSize;
            Action<double,double,double,double> add=(x,z,X,Z)=>{Debug.Log("Zoning setup/edit stroke "+x+","+z+" -> "+X+","+Z);var points=new Point[9];for(int i=0;i<points.Length;i++)points[i]=Point.Lerp(new Point(x,2,z),new Point(X,2,Z),i/8.0);var plan=world.PlanConnectedRoad(points,false,snap:4,lanes:1);session.Construction.CheckRoadPlacement(plan);if(!session.Construction.Commit(plan,out _))throw new Exception("Stale zoning probe road");};
            add(-h+5,-h+8,-h+17,-h+8);add(-h+5,-h+32,-h+17,-h+32);
            RenderRoads();RenderZones();shownNetwork=world.NetworkGeneration;shownZones=session.City.zoning.revision;shownBuildings=session.Entities.BuildingRevision;
            for(int i=0;i<4;i++)yield return null;
            int ordinary=0;var profiles=new List<string>{"operation,"+CityCoreProfile.Header()};var statuses=new List<string>{"operation,status,reason"};var counts=new List<string>{"operation,cells,geometry_roads,resolved_roads,full_rebuild,renderers,rebuilt_chunks"};
            Func<object,string,string> property=(o,name)=>o.GetType().GetProperty(name)?.GetValue(o)?.ToString()??"-1";
            bool raw=Array.IndexOf(args,"-harbor-zone-raw")>=0;
            foreach(string operation in new[]{"add","connect_split","widen","delete","continuous"})
            {
                byte[] beforeOperation=CityCoreSave.Write(session);yield return null;
                ZoningEditProbeScope.Phase=operation;ZoningEditProbeScope.Enabled=true;CityCoreProfile.Enabled=Array.IndexOf(args,"-harbor-zone-fine")>=0;CityCoreProfile.Reset();recorder.Phase=operation;
                try{using(new ZoningEditProbeScope("Construction"))
                {
                    if(operation=="add"){add(-h+5,-h+56,-h+17,-h+56);ordinary=world.Network.RoadIds.Max();}
                    if(operation=="connect_split")add(-h+11,-h+8,-h+11,-h+32);
                    if(operation=="widen")session.Construction.UpgradeRoad(ordinary,2,RoadDirection.Both);
                    if(operation=="delete")session.Construction.RemoveRoad(ordinary);
                    if(operation=="continuous")add(-h+5,-h+80,-h+17,-h+80);
                }
                statuses.Add(operation+",accepted,none");}
                catch(Exception error){if(!beforeOperation.SequenceEqual(CityCoreSave.Write(session)))throw new Exception("Failed operation changed full save",error);statuses.Add(operation+",rejected,\""+error.Message.Replace("\"","\"\"")+"\"");}
                RenderRoads();
                if(raw&&operation=="add"){UnityEngine.Profiling.Profiler.logFile=Path.Combine(output,"window.raw");UnityEngine.Profiling.Profiler.enableBinaryLog=true;UnityEngine.Profiling.Profiler.enabled=true;}
                for(int i=0;i<4;i++)yield return null;
                if(operation=="continuous")
                {
                    using(new ZoningEditProbeScope("Construction"))add(-h+5,-h+104,-h+17,-h+104);
                    RenderRoads();for(int i=0;i<4;i++)yield return null;
                }
                if(raw&&operation=="add"){UnityEngine.Profiling.Profiler.enabled=false;UnityEngine.Profiling.Profiler.enableBinaryLog=false;}
                profiles.Add(operation+","+CityCoreProfile.Row(session.Simulation.Tick,world.ActiveCount,0,0,0,0));
                var zone=session.Simulation.Growth.Zoning;bool accepted=statuses[statuses.Count-1].StartsWith(operation+",accepted,",StringComparison.Ordinal);counts.Add(operation+","+zone.Cells().Count+","+(accepted?property(zone,"LastGeometryRoads"):"0")+","+(accepted?property(zone,"LastResolvedRoads"):"0")+","+(accepted?property(zone,"LastGeometryFull"):"False")+","+zoningViews.RendererCount+","+(accepted?property(zoningViews,"RebuiltChunks"):"0"));
                ZoningEditProbeScope.Enabled=false;CityCoreProfile.Enabled=false;recorder.Phase="between";
            }
            ZoningEditProbeScope.Write(output);recorder.Write(output);File.WriteAllLines(Path.Combine(output,"profile.csv"),profiles);File.WriteAllLines(Path.Combine(output,"operations.csv"),statuses);File.WriteAllLines(Path.Combine(output,"counts.csv"),counts);
            session.Validate();var bytes=CityCoreSave.Write(session);File.WriteAllBytes(Path.Combine(output,"final.hcity"),bytes);
            var restored=CityCoreSave.Read(bytes);restored.Validate();if(!bytes.SequenceEqual(CityCoreSave.Write(restored)))throw new Exception("Zoning edit restore changed facts");
            yield return new WaitForEndOfFrame();var screenshot=new Texture2D(Screen.width,Screen.height,TextureFormat.RGB24,false);screenshot.ReadPixels(new Rect(0,0,Screen.width,Screen.height),0,0);screenshot.Apply();File.WriteAllBytes(Path.Combine(output,"final.png"),screenshot.EncodeToPNG());Destroy(screenshot);
            File.WriteAllText(Path.Combine(output,"Result.txt"),"PASS construction suite and full save roundtrip; operations.csv distinguishes accepted commands and unchanged failures. Scripted domain commands in native Unity, no physical mouse acceptance or FPS claim.\n");Application.Quit(0);
        }
'''
source=source.replace('        void RenderRoads()',probe+'\n        void RenderRoads()')
host.write_text(source,encoding='utf-8')
helper=r'''
using System;using System.IO;using System.Collections.Generic;using System.Diagnostics;using System.Globalization;using Unity.Profiling;using UnityEngine;
namespace HarborCity {
public readonly struct ZoningEditProbeScope:IDisposable {
 public static bool Enabled;public static string Phase="setup";
 static readonly Dictionary<string,ProfilerMarker> markers=new Dictionary<string,ProfilerMarker>();static readonly List<string> rows=new List<string>{"operation,name,ms,allocated_bytes"};
 readonly long start,bytes;readonly string phase,name;readonly bool enabled;readonly ProfilerMarker.AutoScope scope;
 public ZoningEditProbeScope(string name){enabled=Enabled;this.name=name;phase=Phase;start=enabled?Stopwatch.GetTimestamp():0;bytes=enabled?GC.GetAllocatedBytesForCurrentThread():0;scope=default;if(enabled){if(!markers.TryGetValue(name,out var marker)){marker=new ProfilerMarker("City.ZoningEdit."+name);markers.Add(name,marker);}scope=marker.Auto();}}
 public void Dispose(){if(!enabled)return;scope.Dispose();double ms=(Stopwatch.GetTimestamp()-start)*1000.0/Stopwatch.Frequency;long allocated=HarborCity.RoadCore.CityCoreProfile.AllocationAvailable?GC.GetAllocatedBytesForCurrentThread()-bytes:-1;rows.Add(phase+","+name+","+ms.ToString("R",CultureInfo.InvariantCulture)+","+allocated);}
 public static void Write(string output){File.WriteAllLines(Path.Combine(output,"scopes.csv"),rows);}
}
[DefaultExecutionOrder(20000)]public sealed class ZoningEditFrameProbe:MonoBehaviour {
 public string Phase="setup";readonly List<string> rows=new List<string>{"frame,operation,frame_ms"};
 void Update(){rows.Add(Time.frameCount+","+Phase+","+(Time.unscaledDeltaTime*1000).ToString("R",CultureInfo.InvariantCulture));}
 public void Write(string output){File.WriteAllLines(Path.Combine(output,"frames.csv"),rows);}
}}
'''
(root/'Assets/Scripts/Diagnostics/Runtime/ZoningEditProbeScope.cs').write_text(helper,encoding='utf-8')
print('Prepared disposable zoning edit instrumentation')
