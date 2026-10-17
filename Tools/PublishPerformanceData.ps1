param(
    [Parameter(Mandatory)][string[]]$Package,
    [Parameter(Mandatory)][string]$Message,
    [switch]$Push,
    [switch]$DryRun
)
$ErrorActionPreference='Stop'
$tool=Join-Path $PSScriptRoot 'PerformanceData.py'
$publishArguments=@($tool,'publish','--message',$Message)
foreach($id in $Package){$publishArguments+=@('--package',$id)}
if($Push){$publishArguments+='--push'}
if($DryRun){$publishArguments+='--dry-run'}
& python @publishArguments
if($LASTEXITCODE -ne 0){throw 'Performance evidence publication failed; inspect the diagnostic above. No game test was launched.'}
