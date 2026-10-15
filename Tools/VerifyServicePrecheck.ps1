param([string]$Reference='7502947')
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'ProjectLayout.ps1')
$root=Split-Path $PSScriptRoot -Parent
$dir=Join-Path $root 'Temp/CityScenarios/ServicePrecheckChecks';New-Item -ItemType Directory -Force $dir|Out-Null
$mono='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/bin/mono.exe';$csc='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/lib/mono/4.5/csc.exe'
$sources=Get-CityModelSources $root;$harness=Join-Path $PSScriptRoot 'Diagnostics/CityServicePrecheckChecks.cs'
& $mono $csc /nologo /langversion:latest /define:EXPECT_FAST_SOURCE ('/out:'+(Join-Path $dir 'after.exe')) $sources $harness
if($LASTEXITCODE -ne 0){throw 'Service checks compile failed'}
& $mono (Join-Path $dir 'after.exe') (Join-Path $dir 'after.csv');if($LASTEXITCODE -ne 0){throw 'Service checks failed'}
$old=& git show ($Reference+':Assets/Scripts/Runtime/Traffic/CityRoadCoreWorld.cs');if($LASTEXITCODE -ne 0){throw 'Reference unavailable'}
$referenceSource=Join-Path $dir 'CityRoadCoreWorld.cs';[IO.File]::WriteAllLines($referenceSource,$old)
& $mono $csc /nologo /langversion:latest ('/out:'+(Join-Path $dir 'before.exe')) ($sources|Where-Object {$_ -notlike '*CityRoadCoreWorld.cs'}) $referenceSource $harness
if($LASTEXITCODE -ne 0){throw 'Reference compile failed'}
& $mono (Join-Path $dir 'before.exe') (Join-Path $dir 'before.csv');if($LASTEXITCODE -ne 0){throw 'Reference checks failed'}
if((Get-FileHash (Join-Path $dir 'before.csv')).Hash -ne (Get-FileHash (Join-Path $dir 'after.csv')).Hash){throw 'Service admission or exact selected route/anchors changed'}
'PASS reference admission/route/anchors identical; old implementation exists only in external verification'
