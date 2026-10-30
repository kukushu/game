param([ValidateSet(4096,16384)][int]$Capacity=4096,[Parameter(Mandatory=$true)][string]$BuildTag)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
if($BuildTag -notmatch '^[a-zA-Z0-9_-]+$'){throw 'Simple build tag required'}
$out=Join-Path $root ('Temp/ResidentScheduling/'+$BuildTag)
if(Test-Path -LiteralPath (Join-Path $out 'Player/HarborCity.exe')){throw 'Preserve prior Player; use a fresh BuildTag'}
& (Join-Path $PSScriptRoot 'RunResidentSchedulingDiagnosis.ps1') -Variant legacy -BuildTag $BuildTag -PrepareOnly
python (Join-Path $PSScriptRoot 'Diagnostics/PrepareWasteCacheProbe.py') $root $Capacity
if($LASTEXITCODE -ne 0){throw 'Waste probe preparation failed'}
$project=Join-Path $root 'Temp/ResidentScheduling/Unity'
$env:HARBOR_TRAFFIC_VALIDATION_PLAYER=Join-Path $out 'Player/HarborCity.exe'
$env:HARBOR_PLAYER_RELEASE='1';$env:HARBOR_CORE_ATTRIBUTION='1'
$p=Start-Process 'C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Unity.exe' -ArgumentList @('-batchmode','-quit','-projectPath',('"'+$project+'"'),'-executeMethod','HarborCity.CityPlayerBuild.BuildResearchValidation','-logFile',('"'+(Join-Path $out 'build.log')+'"')) -WindowStyle Hidden -PassThru
$p.WaitForExit();if($p.ExitCode -ne 0){throw ('Player build failed: '+$out)}
Write-Output ('Built Temp-only cache experiment '+$Capacity)
