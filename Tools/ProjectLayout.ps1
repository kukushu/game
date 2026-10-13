# Shared project paths. Runtime model sources stay in one assembly; Editor/Unity view files are excluded for Mono tests.
function Get-CityModelSources([string]$ProjectRoot) {
    $assets=Join-Path $ProjectRoot 'Assets'
    @(Get-ChildItem -LiteralPath $assets -Filter 'City*.cs' -Recurse | Where-Object {
        $_.FullName -notmatch '[/\\]Editor[/\\]' -and $_.Name -notin @('CityLandscape.cs','CityRoadView.cs')
    } | Sort-Object Name | ForEach-Object FullName)
}
function Sync-CityWorkspaceProject([string]$SourceRoot,[string]$ProjectRoot) {
    $source=[IO.Path]::GetFullPath($SourceRoot);$project=[IO.Path]::GetFullPath($ProjectRoot)
    $temporary=[IO.Path]::GetFullPath((Join-Path $source 'Temp'))+[IO.Path]::DirectorySeparatorChar
    if(-not $project.StartsWith($temporary,[StringComparison]::OrdinalIgnoreCase)){throw 'Workspace copies must remain under this project Temp directory'}
    $targetAssets=Join-Path $project 'Assets';$sourceAssets=Join-Path $source 'Assets'
    if(Test-Path -LiteralPath $targetAssets){
        # Copying with /E alone leaves renamed scripts, causing duplicate types/GUIDs.
        # Delete only obsolete C# files and their sidecars in the disposable project.
        foreach($file in Get-ChildItem -LiteralPath $targetAssets -Filter '*.cs' -Recurse){
            $relative=$file.FullName.Substring($targetAssets.Length+1)
            if(-not(Test-Path -LiteralPath (Join-Path $sourceAssets $relative))){
                Remove-Item -LiteralPath $file.FullName
                if(Test-Path -LiteralPath ($file.FullName+'.meta')){Remove-Item -LiteralPath ($file.FullName+'.meta')}
            }
        }
        # Remove empty obsolete code directories and their folder GUID sidecars.
        foreach($folder in Get-ChildItem -LiteralPath $targetAssets -Directory -Recurse | Sort-Object FullName -Descending){
            $relative=$folder.FullName.Substring($targetAssets.Length+1)
            if(-not(Test-Path -LiteralPath (Join-Path $sourceAssets $relative)) -and @(Get-ChildItem -LiteralPath $folder.FullName -Force).Count -eq 0){
                Remove-Item -LiteralPath $folder.FullName
                if(Test-Path -LiteralPath ($folder.FullName+'.meta')){Remove-Item -LiteralPath ($folder.FullName+'.meta')}
            }
        }
    }
    foreach($folder in @('Assets','Packages','ProjectSettings')){
        & robocopy (Join-Path $source $folder) (Join-Path $project $folder) /E /NFL /NDL /NJH /NJS /NP | Out-Null
        if($LASTEXITCODE -ge 8){throw "Workspace copy failed: $folder"}
    }
}
