param([string]$Results='Temp/CityScenarios/VehicleViewChecks')
$ErrorActionPreference='Stop';. (Join-Path $PSScriptRoot 'ProjectLayout.ps1');$root=Split-Path $PSScriptRoot -Parent
if(Test-Path -LiteralPath $Results){throw 'Fresh results required'};New-Item -ItemType Directory $Results|Out-Null;$target=[IO.Path]::GetFullPath((Join-Path $root $Results));$project=Join-Path $root 'Temp/RoadCoreUnity';$player=Join-Path $root 'Temp/ManualCityPlayer/HarborCity.exe'
if(@(Get-Process HarborCity -ErrorAction SilentlyContinue).Count){throw 'No overlapping Players allowed'}
Sync-CityWorkspaceProject $root $project
$extra=Join-Path $project 'Assets/Diagnostics/VehicleViewChecks';New-Item -ItemType Directory $extra -Force|Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Diagnostics/HarborCityVehicleViewValidation.cs') -Destination $extra
@{commit=(& git rev-parse HEAD);dirty=@(& git status --porcelain);fixture='Actual Unity display lifecycle on synthetic transport worlds, not a city performance test'}|ConvertTo-Json|Set-Content (Join-Path $target 'environment.json')
$env:HARBOR_TRAFFIC_VALIDATION_PLAYER=$player;$env:HARBOR_CORE_ATTRIBUTION='0'
$p=Start-Process 'C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Unity.exe' -ArgumentList '-batchmode','-quit','-projectPath',('"'+$project+'"'),'-executeMethod','HarborCity.CityTrafficBenchmarkBuild.BuildResearchValidation','-logFile',('"'+(Join-Path $target 'build.log')+'"') -WindowStyle Hidden -PassThru
if(-not $p.WaitForExit(600000)){$p.Kill($true);$p.WaitForExit();throw 'Bounded build timed out'};if($p.ExitCode -ne 0){throw 'Validation build failed'}
$p=Start-Process $player -ArgumentList '-batchmode','-nographics','-harbor-vehicle-view-check',('"'+$target+'"'),'-logFile',('"'+(Join-Path $target 'player.log')+'"') -WindowStyle Hidden -PassThru;$handle=$p.Handle;$watch=[Diagnostics.Stopwatch]::StartNew();$peak=0L;$reason=$null
while(-not $p.WaitForExit(1000)){$p.Refresh();if($p.HasExited){break};$peak=[Math]::Max($peak,$p.WorkingSet64);if($watch.Elapsed.TotalSeconds -gt 180){$reason='wall-limit'}elseif($p.WorkingSet64 -gt 4GB){$reason='memory-limit'};if($reason){$p.Kill($true);$p.WaitForExit();break}}
$p.WaitForExit();@{exitCode=$p.ExitCode;stopped=$reason;peakWorkingSetBytes=$peak;wallSeconds=$watch.Elapsed.TotalSeconds}|ConvertTo-Json|Set-Content (Join-Path $target 'resources.json');Get-Content (Join-Path $target 'Result.txt')
if($reason -or $p.ExitCode -ne 0){throw 'Native geometry checks failed; evidence retained'}
