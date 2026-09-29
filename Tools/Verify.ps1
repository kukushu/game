param([string]$UnityData = 'C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data')
$ErrorActionPreference = 'Stop'
$projectPath = Split-Path $PSScriptRoot -Parent
$monoPath = Join-Path $UnityData 'MonoBleedingEdge/bin/mono.exe'
$compilerPath = Join-Path $UnityData 'MonoBleedingEdge/lib/mono/4.5/csc.exe'
$tempPath = Join-Path $projectPath 'Temp'
New-Item -ItemType Directory -Path $tempPath -Force | Out-Null
$outputPath = Join-Path $tempPath 'CityModelChecks.exe'
& $monoPath $compilerPath /nologo "/out:$outputPath" (Join-Path $projectPath 'Assets/CityModel.cs') (Join-Path $projectPath 'Assets/CityTraffic.cs') (Join-Path $projectPath 'Assets/CityRoads.cs') (Join-Path $projectPath 'Assets/CityBuildings.cs') (Join-Path $projectPath 'Assets/CityHouseholds.cs') (Join-Path $projectPath 'Assets/CityResidents.cs') (Join-Path $projectPath 'Assets/CityResidentTransport.cs') (Join-Path $PSScriptRoot 'CityModelChecks.cs') (Join-Path $PSScriptRoot 'TrafficChecks.cs') (Join-Path $PSScriptRoot 'RoadChecks.cs') (Join-Path $PSScriptRoot 'BuildingChecks.cs') (Join-Path $PSScriptRoot 'HouseholdChecks.cs') (Join-Path $PSScriptRoot 'ResidentTransportChecks.cs')
if ($LASTEXITCODE -ne 0) { throw 'Simulation checks failed to compile.' }
& $monoPath $outputPath
if ($LASTEXITCODE -ne 0) { throw 'Simulation checks failed.' }
