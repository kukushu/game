. (Join-Path $PSScriptRoot 'ProjectLayout.ps1')
$ErrorActionPreference='Stop';$root=Split-Path $PSScriptRoot -Parent
$data='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data';$mono=Join-Path $data 'MonoBleedingEdge/bin/mono.exe';$compiler=Join-Path $data 'MonoBleedingEdge/lib/mono/4.5/csc.exe'
$output=Join-Path $root 'Temp/RoadDraftComparison';$sources=Get-CityModelSources $root
foreach($mode in @('legacy','current')){
    $folder=Join-Path $output $mode;New-Item -ItemType Directory -Path $folder -Force|Out-Null
    $model=Join-Path $folder 'Model.dll';$probe=Join-Path $folder 'Probe.exe'
    $flags=@('/nologo','/langversion:latest','/target:library',"/out:$model");if($mode -eq 'legacy'){$flags+='/define:CITY_ROAD_PLAN_COPY_PER_SEGMENT'}
    & $mono $compiler $flags $sources;if($LASTEXITCODE -ne 0){throw 'Model compile failed'}
    & $mono $compiler /nologo /langversion:latest /r:System.Web.Extensions.dll "/r:$model" "/out:$probe" (Join-Path $PSScriptRoot 'TestCity.cs') (Join-Path $PSScriptRoot 'Diagnostics/RoadDraftProbe.cs');if($LASTEXITCODE -ne 0){throw 'Probe compile failed'}
    & $mono $probe (Join-Path $output ($mode+'.csv'));if($LASTEXITCODE -ne 0){throw 'Draft probe failed'}
}
if(Compare-Object (Get-Content (Join-Path $output 'legacy.csv')) (Get-Content (Join-Path $output 'current.csv'))){throw 'Preview/committed/loaded traffic city states differ'}
'PASS: 36 complete preview graphs, metadata/splits and committed/loaded traffic states identical.'
