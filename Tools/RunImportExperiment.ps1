param([Parameter(Mandatory)][ValidateSet('build','budget','run','facts')][string]$Mode,[Parameter(Mandatory)][string]$Save,[Parameter(Mandatory)][string]$Results,[int]$GridRadius=18,[int]$Spacing=48,[ValidateRange(20,4800)][int]$Steps=2400,[ValidateRange(8,128)][int]$Samples=64,[ValidateRange(30,900)][int]$TimeoutSeconds=600,[ValidateRange(30,600)][int]$StallSeconds=120,[string]$Config='Tools/Scenarios/import-endpoint-comparison.json')
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'ProjectLayout.ps1')
$root=Split-Path $PSScriptRoot -Parent
$configuration=Get-Content -LiteralPath (Join-Path $root $Config) -Raw|ConvertFrom-Json
if($configuration.schema -ne 1 -or ($configuration.imports -join ',') -ne '1,4,12'){throw 'Unsupported fixed comparison manifest'}
if(-not $PSBoundParameters.ContainsKey('GridRadius')){$GridRadius=[int]$configuration.gridRadius}
if(-not $PSBoundParameters.ContainsKey('Spacing')){$Spacing=[int]$configuration.spacing}
if(-not $PSBoundParameters.ContainsKey('Steps')){$Steps=[int]$configuration.steps}
if(-not $PSBoundParameters.ContainsKey('Samples')){$Samples=[int]$configuration.routeSampleFactories}
if($Steps -lt 20 -or $Steps -gt 4800 -or $Samples -lt 8 -or $Samples -gt 128){throw 'Manifest exceeds finite probe limits'}
if(Test-Path -LiteralPath $Results){throw 'Use fresh results; original evidence is preserved'}
$folder=[IO.Path]::GetFullPath((Join-Path $root $Results));New-Item -ItemType Directory -Path $folder|Out-Null
$inputSave=(Resolve-Path -LiteralPath $Save).Path
$world=Join-Path $folder 'CityRoadCoreWorld.observe.cs'
& python (Join-Path $PSScriptRoot 'InstrumentImportExperiment.py') $world
if($LASTEXITCODE -ne 0){throw 'Observation anchors changed; source review required'}
$binary=Build-CityDotNetHost $root $folder (Join-Path $PSScriptRoot 'Diagnostics/CityImportExperiment.cs') '' '' $world
$arguments=@(('"'+$binary+'"'),$Mode,('"'+$inputSave+'"'),('"'+$folder+'"'))
if($Mode -eq 'build'){$arguments+=@($GridRadius,$Spacing)}elseif($Mode -eq 'budget'){$arguments+=$Samples}else{$arguments+=$Steps}
@{commit=(& git rev-parse HEAD);mode=$Mode;inputHash=(Get-FileHash -LiteralPath $inputSave).Hash;configHash=(Get-FileHash -LiteralPath (Join-Path $root $Config)).Hash;gridRadius=$GridRadius;spacing=$Spacing;steps=$Steps;routeSampleFactories=$Samples;timeoutSeconds=$TimeoutSeconds;stallSeconds=$StallSeconds;maxMemoryGB=8;utc=[DateTime]::UtcNow.ToString('o');dotnet=(& dotnet --version);note='Same CitySimulation, ordered Trade and original rules; diagnostic assembly adds departure/acknowledgement observation only'}|ConvertTo-Json|Set-Content (Join-Path $folder 'environment.json')
$process=Start-Process (Get-Command dotnet).Source -ArgumentList $arguments -WindowStyle Hidden -RedirectStandardOutput (Join-Path $folder 'stdout.log') -RedirectStandardError (Join-Path $folder 'stderr.log') -PassThru
$timer=[Diagnostics.Stopwatch]::StartNew();$peakWorking=0L;$peakPrivate=0L;$lastSize=-1L;$progressAt=0.0;$reason=$null;$lowAt=-1.0
$writer=[IO.StreamWriter]::new((Join-Path $folder 'resources.csv'));$writer.WriteLine('seconds,working_bytes,private_bytes,available_mb,stdout_bytes')
try{
 while(-not $process.WaitForExit(1000)){
  $process.Refresh();if($process.HasExited){break};$peakWorking=[Math]::Max($peakWorking,$process.WorkingSet64);$peakPrivate=[Math]::Max($peakPrivate,$process.PrivateMemorySize64)
  $size=(Get-Item -LiteralPath (Join-Path $folder 'stdout.log')).Length;if($size -ne $lastSize){$lastSize=$size;$progressAt=$timer.Elapsed.TotalSeconds}
  $available=(Get-CimInstance Win32_OperatingSystem -Property FreePhysicalMemory).FreePhysicalMemory/1024
  if($available -lt 768){if($lowAt -lt 0){$lowAt=$timer.Elapsed.TotalSeconds}}else{$lowAt=-1}
  $writer.WriteLine(('{0:F3},{1},{2},{3:F1},{4}' -f $timer.Elapsed.TotalSeconds,$process.WorkingSet64,$process.PrivateMemorySize64,$available,$size));$writer.Flush()
  if($timer.Elapsed.TotalSeconds -gt $TimeoutSeconds){$reason='wall-time-limit'}elseif($process.WorkingSet64 -gt 8GB -or $process.PrivateMemorySize64 -gt 8GB){$reason='memory-limit'}elseif($lowAt -ge 0 -and $timer.Elapsed.TotalSeconds-$lowAt -gt 10){$reason='low-physical-memory'}elseif($timer.Elapsed.TotalSeconds-$progressAt -gt $StallSeconds){$reason='no-progress-limit'}
  if($reason){$process.Kill($true);$process.WaitForExit();break}
 }
}finally{$writer.Dispose()}
$process.WaitForExit();@{exitCode=$process.ExitCode;terminatedBy=$reason;wallSeconds=$timer.Elapsed.TotalSeconds;peakWorkingSetBytes=$peakWorking;peakPrivateBytes=$peakPrivate}|ConvertTo-Json|Set-Content (Join-Path $folder 'resources.json')
Get-Content (Join-Path $folder 'stdout.log') -Tail 10
if($reason -or $process.ExitCode -ne 0){Get-Content (Join-Path $folder 'stderr.log') -Tail 15;throw 'Experiment stopped or failed; original and available checkpoints preserved'}
