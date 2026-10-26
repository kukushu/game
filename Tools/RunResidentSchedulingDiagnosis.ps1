param([ValidateSet('stock','detail','memo','legacy','retry','occupancy','occupancy-census','occupancy-trim','occupancy-active')][string]$Variant='stock',[switch]$PrepareOnly,[switch]$RunOnly,[ValidateSet('stock','counts','sampled','memo','legacy','retry','occupancy-timing','occupancy-counts','occupancy-trim','occupancy-active')][string]$Mode='stock',[string]$RunName='',[string]$SourceReference='',[string]$BuildTag='')
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
. (Join-Path $PSScriptRoot 'ProjectLayout.ps1')
if(-not $BuildTag){$BuildTag=$Variant};if($BuildTag -notmatch '^[a-zA-Z0-9_-]+$'){throw 'BuildTag must be a simple directory name'}
$out=Join-Path $root ('Temp/ResidentScheduling/'+$BuildTag)
$project=Join-Path $root 'Temp/ResidentScheduling/Unity'
New-Item -ItemType Directory -Force -Path $out | Out-Null
if($RunOnly){
 if(-not $RunName){throw 'Use a fresh RunName to preserve previous results'}
 $result=Join-Path $root ('Temp/ResidentScheduling/runs/'+$RunName)
 if(Test-Path -LiteralPath $result){throw 'Result directory already exists'}
 New-Item -ItemType Directory -Path $result | Out-Null
 $save=Join-Path (Split-Path -Parent $root) 'test/Temp/CityScenarios/regenerated30000-20261017/city.hcity'
 $player=Join-Path $out 'Player/HarborCity.exe'
 $p=Start-Process -FilePath $player -ArgumentList @('-harbor-city-scenario-window',('"'+$save+'"'),('"'+$result+'"'),'0','-harbor-window-saved-map','-harbor-depot-lane-routing','-screen-width','1440','-screen-height','900','-resident-mode',$Mode,'-logFile',('"'+(Join-Path $result 'player.log')+'"')) -PassThru
 if(-not $p.WaitForExit(240000)){
  [IO.File]::WriteAllText((Join-Path $result 'exit.txt'),'TIMEOUT; measurement Result.txt, if present, does not imply normal shutdown')
  # Own disposable child only; never stop the user's Editor or other Players.
  if($p.Path -eq $player){Stop-Process -Id $p.Id -ErrorAction SilentlyContinue}
  throw "Player exceeded four minutes: $result; normal shutdown not verified"
 }
 [IO.File]::WriteAllText((Join-Path $result 'exit.txt'),[string]$p.ExitCode)
 if($p.ExitCode -ne 0 -or -not(Test-Path -LiteralPath (Join-Path $result 'Result.txt'))){throw "Player failed: $result"}
 Write-Output "Completed $RunName";return
}
Sync-CityWorkspaceProject $root $project
python (Join-Path $PSScriptRoot 'Diagnostics/PrepareResidentProbe.py') $root $project $Variant $SourceReference
if($LASTEXITCODE -ne 0){throw 'Probe preparation failed'}
if($PrepareOnly){return}
$unity='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Unity.exe'
$env:HARBOR_TRAFFIC_VALIDATION_PLAYER=Join-Path $out 'Player/HarborCity.exe'
$env:HARBOR_PLAYER_RELEASE='1';$env:HARBOR_CORE_ATTRIBUTION=if($Variant -eq 'legacy'){'1'}else{'0'}
$p=Start-Process -FilePath $unity -ArgumentList @('-batchmode','-quit','-projectPath',('"'+$project+'"'),'-executeMethod','HarborCity.CityPlayerBuild.BuildResearchValidation','-logFile',('"'+(Join-Path $out 'build.log')+'"')) -WindowStyle Hidden -PassThru
$p.WaitForExit();if($p.ExitCode -ne 0){throw "Build failed: $out/build.log"}
Write-Output "Built $Variant Player"
