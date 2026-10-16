param([string]$Initial='Temp/CityScenarios/grid500-normal-final/initial.hcity',[string]$Reference='50eb510')
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'ProjectLayout.ps1')
$root=Split-Path $PSScriptRoot -Parent
$dir=Join-Path $root 'Temp/CityScenarios/CommuteEstimateChecks';New-Item -ItemType Directory -Force $dir|Out-Null
$mono='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/bin/mono.exe'
$csc='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/lib/mono/4.5/csc.exe'
$sources=Get-CityModelSources $root;$harness=Join-Path $PSScriptRoot 'Diagnostics/CityCommuteEstimateChecks.cs'
& $mono $csc /nologo /langversion:latest /define:EXPECT_COMMUTE_CACHE ('/out:'+(Join-Path $dir 'after.exe')) $sources $harness
if($LASTEXITCODE -ne 0){throw 'Commute checks compile failed'}
& $mono (Join-Path $dir 'after.exe') $Initial (Join-Path $dir 'after.hcity')
if($LASTEXITCODE -ne 0){throw 'Commute lifecycle checks failed'}
$old=& git show ($Reference+':Assets/Scripts/Runtime/Simulation/Residents/CityCoreResidents.cs');if($LASTEXITCODE -ne 0){throw 'Reference unavailable'}
$referenceSource=Join-Path $dir 'CityCoreResidents.cs';[IO.File]::WriteAllLines($referenceSource,$old)
& $mono $csc /nologo /langversion:latest ('/out:'+(Join-Path $dir 'before.exe')) ($sources|Where-Object {$_ -notlike '*CityCoreResidents.cs'}) $referenceSource $harness
if($LASTEXITCODE -ne 0){throw 'Reference compile failed'}
& $mono (Join-Path $dir 'before.exe') $Initial (Join-Path $dir 'before.hcity')
if($LASTEXITCODE -ne 0){throw 'Reference continuation failed'}
if((Get-FileHash (Join-Path $dir 'after.hcity')).Hash -ne (Get-FileHash (Join-Path $dir 'before.hcity')).Hash){throw 'Complete city changed after cache/dynamic edit/restore'}
'PASS exact original-reference full city state, dynamic edit, cache/access/endpoint lifecycle and warm/restored continuation'
