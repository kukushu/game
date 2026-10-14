param([string]$Baseline='0c86984',[string]$Results='Temp/RoadCore/R2BScale',[switch]$AllowBehaviorChange)
. (Join-Path $PSScriptRoot 'ProjectLayout.ps1')
$ErrorActionPreference='Stop';$root=Split-Path $PSScriptRoot -Parent
$output=Join-Path $root $Results;New-Item -ItemType Directory -Path $output -Force|Out-Null
$baselinePath=Join-Path $output 'Baseline';$archive=Join-Path $output 'baseline.zip'
& git archive --format=zip ('--output='+$archive) $Baseline Assets/Scripts
if($LASTEXITCODE -ne 0){throw 'Baseline archive failed'}
Expand-Archive -LiteralPath $archive -DestinationPath $baselinePath -Force
if($AllowBehaviorChange){
    # Same physical actors/stations on both sides. The new junction model stops
    # upstream of ports; do not seed conflicting actors inside its protected area.
    Copy-Item -LiteralPath (Join-Path $root 'Assets/Scripts/Diagnostics/Runtime/CityRoadCoreFixtures.cs') -Destination (Join-Path $baselinePath 'Assets/Scripts/Diagnostics/Runtime/CityRoadCoreFixtures.cs')
}
$mono='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/bin/mono.exe'
$compiler='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/lib/mono/4.5/csc.exe'
foreach($version in @('before','after')){
    $sourceRoot=if($version -eq 'before'){$baselinePath}else{$root};$exe=Join-Path $output ($version+'.exe')
    & $mono $compiler /nologo /langversion:latest ('/out:'+$exe) (Get-CityModelSources $sourceRoot) (Join-Path $root 'Tools/Diagnostics/RoadCoreScaleBenchmark.cs')
    if($LASTEXITCODE -ne 0){throw "$version compile failed"}
    & $mono $exe (Join-Path $output ($version+'.csv')) $version
    if($LASTEXITCODE -ne 0){throw "$version benchmark failed"}
}
$before=Import-Csv (Join-Path $output 'before.csv');$after=Import-Csv (Join-Path $output 'after.csv')
for($i=0;$i -lt $before.Count;$i++){
    if($before[$i].vehicles -ne $after[$i].vehicles -or $before[$i].steps -ne $after[$i].steps){throw 'Unequal workload'}
    if(-not $AllowBehaviorChange -and $before[$i].semantic_sha256 -ne $after[$i].semantic_sha256){throw "State differs in scenario $i"}
}
