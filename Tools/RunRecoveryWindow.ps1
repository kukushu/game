param([Parameter(Mandatory)][string]$Save,[Parameter(Mandatory)][string]$Results,[string]$Reference='',[int]$Road=87,[ValidateSet('in-place','demolition','demolition-repair','service-pickup','service-return','waste-loss','access-rebind','frame-breakdown')][string]$Scenario='in-place',[switch]$FrameRaw,[string]$ViewReference='',[switch]$CheckZoningViews,[string]$SpatialReference='',[switch]$TraceRecoveryRoutes,[string]$RoutingReference='')
if($Scenario -eq 'frame-breakdown' -and $Road -ne 87){throw 'The frame-breakdown probe is the frozen road-87 comparison case'}
if($FrameRaw -and $Scenario -ne 'frame-breakdown'){throw 'FrameRaw requires the frame-breakdown scenario'}
if(($ViewReference -or $CheckZoningViews) -and $Scenario -ne 'frame-breakdown'){throw 'View comparison requires the frame-breakdown scenario'}
$ErrorActionPreference='Stop';. (Join-Path $PSScriptRoot 'ProjectLayout.ps1');$root=Split-Path $PSScriptRoot -Parent
if(Test-Path $Results){throw 'Fresh result path required'};$result=[IO.Path]::GetFullPath((Join-Path $root $Results));New-Item -ItemType Directory $result|Out-Null
$project=Join-Path $root 'Temp/RoadCoreUnity';Sync-CityWorkspaceProject $root $project
if($RoutingReference){$nf=Join-Path $project 'Assets/Scripts/Runtime/Infrastructure/Roads/CityRoadCoreNetwork.cs';$ns=(& git show ($RoutingReference+':Assets/Scripts/Runtime/Infrastructure/Roads/CityRoadCoreNetwork.cs')) -join "`n";if($LASTEXITCODE -ne 0){throw 'Missing frozen routing source'};[IO.File]::WriteAllText($nf,$ns)}
if($SpatialReference){$spatialFile=Join-Path $project 'Assets/Scripts/Runtime/Infrastructure/Roads/CityRoadCoreSpatial.cs';$spatialSource=(& git show ($SpatialReference+':Assets/Scripts/Runtime/Infrastructure/Roads/CityRoadCoreSpatial.cs')) -join "`n";if($LASTEXITCODE -ne 0){throw 'Missing frozen spatial index'};[IO.File]::WriteAllText($spatialFile,$spatialSource)}
foreach($relative in @('Traffic/CityRoadCoreWorld.cs','Simulation/Core/CityCoreRoadBindings.cs')){
 $path=Join-Path $project ('Assets/Scripts/Runtime/'+$relative);if($Reference){$source=(& git show ($Reference+':Assets/Scripts/Runtime/'+$relative)) -join "`n";if($LASTEXITCODE -ne 0){throw 'Missing frozen source'}}else{$source=[IO.File]::ReadAllText($path)}
 $methods=if($relative.StartsWith('Traffic')){@{'public WorldData Snapshot()'='Snapshot';'World(WorldData state,SimulationClock clock,IEnumerable<int> endpoints,bool owned,Network prepared,bool draft=false)'='WorldCreate';'public bool TryResumeStranded(int id)'='Resume';'public void Validate()'='Validate';'public void AssertSafety()'='Safety';'void Reindex()'='Reindex';'public bool RebindAccesses(IReadOnlyList<AccessBinding> bindings)'='RebindAccesses'}}else{@{'public void RefreshIfChanged()'='Bindings'}}
 if($source.Contains('public int ResumeStrandedVehicles()')){$methods['public int ResumeStrandedVehicles()']='Bulk';$methods['bool PlanStrandedResume(Vehicle v,out Remap remap)']='Attempt'}
 foreach($signature in $methods.Keys){$pattern=[regex]::Escape($signature)+'\s*\{';$matches=[regex]::Matches($source,$pattern);if($matches.Count -ne 1){throw ('Instrumentation signature missing: '+$signature)};$label=$methods[$signature];$source=[regex]::Replace($source,$pattern,{param($m) $m.Value+'using var recoveryScope=global::HarborCity.RecoveryCounters.Measure("'+$label+'");'})}
 [IO.File]::WriteAllText($path,$source)
}
$networkPath=Join-Path $project 'Assets/Scripts/Runtime/Infrastructure/Roads/CityRoadCoreNetwork.cs'
$networkSource=[IO.File]::ReadAllText($networkPath);$pattern='void Rebuild\(Network prior\)\s*\{'
if([regex]::Matches($networkSource,$pattern).Count -ne 1){throw 'Network instrumentation signature missing'}
[IO.File]::WriteAllText($networkPath,([regex]::Replace($networkSource,$pattern,{param($m) $m.Value+'using var recoveryNetworkScope=global::HarborCity.RecoveryCounters.Measure("NetworkRebuild");'})))
foreach($spec in @(
 @{file='Infrastructure/Roads/CityRoadCoreAccessRouting.cs';methods=@{'public static List<long> FromLane(Network network,long lane,double distance,RoadAccess target,bool bothDirections,out double end)'='FromLane'}},
 @{file='Infrastructure/Roads/CityRoadCoreSpatial.cs';methods=@{'public RoadSpatialIndex(Network network,RoadSpatialIndex prior=null)'='SpatialBuild';'public void Project(Point p,double radius,bool connections,List<SpatialHit> output)'='SpatialQuery'}},
 @{file='Infrastructure/Roads/CityRoadCoreNetwork.cs';methods=@{'internal List<long> RouteToEither(RoadAccess start,RoadAccess goal,RoadAccess other,out int selected)'='Dijkstra';'public void Project(Point point,double radius,bool includeConnections,List<SpatialHit> output)'='Project'}})){
 $path=Join-Path $project ('Assets/Scripts/Runtime/'+$spec.file);if($Reference -and $spec.file.EndsWith('CityRoadCoreAccessRouting.cs')){$source=(& git show ($Reference+':Assets/Scripts/Runtime/'+$spec.file)) -join "`n";if($LASTEXITCODE -ne 0){throw 'Missing frozen access routing'}}else{$source=[IO.File]::ReadAllText($path)}
 foreach($signature in $spec.methods.Keys){$pattern=[regex]::Escape($signature)+'\s*\{';if([regex]::Matches($source,$pattern).Count -ne 1){throw ('Missing detail marker '+$signature)};$label=$spec.methods[$signature];$source=[regex]::Replace($source,$pattern,{param($m) $m.Value+'using var detailScope=global::HarborCity.RecoveryCounters.Measure("'+$label+'");'})}
 [IO.File]::WriteAllText($path,$source)
}
$hostPath=Join-Path $project 'Assets/Scripts/Runtime/Presentation/Game/HarborCityRoadCoreGame.cs';[IO.File]::WriteAllText($hostPath,([IO.File]::ReadAllText($hostPath).Replace('AddComponent<HarborCityScenarioWindowProbe>()','AddComponent<HarborCityRecoveryWindowProbe>()')))
Copy-Item (Join-Path $PSScriptRoot 'Diagnostics/HarborCityRecoveryWindowProbe.cs') (Join-Path $project 'Assets/Scripts/Diagnostics/Runtime/HarborCityRecoveryWindowProbe.cs')
if($Scenario -ne 'in-place'){
 if($Reference -and $Scenario -ne 'access-rebind'){throw 'This behaviour probe requires the new runtime'}
 [IO.File]::WriteAllText($hostPath,([IO.File]::ReadAllText($hostPath).Replace('AddComponent<HarborCityRecoveryWindowProbe>()','AddComponent<HarborCityDemolitionWindowProbe>()')))
 Copy-Item (Join-Path $PSScriptRoot 'Diagnostics/HarborCityDemolitionWindowProbe.cs') (Join-Path $project 'Assets/Scripts/Diagnostics/Runtime/HarborCityDemolitionWindowProbe.cs')
}
if($Scenario -eq 'frame-breakdown'){
 if($TraceRecoveryRoutes){
  Copy-Item (Join-Path $PSScriptRoot 'Diagnostics/RecoveryRouteTrace.cs') (Join-Path $project 'Assets/Scripts/Diagnostics/Runtime/RecoveryRouteTrace.cs')
 }

 if($ViewReference){$viewFile=Join-Path $project 'Assets/Scripts/Runtime/Presentation/World/HarborCityZoningViews.cs';$viewSource=(& git show ($ViewReference+':Assets/Scripts/Runtime/Presentation/World/HarborCityZoningViews.cs')) -join "`n";if($LASTEXITCODE -ne 0){throw 'Missing frozen zoning view'};[IO.File]::WriteAllText($viewFile,$viewSource)}
 & (Join-Path $PSScriptRoot 'AddDemolitionFrameInstrumentation.ps1') -Project $project
 [IO.File]::WriteAllText($hostPath,([IO.File]::ReadAllText($hostPath).Replace('AddComponent<HarborCityDemolitionWindowProbe>()','AddComponent<HarborCityDemolitionFrameProbe>()')))
 Copy-Item (Join-Path $PSScriptRoot 'Diagnostics/HarborCityDemolitionFrameProbe.cs') (Join-Path $project 'Assets/Scripts/Diagnostics/Runtime/HarborCityDemolitionFrameProbe.cs')
 if($TraceRecoveryRoutes){
  $wf=Join-Path $project 'Assets/Scripts/Runtime/Traffic/CityRoadCoreWorld.cs';$text=[IO.File]::ReadAllText($wf);$sig='bool PlanStrandedResume(Vehicle v,out Remap remap)';$text=[regex]::Replace($text,([regex]::Escape($sig)+'\s*\{'),{param($m)$m.Value+'using var recoveryOwner=global::HarborCity.RecoveryRouteTrace.Owner(v.id,(int)v.Task.purpose);'});[IO.File]::WriteAllText($wf,$text)
  $af=Join-Path $project 'Assets/Scripts/Runtime/Infrastructure/Roads/CityRoadCoreAccessRouting.cs';$text=[IO.File]::ReadAllText($af);$a=$text.IndexOf('public static List<long> FromLane(');$b=$text.IndexOf('static double Cost(',$a);$part=$text.Substring($a,$b-$a);$brace=$part.IndexOf('{');$part=$part.Insert($brace+1,'using var recoveryQuery=global::HarborCity.RecoveryRouteTrace.Begin(network,lane,distance,target,bothDirections);');$part=$part.Replace('return route;','return global::HarborCity.RecoveryRouteTrace.Result(recoveryQuery,route);').Replace('return network.Route(source,target);','return global::HarborCity.RecoveryRouteTrace.Result(recoveryQuery,network.Route(source,target));');[IO.File]::WriteAllText($af,($text.Substring(0,$a)+$part+$text.Substring($b)))
  $nf=Join-Path $project 'Assets/Scripts/Runtime/Infrastructure/Roads/CityRoadCoreNetwork.cs';$text=[IO.File]::ReadAllText($nf);$a=$text.IndexOf('internal List<long> RouteToEither(');$b=$text.IndexOf('internal double MinimumTravelSeconds(',$a);$part=$text.Substring($a,$b-$a);$part=$part.Replace('if(routeObservation!=null)routeObservation.expanded++;','if(routeObservation!=null)routeObservation.expanded++;global::HarborCity.RecoveryRouteTrace.Expand(item.id);').Replace('long link=ConnectionId(current.Id,lane.Id);','long link=ConnectionId(current.Id,lane.Id);global::HarborCity.RecoveryRouteTrace.Edge(false);').Replace('continue;double prefix=','continue;global::HarborCity.RecoveryRouteTrace.Edge(true);double prefix=');[IO.File]::WriteAllText($nf,($text.Substring(0,$a)+$part+$text.Substring($b)))
  $pf=Join-Path $project 'Assets/Scripts/Diagnostics/Runtime/HarborCityDemolitionFrameProbe.cs';$text=[IO.File]::ReadAllText($pf).Replace('DemolitionFrameCounters.Enabled=true;for','RecoveryRouteTrace.Reset();DemolitionFrameCounters.Enabled=true;for').Replace('DemolitionFrameCounters.Write(output);','DemolitionFrameCounters.Write(output);RecoveryRouteTrace.Write(output);');[IO.File]::WriteAllText($pf,$text)
 }
 if($CheckZoningViews){
  $frozen=(& git show '7b285b7:Assets/Scripts/Runtime/Presentation/World/HarborCityZoningViews.cs') -join "`n";if($LASTEXITCODE -ne 0){throw 'Missing frozen view oracle'}
  [IO.File]::WriteAllText((Join-Path $project 'Assets/Scripts/Diagnostics/Runtime/HarborCityZoningViewsReference.cs'),$frozen.Replace('HarborCityZoningViews','HarborCityZoningViewsReference'))
  Copy-Item (Join-Path $PSScriptRoot 'Diagnostics/HarborCityZoningViewDeltaChecks.cs') (Join-Path $project 'Assets/Scripts/Diagnostics/Runtime/HarborCityZoningViewDeltaChecks.cs')
 }
}
$player=Join-Path $result 'Player/HarborCity.exe';$env:HARBOR_PLAYER_RELEASE='0';$env:HARBOR_CORE_ATTRIBUTION='0';$env:HARBOR_TRAFFIC_VALIDATION_PLAYER=$player
$p=Start-Process 'C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Unity.exe' -ArgumentList '-batchmode','-quit','-projectPath',('"'+$project+'"'),'-executeMethod','HarborCity.CityPlayerBuild.BuildResearchValidation','-logFile',('"'+(Join-Path $result 'build.log')+'"') -WindowStyle Hidden -PassThru
if(-not $p.WaitForExit(600000)){$p.Kill($true);throw 'Build timeout'};if($p.ExitCode -ne 0){throw 'Build failed'}
$inputPath=(Resolve-Path $Save).Path
@{source=$(if($Reference){git rev-parse $Reference}else{git rev-parse HEAD});viewReference=$ViewReference;spatialReference=$SpatialReference;routingReference=$RoutingReference;traceRecoveryRoutes=$TraceRecoveryRoutes.IsPresent;dirty=@(git status --porcelain);input=$inputPath;inputHash=(Get-FileHash $inputPath).Hash;assemblyHash=(Get-FileHash (Join-Path $result 'Player/HarborCity_Data/Managed/Assembly-CSharp.dll')).Hash;road=$Road;note='Development instrumented scripted paused edit at exact saved tick; 8s display warmup; not ordinary-game FPS'}|ConvertTo-Json|Set-Content (Join-Path $result 'environment.json')
$frameExtra=if($FrameRaw){'-harbor-demolition-raw'}else{'-harbor-demolition-no-raw'}
$viewCheckFlag=if($CheckZoningViews){'-harbor-demolition-view-check'}else{'-harbor-demolition-no-view-check'}
$p=Start-Process $player -ArgumentList $frameExtra,$viewCheckFlag,'-harbor-city-scenario-window',('"'+$inputPath+'"'),('"'+(Join-Path $result 'window')+'"'),'0','-harbor-window-saved-map','-harbor-recovery-road',$Road,'-harbor-recovery-scenario',$Scenario,'-screen-width','1440','-screen-height','900','-logFile',('"'+(Join-Path $result 'player.log')+'"') -WindowStyle Normal -PassThru
if(-not $p.WaitForExit(240000)){$p.Kill($true);throw 'Player timeout'};if($p.ExitCode -ne 0){throw 'Player failed'}
Get-Content (Join-Path $result 'window/summary.csv');Get-Content (Join-Path $result 'window/Result.txt')
