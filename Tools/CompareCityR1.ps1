param([string]$Baseline='789f7f2',[string]$Results='Temp/RoadCore/R1Pair')
. (Join-Path $PSScriptRoot 'ProjectLayout.ps1')
$ErrorActionPreference='Stop';$root=Split-Path $PSScriptRoot -Parent
$output=Join-Path $root $Results;New-Item -ItemType Directory -Path $output -Force|Out-Null
$baselinePath=Join-Path $output 'Baseline';$archive=Join-Path $output 'baseline.zip'
& git archive --format=zip ('--output='+$archive) $Baseline Assets/Scripts Tools/ProjectLayout.ps1
if($LASTEXITCODE -ne 0){throw 'Baseline archive failed'}
Expand-Archive -LiteralPath $archive -DestinationPath $baselinePath -Force
# Same fixture source for both sides, including historical versions that predate it.
Copy-Item -LiteralPath (Join-Path $root 'Assets/Scripts/Diagnostics/Runtime/CityIntegratedScaleFixture.cs') -Destination (Join-Path $baselinePath 'Assets/Scripts/Diagnostics/Runtime/CityIntegratedScaleFixture.cs')
$mono='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/bin/mono.exe'
$compiler='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/lib/mono/4.5/csc.exe'
foreach($version in @('before','after')){
    $sourceRoot=if($version -eq 'before'){$baselinePath}else{$root};$exe=Join-Path $output ($version+'.exe')
    $defines=if(Test-Path -LiteralPath (Join-Path $sourceRoot 'Assets/Scripts/Runtime/Traffic/CityTripTask.cs')){@()}else{@('/define:BASELINE_INLINE_TASKS')}
    & $mono $compiler /nologo /langversion:latest $defines ('/out:'+$exe) (Get-CityModelSources $sourceRoot) (Join-Path $root 'Tools/Diagnostics/CityR1ScaleBenchmark.cs')
    if($LASTEXITCODE -ne 0){throw "$version compile failed"}
    & $mono $exe (Join-Path $output ($version+'.csv')) $version
    if($LASTEXITCODE -ne 0){throw "$version benchmark failed"}
}
foreach($population in @(100,1000,3000)){
    $before=Get-Content -LiteralPath (Join-Path $output "before.csv.$population.semantic.sha256")
    $after=Get-Content -LiteralPath (Join-Path $output "after.csv.$population.semantic.sha256")
    if($before -ne $after){throw "Paired semantic state differs at $population residents"}
}
