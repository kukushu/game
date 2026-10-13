param([string]$OutputDirectory='Logs/RoadTopology/stage1',[string]$UnityData='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data')
. (Join-Path $PSScriptRoot 'ProjectLayout.ps1')
$ErrorActionPreference='Stop';$root=Split-Path $PSScriptRoot -Parent;$directory=Join-Path $root 'Temp/RoadTopologyProbe';New-Item -ItemType Directory -Path $directory -Force | Out-Null
$mono=Join-Path $UnityData 'MonoBleedingEdge/bin/mono.exe';$compiler=Join-Path $UnityData 'MonoBleedingEdge/lib/mono/4.5/csc.exe'
$sources = Get-CityModelSources $root
$model=Join-Path $directory 'Model.dll';$probe=Join-Path $directory 'Probe.exe'
& $mono $compiler /nologo /langversion:latest /target:library "/out:$model" $sources;if($LASTEXITCODE -ne 0){throw 'Model compile failed'}
& $mono $compiler /nologo /langversion:latest /r:System.Web.Extensions.dll "/r:$model" "/out:$probe" (Join-Path $PSScriptRoot 'Diagnostics/RoadTopologyProbe.cs');if($LASTEXITCODE -ne 0){throw 'Probe compile failed'}
& $mono $probe (Join-Path $root 'Tools/Benchmarks/traffic-city.json') ([IO.Path]::GetFullPath($OutputDirectory));if($LASTEXITCODE -ne 0){throw 'Topology diagnostic failed'}
