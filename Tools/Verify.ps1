param([string]$UnityData = 'C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data')
$ErrorActionPreference = 'Stop'
$projectPath = Split-Path $PSScriptRoot -Parent
$monoPath = Join-Path $UnityData 'MonoBleedingEdge/bin/mono.exe'
$compilerPath = Join-Path $UnityData 'MonoBleedingEdge/lib/mono/4.5/csc.exe'
$tempPath = Join-Path $projectPath 'Temp'
New-Item -ItemType Directory -Path $tempPath -Force | Out-Null
$outputPath = Join-Path $tempPath 'CityModelChecks.exe'
$modelSources = Get-ChildItem -LiteralPath (Join-Path $projectPath 'Assets') -Filter 'City*.cs' | Where-Object { $_.Name -notin @('CityLandscape.cs','CityRoadView.cs') } | ForEach-Object FullName
$checkSources = Get-ChildItem -LiteralPath $PSScriptRoot -Filter '*.cs' | ForEach-Object FullName
& $monoPath $compilerPath /nologo /langversion:latest /r:System.Web.Extensions.dll "/out:$outputPath" $modelSources $checkSources
if ($LASTEXITCODE -ne 0) { throw 'Simulation checks failed to compile.' }
& $monoPath $outputPath
if ($LASTEXITCODE -ne 0) { throw 'Simulation checks failed.' }
