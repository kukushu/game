param([Parameter(Mandatory=$true)][string]$Before,[Parameter(Mandatory=$true)][string]$After,
    [string[]]$ExpectedChangedMarkers=@('City.Traffic.Junction.CrossingRadius.Compute'))
$ErrorActionPreference='Stop'
$a=@(Import-Csv (Join-Path $Before 'summary.csv'));$b=@(Import-Csv (Join-Path $After 'summary.csv'))
if($a.Count -ne 9 -or $b.Count -ne 9){throw 'Expected nine complete runs on each side'}
$aConfig=Get-Content (Join-Path $Before 'configuration.txt');$bConfig=Get-Content (Join-Path $After 'configuration.txt')
foreach($key in @('save_sha256','unity','cpu','os','steps','warmup','repeats','target_fps','maximum_delta')){
    if(($aConfig | Where-Object {$_ -like "$key=*"}) -ne ($bConfig | Where-Object {$_ -like "$key=*"})){throw "Configuration mismatch: $key"}
}
$aSteps=@(Import-Csv (Join-Path $Before 'steps.csv'));$bSteps=@(Import-Csv (Join-Path $After 'steps.csv'))
if($aSteps.Count -ne 2700 -or $bSteps.Count -ne 2700){throw 'Expected 2700 measured steps per version'}
for($i=0;$i -lt $aSteps.Count;$i++){
    foreach($field in @('run','mode','step','tasks')){if($aSteps[$i].$field -ne $bSteps[$i].$field){throw "Step pairing mismatch: $i/$field"}}
}
for($i=0;$i -lt 9;$i++){
    if($a[$i].state_sha256 -ne $b[$i].state_sha256 -or $a[$i].mode -ne $b[$i].mode){throw "Final native state mismatch: run $i"}
}
function Modules([string]$directory){
    $frames=@{};foreach($frame in (Import-Csv (Join-Path $directory 'frames.csv'))){$frames[$frame.ordinal]=$frame}
    $groups=@{};$covered=@{}
    foreach($row in (Import-Csv (Join-Path $directory 'modules.csv'))){
        if($row.path -like '*HarborCityGame.Update()*'){throw "Unexpected second city running in benchmark: $directory"}
        if($row.path -notlike '*CityTrafficBenchmark.Update()*'){continue}
        $frame=$frames[$row.ordinal];if($null -eq $frame -or [int]$frame.steps -eq 0){continue}
        $name=($row.path -split '/')[-1]
        if($name -eq 'City.Traffic.Movement'){
            if([int]$row.calls -ne [int]$frame.steps){throw "Profiler/frame step mismatch: $directory/$($row.ordinal)"}
            $covered[$row.ordinal]=$true
        }
        $key=$frame.mode+'/'+$name
        if(-not $groups.ContainsKey($key)){$groups[$key]=[pscustomobject]@{mode=$frame.mode;node=$name;total_ms=0.0;self_ms=0.0;calls=0.0;gc_bytes=0.0}}
        $g=$groups[$key];$g.total_ms+=[double]$row.total_ms;$g.self_ms+=[double]$row.self_ms;$g.calls+=[double]$row.calls;$g.gc_bytes+=[double]$row.gc_bytes
    }
    foreach($frame in $frames.Values){if([int]$frame.steps -gt 0 -and -not $covered.ContainsKey($frame.ordinal)){throw "Missing Profiler frame: $directory/$($frame.ordinal)"}}
    $groups.Values | Sort-Object mode,node | Export-Csv (Join-Path $directory 'module-summary.csv') -NoTypeInformation
    return $groups
}
$aModules=Modules $Before;$bModules=Modules $After
foreach($key in $aModules.Keys){
    if($aModules[$key].node -ne 'City.Traffic.Movement' -and $ExpectedChangedMarkers -contains $aModules[$key].node){continue}
    if(-not $bModules.ContainsKey($key) -or $aModules[$key].calls -ne $bModules[$key].calls){throw "Simulation module Calls changed: $key"}
}
$report=@()
foreach($mode in @('fixed','1x','3x')){
    $aa=@($a | Where-Object mode -eq $mode);$bb=@($b | Where-Object mode -eq $mode)
    $row=[ordered]@{mode=$mode}
    foreach($metric in @('mean_ms','p95_ms','p99_ms')){
        $old=($aa | Measure-Object $metric -Average).Average;$new=($bb | Measure-Object $metric -Average).Average
        $row["before_$metric"]=$old;$row["after_$metric"]=$new;$row["reduction_$metric"] = if($old -eq 0){0}else{100*($old-$new)/$old}
    }
    $oldGc=0.0;$newGc=0.0
    foreach($node in @('Movement','Services','ResidentsAndIndustry','Dispatch')){
        $oldGc+=$aModules[($mode+'/City.Traffic.'+$node)].gc_bytes
        $newGc+=$bModules[($mode+'/City.Traffic.'+$node)].gc_bytes
    }
    $row['before_profiler_gc_bytes_per_step']=$oldGc/900;$row['after_profiler_gc_bytes_per_step']=$newGc/900
    $row['reduction_profiler_gc']=100*($oldGc-$newGc)/$oldGc
    $report += [pscustomobject]$row
}
$report | Export-Csv (Join-Path $After 'paired-summary.csv') -NoTypeInformation
$report | Format-List
foreach($directory in @($Before,$After)){
    $live=@()
    $clocks=@{};foreach($clock in (Import-Csv (Join-Path $directory 'profiler-frames.csv'))){$clocks[$clock.ordinal]=[double]$clock.wall_ms/1000}
    foreach($mode in @('1x','3x')){
        $measured=@(Import-Csv (Join-Path $directory 'frames.csv') | Where-Object { $_.mode -eq $mode })
        $wall=0.0;foreach($frame in $measured){if(-not $clocks.ContainsKey($frame.ordinal)){throw 'Missing native frame clock'};$wall+=$clocks[$frame.ordinal]}
        $requested=($measured | Measure-Object requested_seconds -Sum).Sum
        $stepCount=($measured | Measure-Object steps -Sum).Sum
        $live += [pscustomobject]@{mode=$mode;frames=$measured.Count;wall_seconds=$wall;requested_sim_seconds=$requested;steps=$stepCount;observed_sim_seconds_per_wall_second=($stepCount*.05/$wall);mean_frame_advance_ms=($measured | Measure-Object advance_ms -Average).Average;mean_steps_per_frame=($stepCount/$measured.Count)}
    }
    $live | Export-Csv (Join-Path $directory 'live-summary.csv') -NoTypeInformation
}
Write-Output 'PASS: identical inputs/configuration, 2700 paired step task counts, nine final native states, all movement frame Calls match actual steps.'
