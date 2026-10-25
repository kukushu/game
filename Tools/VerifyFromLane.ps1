param([string]$Results='Temp/RecoverySharing-20261025/from-lane-checks',[string]$NetworkReference='4ed1761')
$ErrorActionPreference='Stop';. (Join-Path $PSScriptRoot 'ProjectLayout.ps1');$root=Split-Path $PSScriptRoot -Parent
$dir=Join-Path $root $Results;if(Test-Path $dir){throw 'Fresh test output required'};New-Item -ItemType Directory $dir|Out-Null
$mono='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/bin/mono.exe';$csc='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/lib/mono/4.5/csc.exe'
$sources=Get-CityModelSources $root;$frozen=(& git show ($NetworkReference+':Assets/Scripts/Runtime/Infrastructure/Roads/CityRoadCoreNetwork.cs')) -join "`n";if($LASTEXITCODE -ne 0){throw 'Missing frozen routing source'};$file=Join-Path $dir 'Network.reference.cs';[IO.File]::WriteAllText($file,$frozen)
foreach($variant in @('reference','actual')){
 $files=if($variant -eq 'reference'){@($sources|Where-Object {$_ -notlike '*CityRoadCoreNetwork.cs'})+$file}else{$sources}
 & $mono $csc /nologo /langversion:latest ('/out:'+(Join-Path $dir ($variant+'.exe'))) $files (Join-Path $PSScriptRoot 'Diagnostics/CityFromLaneChecks.cs');if($LASTEXITCODE -ne 0){throw 'FromLane checks compile failed'}
 & $mono (Join-Path $dir ($variant+'.exe')) (Join-Path $dir ($variant+'.bin')) | Tee-Object (Join-Path $dir ($variant+'.log'));if($LASTEXITCODE -ne 0){throw 'FromLane checks failed'}
}
$a=(Get-FileHash (Join-Path $dir 'reference.bin')).Hash;$b=(Get-FileHash (Join-Path $dir 'actual.bin')).Hash
@{reference=(git rev-parse $NetworkReference);referenceHash=$a;actualHash=$b;actualSourceHash=(Get-FileHash (Join-Path $root 'Assets/Scripts/Runtime/Infrastructure/Roads/CityRoadCoreNetwork.cs')).Hash}|ConvertTo-Json|Set-Content (Join-Path $dir 'comparison.json')
if($a -ne $b){throw 'Frozen/new route, terminal or cost bytes differ'};'PASS frozen/new Dijkstra outputs identical'
