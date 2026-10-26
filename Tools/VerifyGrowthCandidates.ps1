param([string]$Results='Temp/GrowthCandidates/checks')
$ErrorActionPreference='Stop';$root=Split-Path $PSScriptRoot -Parent;. (Join-Path $PSScriptRoot 'ProjectLayout.ps1')
$out=Join-Path $root $Results;if(Test-Path -LiteralPath $out){throw 'Fresh output required'};New-Item -ItemType Directory -Path $out|Out-Null
$reference='24dee5141e4dc4224e4f86be0ec585c64ebb666d';$frozen=Join-Path $out 'CityCoreGrowth.reference.cs';$source=& git -C $root show ($reference+':Assets/Scripts/Runtime/Simulation/Growth/CityCoreGrowth.cs');if($LASTEXITCODE -ne 0){throw 'Frozen growth unavailable'};[IO.File]::WriteAllLines($frozen,$source)
$save=Join-Path (Split-Path $root -Parent) 'test/Temp/CityScenarios/regenerated30000-20261017/city.hcity'
foreach($version in @('new','old')){$dir=Join-Path $out $version;New-Item -ItemType Directory -Path $dir|Out-Null;$overrides=@{};if($version -eq 'old'){$overrides=@{'CityCoreGrowth.cs'=$frozen}};$binary=Build-CityDotNetHost -ProjectRoot $root -OutputDirectory $dir -Entry (Join-Path $PSScriptRoot 'Diagnostics/CityGrowthCandidateChecks.cs') -SourceOverrides $overrides
 & dotnet $binary $save $dir | Tee-Object (Join-Path $dir 'Result.txt');if($LASTEXITCODE -ne 0){throw "$version growth checks failed"}}
if([IO.File]::ReadAllText((Join-Path $out 'new/states.csv')) -cne [IO.File]::ReadAllText((Join-Path $out 'old/states.csv'))){throw 'Frozen/new complete state mismatch'}
Write-Output 'PASS frozen growth vs grouped candidates: all complete-state checkpoints identical'
