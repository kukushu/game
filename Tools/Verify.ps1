param([string]$UnityData = 'C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data',[switch]$RoadKeyHashPrototype,[switch]$LegacyRoadKeyHash,[switch]$LegacyTopologyQueries,[switch]$LegacyLogicalRouting)
. (Join-Path $PSScriptRoot 'ProjectLayout.ps1')
$ErrorActionPreference = 'Stop'
$projectPath = Split-Path $PSScriptRoot -Parent
$monoPath = Join-Path $UnityData 'MonoBleedingEdge/bin/mono.exe'
$compilerPath = Join-Path $UnityData 'MonoBleedingEdge/lib/mono/4.5/csc.exe'
$tempPath = Join-Path $projectPath 'Temp'
New-Item -ItemType Directory -Path $tempPath -Force | Out-Null
$outputPath = Join-Path $tempPath 'CityModelChecks.exe'
$modelSources = Get-CityModelSources $projectPath
$checkSources = Get-ChildItem -LiteralPath $PSScriptRoot -Filter '*.cs' | ForEach-Object FullName
$flags=@('/nologo','/langversion:latest','/r:System.Web.Extensions.dll',"/out:$outputPath")
if($RoadKeyHashPrototype){Write-Warning 'RoadKeyHashPrototype is obsolete: the optimization is now the default.'}
$defines=@();if($LegacyRoadKeyHash){$defines+='CITY_ROAD_KEY_HASH_DISABLED'}
if($LegacyTopologyQueries){$defines+='CITY_ROAD_TOPOLOGY_QUERIES_DISABLED'}
if($LegacyLogicalRouting){$defines+='CITY_ROAD_ROUTING_LEGACY'}
if($defines.Count){$flags+=('/define:'+($defines -join ';'))}
& $monoPath $compilerPath $flags $modelSources $checkSources
if ($LASTEXITCODE -ne 0) { throw 'Simulation checks failed to compile.' }
& $monoPath $outputPath
if ($LASTEXITCODE -ne 0) { throw 'Simulation checks failed.' }
