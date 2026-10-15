param([string]$Scenario='Tools/Scenarios/mature-grid-500.json',[string]$Results='Temp/CityScenarios/latest',[string]$Load='', [switch]$Profile)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'ProjectLayout.ps1')
$root=Split-Path $PSScriptRoot -Parent
Set-Location -LiteralPath $root
$mono='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/bin/mono.exe'
$compiler='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/lib/mono/4.5/csc.exe'
if(Test-Path -LiteralPath $Results){throw 'Choose a fresh result directory; previous evidence is preserved'}
New-Item -ItemType Directory -Path $Results -Force|Out-Null
$designAsset='Assets/Resources/GameData/Economy/DefaultEconomy.asset'
$design=Join-Path $Results 'design.json'
python (Join-Path $PSScriptRoot 'Diagnostics/ExportScenarioDesign.py') $designAsset $design
if($LASTEXITCODE -ne 0){throw 'Authored design export failed; requires Python and PyYAML'}
$sources=Get-CityModelSources $root
$metadata=@{gitCommit=(& git rev-parse HEAD);dirty=(@(& git status --porcelain));scenarioHash=(Get-FileHash -LiteralPath $Scenario -Algorithm SHA256).Hash;utc=[DateTime]::UtcNow.ToString('o');unity=(Get-Content ProjectSettings/ProjectVersion.txt);mono=(& $mono --version);os=[Environment]::OSVersion.ToString();cpu=(Get-CimInstance Win32_Processor|Select-Object -ExpandProperty Name);ramBytes=(Get-CimInstance Win32_ComputerSystem).TotalPhysicalMemory;profile=[bool]$Profile;sourceHashes=@($sources+(Join-Path $root 'Tools/Diagnostics/CityScenarioRun.cs')|ForEach-Object{@{path=$_.Substring($root.Length+1);hash=(Get-FileHash -LiteralPath $_ -Algorithm SHA256).Hash}})}
$metadata.designAssetHash=(Get-FileHash -LiteralPath $designAsset -Algorithm SHA256).Hash
$metadata|ConvertTo-Json -Depth 8|Set-Content -LiteralPath (Join-Path $Results 'environment.json') -Encoding UTF8
$binary=Join-Path $Results 'CityScenarioRun.exe'
$flags=@('/nologo','/langversion:latest','/r:System.Web.Extensions.dll',('/out:'+$binary))
if($Profile){$flags+='/define:CITY_CORE_ATTRIBUTION'}
& $mono $compiler @flags $sources (Join-Path $root 'Tools/Diagnostics/CityScenarioRun.cs')
if($LASTEXITCODE -ne 0){throw 'Scenario runner compile failed'}
$runArgs=@($Scenario,$Results,$(if($Profile){'profile'}else{'normal'}),$(if($Load){$Load}else{'-'}),$design)
& $mono $binary @runArgs
if($LASTEXITCODE -ne 0){throw "Scenario failed; preserved evidence: $Results"}
Write-Output "Results: $Results/report.json"
