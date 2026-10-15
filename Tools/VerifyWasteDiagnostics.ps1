$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'ProjectLayout.ps1')
$root=Split-Path $PSScriptRoot -Parent
$dir=Join-Path $root 'Temp/CityScenarios/WasteChecks';New-Item -ItemType Directory -Force $dir|Out-Null
$mono='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/bin/mono.exe'
$csc='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/lib/mono/4.5/csc.exe'
& $mono $csc /nologo /langversion:latest ('/out:'+(Join-Path $dir 'checks.exe')) (Get-CityModelSources $root) (Join-Path $root 'Tools/Diagnostics/CityWasteDiagnosticsChecks.cs')
if($LASTEXITCODE -ne 0){throw 'Waste diagnostics checks compile failed'}
& $mono (Join-Path $dir 'checks.exe')
if($LASTEXITCODE -ne 0){throw 'Waste diagnostics checks failed'}
