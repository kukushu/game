param([string]$Results='Temp/RoadPreviewOptimization-20261018/oracle',[string[]]$Save=@())
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'ProjectLayout.ps1')
$root=Split-Path $PSScriptRoot -Parent
if(Test-Path -LiteralPath (Join-Path $root $Results)){throw 'Choose fresh oracle results'}
New-Item -ItemType Directory -Path (Join-Path $root $Results)|Out-Null
$mono='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/bin/mono.exe'
$compiler='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/lib/mono/4.5/csc.exe'
$output=Join-Path $root ($Results+'/Oracle.exe')
& $mono $compiler /nologo /langversion:latest /r:System.Web.Extensions.dll ('/out:'+$output) (Get-CityModelSources $root) (Join-Path $PSScriptRoot 'Diagnostics/CityConnectionConflictChecks.cs') (Join-Path $PSScriptRoot 'Diagnostics/FrozenConnectionConflicts20261018.cs')
if($LASTEXITCODE -ne 0){throw 'Conflict oracle failed to compile'}
& $mono $output (Join-Path $root $Results) @Save
if($LASTEXITCODE -ne 0){throw 'Conflict oracle failed'}
