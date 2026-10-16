param([string]$Before,[string]$After,[switch]$AlignTicks)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'ProjectLayout.ps1')
$root=Split-Path $PSScriptRoot -Parent
$mono='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/bin/mono.exe';$csc='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/lib/mono/4.5/csc.exe'
$output=Join-Path $root 'Temp/CityScenarios/WindowStateChecks.exe'
& $mono $csc /nologo /langversion:latest ('/out:'+$output) (Get-CityModelSources $root) (Join-Path $root 'Tools/Diagnostics/CityWindowStateChecks.cs')
if($LASTEXITCODE -ne 0){throw 'Window state verifier compile failed'}
& $mono $output $Before $After $(if($AlignTicks){"align"}else{"strict"})
if($LASTEXITCODE -ne 0){throw 'Window state equality failed'}
