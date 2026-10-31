# Shared project paths. Runtime model sources stay in one assembly; Editor/Unity view files are excluded for Mono tests.
function Get-CityModelSources([string]$ProjectRoot) {
    $assets=Join-Path $ProjectRoot 'Assets'
    @(Get-ChildItem -LiteralPath $assets -Filter 'City*.cs' -Recurse | Where-Object {
        $_.FullName -notmatch '[/\\]Editor[/\\]' -and $_.Name -ne 'CityLandscape.cs'
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

function Set-CityTrafficReference([string]$SourceRoot,[string]$ProjectRoot,[string]$Reference) {
    $source=[IO.Path]::GetFullPath($SourceRoot);$project=[IO.Path]::GetFullPath($ProjectRoot)
    if(-not $project.StartsWith(([IO.Path]::GetFullPath((Join-Path $source 'Temp'))+[IO.Path]::DirectorySeparatorChar),[StringComparison]::OrdinalIgnoreCase)){throw 'Reference injection is restricted to Temp'}
    foreach($name in @('CityRoadCoreWorld.cs','CityRoadCoreLaneChanges.cs','CityRoadCoreOccupancy.cs')){
        $relative='Assets/Scripts/Runtime/Traffic/'+$name;$target=Join-Path $project $relative
        & git -C $source cat-file -e ($Reference+':'+$relative) 2>$null
        if($LASTEXITCODE -ne 0){
            if($name -ne 'CityRoadCoreOccupancy.cs'){throw 'Frozen traffic source missing'}
            # The baseline predates this partial: exclude it from the disposable
            # assembly, rather than adding a runtime compatibility stub.
            if(Test-Path -LiteralPath $target){Remove-Item -LiteralPath $target}
            if(Test-Path -LiteralPath ($target+'.meta')){Remove-Item -LiteralPath ($target+'.meta')}
            continue
        }
        $text=& git -C $source show ($Reference+':'+$relative);if($LASTEXITCODE -ne 0){throw 'Frozen traffic source read failed'}
        [IO.File]::WriteAllLines($target,$text)
    }
}
# Optional 64-bit test host; same model and entry source, no alternative simulator.
function Build-CityDotNetHost([string]$ProjectRoot,[string]$OutputDirectory,[string]$Entry,[string]$Defines='',[string]$WorldReference='',[string]$WorldSource='',[hashtable]$SourceOverrides=@{}) {
    $sources=Get-CityModelSources $ProjectRoot
    if($WorldSource){
        if($WorldReference){throw 'Choose a reference or instrumented source, not both'}
        $sources=@($sources|Where-Object {$_ -notlike '*CityRoadCoreWorld.cs'})+(Resolve-Path -LiteralPath $WorldSource).Path
    }
    if($WorldReference){
        $override=Join-Path $OutputDirectory 'CityRoadCoreWorld.reference.cs'
        $start=[Diagnostics.ProcessStartInfo]::new('git');$start.UseShellExecute=$false;$start.CreateNoWindow=$true;$start.RedirectStandardOutput=$true;$start.StandardOutputEncoding=[Text.Encoding]::UTF8
        foreach($argument in @('-C',$ProjectRoot,'show',($WorldReference+':Assets/Scripts/Runtime/Traffic/CityRoadCoreWorld.cs'))){$start.ArgumentList.Add($argument)}
        $reader=[Diagnostics.Process]::Start($start);$old=$reader.StandardOutput.ReadToEnd();$reader.WaitForExit();if($reader.ExitCode -ne 0){throw 'Reference source unavailable'}
        [IO.File]::WriteAllText($override,$old);$sources=@($sources|Where-Object {$_ -notlike '*CityRoadCoreWorld.cs'})+$override
    }
    if($SourceOverrides.ContainsKey('CityRoadCoreWorld.cs') -and ($WorldSource -or $WorldReference)){throw 'World override specified twice'}
    foreach($sourceName in $SourceOverrides.Keys){
        if([IO.Path]::GetFileName($sourceName) -ne $sourceName){throw 'Override keys must be existing model source basenames'}
        $matchingSources=@($sources|Where-Object {[IO.Path]::GetFileName($_) -eq $sourceName})
        if($matchingSources.Count -ne 1){throw "Expected one model source to override: $sourceName"}
        $sources=@($sources|Where-Object {[IO.Path]::GetFileName($_) -ne $sourceName})+(Resolve-Path -LiteralPath $SourceOverrides[$sourceName]).Path
    }
    $sources+=@($Entry,(Join-Path $ProjectRoot 'Tools/Diagnostics/CityScenarioJson.cs'))
    $items=($sources|ForEach-Object {'<Compile Include="'+[Security.SecurityElement]::Escape($_)+'" />'}) -join "`n"
    $project=Join-Path $OutputDirectory 'CityModelHost.csproj'
    $xml='<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework><PlatformTarget>x64</PlatformTarget><EnableDefaultCompileItems>false</EnableDefaultCompileItems><ImplicitUsings>disable</ImplicitUsings><Nullable>disable</Nullable><Optimize>true</Optimize><UseSharedCompilation>false</UseSharedCompilation><DefineConstants>CITY_SCENARIO_DOTNET;'+$Defines+'</DefineConstants><NuGetAudit>false</NuGetAudit></PropertyGroup><ItemGroup>'+$items+'</ItemGroup></Project>'
    [IO.File]::WriteAllText($project,$xml)
    $config=Join-Path $OutputDirectory 'NuGet.Config';[IO.File]::WriteAllText($config,'<configuration><packageSources><clear /></packageSources></configuration>')
    @{entry=$Entry;worldReference=$WorldReference;worldSource=$WorldSource;processTarget='x64';targetFramework='net10.0';configuration='Release';sources=@($sources|ForEach-Object {@{path=$_;hash=(Get-FileHash -LiteralPath $_).Hash}})}|ConvertTo-Json -Depth 5|Set-Content (Join-Path $OutputDirectory 'build-sources.json')
    $env:DOTNET_CLI_TELEMETRY_OPTOUT='1';$env:DOTNET_NOLOGO='1'
    & dotnet restore $project --configfile $config --disable-parallel -v quiet | Out-Host
    if($LASTEXITCODE -ne 0){throw 'Offline SDK restore failed; .NET 10 SDK/targeting pack required'}
    & dotnet build $project -c Release --no-restore --disable-build-servers -v quiet '-nodeReuse:false' | Out-Host
    if($LASTEXITCODE -ne 0){throw '64-bit model host build failed'}
    return (Join-Path $OutputDirectory 'bin/Release/net10.0/CityModelHost.dll')
}

# Test-only schema witnesses. Runtime CityCoreSave never calls these helpers.
function New-CityOrderWitnessEntry([string]$ProjectRoot,[string]$Entry,[string]$OutputDirectory){
 $body=[IO.File]::ReadAllText($Entry).Replace('SHA256.HashData(CityCoreSave.Write(s))','SHA256.HashData(CityOrderWire.Witness(s))').Replace('File.WriteAllBytes(','CityOrderWire.WriteNative(')
 $path=Join-Path $OutputDirectory 'OrderWitnessEntry.cs';[IO.File]::WriteAllText($path,$body+[Environment]::NewLine+[IO.File]::ReadAllText((Join-Path $ProjectRoot 'Tools/Diagnostics/CityOrderWire.cs')));return $path
}
function Export-CityMemberOrderFixture([string]$ProjectRoot,[string]$SourceSave,[string]$OutputDirectory){
 $reader=[IO.BinaryReader]::new([IO.File]::OpenRead($SourceSave));try{$magic=$reader.ReadInt32();$version=$reader.ReadInt32()}finally{$reader.Dispose()}
 if($magic -ne 0x48434932){throw 'Not a city fixture'};if($version -eq 18){return (Resolve-Path -LiteralPath $SourceSave).Path};if($version -ne 16 -and $version -ne 17){throw 'Test export expects frozen v16/v17 fixture'}
 $dir=Join-Path $OutputDirectory 'fixture-export';New-Item -ItemType Directory -Path $dir|Out-Null;$frozen=@{}
 foreach($relative in @('Core/CityCoreSave.cs','Core/CityEntities.cs','Industry/CityCoreIndustrySession.cs')){$name=[IO.Path]::GetFileName($relative);$path=Join-Path $dir $name;$source=& git -C $ProjectRoot show ('30ede55:Assets/Scripts/Runtime/Simulation/'+$relative);if($LASTEXITCODE -ne 0){throw 'Frozen fixture reader missing'};[IO.File]::WriteAllLines($path,$source);$frozen[$name]=$path}
 $entry=Join-Path $dir 'Export.cs';[IO.File]::WriteAllText($entry,[IO.File]::ReadAllText((Join-Path $ProjectRoot 'Tools/Diagnostics/CityOrderFixtureExport.cs'))+[Environment]::NewLine+[IO.File]::ReadAllText((Join-Path $ProjectRoot 'Tools/Diagnostics/CityOrderWire.cs')))
 $binary=Build-CityDotNetHost -ProjectRoot $ProjectRoot -OutputDirectory $dir -Entry $entry -SourceOverrides $frozen;$target=Join-Path $dir 'same-input-order18.hcity';& dotnet $binary $SourceSave $target|Out-Host;if($LASTEXITCODE -ne 0){throw 'Frozen same-input fixture export failed'};return $target
}
