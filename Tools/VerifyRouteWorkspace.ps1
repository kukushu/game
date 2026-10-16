param([string]$Results='Temp/CityScenarios/RouteWorkspaceChecks',[string]$Reference='0fb268d')
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'ProjectLayout.ps1')
$root=Split-Path $PSScriptRoot -Parent
if(Test-Path -LiteralPath $Results){throw 'Fresh results required'}
New-Item -ItemType Directory $Results|Out-Null
$dir=[IO.Path]::GetFullPath((Join-Path $root $Results));$sources=Get-CityModelSources $root
$mono='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/bin/mono.exe';$csc='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/lib/mono/4.5/csc.exe'
$old=& git show ($Reference+':Assets/Scripts/Runtime/Infrastructure/Roads/CityRoadCoreNetwork.cs');if($LASTEXITCODE -ne 0){throw 'Network reference unavailable'}
$source=Join-Path $dir 'CityRoadCoreNetwork.reference.cs';[IO.File]::WriteAllLines($source,$old)
foreach($version in @('before','after')){
 $files=if($version -eq 'before'){@($sources|Where-Object{$_ -notlike '*CityRoadCoreNetwork.cs'})+$source}else{$sources}
 $exe=Join-Path $dir ($version+'.exe')
 & $mono $csc /nologo /langversion:latest ('/out:'+$exe) $files (Join-Path $PSScriptRoot 'Diagnostics/CityRouteWorkspaceChecks.cs')
 if($LASTEXITCODE -ne 0){throw 'Route workspace compile failed'}
 & $mono $exe (Join-Path $dir ($version+'.bin'))
 if($LASTEXITCODE -ne 0){throw 'Route correctness failed'}
}
if((Get-FileHash (Join-Path $dir 'before.bin')).Hash -ne (Get-FileHash (Join-Path $dir 'after.bin')).Hash){throw 'Exact route sequence changed'}
'PASS all 600 directed route answers identical to Git reference; no old runtime engine'
