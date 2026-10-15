$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'ProjectLayout.ps1')
$root=Split-Path $PSScriptRoot -Parent
$mono='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/bin/mono.exe'
$csc='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/lib/mono/4.5/csc.exe'
$output=Join-Path $root 'Temp/CityScenarios/seeded-checks.exe'
& $mono $csc /nologo /langversion:latest /r:System.Web.Extensions.dll ('/out:'+$output) (Get-CityModelSources $root) (Join-Path $PSScriptRoot 'Diagnostics/CitySeededNetworkChecks.cs')
if($LASTEXITCODE -ne 0){throw 'Seeded checks compile failed'}
& $mono $output
if($LASTEXITCODE -ne 0){throw 'Seeded network equivalence failed'}
