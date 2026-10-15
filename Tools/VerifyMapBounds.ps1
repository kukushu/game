$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'ProjectLayout.ps1')
$root=Split-Path $PSScriptRoot -Parent
$mono='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/bin/mono.exe'
$csc='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/lib/mono/4.5/csc.exe'
$dir=Join-Path $root 'Temp/CityScenarios/MapChecks';New-Item -ItemType Directory -Force $dir|Out-Null
$output=Join-Path $dir 'checks.exe'
& $mono $csc /nologo /langversion:latest ('/out:'+$output) (Get-CityModelSources $root) (Join-Path $PSScriptRoot 'Diagnostics/CityMapBoundsChecks.cs')
if($LASTEXITCODE -ne 0){throw 'Map checks compile failed'}
& $mono $output
if($LASTEXITCODE -ne 0){throw 'Map checks failed'}
