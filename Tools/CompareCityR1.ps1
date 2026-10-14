param([string]$Baseline='789f7f2',[string]$Results='Temp/RoadCore/R1Pair')
. (Join-Path $PSScriptRoot 'ProjectLayout.ps1')
$ErrorActionPreference='Stop';$root=Split-Path $PSScriptRoot -Parent
$output=Join-Path $root $Results;New-Item -ItemType Directory -Path $output -Force|Out-Null
$baselinePath=Join-Path $output 'Baseline';$archive=Join-Path $output 'baseline.zip'
& git archive --format=zip ('--output='+$archive) $Baseline Assets/Scripts Tools/ProjectLayout.ps1
if($LASTEXITCODE -ne 0){throw 'Baseline archive failed'}
Expand-Archive -LiteralPath $archive -DestinationPath $baselinePath -Force
$mono='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/bin/mono.exe'
$compiler='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/lib/mono/4.5/csc.exe'
foreach($version in @('before','after')){
    $sourceRoot=if($version -eq 'before'){$baselinePath}else{$root};$exe=Join-Path $output ($version+'.exe')
    & $mono $compiler /nologo /langversion:latest ('/out:'+$exe) (Get-CityModelSources $sourceRoot) (Join-Path $root 'Tools/Diagnostics/CityR1ScaleBenchmark.cs')
    if($LASTEXITCODE -ne 0){throw "$version compile failed"}
    & $mono $exe (Join-Path $output ($version+'.csv')) $version
    if($LASTEXITCODE -ne 0){throw "$version benchmark failed"}
}
