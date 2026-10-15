param([switch]$Scale)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'ProjectLayout.ps1')
$root=Split-Path $PSScriptRoot -Parent
$mono='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/bin/mono.exe'
$compiler='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/lib/mono/4.5/csc.exe'
$output=Join-Path $root 'Temp/CityScenarios/checks.exe'
New-Item -ItemType Directory -Path (Split-Path $output) -Force|Out-Null
& $mono $compiler /nologo /langversion:latest ('/out:'+$output) (Get-CityModelSources $root) (Join-Path $PSScriptRoot 'Diagnostics/CityScenarioChecks.cs')
if($LASTEXITCODE -ne 0){throw 'Scenario checks compile failed'}
& $mono $output $(if($Scale){"--scale"}else{"--normal"})
if($LASTEXITCODE -ne 0){throw 'Scenario construction checks failed'}
