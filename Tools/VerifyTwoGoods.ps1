param()
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'ProjectLayout.ps1')
$root=Split-Path $PSScriptRoot -Parent
$mono='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/bin/mono.exe'
$compiler='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/lib/mono/4.5/csc.exe'
$output=Join-Path $root 'Temp/TwoGoods-20261104/Checks.exe'
New-Item -ItemType Directory -Force -Path (Split-Path $output -Parent)|Out-Null
& $mono $compiler /nologo /langversion:latest ('/out:'+$output) (Get-CityModelSources $root) (Join-Path $root 'Tools/Diagnostics/CityTwoGoodsChecks.cs')
if($LASTEXITCODE -ne 0){throw 'Two-product checks compile failed'}
& $mono $output
if($LASTEXITCODE -ne 0){throw 'Two-product checks failed'}
