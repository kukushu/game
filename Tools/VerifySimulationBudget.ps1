param([switch]$GuardScheduling)
$guardFlags=@();if($GuardScheduling){$guardFlags+='/define:CITY_RESIDENT_GUARD_SCHEDULE'}
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'ProjectLayout.ps1')
$root=Split-Path $PSScriptRoot -Parent
$mono='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/bin/mono.exe'
$compiler='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/lib/mono/4.5/csc.exe'
$output=Join-Path $root 'Temp/RoadCore/SimulationBudgetChecks.exe'
& $mono $compiler @guardFlags /nologo /langversion:latest ('/out:'+$output) (Get-CityModelSources $root) (Join-Path $root 'Tools/Diagnostics/CitySimulationBudgetChecks.cs')
if($LASTEXITCODE -ne 0){throw 'Budget verifier compile failed'}
& $mono $output
if($LASTEXITCODE -ne 0){throw 'Budget correctness failed'}
