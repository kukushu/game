param(
    [Parameter(Mandatory=$true)][string]$Results,
    [ValidateSet('Current','Pilot','PilotShortDay')][string]$Profile='Current',
    [ValidateSet(0,120,500)][int]$Population=0,
    [ValidateRange(1,120)][int]$Days=45,
    [ValidateRange(10,900)][int]$TimeoutSeconds=180
)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'ProjectLayout.ps1')
$root=Split-Path $PSScriptRoot -Parent
$output=[IO.Path]::GetFullPath((Join-Path $root $Results))
$tempRoot=[IO.Path]::GetFullPath((Join-Path $root 'Temp'))+[IO.Path]::DirectorySeparatorChar
if(-not $output.StartsWith($tempRoot,[StringComparison]::OrdinalIgnoreCase)){throw 'Results must be under this checkout Temp'}
if(Test-Path -LiteralPath $output){throw 'Choose a new output directory'}
New-Item -ItemType Directory -Path $output|Out-Null
$monoRoot='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge'
$exe=Join-Path $output 'EconomyStudy.exe'
$entry=Join-Path $PSScriptRoot 'Diagnostics/CitySmallEconomyStudy.cs'
$sources=@(Get-CityModelSources $root)+@($entry)
@{commit=(& git -C $root rev-parse HEAD);profile=$Profile;population=$Population;days=$Days;runtime='Unity Mono command line';sources=@($sources|ForEach-Object {@{path=$_.Substring($root.Length+1);sha256=(Get-FileHash -LiteralPath $_).Hash}})}|ConvertTo-Json -Depth 4|Set-Content -LiteralPath (Join-Path $output 'inputs.json') -Encoding utf8
& "$monoRoot/bin/mono.exe" "$monoRoot/lib/mono/4.5/csc.exe" /nologo "/out:$exe" $sources *> (Join-Path $output 'build.log')
if($LASTEXITCODE -ne 0){Get-Content -LiteralPath (Join-Path $output 'build.log');throw 'Study compilation failed'}
$argsForStudy=@(('"'+$exe+'"'),('"'+$output+'"'),$Profile,$Population,$Days)
$process=Start-Process -FilePath "$monoRoot/bin/mono.exe" -ArgumentList $argsForStudy -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $output 'run.log') -RedirectStandardError (Join-Path $output 'stderr.log')
if(-not $process.WaitForExit($TimeoutSeconds*1000)){$process.Kill();throw 'Study timed out. Partial evidence is retained.'}
Get-Content -LiteralPath (Join-Path $output 'run.log')
if($process.ExitCode -ne 0){Get-Content -LiteralPath (Join-Path $output 'stderr.log');throw 'Study failed'}
