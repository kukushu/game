param([string]$Results='Temp/RetailIndex/checks')
$ErrorActionPreference='Stop';$root=Split-Path $PSScriptRoot -Parent;. (Join-Path $PSScriptRoot 'ProjectLayout.ps1')
$out=[IO.Path]::GetFullPath((Join-Path $root $Results));if(Test-Path -LiteralPath $out){throw 'Fresh results required'};New-Item -ItemType Directory -Path $out|Out-Null
$reference='83ec624';$frozen=& git -C $root show ($reference+':Assets/Scripts/Runtime/Simulation/Economy/CityRetailIndex.cs');if($LASTEXITCODE -ne 0){throw 'Frozen retail source unavailable'}
$entry=Join-Path $out 'ChecksWithReference.cs';[IO.File]::WriteAllText($entry,(($frozen -join "`n").Replace('CityRetailIndex','CityRetailIndexFrozen'))+[Environment]::NewLine+[IO.File]::ReadAllText((Join-Path $PSScriptRoot 'Diagnostics/CityRetailIndexChecks.cs')))
$binary=Build-CityDotNetHost $root $out $entry;& dotnet $binary|Tee-Object (Join-Path $out 'dotnet.txt');if($LASTEXITCODE -ne 0){throw 'Retail .NET checks failed'}
$mono='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/bin/mono.exe';$csc='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/lib/mono/4.5/csc.exe';$sources=Get-CityModelSources $root;$exe=Join-Path $out 'checks.exe'
& $mono $csc /nologo /langversion:latest ('/out:'+$exe) $sources $entry;if($LASTEXITCODE -ne 0){throw 'Retail Mono compilation failed'}
& $mono $exe|Tee-Object (Join-Path $out 'mono.txt');if($LASTEXITCODE -ne 0){throw 'Retail Mono checks failed'}
