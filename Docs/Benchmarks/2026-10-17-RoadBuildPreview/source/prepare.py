from pathlib import Path
import re
root=Path.cwd()/"Temp/RoadCoreUnity"
def instrument(rel,signature,name):
 p=root/rel
 s=p.read_text(encoding='utf-8-sig')
 pat=re.escape(signature)+r'\s*\{'
 s,n=re.subn(pat,lambda m:m.group(0)+'using var roadProbeScope=new global::HarborCity.RoadBuildProbeScope("'+name+'");',s,count=1)
 if n!=1: raise RuntimeError(signature)
 p.write_text(s,encoding='utf-8')
base='Assets/Scripts/Runtime/'
for signature,name in [
 ('public World(WorldData state,SimulationClock clock=null,IEnumerable<int> endpoints=null)','WorldCtor'),
 ('public WorldData Snapshot()','WorldSnapshot'),
 ('internal RoadEditPlan PlanFromDraft(World draft)','PlanFromDraft'),
 ('internal bool TryCommit(RoadEditPlan plan)','WorldTryCommit'),
 ('public void Validate()','WorldValidate'),
 ('public void AssertSafety()','WorldSafety')]:
 instrument(base+'Traffic/CityRoadCoreWorld.cs',signature,name)
instrument(base+'Infrastructure/Roads/CityRoadCoreNetwork.cs','public Network(NetworkData data)','NetworkCtor')
instrument(base+'Infrastructure/Roads/CityRoadCoreConstruction.cs','public static RoadEditPlan Connect(World owner,Point[] center,bool oneWay,double speed,double snap,int lanes)','Connect')
instrument(base+'Infrastructure/Roads/CityRoadCoreConstruction.cs','static int Attach(World draft,Point point,double radius)','Attach')
instrument(base+'Simulation/Core/CityCoreConstruction.cs','public int CheckRoadPlacement(RoadEditPlan plan)','PlacementCheck')
instrument(base+'Infrastructure/Zoning/CityCoreZoning.cs','public IReadOnlyList<ZoneCell> Cells()','ZoningCells')
host=base+'Presentation/Game/HarborCityRoadCoreGame.cs'
for sig,name in [('void Preview(Point p)','Preview'),('void RenderRoads()','RoadViews'),('void RenderZones()','ZoneViews')]: instrument(host,sig,name)
p=root/host;s=p.read_text(encoding='utf-8')
anchor='int flag=Array.IndexOf(args,"-harbor-core-validate");'
assert s.count(anchor)==1
s=s.replace(anchor,'if(Array.IndexOf(args,"-harbor-road-probe")>=0)StartCoroutine(MeasureRoadBuild(args));\n            '+anchor)
probe=r'''
        System.Collections.IEnumerator MeasureRoadBuild(string[] args)
        {
            string output=args[Array.IndexOf(args,"-harbor-road-probe")+1];Directory.CreateDirectory(output);
            speed=0;ScenarioMeasuring=true;QualitySettings.vSyncCount=0;Application.targetFrameRate=60;
            for(int i=0;i<5;i++)yield return null;
            byte[] before=CityCoreSave.Write(session);int initialRoads=world.Network.RoadCount;int initialViews=roads.Count;
            var frameRows=new List<string>{"frame,phase,frame_ms"};
            RoadBuildProbeScope.Enabled=true;
            bool raw=Array.IndexOf(args,"-harbor-road-raw")>=0;
            if(raw){UnityEngine.Profiling.Profiler.logFile=Path.Combine(output,"window.raw");UnityEngine.Profiling.Profiler.enableBinaryLog=true;UnityEngine.Profiling.Profiler.enabled=true;}
            double h=session.City.map.buildHalfSize;startPoint=new Point(-h+8,2,-h+8);hasStart=true;building=true;
            for(int i=0;i<3;i++)
            {
                RoadBuildProbeScope.Phase="preview"+i;previewVersion=-1;
                using(new RoadBuildProbeScope("ActionPreview"))Preview(new Point(-h+24+i*2,2,-h+8));
                yield return null;frameRows.Add(Time.frameCount+","+RoadBuildProbeScope.Phase+","+(Time.unscaledDeltaTime*1000).ToString("R",System.Globalization.CultureInfo.InvariantCulture));
            }
            RoadBuildProbeScope.Enabled=false;
            bool previewUnchanged=before.SequenceEqual(CityCoreSave.Write(session));
            RoadBuildProbeScope.Enabled=true;RoadBuildProbeScope.Phase="click";
            RoadEditPlan plan;
            using(new RoadBuildProbeScope("ActionPlan"))plan=world.PlanConnectedRoad(RoadStroke(new Point(-h+28,2,-h+8)),false,snap:4,lanes:1);
            using(new RoadBuildProbeScope("ActionCheck"))session.Construction.CheckRoadPlacement(plan);
            int paid;
            using(new RoadBuildProbeScope("ActionCommit"))if(!session.Construction.Commit(plan,out paid))throw new Exception("Probe plan stale");
            hasStart=false;building=false;constructionPreview.Hide();
            using(new RoadBuildProbeScope("ActionRoadViews"))RenderRoads();
            RoadBuildProbeScope.Phase="nextFrame";yield return null;yield return null;
            for(int i=0;i<3;i++)
            {
                RoadBuildProbeScope.Phase="postEditStep"+i;
                using(new RoadBuildProbeScope("ActionFullStep"))session.Simulation.Advance(.05,1,double.PositiveInfinity);
                yield return null;frameRows.Add(Time.frameCount+","+RoadBuildProbeScope.Phase+","+(Time.unscaledDeltaTime*1000).ToString("R",System.Globalization.CultureInfo.InvariantCulture));
            }
            yield return null;RoadBuildProbeScope.Enabled=false;
            if(raw){UnityEngine.Profiling.Profiler.enabled=false;UnityEngine.Profiling.Profiler.enableBinaryLog=false;}
            RoadBuildProbeScope.Write(output);File.WriteAllLines(Path.Combine(output,"frames.csv"),frameRows);
            session.Validate();byte[] after=CityCoreSave.Write(session);var restored=CityCoreSave.Read(after);restored.Simulation.Growth.Zoning.SetSurface(CityScenarioBuilder.Flat);restored.Validate();
            bool roundtrip=after.SequenceEqual(CityCoreSave.Write(restored));
            File.WriteAllText(Path.Combine(output,"result.txt"),"population="+session.Entities.Members.Count+"\nactive="+world.ActiveCount+"\ninitialRoads="+initialRoads+"\nfinalRoads="+world.Network.RoadCount+"\ninitialRoadViews="+initialViews+"\nfinalRoadViews="+roads.Count+"\npreviewUnchanged="+previewUnchanged+"\nroundtrip="+roundtrip+"\npaid="+paid+"\nProgrammatic UI-call-path construction attribution in native Unity; not mouse acceptance or FPS benchmark.\n");
            if(!previewUnchanged||!roundtrip||world.Network.RoadCount!=initialRoads+1)throw new Exception("Construction probe invariant failed");
            Application.Quit(0);
        }
'''
s=s.replace('        void RenderRoads()',probe+'\n        void RenderRoads()')
p.write_text(s,encoding='utf-8')
helper=r'''
using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Unity.Profiling;
namespace HarborCity {
public readonly struct RoadBuildProbeScope:IDisposable {
 public static bool Enabled;public static string Phase="startup";
 static readonly Dictionary<string,ProfilerMarker> markers=new Dictionary<string,ProfilerMarker>();
 static readonly List<string> rows=new List<string>{"phase,name,ms"};
 readonly long start;readonly string phase,name;readonly bool enabled;readonly ProfilerMarker.AutoScope scope;
 public RoadBuildProbeScope(string name){enabled=Enabled;this.name=name;phase=Phase;start=enabled?Stopwatch.GetTimestamp():0;scope=default;if(enabled){if(!markers.TryGetValue(name,out var marker)){marker=new ProfilerMarker("City.RoadBuild."+name);markers.Add(name,marker);}scope=marker.Auto();}}
 public void Dispose(){if(!enabled)return;scope.Dispose();rows.Add(phase+","+name+","+((Stopwatch.GetTimestamp()-start)*1000.0/Stopwatch.Frequency).ToString("R",CultureInfo.InvariantCulture));}
 public static void Write(string output){File.WriteAllLines(Path.Combine(output,"scopes.csv"),rows);}
}}
'''
(root/'Assets/Scripts/Diagnostics/Runtime/RoadBuildProbeScope.cs').write_text(helper,encoding='utf-8')
print('Prepared isolated native diagnostics; product source unchanged')
