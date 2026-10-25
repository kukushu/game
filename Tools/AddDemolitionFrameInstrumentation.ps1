param([Parameter(Mandatory)][string]$Project)
$ErrorActionPreference='Stop'
function ReadSource([string]$file){[IO.File]::ReadAllText((Join-Path $Project ('Assets/Scripts/Runtime/'+$file))).Replace("`r`n","`n")}
function WriteSource([string]$file,[string]$source){[IO.File]::WriteAllText((Join-Path $Project ('Assets/Scripts/Runtime/'+$file)),$source)}
function ReplaceOne([string]$source,[string]$old,[string]$next){if(([regex]::Matches($source,[regex]::Escape($old))).Count -ne 1){throw ('Nonunique phase anchor: '+$old)};return $source.Replace($old,$next)}
function Methods([string]$file,[hashtable]$methods){$s=ReadSource $file;foreach($signature in $methods.Keys){$pattern=[regex]::Escape($signature)+'\s*\{';if([regex]::Matches($s,$pattern).Count -ne 1){throw ('Missing frame method '+$signature)};$name=$methods[$signature];$s=[regex]::Replace($s,$pattern,{param($m)$m.Value+'using var frameDetailScope=global::HarborCity.DemolitionFrameCounters.Measure("'+$name+'");'})};WriteSource $file $s}
Methods 'Presentation/Game/HarborCityRoadCoreGame.cs' @{'void Update()'='Host.Update';'void OnGUI()'='Host.OnGUI';'void RenderRoads()'='Host.RenderRoads';'void RenderZones()'='Host.RenderZones';'void RenderBuildings()'='Host.RenderBuildings'}
Methods 'Traffic/CityRoadCoreWorld.cs' @{'public void RemoveRoad(int id)'='World.Remove';'internal bool TryCommit(RoadEditPlan plan)'='World.Commit';'public WorldData Snapshot()'='World.Snapshot';'static NetworkData CloneNetwork(NetworkData source)'='World.CloneNetwork';'public void Validate()'='World.Validate';'public void AssertSafety()'='World.Safety';'void Reindex()'='World.Reindex';'bool PlanStrandedResume(Vehicle v,out Remap remap)'='World.ResumePlan';'public int ResumeStrandedVehicles()'='World.ResumeBatch';'public bool RebindAccesses(IReadOnlyList<AccessBinding> bindings)'='World.Rebind';'World(WorldData state,SimulationClock clock,IEnumerable<int> endpoints,bool owned,Network prepared,bool draft=false)'='World.Candidate'}
Methods 'Infrastructure/Roads/CityRoadCoreNetwork.cs' @{'void Rebuild(Network prior)'='Network.Rebuild';'public void Project(Point point,double radius,bool includeConnections,List<SpatialHit> output)'='Network.Project';'internal List<long> RouteToEither(RoadAccess start,RoadAccess goal,RoadAccess other,out int selected)'='Network.Dijkstra'}
Methods 'Infrastructure/Roads/CityRoadCoreSpatial.cs' @{'public RoadSpatialIndex(Network network,RoadSpatialIndex prior=null)'='Spatial.Build';'public void Project(Point p,double radius,bool connections,List<SpatialHit> output)'='Spatial.Query'}
Methods 'Simulation/Services/CityCoreUtilities.cs' @{'public void Refresh()'='Utilities.Refresh'}
Methods 'Infrastructure/Utilities/CityUtilities.cs' @{'public void RecalculateUtilities()'='Utilities.Calculate'}
Methods 'Infrastructure/Utilities/CityUtilityLayout.cs' @{'UtilityLayout SupplyLayout()'='Utilities.Layout'}
Methods 'Infrastructure/Utilities/CityRoadUtilityConduits.cs' @{'internal CityRoadUtilityConduits(Network roads,CityUtilityNetwork manual)'='Utilities.RoadConduits'}
Methods 'Infrastructure/Zoning/CityCoreZoning.cs' @{'public IReadOnlyList<ZoneCell> Cells()'='Zoning.Cells';'void RefreshLandUse(IEnumerable<ZoneCell> targets,bool rebuildStamps)'='Zoning.LandUse';'void RefreshOccupancy(IEnumerable<ZoneCell> targets,bool rebuildIndex,bool geometry)'='Zoning.Occupancy'}
Methods 'Infrastructure/Zoning/CityCoreZoningGeometry.cs' @{'void RebuildGeometry()'='Zoning.Geometry'}
Methods 'Presentation/World/HarborCityZoningViews.cs' @{'public void Sync(IReadOnlyList<ZoneCell> cells,Func<float,float,float> height)'='Views.Zoning';'void SyncChanges(IReadOnlyList<ZoneCell> cells,Func<float,float,float> height)'='Views.ZoningChanges'}
Methods 'Presentation/World/HarborCityRoadViews.cs' @{'public void Sync(World world,Func<float,float,float> height)'='Views.Roads'}
Methods 'Presentation/World/HarborCityVehicleViews.cs' @{'public void Sync(World world)'='Views.Vehicles'}
function Phases([string]$file,[string]$start,[string]$end,[object[]]$anchors){
 $s=ReadSource $file;$a=$s.IndexOf($start);$b=$s.IndexOf($end,$a);if($a -lt 0 -or $b -lt 0){throw 'Missing phase method boundary'};$part=$s.Substring($a,$b-$a)
 $scopeEnd=$part.IndexOf(';',$part.IndexOf('using var frameDetailScope='));if($scopeEnd -lt 0){throw 'Missing outer measurement scope'};$part=$part.Insert($scopeEnd+1,'using var diagPhases=new global::HarborCity.DemolitionFrameCounters.Phases();')
 foreach($item in $anchors){$old=$item[0];$name=$item[1];$part=ReplaceOne $part $old ('diagPhases.Next("'+$name+'");'+$old)}
 WriteSource $file ($s.Substring(0,$a)+$part+$s.Substring($b))
}
Phases 'Infrastructure/Roads/CityRoadCoreNetwork.cs' 'void Rebuild(Network prior)' 'static bool Finite(Point p)' @(
 @('var junctions=new Dictionary<int,Junction>();','Network.Nodes'),@('var roads=new HashSet<int>();','Network.Lanes'),@('var approaches=new Dictionary<int,List<Point>>();','Network.Junctions'),@('var ports=new HashSet<int>(conflictJunctions);','Network.Ports'),@('foreach(var a in lanes)','Network.Connections'),@('connectionConflicts=new ConnectionConflicts(this);','Network.Conflicts'))
