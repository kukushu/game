param([string]$Results='Temp/MillionScaleExecution/guard-scheduling',[switch]$ControlsOnly,[switch]$Growth,[switch]$Temporal,[switch]$TemporalControls)
$ErrorActionPreference='Stop'
if($TemporalControls){$ControlsOnly=$true;$Temporal=$true}
if($ControlsOnly -and $Growth){throw 'Choose controls or growth replay'}
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
foreach($version in $(if($Temporal -and !$ControlsOnly){@('old','guard','new')}else{@('old','new')})){
 $dir=Join-Path $out $version;New-Item -ItemType Directory -Path $dir|Out-Null
 $defines='';$overrides=@{'CityCoreResidents.cs'=$baseline}
 if($version -eq 'guard'){
  $defines='CITY_RESIDENT_GUARD_SCHEDULE'
  foreach($name in @('CityCoreResidents','CityResidentGuardSchedule')){$path=Join-Path $out ($name+'.guard.cs');$body=& git -C $root show ('45877170494a5fc99b82408c920b6460605f1b69:Assets/Scripts/Runtime/Simulation/Residents/'+$name+'.cs');if($LASTEXITCODE -ne 0){throw 'Frozen guard baseline missing'};[IO.File]::WriteAllLines($path,$body);$overrides[($name+'.cs')]=$path}
 }
 if($version -eq 'new'){$defines='CITY_RESIDENT_GUARD_SCHEDULE';if($Temporal){$defines+=';CITY_RESIDENT_TIME_SCHEDULE'};$overrides=@{}}
 $entry=if($TemporalControls){'CityResidentTemporalChecks.cs'}elseif($ControlsOnly){'CityResidentGuardSchedulingChecks.cs'}elseif($Growth){'CityGrowthCandidateChecks.cs'}else{'ResidentWakeReplay.cs'}
 $binary=Build-CityDotNetHost -ProjectRoot $root -OutputDirectory $dir -Entry (Join-Path $PSScriptRoot ('Diagnostics/'+$entry)) -Defines $defines -SourceOverrides $overrides
 if($ControlsOnly){& dotnet $binary $dir | Tee-Object (Join-Path $dir 'result.txt')}else{& dotnet $binary $save $dir | Tee-Object (Join-Path $dir 'result.txt')}
 if($LASTEXITCODE -ne 0){throw ($version+' guard execution failed')}
}
$names=if($ControlsOnly){@('controls.csv')}elseif($Growth){@('states.csv')}else{@('states.csv','requests.csv','final.hcity')}
foreach($name in $names){$reference=(Get-FileHash -LiteralPath (Join-Path $out ('old/'+$name))).Hash;foreach($variant in $(if($Temporal -and !$ControlsOnly){@('guard','new')}else{@('new')})){if($reference -ne (Get-FileHash -LiteralPath (Join-Path $out ($variant+'/'+$name))).Hash){throw ($variant+' execution differs: '+$name)}}}
Write-Output 'PASS frozen polling vs actual guard candidate execution: exact paired results'
