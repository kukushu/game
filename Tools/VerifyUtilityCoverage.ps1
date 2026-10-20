param([string]$Initial='Temp/CityScenarios/grid500-normal-final/initial.hcity')
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'ProjectLayout.ps1')
$root=Split-Path $PSScriptRoot -Parent
$dir=Join-Path $root 'Temp/CityScenarios/UtilityCoverageChecks';New-Item -ItemType Directory -Force $dir|Out-Null
$mono='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/bin/mono.exe'
$csc='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/lib/mono/4.5/csc.exe'
$sources=Get-CityModelSources $root;$harness=Join-Path $PSScriptRoot 'Diagnostics/CityUtilityCoverageChecks.cs'
& $mono $csc /nologo /langversion:latest ('/out:'+(Join-Path $dir 'after.exe')) $sources $harness
if($LASTEXITCODE -ne 0){throw 'Coverage compile failed'}
& $mono (Join-Path $dir 'after.exe') $Initial (Join-Path $dir 'after.csv') (Join-Path $dir 'after.hcity')
if($LASTEXITCODE -ne 0){throw 'Coverage checks failed'}
# Exact geometric coverage is checked against the direct scan inside the harness.
# Full frozen allocation/supply equivalence remains in VerifyUtilityLayout.ps1.
'PASS exact original distance-filtered coverage and dynamic utility invariants; full layout oracle is a separate retained suite'
