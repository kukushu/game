param([string[]]$LegacyDefines=@('CITY_ROAD_TOPOLOGY_QUERIES_DISABLED'),[string]$OutputDirectory='Temp/RoadMigrationReplay')
. (Join-Path $PSScriptRoot 'ProjectLayout.ps1')
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$data='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data'
$mono=Join-Path $data 'MonoBleedingEdge/bin/mono.exe';$compiler=Join-Path $data 'MonoBleedingEdge/lib/mono/4.5/csc.exe'
$output=[IO.Path]::GetFullPath((Join-Path $root $OutputDirectory));$sources=Get-CityModelSources $root
foreach($mode in @('legacy','current')){
    $folder=Join-Path $output $mode;New-Item -ItemType Directory -Path $folder -Force|Out-Null
    $model=Join-Path $folder 'Model.dll';$probe=Join-Path $folder 'Probe.exe'
    $flags=@('/nologo','/langversion:latest','/target:library',"/out:$model")
    if($mode -eq 'legacy'){$flags+=('/define:'+($LegacyDefines -join ';'))}
    & $mono $compiler $flags $sources;if($LASTEXITCODE -ne 0){throw 'Model compile failed'}
    & $mono $compiler /nologo /langversion:latest /r:System.Web.Extensions.dll "/r:$model" "/out:$probe" (Join-Path $PSScriptRoot 'Diagnostics/TrafficIndexReplayProbe.cs');if($LASTEXITCODE -ne 0){throw 'Probe compile failed'}
    & $mono $probe (Join-Path $root 'Tools/Benchmarks/traffic-city.json') (Join-Path $output ($mode+'.csv'));if($LASTEXITCODE -ne 0){throw 'Replay failed'}
}
$a=@(Get-Content (Join-Path $output 'legacy.csv'));$b=@(Get-Content (Join-Path $output 'current.csv'))
if($a.Count -ne 340 -or $b.Count -ne 340 -or (Compare-Object $a $b)){throw 'Full simulation states differ'}
[IO.File]::WriteAllText((Join-Path $output 'PASS.txt'),'PASS: 340 exact complete-state signatures; legacy defines='+($LegacyDefines -join ';'))
Get-Content (Join-Path $output 'PASS.txt')
