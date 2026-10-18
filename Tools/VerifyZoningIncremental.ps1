param([string]$Save='Temp/CityScenarios/regenerated30000-20261017/city.hcity',[Parameter(Mandatory)][string]$Results,[switch]$StressOnly,[switch]$LocalityOnly,[switch]$OccupancyOnly)
$ErrorActionPreference='Stop';. (Join-Path $PSScriptRoot 'ProjectLayout.ps1');$root=Split-Path $PSScriptRoot -Parent
if(Test-Path -LiteralPath $Results){throw 'Fresh result directory required'};New-Item -ItemType Directory -Path $Results|Out-Null
$mono='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/bin/mono.exe';$compiler='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/lib/mono/4.5/csc.exe'
$output=Join-Path $root ($Results+'/Checks.exe')
& $mono $compiler /nologo /langversion:latest ('/out:'+$output) (Get-CityModelSources $root) (Join-Path $PSScriptRoot 'Diagnostics/FrozenCoreZoning20261018.cs') (Join-Path $PSScriptRoot 'Diagnostics/CityZoningIncrementalChecks.cs')
if($LASTEXITCODE -ne 0){throw 'Zoning oracle compilation failed'}
if(@($StressOnly,$LocalityOnly,$OccupancyOnly).Where({$_}).Count -gt 1){throw 'Choose one test mode'}
& $mono $output (Resolve-Path -LiteralPath $Save).Path (Join-Path $root $Results) $(if($StressOnly){'stress'}elseif($LocalityOnly){'locality'}elseif($OccupancyOnly){'occupied'}else{'full'})
if($LASTEXITCODE -ne 0){throw 'Zoning oracle failed; preserve diagnostics'}
