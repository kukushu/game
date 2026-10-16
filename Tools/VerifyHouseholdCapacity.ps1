$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'ProjectLayout.ps1')
$root=Split-Path $PSScriptRoot -Parent
$mono='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/bin/mono.exe'
$csc='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/lib/mono/4.5/csc.exe'
$folder=Join-Path $root 'Temp/CityScenarios/HouseholdCapacityChecks';New-Item -ItemType Directory -Path $folder -Force|Out-Null
$output=Join-Path $folder 'checks.exe'
& $mono $csc /nologo /langversion:latest ('/out:'+$output) (Get-CityModelSources $root) (Join-Path $root 'Tools/Diagnostics/CityHouseholdCapacityChecks.cs')
if($LASTEXITCODE -ne 0){throw 'Capacity checks compile failed'}
& $mono $output
if($LASTEXITCODE -ne 0){throw 'Capacity checks failed'}
