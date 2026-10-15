$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'ProjectLayout.ps1')
$root=Split-Path $PSScriptRoot -Parent
$mono='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/bin/mono.exe'
$compiler='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/lib/mono/4.5/csc.exe'
$output=Join-Path $root 'Temp/CityScenarios/RouteCacheChecks.exe'
New-Item -ItemType Directory -Force (Split-Path $output)|Out-Null
& $mono $compiler /nologo /langversion:latest ('/out:'+$output) (Get-CityModelSources $root) (Join-Path $root 'Tools/Diagnostics/CityRouteCacheChecks.cs')
if($LASTEXITCODE -ne 0){throw 'Route cache checks compile failed'}
& $mono $output
if($LASTEXITCODE -ne 0){throw 'Route cache checks failed'}
