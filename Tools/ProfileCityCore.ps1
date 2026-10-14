param([string]$Results='Temp/RoadCore/Attribution/steps.csv')
. (Join-Path $PSScriptRoot 'ProjectLayout.ps1')
$ErrorActionPreference='Stop';$root=Split-Path $PSScriptRoot -Parent
$target=Join-Path $root $Results;New-Item -ItemType Directory -Path (Split-Path $target -Parent) -Force|Out-Null
$mono='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/bin/mono.exe';$csc='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/lib/mono/4.5/csc.exe'
$exe=Join-Path $root 'Temp/RoadCore/ProfileChecks.exe'
& $mono $csc /nologo /langversion:latest /define:CITY_CORE_ATTRIBUTION ('/out:'+$exe) (Get-CityModelSources $root) (Join-Path $root 'Tools/Diagnostics/CityCoreProfileChecks.cs')
if($LASTEXITCODE -ne 0){throw 'Profile compile failed'}
& $mono $exe $target
if($LASTEXITCODE -ne 0){throw 'Profile run failed'}
