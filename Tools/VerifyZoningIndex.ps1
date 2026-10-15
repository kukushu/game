param([string]$Initial='Temp/CityScenarios/grid500-normal-final/initial.hcity',[string]$Reference='737ee35')
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'ProjectLayout.ps1')
$root=Split-Path $PSScriptRoot -Parent
$dir=Join-Path $root 'Temp/CityScenarios/ZoningChecks';New-Item -ItemType Directory -Force $dir|Out-Null
$mono='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/bin/mono.exe'
$csc='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/lib/mono/4.5/csc.exe'
$sources=Get-CityModelSources $root
$harness=Join-Path $root 'Tools/Diagnostics/CityZoningEquivalenceChecks.cs'
& $mono $csc /nologo /langversion:latest ('/out:'+(Join-Path $dir 'after.exe')) $sources $harness
if($LASTEXITCODE -ne 0){throw 'Zoning checks compile failed'}
& $mono (Join-Path $dir 'after.exe') $Initial (Join-Path $dir 'after.bin')
if($LASTEXITCODE -ne 0){throw 'Zoning checks failed'}
$old=& git show ($Reference+':Assets/Scripts/Runtime/Infrastructure/Zoning/CityCoreZoning.cs')
if($LASTEXITCODE -ne 0){throw 'Reference zoning source unavailable'}
$referenceSource=Join-Path $dir 'CityCoreZoning.cs';[IO.File]::WriteAllLines($referenceSource,$old)
& $mono $csc /nologo /langversion:latest ('/out:'+(Join-Path $dir 'before.exe')) ($sources|Where-Object {$_ -notlike '*CityCoreZoning.cs'}) $referenceSource $harness
if($LASTEXITCODE -ne 0){throw 'Reference compile failed'}
& $mono (Join-Path $dir 'before.exe') $Initial (Join-Path $dir 'before.bin')
if($LASTEXITCODE -ne 0){throw 'Reference zoning checks failed'}
if((Get-FileHash (Join-Path $dir 'after.bin')).Hash -ne (Get-FileHash (Join-Path $dir 'before.bin')).Hash){throw 'Zoning cell geometry/use/occupancy changed'}
'PASS all zoning snapshots identical to Git reference; no legacy runtime fallback'
