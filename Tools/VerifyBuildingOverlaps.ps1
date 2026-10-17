param([string]$Results='Temp/CityScenarios/BuildingOverlapChecks',[string]$Reference='9abf728',[string]$BuildingsSource='')
$ErrorActionPreference='Stop';. (Join-Path $PSScriptRoot 'ProjectLayout.ps1');$root=Split-Path $PSScriptRoot -Parent
if(Test-Path -LiteralPath $Results){throw 'Fresh results required'};New-Item -ItemType Directory $Results|Out-Null
$dir=[IO.Path]::GetFullPath((Join-Path $root $Results));$sources=Get-CityModelSources $root
$mono='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/bin/mono.exe';$csc='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/lib/mono/4.5/csc.exe'
$old=& git show ($Reference+':Assets/Scripts/Runtime/Simulation/Core/CityBuildings.cs');if($LASTEXITCODE -ne 0){throw 'Reference unavailable'}
$copy=Join-Path $dir 'Buildings.reference.cs';[IO.File]::WriteAllLines($copy,$old);$oldSources=@($sources|Where-Object{$_ -notlike '*CityBuildings.cs'})+$copy
if($BuildingsSource){$sources=@($sources|Where-Object{$_ -notlike '*CityBuildings.cs'})+(Resolve-Path -LiteralPath $BuildingsSource).Path}
foreach($label in @('reference','actual')){
 $files=if($label -eq 'reference'){$oldSources}else{$sources};$exe=Join-Path $dir ($label+'.exe');$answer=Join-Path $dir ($label+'.bin')
 & $mono $csc /nologo /langversion:latest ('/out:'+$exe) $files (Join-Path $PSScriptRoot 'Diagnostics/CityBuildingOverlapChecks.cs');if($LASTEXITCODE -ne 0){throw 'Compile failed'}
 $p=Start-Process $mono -ArgumentList ('"'+$exe+'"'),('"'+$answer+'"') -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $dir ($label+'.log')) -RedirectStandardError (Join-Path $dir ($label+'-error.log'));$handle=$p.Handle;$watch=[Diagnostics.Stopwatch]::StartNew();$peak=0L;$reason=$null
 while(-not $p.WaitForExit(1000)){$p.Refresh();if($p.HasExited){break};$peak=[Math]::Max($peak,$p.WorkingSet64);if($watch.Elapsed.TotalSeconds -gt 120){$reason='wall-limit'}elseif($p.WorkingSet64 -gt 4GB){$reason='memory-limit'};if($reason){$p.Kill($true);$p.WaitForExit();break}}
 $p.WaitForExit();@{exitCode=$p.ExitCode;terminatedBy=$reason;wallSeconds=$watch.Elapsed.TotalSeconds;peakWorkingSetBytes=$peak}|ConvertTo-Json|Set-Content (Join-Path $dir ($label+'-resources.json'));Get-Content (Join-Path $dir ($label+'.log'));if($reason -or $p.ExitCode -ne 0){Get-Content (Join-Path $dir ($label+'-error.log'));throw 'Projection check failed; evidence retained'}
}
$expected=(Get-FileHash (Join-Path $dir 'reference.bin')).Hash;$actual=(Get-FileHash (Join-Path $dir 'actual.bin')).Hash
@{reference=$Reference;referenceHash=$expected;actualHash=$actual;buildingsSource=$BuildingsSource;sourceHash=$(if($BuildingsSource){(Get-FileHash $BuildingsSource).Hash}else{(Get-FileHash (Join-Path $root 'Assets/Scripts/Runtime/Simulation/Core/CityBuildings.cs')).Hash})}|ConvertTo-Json|Set-Content (Join-Path $dir 'comparison.json')
if($expected -ne $actual){throw 'Rectangle overlap decision changed'}
'PASS all rectangle overlap decisions identical to frozen reference'
