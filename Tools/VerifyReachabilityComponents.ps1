param([string]$Candidate='Assets/Scripts/Runtime/Infrastructure/Roads/CityLaneReachability.cs')
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'ProjectLayout.ps1')
$root=Split-Path $PSScriptRoot -Parent
$folder=Join-Path $root 'Temp/CityScenarios/ReachabilityComponentChecks';New-Item -ItemType Directory -Force -Path $folder|Out-Null
$mono='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/bin/mono.exe';$csc='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/lib/mono/4.5/csc.exe'
$sources=@(Get-CityModelSources $root|Where-Object {$_ -notlike '*CityLaneReachability.cs'})+(Resolve-Path -LiteralPath $Candidate).Path
$output=Join-Path $folder 'checks.exe'
& $mono $csc /nologo /langversion:latest ('/out:'+$output) $sources (Join-Path $PSScriptRoot 'Diagnostics/CityReachabilityComponentChecks.cs')
if($LASTEXITCODE -ne 0){throw 'Reachability checks compile failed'}
& $mono $output
if($LASTEXITCODE -ne 0){throw 'Reachability checks failed'}
