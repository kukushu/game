param([Parameter(Mandatory)][string]$Save,[Parameter(Mandatory)][string]$Results,[ValidateRange(0,2401)][int]$Steps=400,[switch]$DisableTrace,[ValidateRange(10,600)][int]$TimeoutSeconds=480)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'ProjectLayout.ps1')
$root=Split-Path $PSScriptRoot -Parent
if(Test-Path -LiteralPath $Results){throw 'Fresh results directory required'}
$folder=[IO.Path]::GetFullPath((Join-Path $root $Results));New-Item -ItemType Directory -Path $folder|Out-Null
$source=(Resolve-Path -LiteralPath $Save).Path
$world=Join-Path $folder 'CityRoadCoreWorld.trace.cs'
& python (Join-Path $PSScriptRoot 'InstrumentLogisticsTrace.py') $world
if($LASTEXITCODE -ne 0){throw 'Trace anchors no longer match current movement; inspect source'}
$output=Build-CityDotNetHost $root $folder (Join-Path $PSScriptRoot 'Diagnostics/CityLogisticsTrace.cs') '' '' $world
$mode=if($DisableTrace){'disabled'}else{'trace'}
@{commit=(& git rev-parse HEAD);inputPath=$source;inputHash=(Get-FileHash -LiteralPath $source).Hash;steps=$Steps;mode=$mode;maxMemoryGB=8;timeoutSeconds=$TimeoutSeconds;utc=[DateTime]::UtcNow.ToString('o');productWorldHash=(Get-FileHash -LiteralPath (Join-Path $root 'Assets/Scripts/Runtime/Traffic/CityRoadCoreWorld.cs')).Hash;note='Same actual CitySimulation. Generated copy adds observation calls only; trace timing is NOT uninstrumented performance.'}|ConvertTo-Json|Set-Content (Join-Path $folder 'environment.json')
$run=Start-Process (Get-Command dotnet).Source -ArgumentList ('"'+$output+'"'),('"'+$source+'"'),('"'+$folder+'"'),$Steps,$mode -WindowStyle Hidden -RedirectStandardOutput (Join-Path $folder 'stdout.log') -RedirectStandardError (Join-Path $folder 'stderr.log') -PassThru
$timer=[Diagnostics.Stopwatch]::StartNew();$peak=0L;$reason=$null
while(-not $run.WaitForExit(1000)){
 $run.Refresh();if(-not $run.HasExited){$peak=[Math]::Max($peak,$run.WorkingSet64)}
 if($timer.Elapsed.TotalSeconds -gt $TimeoutSeconds){$reason='wall-time-limit'}elseif($peak -gt 8GB){$reason='memory-limit'}
 if($reason){$run.Kill($true);$run.WaitForExit();break}
}
@{wallSeconds=$timer.Elapsed.TotalSeconds;peakWorkingSetBytes=$peak;terminatedBy=$reason;exitCode=$run.ExitCode}|ConvertTo-Json|Set-Content (Join-Path $folder 'resources.json')
Get-Content (Join-Path $folder 'stdout.log')
if($reason -or $run.ExitCode -ne 0){Get-Content (Join-Path $folder 'stderr.log');throw 'Logistics probe failed; evidence retained'}
