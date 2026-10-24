param([Parameter(Mandatory)][string]$Save,[string]$Baseline='6a75a720ccc6124c119cedd9465e1fa522e32f50',[string]$Results='Temp/FreightOptimization-20261024/paired',[int]$Steps=6200)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'ProjectLayout.ps1')
$root=Split-Path $PSScriptRoot -Parent
$output=Join-Path $root $Results
if(Test-Path -LiteralPath $output){throw 'Choose a fresh result directory'}
New-Item -ItemType Directory -Path $output|Out-Null
$baselineRoot=Join-Path $output 'Baseline';$archive=Join-Path $output 'baseline.zip'
& git archive --format=zip ('--output='+$archive) $Baseline Assets/Scripts Tools/Diagnostics/CityScenarioJson.cs
if($LASTEXITCODE -ne 0){throw 'Baseline archive failed'}
Expand-Archive -LiteralPath $archive -DestinationPath $baselineRoot
foreach($version in @('before','after')){
    $sourceRoot=if($version -eq 'before'){$baselineRoot}else{$root}
    $hostRoot=Join-Path $output $version;New-Item -ItemType Directory -Path $hostRoot|Out-Null
    Build-CityDotNetHost $sourceRoot $hostRoot (Join-Path $root 'Tools/Diagnostics/CityFreightFundingReplay.cs')
    & dotnet (Join-Path $hostRoot 'bin/Release/net10.0/CityModelHost.dll') (Resolve-Path -LiteralPath $Save).Path (Join-Path $output $version) $Steps
    if($LASTEXITCODE -ne 0){throw "$version replay failed"}
}
if((Get-FileHash (Join-Path $output 'before.hcity')).Hash -ne (Get-FileHash (Join-Path $output 'after.hcity')).Hash){throw 'Complete final saved state differs'}
if((Get-FileHash (Join-Path $output 'before.csv')).Hash -ne (Get-FileHash (Join-Path $output 'after.csv')).Hash){throw 'A complete replay checkpoint differs'}
'PASS frozen/current exact full-state checkpoints and final save'
