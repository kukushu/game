param([string]$Results='Temp/MillionScaleExecution/guard-checks')
$ErrorActionPreference='Stop';$root=Split-Path $PSScriptRoot -Parent;. (Join-Path $PSScriptRoot 'ProjectLayout.ps1')
$out=Join-Path $root $Results;if(Test-Path -LiteralPath $out){throw 'Fresh output required'};New-Item -ItemType Directory -Path $out|Out-Null
$frozen=@{};foreach($relative in @('Core/CityEntities.cs','Residents/CityResidents.cs','Residents/CityJobs.cs')){
 $name=[IO.Path]::GetFileName($relative);$path=Join-Path $out $name;$source=& git -C $root show ('93d97f2:Assets/Scripts/Runtime/Simulation/'+$relative);if($LASTEXITCODE -ne 0){throw 'Frozen source unavailable'};[IO.File]::WriteAllLines($path,$source);$frozen[$name]=$path
}
$save=Join-Path (Split-Path $root -Parent) 'test/Temp/CityScenarios/regenerated30000-20261017/city.hcity'
foreach($version in @('old','new')){
 $overrides=@{};$defines='REQUEST_GUARDS';if($version -eq 'old'){$overrides=$frozen;$defines=''}
 foreach($case in @('guards','growth')){
  $dir=Join-Path $out ($version+'/'+$case);New-Item -ItemType Directory -Path $dir -Force|Out-Null
  $entry=Join-Path $PSScriptRoot $(if($case -eq 'guards'){'Diagnostics/CityResidentGuardChecks.cs'}else{'Diagnostics/CityGrowthCandidateChecks.cs'})
  $binary=Build-CityDotNetHost -ProjectRoot $root -OutputDirectory $dir -Entry $entry -Defines $defines -SourceOverrides $overrides
  if($case -eq 'guards'){& dotnet $binary $dir}else{& dotnet $binary $save $dir}
  if($LASTEXITCODE -ne 0){throw ($version+' '+$case+' failed')}
 }
}
foreach($relative in @('guards/guards.csv','growth/states.csv')){if([IO.File]::ReadAllText((Join-Path $out ('old/'+$relative))) -cne [IO.File]::ReadAllText((Join-Path $out ('new/'+$relative)))){throw ('Full state differs: '+$relative)}}
if((Get-FileHash -LiteralPath (Join-Path $out 'old/guards/guards.hcity')).Hash -ne (Get-FileHash -LiteralPath (Join-Path $out 'new/guards/guards.hcity')).Hash){throw 'Saved city wire bytes differ'}
Write-Output 'PASS frozen 93d97f2 vs guarded facts: full state/wire, lifecycle bits, growth/30k checkpoints, cold continuation'
