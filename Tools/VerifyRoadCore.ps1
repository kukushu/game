param([string]$Results='Temp/RoadCore/model.csv')
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$mono='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/bin/mono.exe'
$compiler='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/lib/mono/4.5/csc.exe'
New-Item -ItemType Directory -Path (Join-Path $root 'Temp/RoadCore') -Force|Out-Null
$output=Join-Path $root 'Temp/RoadCore/Checks.exe'
$sources=@('Assets/Scripts/Runtime/Infrastructure/Roads/CityRoadKeyComparer.cs','Assets/Scripts/Runtime/Infrastructure/Roads/CityRoadCoreNetwork.cs','Assets/Scripts/Runtime/Traffic/CityRoadCoreWorld.cs','Assets/Scripts/Runtime/Traffic/CityRoadCoreBusiness.cs','Assets/Scripts/Runtime/Infrastructure/Roads/CityRoadCoreAccess.cs','Assets/Scripts/Runtime/Infrastructure/Roads/CityRoadCoreSave.cs','Assets/Scripts/Diagnostics/Runtime/CityRoadCoreFixtures.cs','Tools/Diagnostics/RoadCoreChecks.cs')|ForEach-Object {Join-Path $root $_}
& $mono $compiler /nologo /langversion:latest /r:System.Web.Extensions.dll ('/out:'+$output) $sources
if($LASTEXITCODE -ne 0){throw 'Road core checks compile failed'}
& $mono $output (Join-Path $root $Results)
if($LASTEXITCODE -ne 0){throw 'Road core checks failed'}
