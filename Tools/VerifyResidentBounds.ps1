param([string]$Initial='Temp/CityScenarios/grid500-normal-final/initial.hcity',[string]$Reference='a29d585')
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'ProjectLayout.ps1')
$root=Split-Path $PSScriptRoot -Parent
$dir=Join-Path $root 'Temp/CityScenarios/BoundsChecks';New-Item -ItemType Directory -Force $dir|Out-Null
$mono='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/bin/mono.exe'
$csc='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/lib/mono/4.5/csc.exe'
$sources=Get-CityModelSources $root
$harness=Join-Path $root 'Tools/Diagnostics/CityResidentBoundsChecks.cs'
& $mono $csc /nologo /langversion:latest ('/out:'+(Join-Path $dir 'after.exe')) $sources $harness
if($LASTEXITCODE -ne 0){throw 'Bounds checks compile failed'}
& $mono (Join-Path $dir 'after.exe') $Initial (Join-Path $dir 'after.hcity')
if($LASTEXITCODE -ne 0){throw 'Bounds checks failed'}
$old=& git show ($Reference+':Assets/Scripts/Runtime/Simulation/Residents/CityCorePopulation.cs')
if($LASTEXITCODE -ne 0){throw 'Reference population source unavailable'}
$referenceSource=Join-Path $dir 'CityCorePopulation.cs';[IO.File]::WriteAllLines($referenceSource,$old)
& $mono $csc /nologo /langversion:latest ('/out:'+(Join-Path $dir 'before.exe')) ($sources|Where-Object {$_ -notlike '*CityCorePopulation.cs'}) $referenceSource $harness
if($LASTEXITCODE -ne 0){throw 'Reference compile failed'}
& $mono (Join-Path $dir 'before.exe') $Initial (Join-Path $dir 'before.hcity')
if($LASTEXITCODE -ne 0){throw 'Reference checks failed'}
if((Get-FileHash (Join-Path $dir 'after.hcity')).Hash -ne (Get-FileHash (Join-Path $dir 'before.hcity')).Hash){throw 'First-step jobs and full state changed'}
'PASS first-step complete city/jobs identical to Git reference; no legacy runtime fallback'
