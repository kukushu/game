param([string]$Results='Temp/MillionScaleExecution/guard-scheduling',[switch]$ControlsOnly,[switch]$Growth)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
. (Join-Path $PSScriptRoot 'ProjectLayout.ps1')
$out=Join-Path $root $Results
if(Test-Path -LiteralPath $out){throw 'Fresh output required'}
New-Item -ItemType Directory -Path $out|Out-Null
$baseline=Join-Path $out 'CityCoreResidents.baseline.cs'
$source=& git -C $root show 'a8430164a9d00d9642f2ea491018738b38393829:Assets/Scripts/Runtime/Simulation/Residents/CityCoreResidents.cs'
if($LASTEXITCODE -ne 0){throw 'Frozen baseline unavailable'}
[IO.File]::WriteAllLines($baseline,$source)
$raw=Join-Path (Split-Path $root -Parent) 'test/Temp/CityScenarios/regenerated30000-20261017/city.hcity'
$save=if($ControlsOnly){''}else{Export-CityMemberOrderFixture $root $raw $out}
foreach($version in @('old','new')){
 $dir=Join-Path $out $version;New-Item -ItemType Directory -Path $dir|Out-Null
 $defines='';$overrides=@{'CityCoreResidents.cs'=$baseline}
 if($version -eq 'new'){$defines='CITY_RESIDENT_GUARD_SCHEDULE';$overrides=@{}}
 $entry=if($ControlsOnly){'CityResidentGuardSchedulingChecks.cs'}elseif($Growth){'CityGrowthCandidateChecks.cs'}else{'ResidentWakeReplay.cs'}
 $binary=Build-CityDotNetHost -ProjectRoot $root -OutputDirectory $dir -Entry (Join-Path $PSScriptRoot ('Diagnostics/'+$entry)) -Defines $defines -SourceOverrides $overrides
 if($ControlsOnly){& dotnet $binary $dir | Tee-Object (Join-Path $dir 'result.txt')}else{& dotnet $binary $save $dir | Tee-Object (Join-Path $dir 'result.txt')}
 if($LASTEXITCODE -ne 0){throw ($version+' guard execution failed')}
}
$names=if($ControlsOnly){@('controls.csv')}elseif($Growth){@('states.csv')}else{@('states.csv','requests.csv','final.hcity')}
foreach($name in $names){if((Get-FileHash -LiteralPath (Join-Path $out ('old/'+$name))).Hash -ne (Get-FileHash -LiteralPath (Join-Path $out ('new/'+$name))).Hash){throw ('Guard execution differs: '+$name)}}
Write-Output 'PASS frozen polling vs actual guard candidate execution: exact paired results'
