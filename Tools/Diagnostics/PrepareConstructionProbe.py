"""Extend the existing disposable native probe; no product instrumentation."""
from pathlib import Path
import re

root = Path.cwd() / 'Temp/RoadCoreUnity/Assets/Scripts/Runtime'

def scopes(relative, methods):
    path = root / relative
    source = path.read_text(encoding='utf-8-sig')
    for signature, label in methods:
        source, count = re.subn(re.escape(signature) + r'\s*\{',
            lambda m: m[0] + 'using var constructionProbeScope=new global::HarborCity.ZoningEditProbeScope("' + label + '");', source, count=1)
        if count != 1:
            raise RuntimeError(signature)
    path.write_text(source, encoding='utf-8')

world_source = (root / 'Traffic/CityRoadCoreWorld.cs').read_text(encoding='utf-8-sig')
constructor = ('World(WorldData state,SimulationClock clock,IEnumerable<int> endpoints,bool owned,Network prepared)'
    if 'bool owned,Network prepared' in world_source else 'public World(WorldData state,SimulationClock clock=null,IEnumerable<int> endpoints=null)')
scopes('Traffic/CityRoadCoreWorld.cs', [
    (constructor, 'World.Create'),
    ('public WorldData Snapshot()', 'World.Snapshot'),
    ('static NetworkData CloneNetwork(NetworkData source)', 'Network.Copy'),
    ('internal RoadEditPlan PlanFromDraft(World draft)', 'World.PlanFromDraft'),
    ('internal bool TryCommit(RoadEditPlan plan)', 'World.TryCommit'),
    ('void ValidateEditReservations(Network candidate,List<Remap> changes)', 'World.EditReservations'),
    ('public void Validate()', 'World.Validate'),
    ('public void AssertSafety()', 'World.AssertSafety'),
    ('void Reindex()', 'World.Reindex'),
])
scopes('Infrastructure/Roads/CityRoadCoreConstruction.cs', [
    ('public static RoadEditPlan Connect(World owner,Point[] center,bool oneWay,double speed,double snap,int lanes)', 'Road.Connect'),
    ('static int Attach(World draft,Point point,double radius)', 'Road.Attach'),
    ('static void JoinCoincidentRoads(World draft,int junction)', 'Road.JoinCoincident'),
])
network_source = (root / 'Infrastructure/Roads/CityRoadCoreNetwork.cs').read_text(encoding='utf-8-sig')
scopes('Infrastructure/Roads/CityRoadCoreNetwork.cs', [('void Rebuild(Network prior)' if 'void Rebuild(Network prior)' in network_source else 'void Rebuild()', 'Network.Rebuild')])
scopes('Simulation/Core/CityCoreConstruction.cs', [
    ('public int CheckRoadPlacement(RoadEditPlan plan)', 'Construction.Placement'),
    ('public bool Commit(RoadEditPlan plan,out int paid)', 'Construction.Commit'),
])

path = root / 'Traffic/CityRoadCoreWorld.cs'
source = path.read_text(encoding='utf-8')
# These scopes are non-overlapping subsets inside Snapshot; per-actor markers
# would themselves distort the allocation result.
source = source.replace('var tasks=new Dictionary<int,TripTask>();var originals=', 'using var actorsProbeScope=new global::HarborCity.ZoningEditProbeScope("Snapshot.TasksAndVehicles");var tasks=new Dictionary<int,TripTask>();var originals=')
path.write_text(source, encoding='utf-8')

path = root.parent / 'Diagnostics/Runtime/ZoningEditProbeScope.cs'
source = path.read_text(encoding='utf-8')
source = source.replace('public string Phase="setup";', 'public string Phase="setup";public HarborCityRoadCoreGame Host;')
source = source.replace('frame,operation,frame_ms', 'frame,operation,frame_ms,tick,active,advance_ms,structure_ms,vehicles_ms')
source = source.replace('(Time.unscaledDeltaTime*1000).ToString("R",CultureInfo.InvariantCulture));', '(Time.unscaledDeltaTime*1000).ToString("R",CultureInfo.InvariantCulture)+","+Host.ScenarioSession.Simulation.Tick+","+Host.ScenarioSession.Transport.ActiveCount+","+Host.ScenarioAdvanceMs.ToString("R",CultureInfo.InvariantCulture)+","+Host.ScenarioStructureMs.ToString("R",CultureInfo.InvariantCulture)+","+Host.ScenarioVehicleMs.ToString("R",CultureInfo.InvariantCulture));')
path.write_text(source, encoding='utf-8')

path = root / 'Presentation/Game/HarborCityRoadCoreGame.cs'
source = path.read_text(encoding='utf-8')
source = source.replace('var recorder=gameObject.AddComponent<ZoningEditFrameProbe>();', 'var recorder=gameObject.AddComponent<ZoningEditFrameProbe>();recorder.Host=this;')
source = source.replace('speed=0;ScenarioMeasuring=true;', 'bool running=Array.IndexOf(args,"-harbor-construction-running")>=0;speed=0;ScenarioMeasuring=true;')
source = source.replace('int ordinary=0;var profiles=', 'if(running){recorder.Phase="warmup";speed=1;double until=Time.realtimeSinceStartupAsDouble+8;while(Time.realtimeSinceStartupAsDouble<until)yield return null;recorder.Phase="normal";for(int i=0;i<60;i++)yield return null;}\n            int ordinary=0;var profiles=')
source = source.replace('byte[] beforeOperation=CityCoreSave.Write(session);yield return null;', 'byte[] beforeOperation=running?null:CityCoreSave.Write(session);yield return null;')
source = source.replace('if(failed){recorder.Phase="validation";', 'if(failed&&!running){recorder.Phase="validation";')
source = source.replace('ZoningEditProbeScope.Write(output);recorder.Write(output);', 'speed=0;yield return null;ZoningEditProbeScope.Write(output);recorder.Write(output);')
path.write_text(source, encoding='utf-8')
print('Prepared transaction stages and optional warmed, normally running host')
