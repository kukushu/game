param([string]$Save='Temp/CityScenarios/grid500-normal-final/inflight.hcity',[string]$Results='Temp/CityScenarios/window500',[switch]$SkipBuild,[switch]$BuildOnly,[int]$TimeoutSeconds=180,[double]$MaxMemoryGB=8,[string]$PlayerPath='Temp/RoadCore/Player/HarborCity.exe',[ValidateSet(0,1,3,20,50)][int[]]$Rates=@(1,3),[switch]$RawProfile,[switch]$CheckRestore,[string]$NetworkReference='',[string]$RoutingReference='',[ValidateRange(15,150)][int]$RawProfileSeconds=30,[ValidateRange(5,120)][int]$RawFrameLimit=60,[long]$RawStartTick=0,[string]$HostSource='',[switch]$SharedWarmup,[ValidateRange(15,150)][int]$MeasureSeconds=60,[ValidateRange(1,64)][double]$BudgetMilliseconds=8,[ValidateRange(1,128)][int]$MaximumSteps=32,[switch]$CoreProfile,[switch]$SavedMap,[switch]$DepotJunctionRouting,[switch]$JunctionTrace,[switch]$DepotLaneReference,[switch]$RoutingEdit)
$ErrorActionPreference='Stop'
if($RoutingEdit -and -not $DepotJunctionRouting){throw 'RoutingEdit requires the junction experiment flag'}
if($RoutingReference -and ($DepotJunctionRouting -or $JunctionTrace)){throw 'Frozen routing reference cannot enable the current experiment'}
if($DepotLaneReference -and (-not $SkipBuild -or $DepotJunctionRouting)){throw 'Lane reference requires its prebuilt independent binary and SkipBuild; cannot combine algorithms'}
. (Join-Path $PSScriptRoot 'ProjectLayout.ps1')
$root=Split-Path $PSScriptRoot -Parent
Set-Location -LiteralPath $root
if(Test-Path -LiteralPath $Results){throw 'Choose a fresh evidence directory'}
$inputFile=(Resolve-Path -LiteralPath $Save).Path
$resultsPath=[IO.Path]::GetFullPath((Join-Path $root $Results))
New-Item -ItemType Directory -Path $resultsPath|Out-Null
$project=Join-Path $root 'Temp/RoadCoreUnity';$player=[IO.Path]::GetFullPath((Join-Path $root $PlayerPath))
$resolvedHostSource=$null;if($HostSource){if($SkipBuild){throw 'A diagnostic presentation source must be built'};$resolvedHostSource=(Resolve-Path -LiteralPath $HostSource).Path}
$resolvedNetworkReference=$null
$resolvedRoutingReference=$null
if($RoutingReference){
 if($NetworkReference -or $SkipBuild){throw 'Routing reference requires a build and cannot be mixed with a Network-only reference'}
 $resolvedRoutingReference=(& git rev-parse --verify ($RoutingReference+'^{commit}')).Trim()
 if($LASTEXITCODE -ne 0 -or $resolvedRoutingReference -notmatch '^[0-9a-f]{40}$'){throw 'Invalid routing reference'}
}
if($NetworkReference){
 if($SkipBuild){throw 'A diagnostic Network reference must be built, never just labelled on an existing binary'}
 $resolvedNetworkReference=(& git rev-parse --verify ($NetworkReference+'^{commit}')).Trim()
 if($LASTEXITCODE -ne 0 -or $resolvedNetworkReference -notmatch '^[0-9a-f]{40}$'){throw 'Invalid diagnostic Network reference'}
}
if(-not $SkipBuild){
 Sync-CityWorkspaceProject $root $project
 if($resolvedHostSource){Copy-Item -LiteralPath $resolvedHostSource -Destination (Join-Path $project 'Assets/Scripts/Runtime/Presentation/Game/HarborCityRoadCoreGame.cs')}
 if($resolvedRoutingReference){
  foreach($file in @('Assets/Scripts/Runtime/Infrastructure/Roads/CityRoadCoreNetwork.cs','Assets/Scripts/Runtime/Infrastructure/Roads/CityRoadCoreAccessRouting.cs','Assets/Scripts/Runtime/Traffic/CityRoadCoreDispatchRoutes.cs','Assets/Scripts/Runtime/Traffic/CityRoadCoreWorld.cs','Assets/Scripts/Runtime/Simulation/Services/CityCoreWaste.cs','Assets/Scripts/Diagnostics/Runtime/HarborCityScenarioWindowProbe.cs')){
   $referenceSource=& git show ($resolvedRoutingReference+':'+$file);if($LASTEXITCODE -ne 0){throw 'Routing reference source unavailable'}
   [IO.File]::WriteAllLines((Join-Path $project $file),$referenceSource)
  }
 }
 if($resolvedNetworkReference){
  $networkFile='Assets/Scripts/Runtime/Infrastructure/Roads/CityRoadCoreNetwork.cs'
  $referenceSource=& git show ($resolvedNetworkReference+':'+$networkFile)
  if($LASTEXITCODE -ne 0){throw 'Diagnostic Network source unavailable'}
  [IO.File]::WriteAllLines((Join-Path $project $networkFile),$referenceSource)
 }
 $env:HARBOR_TRAFFIC_VALIDATION_PLAYER=$player;$env:HARBOR_CORE_ATTRIBUTION=$(if($CoreProfile){'1'}else{'0'})
 $build=Start-Process 'C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Unity.exe' -ArgumentList '-batchmode','-quit','-projectPath',('"'+$project+'"'),'-executeMethod','HarborCity.CityPlayerBuild.BuildResearchValidation','-logFile',('"'+(Join-Path $resultsPath 'build.log')+'"') -WindowStyle Hidden -PassThru
 if(-not $build.WaitForExit(600000)){$build.Kill($true);$build.WaitForExit();throw "Unity build exceeded 600 seconds; build log preserved"};if($build.ExitCode -ne 0){throw 'Unity build failed'}
}
@{hostSource=$resolvedHostSource;hostSourceHash=$(if($resolvedHostSource){(Get-FileHash -LiteralPath $resolvedHostSource).Hash}else{$null});routingReference=$resolvedRoutingReference;networkReference=$resolvedNetworkReference;commit=(& git rev-parse HEAD);dirty=@(& git status --porcelain);inputPath=$inputFile;inputHash=(Get-FileHash -LiteralPath $inputFile).Hash;playerPath=$player;playerHash=(Get-FileHash -LiteralPath $player).Hash;managedHash=(Get-FileHash -LiteralPath (Join-Path (Split-Path $player) 'HarborCity_Data/Managed/Assembly-CSharp.dll')).Hash;cpu=(Get-CimInstance Win32_Processor|Select-Object -ExpandProperty Name);os=[Environment]::OSVersion.ToString();unity=(Get-Content ProjectSettings/ProjectVersion.txt);utc=[DateTime]::UtcNow.ToString('o');warmupSeconds=8;warmupSimulationSteps=$(if($SharedWarmup){200}else{0});budgetMilliseconds=$BudgetMilliseconds;maximumSteps=$MaximumSteps;coreProfile=$CoreProfile.IsPresent;savedMap=$SavedMap.IsPresent;depotJunctionRouting=$DepotJunctionRouting.IsPresent;junctionTrace=$JunctionTrace.IsPresent;depotLaneReference=$DepotLaneReference.IsPresent;routingEdit=$RoutingEdit.IsPresent;measureSeconds=$(if($RawProfile){$RawProfileSeconds}else{$MeasureSeconds});rawProfile=$RawProfile.IsPresent;rawFrameLimit=$RawFrameLimit;rawStartTick=$RawStartTick;checkRestore=$CheckRestore.IsPresent;rates=$Rates;resolution='1440x900';targetFps=60;vsync=0}|ConvertTo-Json -Depth 5|Set-Content (Join-Path $resultsPath 'environment.json')
if($BuildOnly){Write-Output "Unity Player built only; runtime/window validation was not performed";return}
foreach($rate in $Rates){
 foreach($existing in @(Get-Process -Name ([IO.Path]::GetFileNameWithoutExtension($player)) -ErrorAction SilentlyContinue)){
  if(-not $existing.HasExited -and $existing.Path -eq $player){throw "A Player at the same path is still running (PID $($existing.Id)); refuse overlapping window measurements"}
 }
 $target=Join-Path $resultsPath ($rate.ToString()+'x')
 $launch=@('-harbor-city-scenario-window',('"'+$inputFile+'"'),('"'+$target+'"'),$rate,'-harbor-window-seconds',$(if($RawProfile){$RawProfileSeconds}else{$MeasureSeconds}),'-harbor-window-budget-ms',$BudgetMilliseconds,'-harbor-window-max-steps',$MaximumSteps,'-screen-width','1440','-screen-height','900','-logFile',('"'+(Join-Path $resultsPath ($rate.ToString()+'x.log'))+'"'));if($RoutingEdit){$launch+='-harbor-window-routing-edit'};if($DepotLaneReference){$launch+='-harbor-depot-lane-routing'};if($DepotJunctionRouting){$launch+='-harbor-depot-junction-routing'};if($JunctionTrace){$launch+='-harbor-junction-trace'};if($SavedMap){$launch+='-harbor-window-saved-map'};if($SharedWarmup){$launch+='-harbor-window-shared-warmup'};if($CoreProfile){$launch+='-harbor-window-core-profile'};if($RawProfile){$launch+=@('-harbor-window-raw','-harbor-window-duration',$RawProfileSeconds,'-harbor-raw-frame-limit',$RawFrameLimit,'-harbor-raw-start-tick',$(if($rate -eq 0){0}else{$RawStartTick}))};if($CheckRestore){$launch+='-harbor-window-restore'}
 $run=Start-Process $player -ArgumentList $launch -WindowStyle Normal -PassThru
 $processHandle=$run.Handle;$watch=[Diagnostics.Stopwatch]::StartNew();$peak=0L;$stopped=$null
 while(-not $run.WaitForExit(1000)){$run.Refresh();try{$peak=[Math]::Max($peak,$run.WorkingSet64)}catch{if(-not $run.HasExited){throw}};if($watch.Elapsed.TotalSeconds -gt $TimeoutSeconds){$stopped='wall-time-limit'}elseif($peak -gt $MaxMemoryGB*1GB){$stopped='memory-limit'};if($stopped){$run.Kill($true);$run.WaitForExit();break}}
 $run.WaitForExit()
 @{rate=$rate;exitCode=$run.ExitCode;peakWorkingSetBytes=$peak;wallSeconds=$watch.Elapsed.TotalSeconds;terminatedBy=$stopped;timeoutSeconds=$TimeoutSeconds;maxMemoryGB=$MaxMemoryGB;note='OS sampled ~1s; initial and raw simulation files remain outside user saves'}|ConvertTo-Json|Set-Content (Join-Path $resultsPath ($rate.ToString()+'x-resources.json'))
 if($stopped){throw "Visible Player stopped: $stopped; evidence retained"};if($run.ExitCode -ne 0){throw 'Visible scenario failed'}
 if($RoutingEdit -and -not(Test-Path (Join-Path $target 'routing-edit.csv'))){throw 'Player did not execute the requested edit probe'}
 Get-Content (Join-Path $target 'summary.csv')
}
