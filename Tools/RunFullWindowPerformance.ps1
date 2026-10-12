param([Parameter(Mandatory=$true)][string]$OutputDirectory,[ValidateSet('Build','Run')][string]$Stage='Build',[switch]$LegacyRoadKeyHash,[switch]$DevelopmentBuild,[string]$UnityEditor='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Unity.exe')
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent;$output=[IO.Path]::GetFullPath($OutputDirectory);$project=Join-Path $root 'Temp/TrafficBenchmarkProject'
New-Item -ItemType Directory -Path $output -Force | Out-Null
$player=Join-Path $output 'Player/HarborCityFullWindow.exe'
if($Stage -eq 'Build'){
    foreach($old in @('Assets/CityRoadKeyExperiment.cs','Assets/CityRoadKeyExperiment.cs.meta')){$oldPath=Join-Path $project $old;if((Test-Path -LiteralPath $oldPath) -and -not(Test-Path -LiteralPath (Join-Path $root $old))){Remove-Item -LiteralPath $oldPath}}
    foreach($folder in @('Assets','Packages','ProjectSettings')){& robocopy (Join-Path $root $folder) (Join-Path $project $folder) /E /NFL /NDL /NJH /NJS /NP | Out-Null;if($LASTEXITCODE -ge 8){throw 'Copy failed'}}
    $env:HARBOR_FULL_WINDOW_PLAYER=$player;$env:HARBOR_BENCHMARK_LEGACY_ROAD_HASH=if($LegacyRoadKeyHash){'1'}else{'0'}
    $env:HARBOR_FULL_WINDOW_DEVELOPMENT=if($DevelopmentBuild){'1'}else{'0'}
    $hashes=Get-ChildItem -LiteralPath (Join-Path $project 'Assets') -Filter '*.cs' -Recurse | Sort-Object FullName | ForEach-Object {($_.FullName.Substring($project.Length+1))+':'+(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash}
    [IO.File]::WriteAllLines((Join-Path $output 'build-source.txt'),$hashes);[IO.File]::WriteAllText((Join-Path $output 'build-options.txt'),"CITY_TRAFFIC_WINDOW_MEASUREMENT=1`nCITY_ROAD_KEY_HASH_DISABLED="+$env:HARBOR_BENCHMARK_LEGACY_ROAD_HASH+"`nDevelopmentBuild="+$env:HARBOR_FULL_WINDOW_DEVELOPMENT+"`n")
    $p=Start-Process -FilePath $UnityEditor -ArgumentList '-batchmode','-quit','-projectPath',('"'+$project+'"'),'-executeMethod','HarborCity.CityTrafficBenchmarkBuild.BuildFullWindowPlayer','-logFile',('"'+(Join-Path $output 'build.log')+'"') -WindowStyle Hidden -PassThru;$p.WaitForExit();if($p.ExitCode -ne 0){throw 'Full-window build failed'}
}else{
    if(Test-Path (Join-Path $output 'summary.csv')){throw 'Use a fresh output directory'}
    $p=Start-Process -FilePath $player -ArgumentList '-harbor-window-performance',('"'+(Join-Path $root 'Tools/Benchmarks/traffic-city.json')+'"'),'-window-output',('"'+$output+'"'),'-logFile',('"'+(Join-Path $output 'player.log')+'"') -WindowStyle Normal -PassThru
    Write-Output "Visible player PID=$($p.Id); observe and activate the returned game window, then create $output/START.txt."
}
