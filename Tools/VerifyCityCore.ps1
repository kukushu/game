param([string]$Results='Temp/RoadCore/industry-model.csv',[switch]$Attribution)
. (Join-Path $PSScriptRoot 'ProjectLayout.ps1')
$ErrorActionPreference='Stop';$root=Split-Path $PSScriptRoot -Parent
$mono='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/bin/mono.exe'
$compiler='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/lib/mono/4.5/csc.exe'
New-Item -ItemType Directory -Path (Join-Path $root 'Temp/RoadCore') -Force|Out-Null
$output=Join-Path $root 'Temp/RoadCore/CityChecks.exe'
& $mono $compiler /nologo /langversion:latest ('/out:'+$output) (Get-CityModelSources $root) (Join-Path $root 'Tools/Diagnostics/CityCoreIndustryChecks.cs')
if($LASTEXITCODE -ne 0){throw 'City core checks compile failed'}
& $mono $output (Join-Path $root $Results)
if($LASTEXITCODE -ne 0){throw 'City core checks failed'}
$probe=Join-Path $root 'Temp/RoadCore/JobChecks.exe'
& $mono $compiler /nologo /langversion:latest ('/out:'+$probe) (Get-CityModelSources $root) (Join-Path $root 'Tools/Diagnostics/CityJobAllocationChecks.cs')
if($LASTEXITCODE -ne 0){throw 'Job allocation checks compile failed'}
& $mono $probe
if($LASTEXITCODE -ne 0){throw 'Job allocation checks failed'}

$gridCheck=Join-Path $root 'Temp/RoadCore/GridChecks.exe'
& $mono $compiler /nologo /langversion:latest ('/out:'+$gridCheck) (Get-CityModelSources $root) (Join-Path $root 'Tools/Diagnostics/CityRoadGridChecks.cs')
if($LASTEXITCODE -ne 0){throw 'Grid checks compile failed'}
& $mono $gridCheck
if($LASTEXITCODE -ne 0){throw 'Grid checks failed'}

if($Attribution){
    $profileCheck=Join-Path $root 'Temp/RoadCore/CityProfileChecks.exe'
    & $mono $compiler /nologo /langversion:latest /define:CITY_CORE_ATTRIBUTION ('/out:'+$profileCheck) (Get-CityModelSources $root) (Join-Path $root 'Tools/Diagnostics/CityProfileChecks.cs')
    if($LASTEXITCODE -ne 0){throw 'Profile correctness checks compile failed'}
    & $mono $profileCheck
    if($LASTEXITCODE -ne 0){throw 'Profile correctness checks failed'}
}
