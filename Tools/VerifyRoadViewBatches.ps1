param([string]$Results='Temp/RoadViewBatching-20261022/geometry',[string]$Save='Temp/Speed50Analysis-20261021/input.hcity',[string]$Reference='20573fec96cb5d7b6fa533c4b0686dde972150b3')
$ErrorActionPreference='Stop';. (Join-Path $PSScriptRoot 'ProjectLayout.ps1');$root=Split-Path $PSScriptRoot -Parent
if(Test-Path -LiteralPath $Results){throw 'Fresh results required'};New-Item -ItemType Directory $Results|Out-Null
$output=[IO.Path]::GetFullPath((Join-Path $root $Results));$project=Join-Path $root 'Temp/RoadCoreUnity'
if(@(Get-Process HarborCity -ErrorAction SilentlyContinue).Count){throw 'No overlapping Players allowed'}
Sync-CityWorkspaceProject $root $project
$extra=Join-Path $project 'Assets/Diagnostics/RoadBatchChecks';New-Item -ItemType Directory $extra -Force|Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Diagnostics/HarborCityRoadBatchValidation.cs') -Destination $extra
$ref=(& git rev-parse --verify ($Reference+'^{commit}')).Trim();if($LASTEXITCODE -ne 0){throw 'Invalid reference'}
$source=& git show ($ref+':Assets/Scripts/Runtime/Presentation/World/HarborCitySurfaceMesh.cs');if($LASTEXITCODE -ne 0){throw 'Frozen Ribbon source missing'}
[IO.File]::WriteAllLines((Join-Path $extra 'HarborCitySurfaceMeshReference.cs'),@($source|ForEach-Object{$_.Replace('public static class HarborCitySurfaceMesh','public static class HarborCitySurfaceMeshReference')}))
$env:HARBOR_ROAD_BATCH_OUTPUT=$output;$env:HARBOR_ROAD_BATCH_INPUT=(Resolve-Path -LiteralPath $Save).Path
@{reference=$ref;commit=(& git rev-parse HEAD);input=$env:HARBOR_ROAD_BATCH_INPUT;inputHash=(Get-FileHash -LiteralPath $Save).Hash;dirty=@(& git status --porcelain)}|ConvertTo-Json|Set-Content (Join-Path $output 'environment.json')
$p=Start-Process 'C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Unity.exe' -ArgumentList '-batchmode','-quit','-projectPath',('"'+$project+'"'),'-executeMethod','HarborCityRoadBatchValidation.Run','-logFile',('"'+(Join-Path $output 'editor.log')+'"') -WindowStyle Hidden -PassThru
if(-not $p.WaitForExit(300000)){$p.Kill($true);$p.WaitForExit();throw 'Native geometry checks exceeded 300 seconds'}
if(Test-Path (Join-Path $output 'Result.txt')){Get-Content (Join-Path $output 'Result.txt')}
if($p.ExitCode -ne 0){Get-Content (Join-Path $output 'editor.log') -Tail 15;throw 'Native geometry validation failed'}
