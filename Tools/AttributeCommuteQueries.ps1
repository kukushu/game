param([string]$Save='Temp/CityScenarios/challenge30000-traffic10000/threshold.hcity',[string]$Results='Temp/CityScenarios/CommuteAttribution',[int]$TimeoutSeconds=300,[string]$RoutingReference='')
$ErrorActionPreference='Stop';. (Join-Path $PSScriptRoot 'ProjectLayout.ps1');$root=Split-Path $PSScriptRoot -Parent
if(Test-Path -LiteralPath $Results){throw 'Fresh results required'};if($TimeoutSeconds -lt 10 -or $TimeoutSeconds -gt 600){throw 'Finite limits required'}
New-Item -ItemType Directory $Results|Out-Null;$target=[IO.Path]::GetFullPath((Join-Path $root $Results));$inputFile=(Resolve-Path -LiteralPath $Save).Path
$mono='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/bin/mono.exe';$csc='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/lib/mono/4.5/csc.exe';$exe=Join-Path $target 'commute.exe'
$sources=Get-CityModelSources $root
$resolvedReference=$null
if($RoutingReference){
 $resolvedReference=(& git rev-parse --verify ($RoutingReference+'^{commit}')).Trim();if($LASTEXITCODE -ne 0){throw 'Invalid reference'}
 foreach($name in @('CityRoadCoreNetwork.cs','CityRoadCoreAccessRouting.cs','CityRoadCoreDispatchRoutes.cs','CityRoadCoreWorld.cs')){
  $original=@($sources|Where-Object{[IO.Path]::GetFileName($_) -eq $name});if($original.Count -ne 1){throw 'Ambiguous reference source'}
  $relative=$original[0].Substring($root.Length+1).Replace('\','/');$content=& git show ($resolvedReference+':'+$relative);if($LASTEXITCODE -ne 0){throw 'Missing reference source'}
  $copy=Join-Path $target $name;[IO.File]::WriteAllLines($copy,$content);$sources=@($sources|Where-Object{$_ -ne $original[0]})+$copy
 }
}
& $mono $csc /nologo /langversion:latest /define:CITY_CORE_ATTRIBUTION ('/out:'+$exe) $sources (Join-Path $PSScriptRoot 'Diagnostics/CityCommuteAttribution.cs')
if($LASTEXITCODE -ne 0){throw 'Commute attribution compile failed'}
@{routingReference=$resolvedReference;dirty=@(& git status --porcelain);harnessHash=(Get-FileHash (Join-Path $PSScriptRoot 'Diagnostics/CityCommuteAttribution.cs')).Hash;commit=(& git rev-parse HEAD);inputHash=(Get-FileHash $inputFile).Hash;instrumentation='CITY_CORE_ATTRIBUTION observational callback; timings include observer overhead';maxMemoryGB=4;timeoutSeconds=$TimeoutSeconds}|ConvertTo-Json|Set-Content (Join-Path $target 'environment.json')
$p=Start-Process $mono -ArgumentList ('"'+$exe+'"'),('"'+$inputFile+'"'),('"'+$target+'"') -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $target 'stdout.log') -RedirectStandardError (Join-Path $target 'stderr.log');$handle=$p.Handle;$watch=[Diagnostics.Stopwatch]::StartNew();$peak=0L;$reason=$null;$last=0L;$lastProgress=0.0
while(-not $p.WaitForExit(1000)){$p.Refresh();if($p.HasExited){break};$peak=[Math]::Max($peak,$p.WorkingSet64);$length=(Get-Item (Join-Path $target 'stdout.log')).Length;if($length -ne $last){$last=$length;$lastProgress=$watch.Elapsed.TotalSeconds};if($watch.Elapsed.TotalSeconds -gt $TimeoutSeconds){$reason='wall-limit'}elseif($p.WorkingSet64 -gt 4GB){$reason='memory-limit'}elseif($watch.Elapsed.TotalSeconds-$lastProgress -gt 120){$reason='no-progress-limit'};if($reason){$p.Kill($true);$p.WaitForExit();break}}
$p.WaitForExit();@{exitCode=$p.ExitCode;stopped=$reason;wallSeconds=$watch.Elapsed.TotalSeconds;peakWorkingSetBytes=$peak}|ConvertTo-Json|Set-Content (Join-Path $target 'resources.json');Get-Content (Join-Path $target 'stdout.log')
if($reason -or $p.ExitCode -ne 0){Get-Content (Join-Path $target 'stderr.log');throw 'Attribution failed; evidence retained'}
