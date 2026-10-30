param([string]$Save='C:/Users/16934/Documents/unity_game/test/Temp/CityScenarios/regenerated30000-20261017/city.hcity',[string]$Results='Temp/ScaleArchitecture/census')
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
. (Join-Path $PSScriptRoot 'ProjectLayout.ps1')
$target=Join-Path $root $Results
if(Test-Path -LiteralPath (Join-Path $target 'census.csv')){throw 'Choose a fresh census output; prior evidence is preserved'}
New-Item -ItemType Directory -Force -Path $target|Out-Null
$binary=Build-CityDotNetHost -ProjectRoot $root -OutputDirectory $target -Entry (Join-Path $PSScriptRoot 'Diagnostics/CityScaleCensus.cs')
& dotnet $binary (Resolve-Path -LiteralPath $Save).Path (Join-Path $target 'census.csv')
if($LASTEXITCODE -ne 0){throw 'City census failed'}
