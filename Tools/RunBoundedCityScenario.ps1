#requires -Version 7.0
param([string]$Scenario='Tools/Scenarios/mature-grid-3000.json',[string]$Results,[string]$Load='', [switch]$Profile,[switch]$WasteDiagnostics,[int]$TimeoutSeconds=600,[int]$StallSeconds=120,[double]$MaxMemoryGB=8,[ValidateSet("Mono","DotNet64")][string]$Runtime="Mono",[string]$WorldReference="",[switch]$AllowPopulationChange)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
Set-Location -LiteralPath $root
if(-not $Results -or $TimeoutSeconds -lt 10 -or $StallSeconds -lt 10 -or $MaxMemoryGB -lt .5){throw 'Explicit fresh results and finite resource limits required'}
$target=[IO.Path]::GetFullPath((Join-Path $root $Results));$control=$target+'.control'
if((Test-Path -LiteralPath $target) -or (Test-Path -LiteralPath $control)){throw 'Existing evidence is never overwritten'}
New-Item -ItemType Directory -Path $control|Out-Null
$arguments=@('-NoProfile','-File',('"'+(Join-Path $PSScriptRoot 'RunCityScenario.ps1')+'"'),'-Scenario',('"'+$Scenario+'"'),'-Results',('"'+$Results+'"'))
$arguments+=@("-Runtime",$Runtime);if($WorldReference){$arguments+=@("-WorldReference",$WorldReference)}
if($Load){$arguments+=@('-Load',('"'+$Load+'"'))};if($Profile){$arguments+='-Profile'};if($WasteDiagnostics){$arguments+='-WasteDiagnostics'};if($AllowPopulationChange){$arguments+='-AllowPopulationChange'}
$hostExecutable=(Get-Process -Id $PID).Path
$process=Start-Process $hostExecutable -ArgumentList $arguments -WorkingDirectory $root -WindowStyle Hidden -RedirectStandardOutput (Join-Path $control 'stdout.log') -RedirectStandardError (Join-Path $control 'stderr.log') -PassThru
$timer=[Diagnostics.Stopwatch]::StartNew();$lastProgress=0.0;$lastSize=-1L;$peakWorking=0L;$peakPrivate=0L;$reason=$null;$lowMemorySince=-1.0;$seen=@{};$tailTick=$null
$writer=[IO.StreamWriter]::new((Join-Path $control 'resources.csv'));$writer.WriteLine('real_seconds,working_set_bytes,private_bytes,available_memory_mb,observed_processes,timeline_bytes')
try{
 while(-not $process.HasExited){
  Start-Sleep -Milliseconds 1000
  $all=@(Get-CimInstance Win32_Process -Property ProcessId,ParentProcessId,Name)
  $ids=[Collections.Generic.HashSet[int]]::new();[void]$ids.Add($process.Id)
  do{$added=$false;foreach($p in $all){if($ids.Contains([int]$p.ParentProcessId) -and $ids.Add([int]$p.ProcessId)){$added=$true}}}while($added)
  $working=0L;$private=0L
  foreach($id in $ids){$p=Get-Process -Id $id -ErrorAction SilentlyContinue;if($null -ne $p){try{$p.Refresh();$working+=$p.WorkingSet64;$private+=$p.PrivateMemorySize64;$seen[$id.ToString()]=$p.ProcessName}catch{if(-not $p.HasExited){throw}}}}
  $peakWorking=[Math]::Max($peakWorking,$working);$peakPrivate=[Math]::Max($peakPrivate,$private)
  $memory=Get-CimInstance Win32_OperatingSystem -Property FreePhysicalMemory
  $available=[long]$memory.FreePhysicalMemory/1024
  $timeline=Join-Path $target 'timeline.jsonl';$size=if(Test-Path -LiteralPath $timeline){(Get-Item -LiteralPath $timeline).Length}else{0L}
  # Construction has no timeline yet; stdout plus timeline growth are bounded progress evidence.
  $outSize=(Get-Item -LiteralPath (Join-Path $control 'stdout.log')).Length
  $progress=$size+$outSize
  if($progress -ne $lastSize){$lastSize=$progress;$lastProgress=$timer.Elapsed.TotalSeconds}
  $writer.WriteLine(('{0:F3},{1},{2},{3:F1},{4},{5}' -f $timer.Elapsed.TotalSeconds,$working,$private,$available,$ids.Count,$size));$writer.Flush()
  if($available -lt 768){if($lowMemorySince -lt 0){$lowMemorySince=$timer.Elapsed.TotalSeconds}}else{$lowMemorySince=-1}
  if($timer.Elapsed.TotalSeconds -gt $TimeoutSeconds){$reason='wall-time-limit'}elseif($working -gt $MaxMemoryGB*1GB -or $private -gt $MaxMemoryGB*1GB){$reason='memory-limit'}elseif($lowMemorySince -ge 0 -and $timer.Elapsed.TotalSeconds-$lowMemorySince -gt 10){$reason='sustained-low-physical-memory'}elseif($timer.Elapsed.TotalSeconds-$lastProgress -gt $StallSeconds){$reason='no-observed-progress-limit'}
  if($reason){if(-not $process.HasExited){$process.Kill($true)};break}
 }
 $process.WaitForExit();$process.Refresh()
}catch{$reason="supervisor-error";[IO.File]::WriteAllText((Join-Path $control "supervisor-error.txt"),$_.ToString());if(-not $process.HasExited){$process.Kill($true)};$process.WaitForExit()}finally{$writer.Dispose()}
@{exitCode=$process.ExitCode;terminatedBy=$reason;wallSeconds=$timer.Elapsed.TotalSeconds;peakWorkingSetBytes=$peakWorking;peakPrivateBytes=$peakPrivate;maxMemoryGB=$MaxMemoryGB;timeoutSeconds=$TimeoutSeconds;stallSeconds=$StallSeconds;processes=$seen;note='OS-observed process-tree memory sampled ~1s; may miss shorter peaks; progress watchdog is file growth, not proof of model health. Forced termination cannot guarantee an in-memory failure save; latest flushed timeline and prior checkpoints retained.'}|ConvertTo-Json -Depth 5|Set-Content -Encoding UTF8 (Join-Path $control 'supervisor.json')
Get-Content -LiteralPath (Join-Path $control 'stdout.log') -Tail 8
if($reason){throw "Bounded run terminated: $reason; evidence $control"};if($process.ExitCode -ne 0){Get-Content -LiteralPath (Join-Path $control 'stderr.log') -Tail 12;throw 'Scenario failed; preserved output and checkpoints'}
