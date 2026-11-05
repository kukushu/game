param([Parameter(Mandatory=$true)][string]$Save,[Parameter(Mandatory=$true)][string]$Results,[ValidateSet('checks','census','benchmark')][string]$Mode='checks',[int]$WarmSteps=200,[int]$Steps=800)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'ProjectLayout.ps1')
$root=Split-Path $PSScriptRoot -Parent
Set-Location -LiteralPath $root
if($WarmSteps -lt 0 -or $WarmSteps -gt 20000 -or $Steps -lt 1 -or $Steps -gt 4000){throw 'Bounded warmup/steps required'}
$inputFile=(Resolve-Path -LiteralPath $Save).Path
$output=[IO.Path]::GetFullPath((Join-Path $root $Results))
$temporary=[IO.Path]::GetFullPath((Join-Path $root 'Temp'))+[IO.Path]::DirectorySeparatorChar
if(-not $output.StartsWith($temporary,[StringComparison]::OrdinalIgnoreCase)){throw 'Outputs must stay in this project Temp'}
if(Test-Path -LiteralPath $output){throw 'Choose fresh evidence output'}
New-Item -ItemType Directory -Path $output|Out-Null
$reference='0342fde1caf5991611e35798561f8b386f0bc275'
$frozen=@{}
if($Mode -ne 'census'){
    foreach($name in @('CityRoadCoreWorld.cs','CityRoadCoreOccupancy.cs','CityRoadCoreLaneChanges.cs')){
        $path=Join-Path $output ($name+'.reference.cs');$text=& git show ($reference+':Assets/Scripts/Runtime/Traffic/'+$name)
        if($LASTEXITCODE -ne 0){throw 'Frozen traffic source unavailable'}
        [IO.File]::WriteAllLines($path,$text);$frozen[$name]=$path
    }
}
$binaries=@{}
foreach($variant in @('reference','candidate')){
    $hostDirectory=Join-Path $output ($variant+'-host');New-Item -ItemType Directory -Path $hostDirectory|Out-Null
    $overrides=@{};$defines=''
    if($Mode -eq 'census'){$defines='TRAFFIC_HOTPATH_CENSUS';if($variant -eq 'reference'){$defines+=';TRAFFIC_HOTPATH_REFERENCE'}}
    elseif($variant -eq 'reference'){$overrides=$frozen}
    $binaries[$variant]=Build-CityDotNetHost -ProjectRoot $root -OutputDirectory $hostDirectory -Entry (Join-Path $PSScriptRoot 'Diagnostics/CityTrafficHotpathProbe.cs') -Defines $defines -SourceOverrides $overrides
}
$order=if($Mode -eq 'benchmark'){@('reference','candidate','candidate','reference')}else{@('reference','candidate')}
$counts=@{reference=0;candidate=0};$runs=@()
foreach($variant in $order){
    $counts[$variant]++;$target=Join-Path $output ($variant+'-'+$counts[$variant]);$runs+=$target
    $arguments=@($binaries[$variant],$inputFile,$target,$WarmSteps,$Steps);if($Mode -ne 'benchmark'){$arguments+='verify'}
    & dotnet @arguments
    if($LASTEXITCODE -ne 0){throw "$variant $Mode failed"}
}
foreach($file in @('start.hcity','final.hcity','states.csv')){
    $expected=(Get-FileHash -LiteralPath (Join-Path $runs[0] $file)).Hash
    foreach($run in $runs){if((Get-FileHash -LiteralPath (Join-Path $run $file)).Hash -ne $expected){throw "Full-state mismatch: $file $run"}}
}
if($Mode -ne 'benchmark'){
    if([IO.File]::ReadAllText((Join-Path $runs[0] 'small-states.csv')) -cne [IO.File]::ReadAllText((Join-Path $runs[1] 'small-states.csv'))){throw 'Small edit/cold state mismatch'}
}
@{mode=$Mode;baseline=$reference;candidate=(& git rev-parse HEAD);dirty=@(& git status --porcelain);input=$inputFile;inputHash=(Get-FileHash -LiteralPath $inputFile).Hash;warmSteps=$WarmSteps;steps=$Steps;runs=$runs;note='DotNet64 model only; census/check oracle overhead is not Player performance'}|ConvertTo-Json -Depth 4|Set-Content (Join-Path $output 'environment.json')
Write-Output "PASS ${Mode}: complete state matches frozen behavior; separate Player validation required"
