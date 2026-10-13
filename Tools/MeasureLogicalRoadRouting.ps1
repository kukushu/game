param([string]$OutputDirectory='Logs/RoadRouting/current',[switch]$LegacyRouting)
. (Join-Path $PSScriptRoot 'ProjectLayout.ps1')
$ErrorActionPreference='Stop';$root=Split-Path $PSScriptRoot -Parent;$directory=Join-Path $root 'Temp/LogicalRoutingProbe';New-Item -ItemType Directory -Path $directory -Force|Out-Null
$data='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data';$mono=Join-Path $data 'MonoBleedingEdge/bin/mono.exe';$compiler=Join-Path $data 'MonoBleedingEdge/lib/mono/4.5/csc.exe'
$model=Join-Path $directory 'Model.dll';$probe=Join-Path $directory 'Probe.exe';$sources=Get-CityModelSources $root
$flags=@('/nologo','/langversion:latest','/target:library',"/out:$model");if($LegacyRouting){$flags+='/define:CITY_ROAD_ROUTING_LEGACY'}
& $mono $compiler $flags $sources;if($LASTEXITCODE -ne 0){throw 'Model compile failed'}
& $mono $compiler /nologo /langversion:latest /r:System.Web.Extensions.dll "/r:$model" "/out:$probe" (Join-Path $PSScriptRoot 'Diagnostics/LogicalRoutingProbe.cs');if($LASTEXITCODE -ne 0){throw 'Probe compile failed'}
& $mono $probe (Join-Path $root 'Tools/Benchmarks/traffic-city.json') ([IO.Path]::GetFullPath((Join-Path $root $OutputDirectory)));if($LASTEXITCODE -ne 0){throw 'Routing measurement failed'}
