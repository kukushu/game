param([string]$Save,[string]$Results)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'ProjectLayout.ps1')
$root=Split-Path $PSScriptRoot -Parent
$mono='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/bin/mono.exe'
$csc='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/lib/mono/4.5/csc.exe'
if(Test-Path -LiteralPath $Results){throw 'Fresh results required'}
New-Item -ItemType Directory -Path $Results|Out-Null
$sources=Get-CityModelSources $root
@{commit=(& git rev-parse HEAD);dirty=@(& git status --porcelain);inputHash=(Get-FileHash -LiteralPath $Save).Hash;utc=[DateTime]::UtcNow.ToString('o');sourceHashes=@($sources|ForEach-Object{@{path=$_.Substring($root.Length+1);hash=(Get-FileHash -LiteralPath $_).Hash}});note='Cold step attributed build, exact profiling on/off full-state check; not normal frame performance'}|ConvertTo-Json -Depth 5|Set-Content (Join-Path $Results 'environment.json')
$output=Join-Path $Results 'cold.exe'
& $mono $csc /nologo /langversion:latest /define:CITY_CORE_ATTRIBUTION ('/out:'+$output)  $sources (Join-Path $PSScriptRoot 'Diagnostics/CityColdScaleChecks.cs')
if($LASTEXITCODE -ne 0){throw 'Cold probe compile failed'}
& $mono $output $Save (Join-Path $Results 'attribution.csv')
if($LASTEXITCODE -ne 0){throw 'Cold profiling failed'}
