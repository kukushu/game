param([string]$Results='Temp/CityScenarios/TravelTimeQuoteChecks',[string]$NetworkSource='',[string]$AccessSource='',[string]$Save='Temp/CityScenarios/challenge30000-traffic10000/threshold.hcity',[switch]$CompareReference,[string]$Reference='559a5df')
$ErrorActionPreference='Stop';. (Join-Path $PSScriptRoot 'ProjectLayout.ps1');$root=Split-Path $PSScriptRoot -Parent
if(Test-Path -LiteralPath $Results){throw 'Fresh results required'};New-Item -ItemType Directory $Results|Out-Null;$dir=[IO.Path]::GetFullPath((Join-Path $root $Results));$inputFile=(Resolve-Path $Save).Path
$baseSources=Get-CityModelSources $root;$sources=$baseSources
if($NetworkSource){$sources=@($sources|Where-Object{$_ -notlike '*CityRoadCoreNetwork.cs'})+(Resolve-Path -LiteralPath $NetworkSource).Path}
if($AccessSource){$sources=@($sources|Where-Object{$_ -notlike '*CityRoadCoreAccessRouting.cs'})+(Resolve-Path -LiteralPath $AccessSource).Path}
$mono='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/bin/mono.exe';$csc='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/lib/mono/4.5/csc.exe'
function Invoke-Checks([string]$label,[string[]]$files){
 $exe=Join-Path $dir ($label+'.exe');$answers=Join-Path $dir ($label+'-answers.bin')
 & $mono $csc /nologo /langversion:latest ('/out:'+$exe) $files (Join-Path $PSScriptRoot 'Diagnostics/CityTravelTimeQuoteChecks.cs');if($LASTEXITCODE -ne 0){throw 'Quote checks compile failed'}
 $p=Start-Process $mono -ArgumentList ('"'+$exe+'"'),('"'+$inputFile+'"'),('"'+$answers+'"') -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $dir ($label+'-stdout.log')) -RedirectStandardError (Join-Path $dir ($label+'-stderr.log'));$handle=$p.Handle;$watch=[Diagnostics.Stopwatch]::StartNew();$peak=0L;$reason=$null
 while(-not $p.WaitForExit(1000)){$p.Refresh();if($p.HasExited){break};$peak=[Math]::Max($peak,$p.WorkingSet64);if($watch.Elapsed.TotalSeconds -gt 180){$reason='wall-limit'}elseif($p.WorkingSet64 -gt 4GB){$reason='memory-limit'};if($reason){$p.Kill($true);$p.WaitForExit();break}}
 $p.WaitForExit();@{exitCode=$p.ExitCode;stopped=$reason;wallSeconds=$watch.Elapsed.TotalSeconds;peakWorkingSetBytes=$peak}|ConvertTo-Json|Set-Content (Join-Path $dir ($label+'-resources.json'));Get-Content (Join-Path $dir ($label+'-stdout.log'));if($reason -or $p.ExitCode -ne 0){Get-Content (Join-Path $dir ($label+'-stderr.log'));throw 'Quote checks failed; evidence retained'}
}
$ref=$null
if($CompareReference){
 $ref=(& git rev-parse --verify ($Reference+'^{commit}')).Trim();if($LASTEXITCODE -ne 0){throw 'Invalid reference'};$oldSources=$baseSources
 foreach($name in @('CityRoadCoreNetwork.cs','CityRoadCoreAccessRouting.cs')){
  $original=@($baseSources|Where-Object{[IO.Path]::GetFileName($_) -eq $name});if($original.Count -ne 1){throw 'Ambiguous reference source'}
  $relative=$original[0].Substring($root.Length+1).Replace('\','/');$text=& git show ($ref+':'+$relative);if($LASTEXITCODE -ne 0){throw 'Missing reference source'}
  $copy=Join-Path $dir ($name+'.reference.cs');[IO.File]::WriteAllLines($copy,$text);$oldSources=@($oldSources|Where-Object{$_ -ne $original[0]})+$copy
 }
 Invoke-Checks 'reference' $oldSources
}
Invoke-Checks 'actual' $sources
$actual=(Get-FileHash (Join-Path $dir 'actual-answers.bin')).Hash;$expected=if($CompareReference){(Get-FileHash (Join-Path $dir 'reference-answers.bin')).Hash}else{$null}
@{commit=(& git rev-parse HEAD);reference=$ref;inputHash=(Get-FileHash $inputFile).Hash;actualAnswers=$actual;referenceAnswers=$expected;sources=@($sources|ForEach-Object{@{path=$_;hash=(Get-FileHash $_).Hash}})}|ConvertTo-Json -Depth 4|Set-Content (Join-Path $dir 'comparison.json')
if($CompareReference -and $actual -ne $expected){throw 'Exact route/distance/direction answers differ from reference'}
if($CompareReference){'PASS all route sequences, directions and endpoint distance bits exactly equal frozen reference'}
