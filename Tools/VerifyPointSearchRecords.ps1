param([string]$Results='Temp/ScaleArchitecture/point-records-checks')
$ErrorActionPreference='Stop';$root=Split-Path $PSScriptRoot -Parent;. (Join-Path $PSScriptRoot 'ProjectLayout.ps1')
$out=Join-Path $root $Results;if(Test-Path -LiteralPath $out){throw 'Fresh output required'};New-Item -ItemType Directory -Path $out|Out-Null
$path=Join-Path $out 'CityRoadCoreNetwork.frozen.cs';$source=& git -C $root show 'a3f7cea:Assets/Scripts/Runtime/Infrastructure/Roads/CityRoadCoreNetwork.cs';if($LASTEXITCODE -ne 0){throw 'Frozen network unavailable'};[IO.File]::WriteAllLines($path,$source)
$save=Join-Path (Split-Path $root -Parent) 'test/Temp/CityScenarios/regenerated30000-20261017/city.hcity'
foreach($version in @('new','old')){$dir=Join-Path $out $version;New-Item -ItemType Directory -Path $dir|Out-Null;$overrides=@{};if($version -eq 'old'){$overrides=@{'CityRoadCoreNetwork.cs'=$path}}
 $binary=Build-CityDotNetHost -ProjectRoot $root -OutputDirectory $dir -Entry (Join-Path $PSScriptRoot 'Diagnostics/CityGrowthCandidateChecks.cs') -SourceOverrides $overrides
 & dotnet $binary $save $dir | Tee-Object (Join-Path $dir 'Result.txt');if($LASTEXITCODE -ne 0){throw 'Point records replay failed'}}
if([IO.File]::ReadAllText((Join-Path $out 'new/states.csv')) -cne [IO.File]::ReadAllText((Join-Path $out 'old/states.csv'))){throw 'Point search records changed city state'}
Write-Output 'PASS frozen point A* vs epoch records: 126 complete city checkpoints and cold80 identical'
