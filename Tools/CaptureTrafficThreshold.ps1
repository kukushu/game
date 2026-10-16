param([string]$Save,[string]$Results,[int]$Target=10000,[int]$MaxSteps=4800,[int]$TimeoutSeconds=600)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'ProjectLayout.ps1')
$root=Split-Path $PSScriptRoot -Parent
$mono='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/bin/mono.exe'
$csc='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/lib/mono/4.5/csc.exe'
if($Target -lt 1 -or $Target -gt 100000 -or $MaxSteps -lt 1 -or $MaxSteps -gt 24000 -or $TimeoutSeconds -lt 10 -or $TimeoutSeconds -gt 1800){throw 'Finite capture limits required'}
if(Test-Path -LiteralPath $Results){throw 'Fresh results required'}
New-Item -ItemType Directory -Path $Results|Out-Null
$captureDirectory=[IO.Path]::GetFullPath((Join-Path $root $Results));$source=(Resolve-Path -LiteralPath $Save).Path
$sources=Get-CityModelSources $root
@{commit=(& git rev-parse HEAD);dirty=@(& git status --porcelain);inputHash=(Get-FileHash -LiteralPath $source).Hash;target=$Target;maxSteps=$MaxSteps;sourceHashes=@($sources|ForEach-Object{@{path=$_.Substring($root.Length+1);hash=(Get-FileHash -LiteralPath $_).Hash}})}|ConvertTo-Json -Depth 5|Set-Content (Join-Path $captureDirectory 'environment.json')
$output=Join-Path $captureDirectory 'capture.exe' 
& $mono $csc /nologo /langversion:latest /r:System.Web.Extensions.dll ('/out:'+$output) $sources (Join-Path $PSScriptRoot 'Diagnostics/CityTrafficThresholdCapture.cs')
if($LASTEXITCODE -ne 0){throw 'Capture compile failed'}
$arguments=@(('"'+$output+'"'),('"'+$source+'"'),('"'+(Join-Path $captureDirectory 'capture.json')+'"'),('"'+(Join-Path $captureDirectory 'threshold.hcity')+'"'),$Target.ToString(),$MaxSteps.ToString())
$run=Start-Process $mono -ArgumentList $arguments -WindowStyle Hidden -RedirectStandardOutput (Join-Path $captureDirectory 'stdout.log') -RedirectStandardError (Join-Path $captureDirectory 'stderr.log') -PassThru
$timer=[Diagnostics.Stopwatch]::StartNew();$peak=0L;$reason=$null
while(-not $run.WaitForExit(1000)){$run.Refresh();if(-not $run.HasExited){$peak=[Math]::Max($peak,$run.WorkingSet64)};if($timer.Elapsed.TotalSeconds -gt $TimeoutSeconds){$reason='wall-time-limit'}elseif($peak -gt 8GB){$reason='memory-limit'};if($reason){$run.Kill($true);$run.WaitForExit();break}}
@{wallSeconds=$timer.Elapsed.TotalSeconds;peakWorkingSetBytes=$peak;terminatedBy=$reason;inputHash=(Get-FileHash -LiteralPath $source).Hash;target=$Target;maxSteps=$MaxSteps;timeoutSeconds=$TimeoutSeconds;note='Normal saved-city continuation to an actual traffic threshold; not a performance benchmark'}|ConvertTo-Json|Set-Content (Join-Path $captureDirectory 'resources.json')
Get-Content (Join-Path $captureDirectory 'stdout.log')
if($reason -or $run.ExitCode -ne 0){Get-Content (Join-Path $captureDirectory 'stderr.log');throw 'Traffic capture failed or stopped; evidence retained'}
