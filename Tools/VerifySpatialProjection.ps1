param([string]$Results='Temp/CityScenarios/SpatialProjectionChecks',[string]$Reference='9abf728',[string]$Save='Temp/CityScenarios/challenge30000-traffic10000/threshold.hcity')
$ErrorActionPreference='Stop';. (Join-Path $PSScriptRoot 'ProjectLayout.ps1');$root=Split-Path $PSScriptRoot -Parent
if(Test-Path -LiteralPath $Results){throw 'Fresh results required'};New-Item -ItemType Directory $Results|Out-Null
$dir=[IO.Path]::GetFullPath((Join-Path $root $Results));$sources=Get-CityModelSources $root
$mono='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/bin/mono.exe';$csc='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/lib/mono/4.5/csc.exe'
$old=& git show ($Reference+':Assets/Scripts/Runtime/Infrastructure/Roads/CityRoadCoreSpatial.cs');if($LASTEXITCODE -ne 0){throw 'Reference unavailable'}
$copy=Join-Path $dir 'Spatial.reference.cs';[IO.File]::WriteAllLines($copy,$old);$oldSources=@($sources|Where-Object{$_ -notlike '*CityRoadCoreSpatial.cs'})+$copy
foreach($label in @('reference','actual')){
 $files=if($label -eq 'reference'){$oldSources}else{$sources};$exe=Join-Path $dir ($label+'.exe');$answer=Join-Path $dir ($label+'.bin')
 & $mono $csc /nologo /langversion:latest ('/out:'+$exe) $files (Join-Path $PSScriptRoot 'Diagnostics/CitySpatialProjectionChecks.cs');if($LASTEXITCODE -ne 0){throw 'Compile failed'}
 $p=Start-Process $mono -ArgumentList ('"'+$exe+'"'),('"'+(Resolve-Path $Save).Path+'"'),('"'+$answer+'"') -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $dir ($label+'.log')) -RedirectStandardError (Join-Path $dir ($label+'-error.log'));$handle=$p.Handle;$watch=[Diagnostics.Stopwatch]::StartNew();$peak=0L;$reason=$null
 while(-not $p.WaitForExit(1000)){$p.Refresh();if($p.HasExited){break};$peak=[Math]::Max($peak,$p.WorkingSet64);if($watch.Elapsed.TotalSeconds -gt 120){$reason='wall-limit'}elseif($p.WorkingSet64 -gt 4GB){$reason='memory-limit'};if($reason){$p.Kill($true);$p.WaitForExit();break}}
 $p.WaitForExit();@{exitCode=$p.ExitCode;terminatedBy=$reason;wallSeconds=$watch.Elapsed.TotalSeconds;peakWorkingSetBytes=$peak}|ConvertTo-Json|Set-Content (Join-Path $dir ($label+'-resources.json'));Get-Content (Join-Path $dir ($label+'.log'));if($reason -or $p.ExitCode -ne 0){Get-Content (Join-Path $dir ($label+'-error.log'));throw 'Projection check failed; evidence retained'}
}
$expected=(Get-FileHash (Join-Path $dir 'reference.bin')).Hash;$actual=(Get-FileHash (Join-Path $dir 'actual.bin')).Hash
@{reference=$Reference;referenceHash=$expected;actualHash=$actual;inputHash=(Get-FileHash $Save).Hash}|ConvertTo-Json|Set-Content (Join-Path $dir 'comparison.json')
if($expected -ne $actual){throw 'Spatial output changed (including order or double bits)'}
'PASS exact ordered spatial results identical to frozen reference'
