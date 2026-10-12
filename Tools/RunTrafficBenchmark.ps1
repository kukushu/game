param(
    [Parameter(Mandatory=$true)][string]$OutputDirectory,
    [ValidateSet('All','Build','Run','Export')][string]$Stage='All',
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
