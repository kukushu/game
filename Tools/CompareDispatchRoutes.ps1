param([string]$Baseline='8d37b3f',[string]$Results='Temp/RoadCore/BlueprintPair')
. (Join-Path $PSScriptRoot 'ProjectLayout.ps1')
$ErrorActionPreference='Stop';$root=Split-Path $PSScriptRoot -Parent
$output=Join-Path $root $Results;New-Item -ItemType Directory -Path $output -Force|Out-Null
$baselinePath=Join-Path $output 'Baseline';$archive=Join-Path $output 'baseline.zip'
& git archive --format=zip ('--output='+$archive) $Baseline Assets/Scripts Tools/ProjectLayout.ps1
if($LASTEXITCODE -ne 0){throw 'Baseline snapshot archive failed'}
Expand-Archive -LiteralPath $archive -DestinationPath $baselinePath -Force
$mono='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/bin/mono.exe'
$compiler='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/lib/mono/4.5/csc.exe'
foreach($version in @('before','after')){
    $sourceRoot=if($version -eq 'before'){$baselinePath}else{$root};$exe=Join-Path $output ($version+'.exe')
    $driver=Join-Path $root 'Tools/Diagnostics/CityCoreDispatchBenchmark.cs'
    if($version -eq 'before'){
        $driver=Join-Path $output 'HistoricalDispatchDriver.cs'
        $driverText=[IO.File]::ReadAllText((Join-Path $root 'Tools/Diagnostics/CityCoreDispatchBenchmark.cs'))
        if(-not(Test-Path -LiteralPath (Join-Path $sourceRoot 'Assets/Scripts/Runtime/Simulation/Core/CitySimulation.cs'))){
            # Same benchmark body/configuration; adapt only the historical dispatch API.
            # This is test tooling, never a runtime engine fallback.
            $driverText=$driverText.Replace('session.Simulation.RequestCommute','session.Transport.DispatchResident')
        }
        [IO.File]::WriteAllText($driver,$driverText,[Text.UTF8Encoding]::new($false))
    }
    & $mono $compiler /nologo /langversion:latest ('/out:'+$exe) (Get-CityModelSources $sourceRoot) $driver
    if($LASTEXITCODE -ne 0){throw "$version compile failed"}
    & $mono $exe (Join-Path $output ($version+'.csv')) $version
    if($LASTEXITCODE -ne 0){throw "$version benchmark failed"}
}
