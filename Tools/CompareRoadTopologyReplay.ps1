$ErrorActionPreference='Stop';$root=Split-Path $PSScriptRoot -Parent;$data='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data';$mono=Join-Path $data 'MonoBleedingEdge/bin/mono.exe';$compiler=Join-Path $data 'MonoBleedingEdge/lib/mono/4.5/csc.exe'
$directory=Join-Path $root 'Temp/RoadTopologyReplay';New-Item -ItemType Directory -Path $directory -Force | Out-Null
$sources=Get-ChildItem -LiteralPath (Join-Path $root 'Assets') -Filter 'City*.cs' | Where-Object {$_.Name -notin @('CityLandscape.cs','CityRoadView.cs')} | ForEach-Object FullName
$model=Join-Path $directory 'Model.dll';$probe=Join-Path $directory 'Probe.exe'
& $mono $compiler /nologo /langversion:latest /target:library "/out:$model" $sources;if($LASTEXITCODE -ne 0){throw 'Model compile failed'}
& $mono $compiler /nologo /langversion:latest /r:System.Web.Extensions.dll "/r:$model" "/out:$probe" (Join-Path $PSScriptRoot 'Diagnostics/TrafficIndexReplayProbe.cs');if($LASTEXITCODE -ne 0){throw 'Probe compile failed'}
foreach($mode in @('control','topology')){& $mono $probe (Join-Path $root 'Tools/Benchmarks/traffic-city.json') (Join-Path $directory ($mode+'.csv')) $mode;if($LASTEXITCODE -ne 0){throw 'Replay failed'}}
$a=@(Get-Content (Join-Path $directory 'control.csv'));$b=@(Get-Content (Join-Path $directory 'topology.csv'));if($a.Count -ne 340 -or $b.Count -ne 340 -or (Compare-Object $a $b)){throw 'Complete city states differ'}
'PASS: 340 exact city states, including accumulator; topology queried and vehicle positions roundtripped before each step.'
