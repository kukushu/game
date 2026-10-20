param([switch]$Window,[switch]$City,[switch]$CityWindow,[switch]$Construction,[switch]$Trading,[switch]$Profile,[switch]$GrowingProfile,[string]$ProfileLabel='before')
. (Join-Path $PSScriptRoot 'ProjectLayout.ps1')
$ErrorActionPreference='Stop';$root=Split-Path $PSScriptRoot -Parent
$project=Join-Path $root 'Temp/RoadCoreUnity';$output=Join-Path $root 'Temp/RoadCore';$player=Join-Path $output 'Player/HarborCity.exe'
New-Item -ItemType Directory -Path $output -Force|Out-Null
Sync-CityWorkspaceProject $root $project
$env:HARBOR_TRAFFIC_VALIDATION_PLAYER=$player
$env:HARBOR_CORE_ATTRIBUTION=if($Profile -or $GrowingProfile){'1'}else{'0'}
$process=Start-Process -FilePath 'C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Unity.exe' -ArgumentList '-batchmode','-quit','-projectPath',('"'+$project+'"'),'-executeMethod','HarborCity.CityPlayerBuild.BuildResearchValidation','-logFile',('"'+(Join-Path $output 'build.log')+'"') -WindowStyle Hidden -PassThru
$process.WaitForExit();if($process.ExitCode -ne 0){throw 'Core Unity build failed'}
$mode=if($GrowingProfile){'-harbor-city-growing-profile'}elseif($Profile){'-harbor-city-profile'}elseif($Trading){'-harbor-city-trade-validate'}elseif($Construction){'-harbor-city-construction-validate'}elseif($CityWindow){'-harbor-city-window'}elseif($Window){'-harbor-core-window'}elseif($City){'-harbor-city-core-validate'}else{'-harbor-core-validate'}
$result=Join-Path $output $(if($GrowingProfile){'GrowingProfile-'+$ProfileLabel}elseif($Profile){'Profile-'+$ProfileLabel}elseif($Trading){'Trading'}elseif($Construction){'Construction'}elseif($CityWindow){'CityWindow'}elseif($Window){'Window'}elseif($City){'Industry'}else{'Native'})
$process=Start-Process -FilePath $player -ArgumentList $mode,('"'+$result+'"'),'-logFile',('"'+(Join-Path $output 'player.log')+'"') -WindowStyle $(if($Window -or $City -or $CityWindow -or $Construction -or $Trading){'Normal'}else{'Hidden'}) -PassThru
if(-not $process.WaitForExit(180000)){throw "Player did not finish; inspect $output/player.log (PID $($process.Id))"}
if($process.ExitCode -ne 0){throw 'Core player validation failed'}
Get-Content (Join-Path $result $(if($Profile -or $GrowingProfile){'ProfileResult.txt'}elseif($Trading){'TradeResult.txt'}elseif($Construction){'ConstructionResult.txt'}elseif($CityWindow){'city-window.csv'}elseif($Window){'window.csv'}elseif($City){'IndustryResult.txt'}else{'Result.txt'}))
