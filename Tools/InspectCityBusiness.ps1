param([string]$Save,[string]$Results,[int]$TimeoutSeconds=180)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'ProjectLayout.ps1')
$root=Split-Path $PSScriptRoot -Parent
$mono='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/bin/mono.exe'
$csc='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/lib/mono/4.5/csc.exe'
if($TimeoutSeconds -lt 10 -or $TimeoutSeconds -gt 600){throw 'Finite capture limits required'}
if(Test-Path -LiteralPath $Results){throw 'Fresh results required'}
New-Item -ItemType Directory -Path $Results|Out-Null
$captureDirectory=[IO.Path]::GetFullPath((Join-Path $root $Results));$source=(Resolve-Path -LiteralPath $Save).Path
$output=Build-CityDotNetHost $root $captureDirectory (Join-Path $PSScriptRoot 'Diagnostics/CityBusinessHealthProbe.cs')
$mono=(Get-Command dotnet).Source
$arguments=@(('"'+$output+'"'),('"'+$source+'"'),('"'+(Join-Path $captureDirectory 'health.json')+'"'))
$run=Start-Process $mono -ArgumentList $arguments -WindowStyle Hidden -RedirectStandardOutput (Join-Path $captureDirectory 'stdout.log') -RedirectStandardError (Join-Path $captureDirectory 'stderr.log') -PassThru
$timer=[Diagnostics.Stopwatch]::StartNew();$peak=0L;$reason=$null
while(-not $run.WaitForExit(1000)){$run.Refresh();if(-not $run.HasExited){$peak=[Math]::Max($peak,$run.WorkingSet64)};if($timer.Elapsed.TotalSeconds -gt $TimeoutSeconds){$reason='wall-time-limit'}elseif($peak -gt 8GB){$reason='memory-limit'};if($reason){$run.Kill($true);$run.WaitForExit();break}}
@{wallSeconds=$timer.Elapsed.TotalSeconds;peakWorkingSetBytes=$peak;terminatedBy=$reason;inputHash=(Get-FileHash -LiteralPath $source).Hash;timeoutSeconds=$TimeoutSeconds;note='Cold read-only business inspection; no CitySimulation.Advance'}|ConvertTo-Json|Set-Content (Join-Path $captureDirectory 'resources.json')
Get-Content (Join-Path $captureDirectory 'stdout.log')
if($reason -or $run.ExitCode -ne 0){Get-Content (Join-Path $captureDirectory 'stderr.log');throw 'Business inspection failed or stopped; evidence retained'}
