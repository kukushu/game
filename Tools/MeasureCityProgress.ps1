param([string]$Save,[string]$Results)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'ProjectLayout.ps1')
$root=Split-Path $PSScriptRoot -Parent
$mono='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/bin/mono.exe'
$csc='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/lib/mono/4.5/csc.exe'
if(Test-Path -LiteralPath $Results){throw 'Fresh results required'}
New-Item -ItemType Directory -Path $Results|Out-Null
$target=[IO.Path]::GetFullPath((Join-Path $root $Results));$source=(Resolve-Path -LiteralPath $Save).Path
$output=Join-Path $target 'progress.exe'
& $mono $csc /nologo /langversion:latest /r:System.Web.Extensions.dll ('/out:'+$output) (Get-CityModelSources $root) (Join-Path $PSScriptRoot 'Diagnostics/CityProgressProbe.cs')
if($LASTEXITCODE -ne 0){throw 'Progress probe compile failed'}
$run=Start-Process $mono -ArgumentList ('"'+$output+'"'),('"'+$source+'"'),('"'+(Join-Path $target 'progress.json')+'"'),('"'+(Join-Path $target 'continuation.hcity')+'"') -WindowStyle Hidden -RedirectStandardOutput (Join-Path $target 'stdout.log') -RedirectStandardError (Join-Path $target 'stderr.log') -PassThru
$timer=[Diagnostics.Stopwatch]::StartNew();$peak=0L;$reason=$null
while(-not $run.WaitForExit(1000)){$run.Refresh();if(-not $run.HasExited){$peak=[Math]::Max($peak,$run.WorkingSet64)};if($timer.Elapsed.TotalSeconds -gt 180){$reason='wall-time-limit'}elseif($peak -gt 8GB){$reason='memory-limit'};if($reason){$run.Kill($true);$run.WaitForExit();break}}
@{wallSeconds=$timer.Elapsed.TotalSeconds;peakWorkingSetBytes=$peak;terminatedBy=$reason;inputHash=(Get-FileHash -LiteralPath $source).Hash;note='Cold diagnostic 1200-step continuation, not a performance benchmark'}|ConvertTo-Json|Set-Content (Join-Path $target 'resources.json')
Get-Content (Join-Path $target 'stdout.log')
if($reason -or $run.ExitCode -ne 0){Get-Content (Join-Path $target 'stderr.log');throw 'Progress diagnosis failed or stopped; evidence retained'}
