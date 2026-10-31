param([string]$Results='Temp/MillionScaleExecution/plan-invalidation', [switch]$TargetedOnly, [switch]$Growth)
$ErrorActionPreference='Stop'
if($TargetedOnly -and $Growth){throw 'Choose targeted or growth replay'}
$root=Split-Path $PSScriptRoot -Parent
. (Join-Path $PSScriptRoot 'ProjectLayout.ps1')
$out=Join-Path $root $Results
if(Test-Path -LiteralPath $out){throw 'Fresh output required'}
New-Item -ItemType Directory -Path $out|Out-Null
$reference='e7fd202066e34bcbcecf2581f8f59390caae30be'
$frozen=@{}
foreach($relative in @('Simulation/Residents/CityCoreResidents.cs','Traffic/CityRoadCoreWorld.cs')){
    $name=[IO.Path]::GetFileName($relative);$path=Join-Path $out $name
    $source=& git -C $root show ($reference+':Assets/Scripts/Runtime/'+$relative)
    if($LASTEXITCODE -ne 0){throw 'Frozen source missing'}
    [IO.File]::WriteAllLines($path,$source);$frozen[$name]=$path
}
$raw=Join-Path (Split-Path $root -Parent) 'test/Temp/CityScenarios/regenerated30000-20261017/city.hcity'
# Frozen reader exports a TEST copy. Runtime still accepts only native format18.
$save=Export-CityMemberOrderFixture $root $raw $out
foreach($version in @('new','old')){
    $dir=Join-Path $out $version;New-Item -ItemType Directory -Path $dir|Out-Null
    $defines='PLAN_INVALIDATIONS';$overrides=@{}
    if($version -eq 'old'){$defines='';$overrides=$frozen}
    $entry=Join-Path $PSScriptRoot 'Diagnostics/CityPlanInvalidationChecks.cs'
    if($Growth){
        $entry=Join-Path $dir 'GrowthChecks.cs'
        $body=[IO.File]::ReadAllText((Join-Path $PSScriptRoot 'Diagnostics/CityGrowthCandidateChecks.cs'))
        if($version -eq 'new'){
            $anchor='s.Transport.DepotLaneRoutingEnabled=true;'
            if(([regex]::Matches($body,[regex]::Escape($anchor))).Count -ne 1){throw 'Growth observer anchor changed'}
            $body=$body.Replace($anchor,'s.Residents.PlansInvalidated+=e=>{};'+$anchor)
        }
        [IO.File]::WriteAllText($entry,$body)
    }
    $binary=Build-CityDotNetHost -ProjectRoot $root -OutputDirectory $dir -Entry $entry -Defines $defines -SourceOverrides $overrides
    $runArgs=@($save,$dir);if($TargetedOnly){$runArgs+='targeted'}
    & dotnet $binary @runArgs | Tee-Object (Join-Path $dir 'result.txt')
    if($LASTEXITCODE -ne 0){throw ($version+' invalidation checks failed')}
    if($TargetedOnly){break}
}
if(!$TargetedOnly){
    $names=if($Growth){@('states.csv')}else{@('states.csv','requests.csv')}
    foreach($name in $names){
        if([IO.File]::ReadAllText((Join-Path $out ('old/'+$name))) -cne [IO.File]::ReadAllText((Join-Path $out ('new/'+$name)))){throw ('Replay differs: '+$name)}
    }
    if($Growth){Write-Output 'PASS frozen e7fd202 vs observed invalidations: exact growth native18 state checkpoints and cold80'}
    else{Write-Output 'PASS frozen e7fd202 vs observed invalidations: exact requests/native18 states and cold80'}
}
