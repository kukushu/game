param([Parameter(Mandatory)][string]$Player,[Parameter(Mandatory)][string]$Results,[string]$Save='Temp/CityScenarios/regenerated30000-20261017/city.hcity',[switch]$Fine,[switch]$Raw,[switch]$Running,[ValidateSet('add','connect_split','widen','delete','continuous','all')][string]$RawOperation='add',[int]$TimeoutSeconds=180)
$ErrorActionPreference='Stop'
if(Test-Path -LiteralPath $Results){throw 'Fresh result directory required'}
if(Get-Process HarborCity -ErrorAction SilentlyContinue){throw 'Refusing overlapping HarborCity Players'}
$binary=(Resolve-Path -LiteralPath $Player).Path;$inputSave=(Resolve-Path -LiteralPath $Save).Path
New-Item -ItemType Directory -Path $Results|Out-Null;$output=(Resolve-Path -LiteralPath $Results).Path
$parameters=@('-harbor-city-inspect',('"'+$inputSave+'"'),'-harbor-zone-probe',('"'+$output+'"'),'-screen-fullscreen','0','-screen-width','1440','-screen-height','900','-logFile',('"'+(Join-Path $output 'player.log')+'"'))
if($Fine){$parameters+='-harbor-zone-fine'};if($Raw){$parameters+=@('-harbor-zone-raw','-harbor-zone-raw-operation',$RawOperation)}
if($Running){$parameters+='-harbor-construction-running'}
$started=[DateTime]::UtcNow.ToString('o');$run=Start-Process $binary -ArgumentList $parameters -WindowStyle Hidden -PassThru;$handle=$run.Handle;$watch=[Diagnostics.Stopwatch]::StartNew();$peak=0L;$stopped=$null
while(-not $run.WaitForExit(1000)){$run.Refresh();if($run.HasExited){break};$peak=[Math]::Max($peak,$run.WorkingSet64);if($watch.Elapsed.TotalSeconds -gt $TimeoutSeconds){$stopped='timeout'}elseif($peak -gt 8GB){$stopped='memory-limit'};if($stopped){$run.Kill();$run.WaitForExit();break}}
$run.WaitForExit()
@{started_at_utc=$started;finished_at_utc=[DateTime]::UtcNow.ToString('o');commit=(git rev-parse HEAD);dirty=@(git status --porcelain);running=$Running.IsPresent;fine=$Fine.IsPresent;raw=$Raw.IsPresent;rawOperation=$RawOperation;input=$inputSave;input_sha256=(Get-FileHash $inputSave).Hash;player=$binary;assembly_sha256=(Get-FileHash (Join-Path (Split-Path $binary) 'HarborCity_Data/Managed/Assembly-CSharp.dll')).Hash;exitCode=$run.ExitCode;stopped=$stopped;peakWorkingSetBytes=$peak;cpu=(Get-CimInstance Win32_Processor).Name;os=[Environment]::OSVersion.ToString()}|ConvertTo-Json -Depth 4|Set-Content (Join-Path $output 'environment.json')
if($stopped -or $run.ExitCode -ne 0 -or -not(Test-Path (Join-Path $output 'Result.txt'))){Get-Content (Join-Path $output 'player.log') -Tail 12;throw 'Native zoning probe failed; evidence retained'}
Get-Content (Join-Path $output 'Result.txt')