Phases 'Infrastructure/Roads/CityRoadCoreSpatial.cs' 'public RoadSpatialIndex(Network network,RoadSpatialIndex prior=null)' 'public void Project(Point p' @(
 @('HashSet<long> changed=null;','Spatial.CopyBuckets'),@('foreach(var road in network.Drivables)shapes.Add','Spatial.Shapes'),@('if(prior!=null)','Spatial.CompareShapes'),@('foreach(var bucket in prior.cells)','Spatial.ScanBuckets'),@("            foreach(var road in network.Drivables)`n            {`n                if(changed!=null",'Spatial.Segments'))
Phases 'Traffic/CityRoadCoreWorld.cs' 'public void RemoveRoad(int id)' 'public void Validate()' @(
 @('var n=CopyNetwork();','Remove.CopyAndBuild'),@('var remaps=new List<Remap>();','Remove.VehicleRoutes'),@('ValidateEditReservations(replacement,remaps);','Remove.Reservations'),@('var accessPlan=data.accesses.ConvertAll','Remove.Accesses'),@('new RoadEditPlan(this,n,replacement,remaps,interrupted,accessPlan,demolition:true).TryCommit();','Remove.Commit'))
Phases 'Traffic/CityRoadCoreWorld.cs' 'public WorldData Snapshot()' 'NetworkData CopyNetwork()' @(
 @('var copy=new WorldData','Snapshot.HeaderNetwork'),@('foreach(var access in data.accesses)','Snapshot.AccessDepots'),@('var tasks=new Dictionary<int,TripTask>();','Snapshot.Tasks'),@('foreach(var v in data.vehicles)','Snapshot.Actors'))
