param([string]$Results='Temp/ResidentReadReuse/replay',[int]$Steps=4000)
$ErrorActionPreference='Stop';$root=Split-Path $PSScriptRoot -Parent;. (Join-Path $PSScriptRoot 'ProjectLayout.ps1')
$out=Join-Path $root $Results;if(Test-Path -LiteralPath $out){throw 'Fresh output required'};New-Item -ItemType Directory -Path $out|Out-Null
$source='Assets/Scripts/Runtime/Simulation/Residents/CityCoreResidents.cs'
$baseline='2b261f3ed9f169d6de500827ed218f5839de8d8c'
$old=Join-Path $out 'CityCoreResidents.frozen.cs';$text=& git -C $root show ($baseline+':'+$source);if($LASTEXITCODE -ne 0){throw 'Frozen source unavailable'};[IO.File]::WriteAllLines($old,$text)
$save=Join-Path (Split-Path $root -Parent) 'test/Temp/CityScenarios/regenerated30000-20261017/city.hcity'
foreach($version in @('old','new')){
 $dir=Join-Path $out $version;New-Item -ItemType Directory -Path $dir|Out-Null
 $overrides=@{};if($version -eq 'old'){$overrides['CityCoreResidents.cs']=$old}
 $binary=Build-CityDotNetHost -ProjectRoot $root -OutputDirectory $dir -Entry (Join-Path $PSScriptRoot 'Diagnostics/CityResidentReadReplay.cs') -SourceOverrides $overrides
 & dotnet $binary $save $dir $Steps | Tee-Object (Join-Path $dir 'Result.txt');if($LASTEXITCODE -ne 0){throw "$version replay failed"}
}
$a=[IO.File]::ReadAllText((Join-Path $out 'old/states.csv'));$b=[IO.File]::ReadAllText((Join-Path $out 'new/states.csv'));if($a -cne $b){throw 'Frozen/current complete city states differ'}
Write-Output 'PASS frozen/current complete-state checkpoint equality; both run original simulation, not an alternate simulator'
