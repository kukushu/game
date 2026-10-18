param([Parameter(Mandatory)][string]$Output,[Parameter(Mandatory)][string]$Log,[string]$Reference='',[switch]$Construction,[string]$CoreReference='')
$ErrorActionPreference='Stop';. (Join-Path $PSScriptRoot 'ProjectLayout.ps1');$root=Split-Path $PSScriptRoot -Parent
Sync-CityWorkspaceProject $root (Join-Path $root 'Temp/RoadCoreUnity')
if($Reference){
    foreach($relative in @('Assets/Scripts/Runtime/Infrastructure/Zoning/CityCoreZoning.cs','Assets/Scripts/Runtime/Presentation/World/HarborCityZoningViews.cs')){
        $frozen=& git show ($Reference+':'+$relative);if($LASTEXITCODE -ne 0){throw 'Reference unavailable'}
        [IO.File]::WriteAllLines((Join-Path $root ('Temp/RoadCoreUnity/'+$relative)),$frozen)
    }
    foreach($relative in @('Assets/Scripts/Runtime/Infrastructure/Zoning/CityCoreZoningGeometry.cs','Assets/Scripts/Editor/CityZoningViewChecks.cs')){
        $temporaryFile=Join-Path $root ('Temp/RoadCoreUnity/'+$relative);if(Test-Path -LiteralPath $temporaryFile){Remove-Item -LiteralPath $temporaryFile}
        if(Test-Path -LiteralPath ($temporaryFile+'.meta')){Remove-Item -LiteralPath ($temporaryFile+'.meta')}
    }
}
if($CoreReference){
    foreach($relative in @('Assets/Scripts/Runtime/Traffic/CityRoadCoreWorld.cs','Assets/Scripts/Runtime/Infrastructure/Roads/CityRoadCoreConstruction.cs','Assets/Scripts/Runtime/Infrastructure/Roads/CityRoadCoreNetwork.cs','Assets/Scripts/Runtime/Infrastructure/Roads/CityLaneConnection.cs','Assets/Scripts/Runtime/Infrastructure/Roads/CityRoadCoreSpatial.cs')){
        $frozen=& git show ($CoreReference+':'+$relative);if($LASTEXITCODE -ne 0){throw 'Core reference unavailable'}
        [IO.File]::WriteAllLines((Join-Path $root ('Temp/RoadCoreUnity/'+$relative)),$frozen)
    }
}
& python (Join-Path $PSScriptRoot 'Diagnostics/PrepareZoningEditProbe.py');if($LASTEXITCODE -ne 0){throw 'Instrumentation failed'}
if($Construction){& python (Join-Path $PSScriptRoot 'Diagnostics/PrepareConstructionProbe.py');if($LASTEXITCODE -ne 0){throw 'Construction instrumentation failed'}}
$env:HARBOR_TRAFFIC_VALIDATION_PLAYER=[IO.Path]::GetFullPath((Join-Path $root $Output));$env:HARBOR_CORE_ATTRIBUTION='1'
$run=Start-Process 'C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Unity.exe' -ArgumentList '-batchmode','-quit','-projectPath',('"'+(Join-Path $root 'Temp/RoadCoreUnity')+'"'),'-executeMethod','HarborCity.CityTrafficBenchmarkBuild.BuildResearchValidation','-logFile',('"'+[IO.Path]::GetFullPath((Join-Path $root $Log))+'"') -WindowStyle Hidden -PassThru;$handle=$run.Handle
if(-not $run.WaitForExit(600000)){$run.Kill();throw 'Diagnostic build timeout'};$run.WaitForExit();if($run.ExitCode -ne 0){throw 'Diagnostic build failed; log retained'}
$directory=Split-Path $env:HARBOR_TRAFFIC_VALIDATION_PLAYER
@{commit=(git rev-parse HEAD);dirty=@(git status --porcelain);sources=@(Get-ChildItem (Join-Path $root 'Temp/RoadCoreUnity/Assets/Scripts') -Recurse -Filter '*.cs'|ForEach-Object{@{path=$_.FullName;sha256=(Get-FileHash $_.FullName).Hash}})}|ConvertTo-Json -Depth 5|Set-Content (Join-Path $directory 'build-sources.json')
