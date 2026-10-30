param([string]$Save='C:/Users/16934/Documents/unity_game/test/Temp/CityScenarios/regenerated30000-20261017/city.hcity',[string]$Results='Temp/ScaleArchitecture/wake-shadow')
$ErrorActionPreference='Stop';$root=Split-Path $PSScriptRoot -Parent
. (Join-Path $PSScriptRoot 'ProjectLayout.ps1')
$out=Join-Path $root $Results;if(Test-Path -LiteralPath $out){throw 'Fresh shadow output required'}
New-Item -ItemType Directory -Path $out|Out-Null
python (Join-Path $PSScriptRoot 'Diagnostics/PrepareResidentWakeShadow.py') $root (Join-Path $out 'sources')
if($LASTEXITCODE -ne 0){throw 'Shadow patch preparation failed'}
foreach($version in @('old','shadow')){
 $dir=Join-Path $out $version;New-Item -ItemType Directory -Path $dir|Out-Null
 $entry=Join-Path $PSScriptRoot 'Diagnostics/ResidentWakeReplay.cs';$overrides=@{};$defines=''
 if($version -eq 'shadow'){$entry=Join-Path $out 'sources/ShadowSupport.cs';$defines='WAKE_SHADOW';$overrides=@{'CityCoreResidents.cs'=(Join-Path $out 'sources/CityCoreResidents.shadow.cs')}}
 $binary=Build-CityDotNetHost -ProjectRoot $root -OutputDirectory $dir -Entry $entry -Defines $defines -SourceOverrides $overrides
 & dotnet $binary (Resolve-Path -LiteralPath $Save).Path $dir | Tee-Object (Join-Path $dir 'Result.txt')
 if($LASTEXITCODE -ne 0){throw ($version+' wake replay failed')}
}
if([IO.File]::ReadAllText((Join-Path $out 'old/states.csv')) -cne [IO.File]::ReadAllText((Join-Path $out 'shadow/states.csv'))){throw 'Shadow changed complete city state'}
if((Get-FileHash -LiteralPath (Join-Path $out 'old/final.hcity')).Hash -ne (Get-FileHash -LiteralPath (Join-Path $out 'shadow/final.hcity')).Hash){throw 'Shadow changed final full city state'}
& dotnet (Join-Path $out 'shadow/bin/Release/net10.0/CityModelHost.dll') (Resolve-Path -LiteralPath $Save).Path (Join-Path $out 'shadow') negative-control | Tee-Object (Join-Path $out 'negative-control.txt')
if($LASTEXITCODE -ne 0){throw 'Shadow negative control failed'}
Write-Output 'PASS guard-only shadow versus original complete city checkpoints; no polling replaced, not a CPU performance claim'
