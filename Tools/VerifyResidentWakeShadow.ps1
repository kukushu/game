param([string]$Save='C:/Users/16934/Documents/unity_game/test/Temp/CityScenarios/regenerated30000-20261017/city.hcity',[string]$Results='Temp/ScaleArchitecture/wake-shadow',[switch]$Full,[switch]$ControlsOnly,[switch]$Growth)
$ErrorActionPreference='Stop';if($ControlsOnly -and $Growth){throw 'Choose controls or growth'};$root=Split-Path $PSScriptRoot -Parent
. (Join-Path $PSScriptRoot 'ProjectLayout.ps1')
$out=Join-Path $root $Results;if(Test-Path -LiteralPath $out){throw 'Fresh shadow output required'}
New-Item -ItemType Directory -Path $out|Out-Null
$baselineSource=Join-Path $out 'CityResidents.baseline.cs'
$baseline=& git -C $root show '26d560073660285a7ca1a257f7baaccbc49d73db:Assets/Scripts/Runtime/Simulation/Residents/CityResidents.cs'
if($LASTEXITCODE -ne 0){throw 'Frozen MS010 baseline missing'}
[IO.File]::WriteAllLines($baselineSource,$baseline)
$Save=Export-CityMemberOrderFixture $root (Resolve-Path -LiteralPath $Save).Path $out
$prepareArgs=@($root,(Join-Path $out 'sources'));if($Full){$prepareArgs+='--full'};if($Growth){$prepareArgs+='--growth'}
python (Join-Path $PSScriptRoot 'Diagnostics/PrepareResidentWakeShadow.py') @prepareArgs
if($LASTEXITCODE -ne 0){throw 'Shadow patch preparation failed'}
foreach($version in @('old','shadow')){
 if($ControlsOnly -and $version -eq 'old'){continue}
 $dir=Join-Path $out $version;New-Item -ItemType Directory -Path $dir|Out-Null
 $entry=Join-Path $PSScriptRoot $(if($Growth){'Diagnostics/CityGrowthCandidateChecks.cs'}else{'Diagnostics/ResidentWakeReplay.cs'});$overrides=@{};$defines=''
 if($version -eq 'shadow'){$entry=Join-Path $out 'sources/ShadowSupport.cs';$defines='WAKE_SHADOW';if($Full){$defines+=';FULL_WAKE_SHADOW'};$overrides=@{'CityCoreResidents.cs'=(Join-Path $out 'sources/CityCoreResidents.shadow.cs')}}
 $binary=Build-CityDotNetHost -ProjectRoot $root -OutputDirectory $dir -Entry $entry -Defines $defines -SourceOverrides $overrides
 $runArgs=@((Resolve-Path -LiteralPath $Save).Path,$dir);if($ControlsOnly){$runArgs+='negative-control'}
 & dotnet $binary @runArgs | Tee-Object (Join-Path $dir 'Result.txt')
 if($LASTEXITCODE -ne 0){throw ($version+' wake replay failed')}
}
if($ControlsOnly){Write-Output 'PASS shadow targeted controls only';return}
if([IO.File]::ReadAllText((Join-Path $out 'old/states.csv')) -cne [IO.File]::ReadAllText((Join-Path $out 'shadow/states.csv'))){throw 'Shadow changed complete city state'}
if($Growth){Write-Output 'PASS growth shadow: complete state checkpoints and cold continuations identical';return}
if([IO.File]::ReadAllText((Join-Path $out 'old/requests.csv')) -cne [IO.File]::ReadAllText((Join-Path $out 'shadow/requests.csv'))){throw 'Shadow changed request sequence'}
if((Get-FileHash -LiteralPath (Join-Path $out 'old/final.hcity')).Hash -ne (Get-FileHash -LiteralPath (Join-Path $out 'shadow/final.hcity')).Hash){throw 'Shadow changed final full city state'}
& dotnet (Join-Path $out 'shadow/bin/Release/net10.0/CityModelHost.dll') (Resolve-Path -LiteralPath $Save).Path (Join-Path $out 'shadow') negative-control | Tee-Object (Join-Path $out 'negative-control.txt')
if($LASTEXITCODE -ne 0){throw 'Shadow negative control failed'}
Write-Output 'PASS shadow versus original complete city checkpoints; no polling replaced, not a CPU performance claim'
