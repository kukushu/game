param([string]$Reference='c964820',[string]$PrototypeWorld='')
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'ProjectLayout.ps1')
$root=Split-Path $PSScriptRoot -Parent
$dir=Join-Path $root 'Temp/CityScenarios/PassengerPrecheckChecks';New-Item -ItemType Directory -Force $dir|Out-Null
$mono='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/bin/mono.exe';$csc='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/lib/mono/4.5/csc.exe'
$sources=Get-CityModelSources $root;$harness=Join-Path $PSScriptRoot 'Diagnostics/CityPassengerPrecheckChecks.cs'
$candidateSources=$sources
if($PrototypeWorld){$candidateSources=@($sources|Where-Object {$_ -notlike '*CityRoadCoreWorld.cs'})+(Resolve-Path -LiteralPath $PrototypeWorld).Path;$fast=@('/define:EXPECT_FAST_SOURCE')}else{$fast=@()}
& $mono $csc /nologo /langversion:latest @fast ('/out:'+(Join-Path $dir 'after.exe')) $candidateSources $harness
if($LASTEXITCODE -ne 0){throw 'Passenger checks compile failed'}
& $mono (Join-Path $dir 'after.exe') (Join-Path $dir 'after.csv');if($LASTEXITCODE -ne 0){throw 'Passenger checks failed'}
$old=& git show ($Reference+':Assets/Scripts/Runtime/Traffic/CityRoadCoreWorld.cs');if($LASTEXITCODE -ne 0){throw 'Reference unavailable'}
$referenceSource=Join-Path $dir 'CityRoadCoreWorld.cs';[IO.File]::WriteAllLines($referenceSource,$old)
& $mono $csc /nologo /langversion:latest ('/out:'+(Join-Path $dir 'before.exe')) ($sources|Where-Object {$_ -notlike '*CityRoadCoreWorld.cs'}) $referenceSource $harness
if($LASTEXITCODE -ne 0){throw 'Reference compile failed'}
& $mono (Join-Path $dir 'before.exe') (Join-Path $dir 'before.csv');if($LASTEXITCODE -ne 0){throw 'Reference checks failed'}
if((Get-FileHash (Join-Path $dir 'before.csv')).Hash -ne (Get-FileHash (Join-Path $dir 'after.csv')).Hash){throw 'Passenger admission or exact selected route/anchors changed'}
'PASS reference admission/route/anchors identical; old implementation exists only in external verification'
