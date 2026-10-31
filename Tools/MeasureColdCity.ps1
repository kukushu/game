param([string]$Save,[string]$Results,[ValidateRange(0,20000)][int]$WarmSteps=0,[ValidateRange(1,4000)][int]$Steps=1,[ValidateSet('Mono','DotNet64')][string]$Runtime='Mono',[ValidateRange(10,900)][int]$TimeoutSeconds=600)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'ProjectLayout.ps1')
$root=Split-Path $PSScriptRoot -Parent
$mono='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/bin/mono.exe'
$csc='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/lib/mono/4.5/csc.exe'
if(Test-Path -LiteralPath $Results){throw 'Fresh results required'}
New-Item -ItemType Directory -Path $Results|Out-Null
$sources=Get-CityModelSources $root
@{commit=(& git rev-parse HEAD);dirty=@(& git status --porcelain);inputHash=(Get-FileHash -LiteralPath $Save).Hash;utc=[DateTime]::UtcNow.ToString('o');warmSteps=$WarmSteps;steps=$Steps;runtime=$Runtime;sourceHashes=@($sources+(Join-Path $PSScriptRoot 'Diagnostics/CityColdScaleChecks.cs')|ForEach-Object{@{path=$_.Substring($root.Length+1);hash=(Get-FileHash -LiteralPath $_).Hash}});note='Attributed model window, warmup outside timing; exact profiling on/off full-state check; not normal frame performance'}|ConvertTo-Json -Depth 5|Set-Content (Join-Path $Results 'environment.json')
$output=Join-Path $Results 'cold.exe'
if($Runtime -eq 'DotNet64'){$output=Build-CityDotNetHost $root ([IO.Path]::GetFullPath($Results)) (Join-Path $PSScriptRoot 'Diagnostics/CityColdScaleChecks.cs') 'CITY_CORE_ATTRIBUTION';$mono=(Get-Command dotnet).Source}else{
& $mono $csc /nologo /langversion:latest /define:CITY_CORE_ATTRIBUTION ('/out:'+$output)  $sources (Join-Path $PSScriptRoot 'Diagnostics/CityColdScaleChecks.cs')
if($LASTEXITCODE -ne 0){throw 'Cold probe compile failed'}
}
$arguments=@(('"'+[IO.Path]::GetFullPath($output)+'"'),('"'+(Resolve-Path -LiteralPath $Save).Path+'"'),('"'+[IO.Path]::GetFullPath((Join-Path $Results 'attribution.csv'))+'"'),$WarmSteps,$Steps)
$run=Start-Process $mono -ArgumentList $arguments -WindowStyle Hidden -RedirectStandardOutput (Join-Path $Results 'stdout.log') -RedirectStandardError (Join-Path $Results 'stderr.log') -PassThru
$timer=[Diagnostics.Stopwatch]::StartNew();$peak=0L;$reason=$null
while(-not $run.WaitForExit(1000)){$run.Refresh();if(-not $run.HasExited){$peak=[Math]::Max($peak,$run.WorkingSet64)};if($timer.Elapsed.TotalSeconds -gt $TimeoutSeconds){$reason='wall-time-limit'}elseif($peak -gt 8GB){$reason='memory-limit'};if($reason){$run.Kill($true);$run.WaitForExit();break}}
@{exitCode=$run.ExitCode;wallSeconds=$timer.Elapsed.TotalSeconds;peakWorkingSetBytes=$peak;terminatedBy=$reason;timeoutSeconds=$TimeoutSeconds;note='Own bounded diagnostic process only; warmup and reference replay excluded from measured window'}|ConvertTo-Json|Set-Content (Join-Path $Results 'resources.json')
Get-Content (Join-Path $Results 'stdout.log')
if($reason -or $run.ExitCode -ne 0){Get-Content (Join-Path $Results 'stderr.log');throw 'Profiling failed or stopped; evidence retained'}
