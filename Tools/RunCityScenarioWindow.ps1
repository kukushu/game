param([string]$Save='Temp/CityScenarios/grid500-normal-final/inflight.hcity',[string]$Results='Temp/CityScenarios/window500',[switch]$SkipBuild,[switch]$BuildOnly,[int]$TimeoutSeconds=180,[double]$MaxMemoryGB=8)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'ProjectLayout.ps1')
$root=Split-Path $PSScriptRoot -Parent
Set-Location -LiteralPath $root
if(Test-Path -LiteralPath $Results){throw 'Choose a fresh evidence directory'}
$inputFile=(Resolve-Path -LiteralPath $Save).Path
$resultsPath=[IO.Path]::GetFullPath((Join-Path $root $Results))
New-Item -ItemType Directory -Path $resultsPath|Out-Null
$project=Join-Path $root 'Temp/RoadCoreUnity';$player=Join-Path $root 'Temp/RoadCore/Player/HarborCity.exe'
if(-not $SkipBuild){
 Sync-CityWorkspaceProject $root $project
 $env:HARBOR_TRAFFIC_VALIDATION_PLAYER=$player;$env:HARBOR_CORE_ATTRIBUTION='0'
 $build=Start-Process 'C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Unity.exe' -ArgumentList '-batchmode','-quit','-projectPath',('"'+$project+'"'),'-executeMethod','HarborCity.CityTrafficBenchmarkBuild.BuildResearchValidation','-logFile',('"'+(Join-Path $resultsPath 'build.log')+'"') -WindowStyle Hidden -PassThru
 if(-not $build.WaitForExit(600000)){$build.Kill($true);$build.WaitForExit();throw "Unity build exceeded 600 seconds; build log preserved"};if($build.ExitCode -ne 0){throw 'Unity build failed'}
}
@{commit=(& git rev-parse HEAD);dirty=@(& git status --porcelain);inputPath=$inputFile;inputHash=(Get-FileHash -LiteralPath $inputFile).Hash;playerHash=(Get-FileHash -LiteralPath $player).Hash;cpu=(Get-CimInstance Win32_Processor|Select-Object -ExpandProperty Name);os=[Environment]::OSVersion.ToString();unity=(Get-Content ProjectSettings/ProjectVersion.txt);utc=[DateTime]::UtcNow.ToString('o');warmupSeconds=8;measureSeconds=60;resolution='1440x900';targetFps=60;vsync=0}|ConvertTo-Json -Depth 5|Set-Content (Join-Path $resultsPath 'environment.json')
if($BuildOnly){Write-Output "Unity Player built only; runtime/window validation was not performed";return}
foreach($rate in 1,3){
 $target=Join-Path $resultsPath ($rate.ToString()+'x')
 $run=Start-Process $player -ArgumentList '-harbor-city-scenario-window',('"'+$inputFile+'"'),('"'+$target+'"'),$rate,'-screen-width','1440','-screen-height','900','-logFile',('"'+(Join-Path $resultsPath ($rate.ToString()+'x.log'))+'"') -WindowStyle Normal -PassThru
 $watch=[Diagnostics.Stopwatch]::StartNew();$peak=0L;$stopped=$null
 while(-not $run.WaitForExit(1000)){$run.Refresh();try{$peak=[Math]::Max($peak,$run.WorkingSet64)}catch{if(-not $run.HasExited){throw}};if($watch.Elapsed.TotalSeconds -gt $TimeoutSeconds){$stopped='wall-time-limit'}elseif($peak -gt $MaxMemoryGB*1GB){$stopped='memory-limit'};if($stopped){$run.Kill($true);$run.WaitForExit();break}}
 @{rate=$rate;peakWorkingSetBytes=$peak;wallSeconds=$watch.Elapsed.TotalSeconds;terminatedBy=$stopped;timeoutSeconds=$TimeoutSeconds;maxMemoryGB=$MaxMemoryGB;note='OS sampled ~1s; initial and raw simulation files remain outside user saves'}|ConvertTo-Json|Set-Content (Join-Path $resultsPath ($rate.ToString()+'x-resources.json'))
 if($stopped){throw "Visible Player stopped: $stopped; evidence retained"};if($run.ExitCode -ne 0){throw 'Visible scenario failed'}
 Get-Content (Join-Path $target 'summary.csv')
}
