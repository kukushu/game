param([Parameter(Mandatory=$true)][string]$Before,[Parameter(Mandatory=$true)][string]$After,[string[]]$ExpectedChangedSources=@())
$ErrorActionPreference='Stop'
$a=@(Import-Csv (Join-Path $Before 'summary.csv'));$b=@(Import-Csv (Join-Path $After 'summary.csv'))
if($a.Count -ne 6 -or $b.Count -ne 6){throw 'Expected six full rendered runs on each side'}
foreach($key in @('unity','save_sha256','resolution','steps','warmup','repeats','target_fps','vsync','maximum_delta','gc_allocation_control_passed')){
    $left=Get-Content (Join-Path $Before 'configuration.txt') | Where-Object {$_ -like "$key=*"};$right=Get-Content (Join-Path $After 'configuration.txt') | Where-Object {$_ -like "$key=*"};if($left -ne $right){throw "Configuration differs: $key"}
}
$sourceChanges=@(Compare-Object (Get-Content (Join-Path $Before 'build-source.txt')) (Get-Content (Join-Path $After 'build-source.txt')))
foreach($change in $sourceChanges){$file=($change.InputObject -split ':',2)[0];if($file -notin $ExpectedChangedSources){throw "Unexpected changed full-window source: $file"}}
if($sourceChanges.Count -gt 0){$sourceChanges | Export-Csv (Join-Path $After 'source-changes.csv') -NoTypeInformation}
for($i=0;$i -lt 6;$i++){if($a[$i].state_sha256 -ne $b[$i].state_sha256 -or $a[$i].mode -ne $b[$i].mode -or $a[$i].steps -ne '300' -or $b[$i].steps -ne '300'){throw "Full-game state/step mismatch: $i"}}
foreach($directory in @($Before,$After)){
    $frames=@(Import-Csv (Join-Path $directory 'frames.csv'))
    foreach($run in 0..5){$selected=@($frames | Where-Object run -eq $run);if(($selected | Measure-Object steps -Sum).Sum -ne 300){throw "Frame step total incomplete: $directory/$run"}}
    if($frames | Where-Object focused -ne '1'){throw "Unfocused samples: $directory"}
    if((Get-Content (Join-Path $directory 'configuration.txt')) -contains 'gc_allocation_control_passed=True'){
        if(($frames | Measure-Object gc_bytes -Sum).Sum -le 0){throw "Known allocation probe passed but measured GC is empty: $directory"}
    }
}
$rows=foreach($mode in @('1x','3x')){
    $old=@($a | Where-Object mode -eq $mode);$new=@($b | Where-Object mode -eq $mode)
    # Older capture builds checked Valid only; an all-zero Player counter is unverified, not zero allocation.
    $oldGc=($old | Measure-Object gc_bytes_per_frame -Average).Average;$newGc=($new | Measure-Object gc_bytes_per_frame -Average).Average
    if($oldGc -le 0){$oldGc=-1};if($newGc -le 0){$newGc=-1}
    [pscustomobject]@{mode=$mode;before_fps=($old | Measure-Object fps -Average).Average;after_fps=($new | Measure-Object fps -Average).Average;before_simulation_rate=($old | Measure-Object simulation_seconds_per_wall_second -Average).Average;after_simulation_rate=($new | Measure-Object simulation_seconds_per_wall_second -Average).Average;before_p95_frame_ms=($old | Measure-Object p95_frame_ms -Average).Average;after_p95_frame_ms=($new | Measure-Object p95_frame_ms -Average).Average;before_gc_bytes_per_frame=$oldGc;after_gc_bytes_per_frame=$newGc}
}
$rows | Export-Csv (Join-Path $After 'paired-summary.csv') -NoTypeInformation;$rows | Format-List
'PASS: full rendered game, six paired 300-step states, source differences restricted to declared files, same configuration, complete frame step counts, all samples focused.'
