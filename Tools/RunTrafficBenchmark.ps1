param(
    [Parameter(Mandatory=$true)][string]$OutputDirectory,
    [ValidateSet('All','Build','Run','Export')][string]$Stage='All',
    [switch]$DisableRadiusCache,
    [switch]$DisableEdgeIndex,
    [string]$SavePath,
    [string]$UnityEditor='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Unity.exe'
)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
if(-not $SavePath){$SavePath=Join-Path $PSScriptRoot 'Benchmarks/traffic-city.json'}
$SavePath=(Resolve-Path -LiteralPath $SavePath).Path
$output=[IO.Path]::GetFullPath($OutputDirectory)
$project=Join-Path $root 'Temp/TrafficBenchmarkProject'
$player=Join-Path $output 'Player/TrafficBenchmark.exe'
New-Item -ItemType Directory -Path $output -Force | Out-Null
function UnityJob([string]$method,[string]$log){
    $process=Start-Process -FilePath $UnityEditor -ArgumentList '-batchmode','-quit','-projectPath',('"'+$project+'"'),'-executeMethod',$method,'-logFile',('"'+$log+'"') -WindowStyle Hidden -PassThru
    $process.WaitForExit();if($process.ExitCode -ne 0){throw "Unity failed: $log"}
}
if($Stage -in @('All','Build')){
    foreach($folder in @('Assets','Packages','ProjectSettings')){
        & robocopy (Join-Path $root $folder) (Join-Path $project $folder) /E /NFL /NDL /NJH /NJS /NP | Out-Null
        if($LASTEXITCODE -ge 8){throw "Copy failed: $folder"}
    }
    $env:HARBOR_BENCHMARK_PLAYER=$player
    $env:HARBOR_BENCHMARK_DISABLE_RADIUS_CACHE=if($DisableRadiusCache){'1'}else{'0'}
    $env:HARBOR_BENCHMARK_DISABLE_EDGE_INDEX=if($DisableEdgeIndex){'1'}else{'0'}
    $sourceHashes=Get-ChildItem -LiteralPath (Join-Path $project 'Assets') -Filter '*.cs' -Recurse | Sort-Object FullName | ForEach-Object {($_.FullName.Substring($project.Length+1))+':'+(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash}
    [IO.File]::WriteAllLines((Join-Path $output 'build-source.txt'),$sourceHashes)
    [IO.File]::WriteAllText((Join-Path $output 'build-options.txt'),"CITY_TRAFFIC_BENCHMARK=1`nCITY_JUNCTION_RADIUS_CACHE_DISABLED="+$env:HARBOR_BENCHMARK_DISABLE_RADIUS_CACHE+"`nCITY_TRAFFIC_EDGE_INDEX_DISABLED="+$env:HARBOR_BENCHMARK_DISABLE_EDGE_INDEX)
    UnityJob 'HarborCity.CityTrafficBenchmarkBuild.Build' (Join-Path $output 'build.log')
}
if($Stage -in @('All','Run')){
    if(Test-Path -LiteralPath (Join-Path $output 'summary.csv')){throw 'Choose a fresh output directory; benchmark evidence must not be overwritten.'}
    Copy-Item -LiteralPath $SavePath -Destination (Join-Path $output 'input.json')
    $version=(& git -C $root rev-parse HEAD).Trim()
    $process=Start-Process -FilePath $player -ArgumentList '-harbor-traffic-benchmark',('"'+$SavePath+'"'),'-benchmark-output',('"'+$output+'"'),'-benchmark-version',$version,'-logFile',('"'+(Join-Path $output 'player.log')+'"') -WindowStyle Hidden -PassThru
    $process.WaitForExit();if($process.ExitCode -ne 0 -or -not (Test-Path (Join-Path $output 'PASS.txt'))){throw "Player failed: $output"}
}
if($Stage -in @('All','Export')){
    $env:HARBOR_BENCHMARK_RESULTS=$output
    UnityJob 'HarborCity.CityTrafficBenchmarkBuild.Export' (Join-Path $output 'export.log')
}
Write-Output "Traffic benchmark results: $output"
