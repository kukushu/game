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

# Optional 64-bit test host; same model and entry source, no alternative simulator.
function Build-CityDotNetHost([string]$ProjectRoot,[string]$OutputDirectory,[string]$Entry,[string]$Defines='',[string]$WorldReference='') {
    $sources=Get-CityModelSources $ProjectRoot
    if($WorldReference){
        $override=Join-Path $OutputDirectory 'CityRoadCoreWorld.reference.cs'
        $start=[Diagnostics.ProcessStartInfo]::new('git');$start.UseShellExecute=$false;$start.CreateNoWindow=$true;$start.RedirectStandardOutput=$true;$start.StandardOutputEncoding=[Text.Encoding]::UTF8
        foreach($argument in @('-C',$ProjectRoot,'show',($WorldReference+':Assets/Scripts/Runtime/Traffic/CityRoadCoreWorld.cs'))){$start.ArgumentList.Add($argument)}
        $reader=[Diagnostics.Process]::Start($start);$old=$reader.StandardOutput.ReadToEnd();$reader.WaitForExit();if($reader.ExitCode -ne 0){throw 'Reference source unavailable'}
        [IO.File]::WriteAllText($override,$old);$sources=@($sources|Where-Object {$_ -notlike '*CityRoadCoreWorld.cs'})+$override
    }
    $sources+=@($Entry,(Join-Path $ProjectRoot 'Tools/Diagnostics/CityScenarioJson.cs'))
    $items=($sources|ForEach-Object {'<Compile Include="'+[Security.SecurityElement]::Escape($_)+'" />'}) -join "`n"
    $project=Join-Path $OutputDirectory 'CityModelHost.csproj'
    $xml='<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework><PlatformTarget>x64</PlatformTarget><EnableDefaultCompileItems>false</EnableDefaultCompileItems><ImplicitUsings>disable</ImplicitUsings><Nullable>disable</Nullable><Optimize>true</Optimize><UseSharedCompilation>false</UseSharedCompilation><DefineConstants>CITY_SCENARIO_DOTNET;'+$Defines+'</DefineConstants><NuGetAudit>false</NuGetAudit></PropertyGroup><ItemGroup>'+$items+'</ItemGroup></Project>'
    [IO.File]::WriteAllText($project,$xml)
    $config=Join-Path $OutputDirectory 'NuGet.Config';[IO.File]::WriteAllText($config,'<configuration><packageSources><clear /></packageSources></configuration>')
    @{entry=$Entry;worldReference=$WorldReference;processTarget='x64';targetFramework='net10.0';configuration='Release';sources=@($sources|ForEach-Object {@{path=$_;hash=(Get-FileHash -LiteralPath $_).Hash}})}|ConvertTo-Json -Depth 5|Set-Content (Join-Path $OutputDirectory 'build-sources.json')
    $env:DOTNET_CLI_TELEMETRY_OPTOUT='1';$env:DOTNET_NOLOGO='1'
    & dotnet restore $project --configfile $config --disable-parallel -v quiet | Out-Host
    if($LASTEXITCODE -ne 0){throw 'Offline SDK restore failed; .NET 10 SDK/targeting pack required'}
    & dotnet build $project -c Release --no-restore --disable-build-servers -v quiet '-nodeReuse:false' | Out-Host
    if($LASTEXITCODE -ne 0){throw '64-bit model host build failed'}
    return (Join-Path $OutputDirectory 'bin/Release/net10.0/CityModelHost.dll')
}
