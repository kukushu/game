param([string]$Results='Temp/ScaleArchitecture/attendance-roster-checks')
$ErrorActionPreference='Stop';$root=Split-Path $PSScriptRoot -Parent;. (Join-Path $PSScriptRoot 'ProjectLayout.ps1')
$out=Join-Path $root $Results;if(Test-Path -LiteralPath $out){throw 'Fresh output required'};New-Item -ItemType Directory -Path $out|Out-Null
$frozen=@{};foreach($relative in @('Core/CityEntities.cs','Core/CityCoreSave.cs','Residents/CityResidents.cs','Industry/CityProductionSystem.cs')){
 $name=[IO.Path]::GetFileName($relative);$path=Join-Path $out $name;$source=& git -C $root show ('a3f7cea:Assets/Scripts/Runtime/Simulation/'+$relative);if($LASTEXITCODE -ne 0){throw 'Frozen source unavailable'};[IO.File]::WriteAllLines($path,$source);$frozen[$name]=$path
}
$save=Join-Path (Split-Path $root -Parent) 'test/Temp/CityScenarios/regenerated30000-20261017/city.hcity'
foreach($version in @('old','new')){
 $overrides=@{};$defines='ATTENDANCE_INDEX';if($version -eq 'old'){$overrides=$frozen;$defines=''}
 foreach($case in @('attendance','growth')){
  $dir=Join-Path $out ($version+'/'+$case);New-Item -ItemType Directory -Path $dir -Force|Out-Null
  $entry=Join-Path $PSScriptRoot $(if($case -eq 'attendance'){'Diagnostics/CityAttendanceRosterChecks.cs'}else{'Diagnostics/CityGrowthCandidateChecks.cs'})
  $binary=Build-CityDotNetHost -ProjectRoot $root -OutputDirectory $dir -Entry $entry -Defines $defines -SourceOverrides $overrides
  if($case -eq 'attendance'){& dotnet $binary $dir}else{& dotnet $binary $save $dir}
  if($LASTEXITCODE -ne 0){throw ($version+' '+$case+' failed')}
 }
}
foreach($relative in @('attendance/attendance.csv','growth/states.csv')){if([IO.File]::ReadAllText((Join-Path $out ('old/'+$relative))) -cne [IO.File]::ReadAllText((Join-Path $out ('new/'+$relative)))){throw ('Full state or wire schema differs: '+$relative)}}
if((Get-FileHash -LiteralPath (Join-Path $out 'old/attendance/attendance.hcity')).Hash -ne (Get-FileHash -LiteralPath (Join-Path $out 'new/attendance/attendance.hcity')).Hash){throw 'Saved city wire bytes differ'}
Write-Output 'PASS frozen full polling vs attendance index: exact city/save hashes, 126 growth/30k checkpoints, lifecycle writes and cold80'