# Record counts once, avoiding a per-route-segment dictionary operation.
$file='Traffic/CityRoadCoreWorld.cs';$s=ReadSource $file;$a=$s.IndexOf('public void RemoveRoad(int id)');$b=$s.IndexOf('public void Validate()',$a);$part=$s.Substring($a,$b-$a)
$part=ReplaceOne $part 'var remaps=new List<Remap>();' 'int diagActors=0,diagRouteSegments=0;var remaps=new List<Remap>();'
$part=ReplaceOne $part "foreach(var v in data.vehicles)`n            {" "foreach(var v in data.vehicles)`n            {diagActors++;"
$old='for(int i=v.cursor;i<v.Plan.segments.Count;i++)if(!replacement.Contains(v.Plan.segments[i])){invalid=true;break;}'
$part=ReplaceOne $part $old 'for(int i=v.cursor;i<v.Plan.segments.Count;i++){diagRouteSegments++;if(!replacement.Contains(v.Plan.segments[i])){invalid=true;break;}}'
$part=ReplaceOne $part 'ValidateEditReservations(replacement,remaps);' 'global::HarborCity.DemolitionFrameCounters.Count("Remove.ActorVisits",diagActors);global::HarborCity.DemolitionFrameCounters.Count("Remove.RouteSegments",diagRouteSegments);global::HarborCity.DemolitionFrameCounters.Count("Remove.Remaps",remaps.Count);global::HarborCity.DemolitionFrameCounters.Count("Remove.Interruptions",interrupted.Count);ValidateEditReservations(replacement,remaps);'
WriteSource $file ($s.Substring(0,$a)+$part+$s.Substring($b))
Phases 'Presentation/World/HarborCityZoningViews.cs' 'public void Sync(IReadOnlyList<ZoneCell> cells' 'void SyncChanges(' @(
 @('var groups=new Dictionary<(int x,int z,int use),List<Plot>>();','ZoningView.Regroup'),@('var replacements=new Dictionary<(int x,int z,int use),Chunk>();','ZoningView.CompareAndBuild'),@('foreach(var pair in chunks)if(!groups.ContainsKey(pair.Key)','ZoningView.CommitAndRemember'))
Methods 'Presentation/World/HarborCityZoningViews.cs' @{'Chunk BuildChunk((int x,int z,int use) key,List<Plot> plots,Func<float,float,float> height)'='ZoningView.BuildChunk'}
$file='Presentation/World/HarborCityZoningViews.cs';$s=ReadSource $file
# The original Build sample encloses the phases. Dispose phases before its
# AutoScope ends; otherwise raw Profiler nesting is invalid despite valid clocks.
$decl='using var diagPhases=new global::HarborCity.DemolitionFrameCounters.Phases();'
$s=ReplaceOne $s $decl ''
$anchor="using(buildSample.Auto())`n            {"
$s=ReplaceOne $s $anchor ($anchor+$decl)
$s=ReplaceOne $s 'bool invalidate=surfaceDirty||!Equals(surface,height);' 'global::HarborCity.DemolitionFrameCounters.Count("ZoningView.CellsBefore",previous.Count);global::HarborCity.DemolitionFrameCounters.Count("ZoningView.CellsAfter",cells?.Count??0);bool invalidate=surfaceDirty||!Equals(surface,height);global::HarborCity.DemolitionFrameCounters.Count("ZoningView.SurfaceInvalid",invalidate?1:0);'
$s=ReplaceOne $s 'surface=height;surfaceDirty=false;RebuiltChunks=replacements.Count;PlotCount=count;' 'global::HarborCity.DemolitionFrameCounters.Count("ZoningView.TotalChunks",next.Count);global::HarborCity.DemolitionFrameCounters.Count("ZoningView.RebuiltChunks",replacements.Count);surface=height;surfaceDirty=false;RebuiltChunks=replacements.Count;PlotCount=count;'
WriteSource $file $s
Phases 'Infrastructure/Zoning/CityCoreZoningGeometry.cs' 'void RebuildGeometry()' "`n    }`n}" @(
 @('var pending=new SortedSet<int>();','ZoningGeometry.RoadChanges'),@('var now=new Dictionary<long,Drivable>','ZoningGeometry.DrivableChanges'),@('foreach(var run in removed){var old=run.accepted.ToArray();','ZoningGeometry.RemovedClaims'),@('foreach(int id in staticDirty)if(runs.TryGetValue','ZoningGeometry.LocalPlacement'),@('while(pending.Count>0)','ZoningGeometry.Resolve'),@('cells.Clear();foreach(int id in runs.Keys.OrderBy','ZoningGeometry.Flatten'))
$file='Infrastructure/Zoning/CityCoreZoningGeometry.cs';$s=ReadSource $file
$s=ReplaceOne $s 'cells.Clear();foreach(int id in runs.Keys.OrderBy(id=>id))cells.AddRange(runs[id].accepted);' 'cells.Clear();foreach(int id in runs.Keys.OrderBy(id=>id))cells.AddRange(runs[id].accepted);global::HarborCity.DemolitionFrameCounters.Count("ZoningGeometry.Full",full?1:0);global::HarborCity.DemolitionFrameCounters.Count("ZoningGeometry.Roads",LastGeometryRoads);global::HarborCity.DemolitionFrameCounters.Count("ZoningGeometry.Resolved",LastResolvedRoads);global::HarborCity.DemolitionFrameCounters.Count("ZoningGeometry.Cells",cells.Count);'
WriteSource $file $s
