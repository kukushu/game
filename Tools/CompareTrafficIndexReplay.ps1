param([string]$UnityData='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data')
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$mono=Join-Path $UnityData 'MonoBleedingEdge/bin/mono.exe';$compiler=Join-Path $UnityData 'MonoBleedingEdge/lib/mono/4.5/csc.exe'
$sources=Get-ChildItem -LiteralPath (Join-Path $root 'Assets') -Filter 'City*.cs' | Where-Object {$_.Name -notin @('CityLandscape.cs','CityRoadView.cs')} | ForEach-Object FullName
$hashFiles=@()
foreach($mode in @('scan','index')){
    $directory=Join-Path $root "Temp/TrafficIndexReplay/$mode";New-Item -ItemType Directory -Path $directory -Force | Out-Null
    $model=Join-Path $directory 'Model.dll';$probe=Join-Path $directory 'Probe.exe'
    $flags=@('/nologo','/langversion:latest','/target:library',"/out:$model");if($mode -eq 'scan'){$flags+='/define:CITY_TRAFFIC_EDGE_INDEX_DISABLED'}
    & $mono $compiler $flags $sources;if($LASTEXITCODE -ne 0){throw 'Model compilation failed'}
    & $mono $compiler /nologo /langversion:latest /r:System.Web.Extensions.dll "/r:$model" "/out:$probe" (Join-Path $PSScriptRoot 'Diagnostics/TrafficIndexReplayProbe.cs')
    if($LASTEXITCODE -ne 0){throw 'Replay compilation failed'}
    $hashFile=Join-Path $directory 'step-states.csv';$hashFiles+=$hashFile
    & $mono $probe (Join-Path $PSScriptRoot 'Benchmarks/traffic-city.json') $hashFile;if($LASTEXITCODE -ne 0){throw 'Replay failed'}
}
$old=@(Get-Content -LiteralPath $hashFiles[0]);$new=@(Get-Content -LiteralPath $hashFiles[1])
if($old.Count -ne 340 -or $new.Count -ne 340){throw 'Incomplete replay'}
for($i=0;$i -lt 340;$i++){if($old[$i] -ne $new[$i]){throw "Complete state diverged at step $i"}}
'PASS: all 340 per-step states equal, including positions, lanes, waits, routes, tasks, economy and accumulator'
