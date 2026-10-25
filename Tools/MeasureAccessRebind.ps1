param([Parameter(Mandatory)][string]$Save,[Parameter(Mandatory)][string]$Results,[string]$Reference='')
$ErrorActionPreference='Stop';. (Join-Path $PSScriptRoot 'ProjectLayout.ps1');$root=Split-Path $PSScriptRoot -Parent
$output=Join-Path $root $Results;if(Test-Path $output){throw 'Fresh result directory required'};New-Item -ItemType Directory $output|Out-Null
$overrides=@{}
foreach($spec in @(
 @{file='Traffic/CityRoadCoreWorld.cs';methods=@{'public WorldData Snapshot()'='Snapshot';'World(WorldData state,SimulationClock clock,IEnumerable<int> endpoints,bool owned,Network prepared,bool draft=false)'='WorldCreate';'public bool RebindAccesses(IReadOnlyList<AccessBinding> bindings)'='RebindAccesses';'public void Validate()'='Validate';'public void AssertSafety()'='Safety';'void Reindex()'='Reindex';'public int ResumeStrandedVehicles()'='Bulk'}},
 @{file='Infrastructure/Roads/CityRoadCoreNetwork.cs';methods=@{'void Rebuild(Network prior)'='NetworkRebuild';'internal List<long> RouteToEither(RoadAccess start,RoadAccess goal,RoadAccess other,out int selected)'='Dijkstra';'public void Project(Point point,double radius,bool includeConnections,List<SpatialHit> output)'='Project'}},
 @{file='Infrastructure/Roads/CityRoadCoreAccessRouting.cs';methods=@{'public static List<long> FromLane(Network network,long lane,double distance,RoadAccess target,bool bothDirections,out double end)'='FromLane'}},
 @{file='Infrastructure/Roads/CityRoadCoreSpatial.cs';methods=@{'public RoadSpatialIndex(Network network,RoadSpatialIndex prior=null)'='SpatialBuild';'public void Project(Point p,double radius,bool connections,List<SpatialHit> output)'='SpatialQuery'}},
 @{file='Simulation/Core/CityCoreRoadBindings.cs';methods=@{'public void RefreshIfChanged()'='Bindings'}})){
 $path='Assets/Scripts/Runtime/'+$spec.file
 if($Reference -and ($spec.file.EndsWith('CityRoadCoreWorld.cs') -or $spec.file.EndsWith('CityRoadCoreAccessRouting.cs'))){$source=(& git show ($Reference+':'+$path)) -join "`n";if($LASTEXITCODE -ne 0){throw 'Missing frozen World'}}else{$source=[IO.File]::ReadAllText((Join-Path $root $path))}
 foreach($signature in $spec.methods.Keys){$pattern=[regex]::Escape($signature)+'\s*\{';if([regex]::Matches($source,$pattern).Count -ne 1){throw ('Missing instrumentation signature '+$signature)};$label=$spec.methods[$signature];$source=[regex]::Replace($source,$pattern,{param($m)$m.Value+'using var rebindScope=global::RebindMeters.Measure("'+$label+'");'})}
 $target=Join-Path $output ([IO.Path]::GetFileName($path));[IO.File]::WriteAllText($target,$source);$overrides[[IO.Path]::GetFileName($path)]=$target
}
Build-CityDotNetHost $root $output (Join-Path $PSScriptRoot 'Diagnostics/CityAccessRebindMeasure.cs') -SourceOverrides $overrides
& dotnet (Join-Path $output 'bin/Release/net10.0/CityModelHost.dll') (Resolve-Path -LiteralPath $Save).Path (Join-Path $output 'results');if($LASTEXITCODE -ne 0){throw 'Rebind measurement failed'}
