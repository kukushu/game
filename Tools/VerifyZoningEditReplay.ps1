param([Parameter(Mandatory)][string]$Results,[string]$Save='Temp/CityScenarios/regenerated30000-20261017/city.hcity',[string]$FrozenReference='')
$ErrorActionPreference='Stop';. (Join-Path $PSScriptRoot 'ProjectLayout.ps1');$root=Split-Path $PSScriptRoot -Parent
if(Test-Path -LiteralPath $Results){throw 'Fresh replay results required'};New-Item -ItemType Directory -Path $Results|Out-Null;$output=(Resolve-Path $Results).Path;$overrides=@{}
if($FrozenReference){
    $old=& git show ($FrozenReference+':Assets/Scripts/Runtime/Infrastructure/Zoning/CityCoreZoning.cs');if($LASTEXITCODE -ne 0){throw 'Frozen source missing'}
    $oldPath=Join-Path $output 'CityCoreZoning.frozen.cs';[IO.File]::WriteAllLines($oldPath,$old);$emptyPath=Join-Path $output 'NoIncrementalGeometry.cs';[IO.File]::WriteAllText($emptyPath,'// Frozen zoning has no incremental geometry partial implementation.')
    $overrides=@{'CityCoreZoning.cs'=$oldPath;'CityCoreZoningGeometry.cs'=$emptyPath}
}
$binary=Build-CityDotNetHost $root $output (Join-Path $PSScriptRoot 'Diagnostics/CityZoningEditReplay.cs') -SourceOverrides $overrides
$source=(Resolve-Path -LiteralPath $Save).Path;$start=[DateTime]::UtcNow.ToString('o')
$run=Start-Process (Get-Command dotnet).Source -ArgumentList ('"'+$binary+'"'),('"'+$source+'"'),('"'+$output+'"') -WindowStyle Hidden -RedirectStandardOutput (Join-Path $output 'stdout.log') -RedirectStandardError (Join-Path $output 'stderr.log') -PassThru;$handle=$run.Handle;$watch=[Diagnostics.Stopwatch]::StartNew();$stopped=$null;$peak=0L
while(-not $run.WaitForExit(1000)){$run.Refresh();if($run.HasExited){break};$peak=[Math]::Max($peak,$run.WorkingSet64);if($watch.Elapsed.TotalSeconds -gt 240){$stopped='timeout'}elseif($peak -gt 8GB){$stopped='memory-limit'};if($stopped){$run.Kill();$run.WaitForExit();break}}
$run.WaitForExit();@{started_at_utc=$start;finished_at_utc=[DateTime]::UtcNow.ToString('o');exitCode=$run.ExitCode;stopped=$stopped;peakWorkingSetBytes=$peak;frozenReference=$FrozenReference;commit=(git rev-parse HEAD);input_sha256=(Get-FileHash $source).Hash;runtime='DotNet64';note='Same runtime model/state replay, not native frame timing'}|ConvertTo-Json|Set-Content (Join-Path $output 'environment.json')
if($stopped -or $run.ExitCode -ne 0){Get-Content (Join-Path $output 'stderr.log') -Tail 12;throw 'Replay failed; evidence retained'};Get-Content (Join-Path $output 'Result.txt')
