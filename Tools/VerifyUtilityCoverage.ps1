param([string]$Initial='Temp/CityScenarios/grid500-normal-final/initial.hcity',[string]$Reference='c729ad2')
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'ProjectLayout.ps1')
$root=Split-Path $PSScriptRoot -Parent
$dir=Join-Path $root 'Temp/CityScenarios/UtilityCoverageChecks';New-Item -ItemType Directory -Force $dir|Out-Null
$mono='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/bin/mono.exe'
$csc='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/lib/mono/4.5/csc.exe'
$sources=Get-CityModelSources $root;$harness=Join-Path $PSScriptRoot 'Diagnostics/CityUtilityCoverageChecks.cs'
& $mono $csc /nologo /langversion:latest ('/out:'+(Join-Path $dir 'after.exe')) $sources $harness
if($LASTEXITCODE -ne 0){throw 'Coverage compile failed'}
& $mono (Join-Path $dir 'after.exe') $Initial (Join-Path $dir 'after.csv') (Join-Path $dir 'after.hcity')
if($LASTEXITCODE -ne 0){throw 'Coverage checks failed'}
$old=& git show ($Reference+':Assets/Scripts/Runtime/Infrastructure/Utilities/CityUtilities.cs');if($LASTEXITCODE -ne 0){throw 'Reference unavailable'}
$referenceSource=Join-Path $dir 'CityUtilities.cs';[IO.File]::WriteAllLines($referenceSource,$old)
& $mono $csc /nologo /langversion:latest ('/out:'+(Join-Path $dir 'before.exe')) ($sources|Where-Object {$_ -notlike '*CityUtilities.cs'}) $referenceSource $harness
if($LASTEXITCODE -ne 0){throw 'Reference compile failed'}
& $mono (Join-Path $dir 'before.exe') $Initial (Join-Path $dir 'before.csv') (Join-Path $dir 'before.hcity')
if($LASTEXITCODE -ne 0){throw 'Reference coverage checks failed'}
foreach($file in @('csv','hcity')){if((Get-FileHash (Join-Path $dir ('before.'+$file))).Hash -ne (Get-FileHash (Join-Path $dir ('after.'+$file))).Hash){throw "Reference utility $file differs"}}
'PASS every supply component/flag/capacity and full saved city matches original global scan after dynamic edits/budgets'
