param([string]$Results='Temp/CityScenarios/ZoneViewChecks',[string]$Reference='2542b35',[string]$ViewReference='7b285b7')
$ErrorActionPreference='Stop';. (Join-Path $PSScriptRoot 'ProjectLayout.ps1');$root=Split-Path $PSScriptRoot -Parent
if(Test-Path -LiteralPath $Results){throw 'Fresh results required'};New-Item -ItemType Directory $Results|Out-Null;$target=[IO.Path]::GetFullPath((Join-Path $root $Results));$project=Join-Path $root 'Temp/RoadCoreUnity';$player=Join-Path $root 'Temp/ManualCityPlayer/HarborCity.exe'
if(@(Get-Process HarborCity -ErrorAction SilentlyContinue).Count){throw 'No overlapping Players allowed'}
Sync-CityWorkspaceProject $root $project
$extra=Join-Path $project 'Assets/Diagnostics/ZoneViewChecks';New-Item -ItemType Directory $extra -Force|Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Diagnostics/HarborCityZoningBatchValidation.cs') -Destination $extra
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Diagnostics/HarborCityZoningViewDeltaChecks.cs') -Destination $extra
$oldView=(& git show ($ViewReference+':Assets/Scripts/Runtime/Presentation/World/HarborCityZoningViews.cs')) -join "`n";if($LASTEXITCODE -ne 0){throw 'Missing frozen view reference'}
[IO.File]::WriteAllText((Join-Path $extra 'HarborCityZoningViewsReference.cs'),$oldView.Replace('HarborCityZoningViews','HarborCityZoningViewsReference'))
$ref=(& git rev-parse --verify ($Reference+'^{commit}')).Trim();if($LASTEXITCODE -ne 0){throw 'Invalid reference'}
$old=& git show ($ref+':Assets/Scripts/Runtime/Presentation/World/HarborCitySurfaceMesh.cs');if($LASTEXITCODE -ne 0){throw 'Missing reference geometry'}
[IO.File]::WriteAllLines((Join-Path $extra 'HarborCitySurfaceMeshReference.cs'),@($old|ForEach-Object{$_.Replace('public static class HarborCitySurfaceMesh','public static class HarborCitySurfaceMeshReference')}))
@{reference=$ref;viewReference=(& git rev-parse $ViewReference);commit=(& git rev-parse HEAD);dirty=@(& git status --porcelain);fixture='Frozen original Plot and view sources, actual Unity Vector3/Quaternion/Mesh; not a city performance test'}|ConvertTo-Json|Set-Content (Join-Path $target 'environment.json')
$env:HARBOR_TRAFFIC_VALIDATION_PLAYER=$player;$env:HARBOR_CORE_ATTRIBUTION='0'
$p=Start-Process 'C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Unity.exe' -ArgumentList '-batchmode','-quit','-projectPath',('"'+$project+'"'),'-executeMethod','HarborCity.CityPlayerBuild.BuildResearchValidation','-logFile',('"'+(Join-Path $target 'build.log')+'"') -WindowStyle Hidden -PassThru
if(-not $p.WaitForExit(600000)){$p.Kill($true);$p.WaitForExit();throw 'Bounded build timed out'};if($p.ExitCode -ne 0){throw 'Validation build failed'}
$p=Start-Process $player -ArgumentList '-batchmode','-nographics','-harbor-zone-view-check',('"'+$target+'"'),'-logFile',('"'+(Join-Path $target 'player.log')+'"') -WindowStyle Hidden -PassThru;$handle=$p.Handle;$watch=[Diagnostics.Stopwatch]::StartNew();$peak=0L;$reason=$null
while(-not $p.WaitForExit(1000)){$p.Refresh();if($p.HasExited){break};$peak=[Math]::Max($peak,$p.WorkingSet64);if($watch.Elapsed.TotalSeconds -gt 180){$reason='wall-limit'}elseif($p.WorkingSet64 -gt 4GB){$reason='memory-limit'};if($reason){$p.Kill($true);$p.WaitForExit();break}}
$p.WaitForExit();@{exitCode=$p.ExitCode;stopped=$reason;peakWorkingSetBytes=$peak;wallSeconds=$watch.Elapsed.TotalSeconds}|ConvertTo-Json|Set-Content (Join-Path $target 'resources.json');Get-Content (Join-Path $target 'Result.txt')
if($reason -or $p.ExitCode -ne 0){throw 'Native geometry checks failed; evidence retained'}
