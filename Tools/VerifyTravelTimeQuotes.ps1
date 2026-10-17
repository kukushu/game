param([string]$Results='Temp/CityScenarios/TravelTimeQuoteChecks',[string]$NetworkSource='',[string]$AccessSource='',[string]$Save='Temp/CityScenarios/challenge30000-traffic10000/threshold.hcity')
$ErrorActionPreference='Stop';. (Join-Path $PSScriptRoot 'ProjectLayout.ps1');$root=Split-Path $PSScriptRoot -Parent
if(Test-Path -LiteralPath $Results){throw 'Fresh results required'};New-Item -ItemType Directory $Results|Out-Null;$dir=[IO.Path]::GetFullPath((Join-Path $root $Results))
$sources=Get-CityModelSources $root
if($NetworkSource){$sources=@($sources|Where-Object{$_ -notlike '*CityRoadCoreNetwork.cs'})+(Resolve-Path -LiteralPath $NetworkSource).Path}
if($AccessSource){$sources=@($sources|Where-Object{$_ -notlike '*CityRoadCoreAccessRouting.cs'})+(Resolve-Path -LiteralPath $AccessSource).Path}
$mono='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/bin/mono.exe';$csc='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/lib/mono/4.5/csc.exe';$exe=Join-Path $dir 'quotes.exe'
& $mono $csc /nologo /langversion:latest ('/out:'+$exe) $sources (Join-Path $PSScriptRoot 'Diagnostics/CityTravelTimeQuoteChecks.cs');if($LASTEXITCODE -ne 0){throw 'Quote checks compile failed'}
$p=Start-Process $mono -ArgumentList ('"'+$exe+'"'),('"'+(Resolve-Path $Save).Path+'"') -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $dir 'stdout.log') -RedirectStandardError (Join-Path $dir 'stderr.log');$handle=$p.Handle;$watch=[Diagnostics.Stopwatch]::StartNew();$peak=0L;$reason=$null
while(-not $p.WaitForExit(1000)){$p.Refresh();if($p.HasExited){break};$peak=[Math]::Max($peak,$p.WorkingSet64);if($watch.Elapsed.TotalSeconds -gt 180){$reason='wall-limit'}elseif($p.WorkingSet64 -gt 4GB){$reason='memory-limit'};if($reason){$p.Kill($true);$p.WaitForExit();break}}
$p.WaitForExit();@{exitCode=$p.ExitCode;stopped=$reason;wallSeconds=$watch.Elapsed.TotalSeconds;peakWorkingSetBytes=$peak}|ConvertTo-Json|Set-Content (Join-Path $dir 'resources.json');Get-Content (Join-Path $dir 'stdout.log');if($reason -or $p.ExitCode -ne 0){Get-Content (Join-Path $dir 'stderr.log');throw 'Quote checks failed; evidence retained'}
