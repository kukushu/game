param([string]$Initial='Temp/CityScenarios/challenge30000-traffic10000/threshold.hcity',[string]$Results='Temp/CityScenarios/DemandChecks',[string]$Reference='16d34ea')
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'ProjectLayout.ps1')
$root=Split-Path $PSScriptRoot -Parent
$dir=[IO.Path]::GetFullPath((Join-Path $root $Results));if(Test-Path -LiteralPath $dir){throw 'Choose fresh results'};New-Item -ItemType Directory $dir|Out-Null
$mono='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/bin/mono.exe';$csc='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/lib/mono/4.5/csc.exe'
$sources=Get-CityModelSources $root;$harness=Join-Path $root 'Tools/Diagnostics/CityDemandChecks.cs'
$old=& git show ($Reference+':Assets/Scripts/Runtime/Simulation/Growth/CityDevelopment.cs');if($LASTEXITCODE -ne 0){throw 'Reference unavailable'}
$referenceSource=Join-Path $dir 'CityDevelopment.cs';[IO.File]::WriteAllLines($referenceSource,$old)
foreach($version in @('before','after')){
 $exe=Join-Path $dir ($version+'.exe');$files=if($version -eq 'before'){@($sources|Where-Object{$_ -notlike '*CityDevelopment.cs'})+$referenceSource}else{$sources}
 & $mono $csc /nologo /langversion:latest ('/out:'+$exe) $files $harness
 if($LASTEXITCODE -ne 0){throw 'Demand verifier compile failed'}
 $output=Join-Path $dir $version
 $p=Start-Process $mono -ArgumentList ('"'+$exe+'"'),('"'+(Resolve-Path -LiteralPath $Initial).Path+'"'),('"'+$output+'"') -PassThru -WindowStyle Hidden -RedirectStandardOutput (Join-Path $dir ($version+'.log')) -RedirectStandardError (Join-Path $dir ($version+'.err'))
 $handle=$p.Handle;$timer=[Diagnostics.Stopwatch]::StartNew();$peak=0L;$stopped=$null
 while(-not $p.WaitForExit(1000)){$p.Refresh();if($p.HasExited){break};$peak=[Math]::Max($peak,$p.WorkingSet64);if($timer.Elapsed.TotalSeconds -gt 120){$stopped='time-limit'}elseif($p.WorkingSet64 -gt 4GB){$stopped='memory-limit'};if($stopped){$p.Kill($true);$p.WaitForExit();break}}
 $p.WaitForExit()
 @{wallSeconds=$timer.Elapsed.TotalSeconds;peakWorkingSetBytes=$peak;exitCode=$p.ExitCode;stopped=$stopped}|ConvertTo-Json|Set-Content (Join-Path $dir ($version+'-resources.json'))
 if($stopped -or $p.ExitCode -ne 0){Get-Content (Join-Path $dir ($version+'.err'));throw 'Bounded demand check failed'}
 Get-Content (Join-Path $dir ($version+'.log'))
}
if((Get-FileHash (Join-Path $dir 'before/continued.hcity')).Hash -ne (Get-FileHash (Join-Path $dir 'after/continued.hcity')).Hash){throw 'Full city state changed at same tick'}
'PASS complete saved facts identical after 240 same-city normal steps; original saves untouched'
