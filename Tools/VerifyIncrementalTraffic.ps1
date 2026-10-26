param([string]$Results='Temp/IncrementalTraffic/checks',[int]$Steps=4000)
$ErrorActionPreference='Stop';$root=Split-Path $PSScriptRoot -Parent;. (Join-Path $PSScriptRoot 'ProjectLayout.ps1')
$out=Join-Path $root $Results;if(Test-Path -LiteralPath $out){throw 'Fresh output required'};New-Item -ItemType Directory -Path $out|Out-Null
$baseline='0bbd20f0a255a7055a6f0ad4a971a265e18531e7';$old=@{}
foreach($name in @('CityRoadCoreWorld.cs','CityRoadCoreLaneChanges.cs')){
 $path=Join-Path $out ($name+'.frozen.cs');$source=& git -C $root show ($baseline+':Assets/Scripts/Runtime/Traffic/'+$name);if($LASTEXITCODE -ne 0){throw 'Frozen source unavailable'};[IO.File]::WriteAllLines($path,$source);$old[$name]=$path
}
$empty=Join-Path $out 'Empty.cs';[IO.File]::WriteAllText($empty,'// Baseline has no incremental partial.');$old['CityRoadCoreOccupancy.cs']=$empty
$save=Join-Path (Split-Path $root -Parent) 'test/Temp/CityScenarios/regenerated30000-20261017/city.hcity'
foreach($version in @('new','old')){
 $dir=Join-Path $out $version;New-Item -ItemType Directory -Path $dir|Out-Null
 $overrides=@{};$defines='INCREMENTAL_INDEX_TEST';if($version -eq 'old'){$overrides=$old;$defines=''}
 $binary=Build-CityDotNetHost -ProjectRoot $root -OutputDirectory $dir -Entry (Join-Path $PSScriptRoot 'Diagnostics/CityIncrementalTrafficChecks.cs') -Defines $defines -SourceOverrides $overrides
 & dotnet $binary $save $dir $Steps | Tee-Object (Join-Path $dir 'Result.txt');if($LASTEXITCODE -ne 0){throw "$version traffic checks failed"}
}
if([IO.File]::ReadAllText((Join-Path $out 'old/states.csv')) -cne [IO.File]::ReadAllText((Join-Path $out 'new/states.csv'))){throw 'Old/new complete states differ'}
Write-Output 'PASS frozen/full vs incremental complete-state checkpoints identical'
