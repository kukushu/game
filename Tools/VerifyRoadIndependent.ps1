param([string]$Results='Temp/RoadIndependentChecks',[string]$Reference='8c208aba618a2fea0bf7086625522ad1f48cf907')
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'ProjectLayout.ps1')
$root=Split-Path $PSScriptRoot -Parent
if(Test-Path -LiteralPath $Results){throw 'Fresh result directory required'}
New-Item -ItemType Directory -Path $Results | Out-Null
$output=(Resolve-Path -LiteralPath $Results).Path
foreach($mode in @('baseline','current')){
    $directory=Join-Path $output $mode;New-Item -ItemType Directory -Path $directory | Out-Null
    $overrides=@{}
    if($mode -eq 'baseline'){
        foreach($relative in @('Traffic/CityRoadCoreWorld.cs','Infrastructure/Roads/CityRoadCoreConstruction.cs','Simulation/Residents/CityCoreResidents.cs','Traffic/CityRoadCoreDispatchRoutes.cs','Infrastructure/Roads/CityRoadCoreEdit.cs','Simulation/Core/CityCoreConstruction.cs','Infrastructure/Utilities/CityUtilityLayout.cs')){
            $source=& git show ($Reference+':Assets/Scripts/Runtime/'+$relative)
            if($LASTEXITCODE -ne 0){throw 'Frozen source missing'}
            $name=Split-Path $relative -Leaf;$path=Join-Path $directory ('frozen-'+$name)
            [IO.File]::WriteAllLines($path,$source);$overrides[$name]=$path
        }
    }
    $binary=Build-CityDotNetHost $root $directory (Join-Path $PSScriptRoot 'Diagnostics/CityRoadIndependentChecks.cs') -SourceOverrides $overrides
    & dotnet $binary $mode $directory
    if($LASTEXITCODE -ne 0){throw ($mode+' independent-road checks failed')}
}
if((Get-FileHash (Join-Path $output 'baseline/independent.csv')).Hash -ne (Get-FileHash (Join-Path $output 'current/independent.csv')).Hash){throw 'Frozen/current complete state or acceptance differs'}
Write-Output ('PASS frozen/current complete states and outcomes match: '+$Reference)
