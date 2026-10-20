param([string]$OutputDirectory='Temp/RoadMigrationNative')
. (Join-Path $PSScriptRoot 'ProjectLayout.ps1')
$ErrorActionPreference='Stop';$root=Split-Path $PSScriptRoot -Parent
$output=[IO.Path]::GetFullPath((Join-Path $root $OutputDirectory));$project=Join-Path $root 'Temp/TrafficBenchmarkProject'
New-Item -ItemType Directory -Path $output -Force|Out-Null
Sync-CityWorkspaceProject $root $project
$player=Join-Path $output 'Player/HarborCity.exe';$env:HARBOR_TRAFFIC_VALIDATION_PLAYER=$player
$unity='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Unity.exe'
$process=Start-Process -FilePath $unity -ArgumentList '-batchmode','-quit','-projectPath',('"'+$project+'"'),'-executeMethod','HarborCity.CityPlayerBuild.BuildResearchValidation','-logFile',('"'+(Join-Path $output 'build.log')+'"') -WindowStyle Hidden -PassThru
$process.WaitForExit();if($process.ExitCode -ne 0){throw 'Native build failed'}
$results=Join-Path $output 'Results'
$process=Start-Process -FilePath $player -ArgumentList '-harbor-validate',('"'+$results+'"'),'-logFile',('"'+(Join-Path $output 'player.log')+'"') -WindowStyle Hidden -PassThru
$process.WaitForExit();if($process.ExitCode -ne 0 -or -not(Test-Path (Join-Path $results 'Result.txt'))){throw 'Native validation failed'}
Get-Content (Join-Path $results 'Result.txt')
