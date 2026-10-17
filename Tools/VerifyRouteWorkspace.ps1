param([string]$Results='Temp/CityScenarios/RouteWorkspaceChecks',[string]$Reference='559a5df',[string]$NetworkSource='',[string]$AccessSource='')
$ErrorActionPreference='Stop';. (Join-Path $PSScriptRoot 'ProjectLayout.ps1');$root=Split-Path $PSScriptRoot -Parent
if(Test-Path -LiteralPath $Results){throw 'Fresh results required'};New-Item -ItemType Directory $Results|Out-Null
$dir=[IO.Path]::GetFullPath((Join-Path $root $Results));$baseSources=Get-CityModelSources $root;$sources=$baseSources;$oldSources=$baseSources
$mono='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/bin/mono.exe';$csc='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/lib/mono/4.5/csc.exe'
foreach($name in @('CityRoadCoreNetwork.cs','CityRoadCoreAccessRouting.cs')){
 $original=@($baseSources|Where-Object{[IO.Path]::GetFileName($_) -eq $name});if($original.Count -ne 1){throw 'Ambiguous source'}
 $relative=$original[0].Substring($root.Length+1).Replace('\','/');$old=& git show ($Reference+':'+$relative);if($LASTEXITCODE -ne 0){throw 'Reference unavailable; choose a reference supporting current query API'}
 $copy=Join-Path $dir ($name+'.reference.cs');[IO.File]::WriteAllLines($copy,$old);$oldSources=@($oldSources|Where-Object{$_ -ne $original[0]})+$copy
}
if($NetworkSource){$sources=@($sources|Where-Object{$_ -notlike '*CityRoadCoreNetwork.cs'})+(Resolve-Path $NetworkSource).Path}
if($AccessSource){$sources=@($sources|Where-Object{$_ -notlike '*CityRoadCoreAccessRouting.cs'})+(Resolve-Path $AccessSource).Path}
foreach($version in @('before','after')){
 $files=if($version -eq 'before'){$oldSources}else{$sources};$exe=Join-Path $dir ($version+'.exe')
 & $mono $csc /nologo /langversion:latest ('/out:'+$exe) $files (Join-Path $PSScriptRoot 'Diagnostics/CityRouteWorkspaceChecks.cs');if($LASTEXITCODE -ne 0){throw 'Route compile failed'}
 $p=Start-Process $mono -ArgumentList ('"'+$exe+'"'),('"'+(Join-Path $dir ($version+'.bin'))+'"') -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $dir ($version+'.log')) -RedirectStandardError (Join-Path $dir ($version+'-error.log'));$handle=$p.Handle;$watch=[Diagnostics.Stopwatch]::StartNew();$peak=0L;$reason=$null
 while(-not $p.WaitForExit(1000)){$p.Refresh();if($p.HasExited){break};$peak=[Math]::Max($peak,$p.WorkingSet64);if($watch.Elapsed.TotalSeconds -gt 90){$reason='wall-limit'}elseif($peak -gt 2GB){$reason='memory-limit'};if($reason){$p.Kill($true);$p.WaitForExit();break}}
 $p.WaitForExit();@{exitCode=$p.ExitCode;stopped=$reason;wallSeconds=$watch.Elapsed.TotalSeconds;peakWorkingSetBytes=$peak}|ConvertTo-Json|Set-Content (Join-Path $dir ($version+'-resources.json'));Get-Content (Join-Path $dir ($version+'.log'));if($reason -or $p.ExitCode -ne 0){throw 'Route correctness failed; evidence retained'}
}
$before=(Get-FileHash (Join-Path $dir 'before.bin')).Hash;$after=(Get-FileHash (Join-Path $dir 'after.bin')).Hash
@{reference=$Reference;before=$before;after=$after;networkSource=$NetworkSource;accessSource=$AccessSource;note='600 exact single-goal route answers and retained result independence; no old runtime fallback'}|ConvertTo-Json|Set-Content (Join-Path $dir 'comparison.json')
if($before -ne $after){throw 'Exact route sequence changed'}
'PASS all 600 directed route answers identical to Git reference; no old runtime engine'
