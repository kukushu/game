param([string]$UnityData='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data')
. (Join-Path $PSScriptRoot 'ProjectLayout.ps1')
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$mono=Join-Path $UnityData 'MonoBleedingEdge/bin/mono.exe';$compiler=Join-Path $UnityData 'MonoBleedingEdge/lib/mono/4.5/csc.exe'
$sources = Get-CityModelSources $root
$output=Join-Path $root 'Temp/RoadKeyDistribution.exe'
New-Item -ItemType Directory -Path (Split-Path $output -Parent) -Force | Out-Null
& $mono $compiler /nologo /langversion:latest /r:System.Web.Extensions.dll "/out:$output" $sources (Join-Path $PSScriptRoot 'Diagnostics/RoadKeyDistribution.cs')
if($LASTEXITCODE -ne 0){throw 'Road-key research probe did not compile'}
& $mono $output (Join-Path $PSScriptRoot 'Benchmarks/traffic-city.json')
if($LASTEXITCODE -ne 0){throw 'Road-key research probe failed'}
