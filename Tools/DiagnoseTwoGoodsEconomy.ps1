param([Parameter(Mandatory=$true)][string]$Results,[switch]$WaterBudgetControl)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'ProjectLayout.ps1')
$root=Split-Path $PSScriptRoot -Parent
Set-Location -LiteralPath $root
$output=[IO.Path]::GetFullPath((Join-Path $root $Results))
$temporary=[IO.Path]::GetFullPath((Join-Path $root 'Temp'))+[IO.Path]::DirectorySeparatorChar
if(-not $output.StartsWith($temporary,[StringComparison]::OrdinalIgnoreCase)){throw 'Diagnostic outputs must remain inside this project Temp directory'}
$control=$output+'.control'
if((Test-Path -LiteralPath $output) -or (Test-Path -LiteralPath $control)){throw 'Choose fresh evidence output; existing files are preserved'}
New-Item -ItemType Directory -Path $control -Force|Out-Null
$mono='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/bin/mono.exe'
$compiler='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/lib/mono/4.5/csc.exe'
$exe=Join-Path $control 'EconomyProbe.exe'
& $mono $compiler /nologo /langversion:latest ('/out:'+$exe) (Get-CityModelSources $root) (Join-Path $PSScriptRoot 'Diagnostics/CityTwoGoodsEconomyProbe.cs') *> (Join-Path $control 'build.log')
if($LASTEXITCODE -ne 0){Get-Content (Join-Path $control 'build.log') -Tail 12;throw 'Economy probe compilation failed'}
$mode=if($WaterBudgetControl){'water-cut'}else{'uninterrupted'}
& $mono $exe $output $mode *> (Join-Path $control 'run.log')
if($LASTEXITCODE -ne 0){Get-Content (Join-Path $control 'run.log') -Tail 12;throw 'Economy probe failed'}
Get-Content (Join-Path $control 'run.log')
