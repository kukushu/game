param([string]$Save,[string]$Results,[int]$Steps=3,[int]$TimeoutSeconds=600,[ValidateSet("Mono","DotNet64")][string]$Runtime="Mono",[int]$WarmupSteps=0,[string]$NetworkSource='',[switch]$InspectCommuteCache)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'ProjectLayout.ps1')
$root=Split-Path $PSScriptRoot -Parent
$mono='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/bin/mono.exe'
$csc='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/lib/mono/4.5/csc.exe'
if(Test-Path -LiteralPath $Results){throw 'Fresh results required'}
if($Steps -lt 1 -or $Steps -gt 20 -or $TimeoutSeconds -gt 1200 -or $TimeoutSeconds -lt 10 -or $WarmupSteps -lt 0 -or $WarmupSteps -gt 1000){throw 'Finite limits required'}
New-Item -ItemType Directory -Path $Results|Out-Null
$target=[IO.Path]::GetFullPath((Join-Path $root $Results));$source=(Resolve-Path -LiteralPath $Save).Path
$sources=Get-CityModelSources $root
if($NetworkSource){
 if($Runtime -ne 'Mono'){throw 'Temporary Network override is supported only by the Mono diagnostic host'}
 $sources=@($sources|Where-Object{$_ -notlike '*CityRoadCoreNetwork.cs'})+(Resolve-Path -LiteralPath $NetworkSource).Path
}
@{commit=(& git rev-parse HEAD);dirty=@(& git status --porcelain);inputHash=(Get-FileHash -LiteralPath $source).Hash;steps=$Steps;warmupSteps=$WarmupSteps;networkSource=$NetworkSource;inspectCommuteCache=$InspectCommuteCache.IsPresent;hostRuntime=$Runtime;probeHash=(Get-FileHash -LiteralPath (Join-Path $PSScriptRoot 'Diagnostics/CityStepProbe.cs')).Hash;sourceHashes=@($sources|ForEach-Object{@{path=$_.Substring($root.Length+1);hash=(Get-FileHash -LiteralPath $_).Hash}})}|ConvertTo-Json -Depth 5|Set-Content (Join-Path $target 'environment.json')
if($Runtime -eq 'DotNet64'){$output=Build-CityDotNetHost $root $target (Join-Path $PSScriptRoot 'Diagnostics/CityStepProbe.cs') 'CITY_CORE_ATTRIBUTION';$hostExecutable=(Get-Command dotnet).Source}else{
$output=Join-Path $target 'steps.exe'
& $mono $csc /nologo /langversion:latest /define:CITY_CORE_ATTRIBUTION ('/out:'+$output) $sources (Join-Path $PSScriptRoot 'Diagnostics/CityStepProbe.cs')
if($LASTEXITCODE -ne 0){throw 'Step probe compile failed'};$hostExecutable=$mono
}
$run=Start-Process $hostExecutable -ArgumentList ('"'+$output+'"'),('"'+$source+'"'),('"'+$target+'"'),$Steps,$WarmupSteps,$(if($InspectCommuteCache){1}else{0}) -WindowStyle Hidden -RedirectStandardOutput (Join-Path $target 'stdout.log') -RedirectStandardError (Join-Path $target 'stderr.log') -PassThru
$processHandle=$run.Handle;$timer=[Diagnostics.Stopwatch]::StartNew();$peak=0L;$reason=$null
while(-not $run.WaitForExit(1000)){$run.Refresh();if(-not $run.HasExited){$peak=[Math]::Max($peak,$run.WorkingSet64)};if($timer.Elapsed.TotalSeconds -gt $TimeoutSeconds){$reason='wall-time-limit'}elseif($peak -gt 8GB){$reason='memory-limit'};if($reason){$run.Kill($true);$run.WaitForExit();break}}
$run.WaitForExit()
@{exitCode=$run.ExitCode;wallSeconds=$timer.Elapsed.TotalSeconds;peakWorkingSetBytes=$peak;terminatedBy=$reason;timeoutSeconds=$TimeoutSeconds;note='Attributed few-step probe; not a normal build performance or complete-city pass'}|ConvertTo-Json|Set-Content (Join-Path $target 'resources.json')
Get-Content (Join-Path $target 'stdout.log')
if($reason -or $run.ExitCode -ne 0){Get-Content (Join-Path $target 'stderr.log');throw 'Step diagnosis failed/stopped; evidence retained'}
