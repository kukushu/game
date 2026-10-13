param([switch]$Window)
. (Join-Path $PSScriptRoot 'ProjectLayout.ps1')
$ErrorActionPreference='Stop';$root=Split-Path $PSScriptRoot -Parent
$project=Join-Path $root 'Temp/RoadCoreUnity';$output=Join-Path $root 'Temp/RoadCore';$player=Join-Path $output 'Player/HarborCity.exe'
New-Item -ItemType Directory -Path $output -Force|Out-Null
Sync-CityWorkspaceProject $root $project
$env:HARBOR_TRAFFIC_VALIDATION_PLAYER=$player
$process=Start-Process -FilePath 'C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Unity.exe' -ArgumentList '-batchmode','-quit','-projectPath',('"'+$project+'"'),'-executeMethod','HarborCity.CityTrafficBenchmarkBuild.BuildResearchValidation','-logFile',('"'+(Join-Path $output 'build.log')+'"') -WindowStyle Hidden -PassThru
$process.WaitForExit();if($process.ExitCode -ne 0){throw 'Core Unity build failed'}
$mode=if($Window){'-harbor-core-window'}else{'-harbor-core-validate'}
$result=Join-Path $output $(if($Window){'Window'}else{'Native'})
$process=Start-Process -FilePath $player -ArgumentList $mode,('"'+$result+'"'),'-logFile',('"'+(Join-Path $output 'player.log')+'"') -WindowStyle $(if($Window){'Normal'}else{'Hidden'}) -PassThru
if(-not $process.WaitForExit(180000)){throw "Player did not finish; inspect $output/player.log (PID $($process.Id))"}
if($process.ExitCode -ne 0){throw 'Core player validation failed'}
Get-Content (Join-Path $result $(if($Window){'window.csv'}else{'Result.txt'}))
