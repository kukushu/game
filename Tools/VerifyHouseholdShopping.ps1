param([string]$Results='Temp/HouseholdShopping/checks',[ValidateSet('Unit','Replay')][string]$Mode='Unit',[string]$Save='',[int]$Steps=3000,[string]$Reference='b91acda',[switch]$Transitions,[ValidateSet('DotNet64','Mono')][string]$Runtime='DotNet64')
$ErrorActionPreference='Stop';. (Join-Path $PSScriptRoot 'ProjectLayout.ps1');$root=Split-Path $PSScriptRoot -Parent;$dir=[IO.Path]::GetFullPath((Join-Path $root $Results))
if(Test-Path $dir){throw 'Fresh output required'};New-Item -ItemType Directory $dir|Out-Null
if($Mode -eq 'Replay' -and (-not $Save -or $Steps -lt 1 -or $Steps -gt 5000)){throw 'Replay requires existing save and bounded 1..5000 steps'}
$entry=Join-Path $PSScriptRoot 'Diagnostics/CityShoppingScheduleChecks.cs'
foreach($kind in $(if($Mode -eq 'Unit'){@('candidate')}else{@('reference','candidate')})){
    $folder=Join-Path $dir $kind;New-Item -ItemType Directory $folder|Out-Null;$override=@{}
    foreach($relative in @('Economy/CityCoreConsumption.cs','Residents/CityHouseholds.cs','Core/CityEntities.cs')){
        $path=Join-Path $root ('Assets/Scripts/Runtime/Simulation/'+$relative);$name=[IO.Path]::GetFileName($path)
        if($kind -eq 'reference'){$text=(& git -C $root show ($Reference+':Assets/Scripts/Runtime/Simulation/'+$relative)) -join "`n";if($LASTEXITCODE -ne 0){throw 'Frozen shopping sources missing'}}else{$text=[IO.File]::ReadAllText($path)}
        if($name -eq 'CityCoreConsumption.cs'){
            $needle='if(best==null)continue;person.retryAt=';if(-not $text.Contains($needle)){throw 'Review shopping decision trace insertion'}
            $text=$text.Replace($needle,'ShoppingReplayTrace.Record(time.Tick,family.id,person.id,best?.id??0);'+$needle)
        }
        $target=Join-Path $folder $name;[IO.File]::WriteAllText($target,$text);$override[$name]=$target
    }
    $defines=if($kind -eq 'candidate'){'CITY_HOUSEHOLD_SHOPPING_SCHEDULE'}else{''}
    if($Runtime -eq 'DotNet64'){$binary=Build-CityDotNetHost $root $folder $entry $defines -SourceOverrides $override;$hostExe=(Get-Command dotnet).Source}
    else{
        $hostExe='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/bin/mono.exe';$csc='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/lib/mono/4.5/csc.exe';$binary=Join-Path $folder 'checks.exe';$sources=Get-CityModelSources $root;foreach($name in $override.Keys){$sources=@($sources|Where-Object {[IO.Path]::GetFileName($_) -ne $name})+$override[$name]};$flags=@();if($defines){$flags+=('/define:'+$defines)};& $hostExe $csc /nologo /langversion:latest @flags ('/out:'+$binary) $sources $entry;if($LASTEXITCODE -ne 0){throw 'Shopping checks compile failed'}
    }
    $argsList=@(('"'+$binary+'"'),$(if($Mode -eq 'Unit'){'unit'}else{'replay'}));if($Mode -eq 'Replay'){$argsList+=@(('"'+(Resolve-Path $Save).Path+'"'),('"'+$folder+'"'),$Steps,$Transitions.IsPresent)}
    $run=Start-Process $hostExe -ArgumentList $argsList -WindowStyle Hidden -RedirectStandardOutput (Join-Path $folder 'stdout.log') -RedirectStandardError (Join-Path $folder 'stderr.log') -PassThru
    $timer=[Diagnostics.Stopwatch]::StartNew();$peak=0L;while(-not $run.WaitForExit(1000)){$run.Refresh();if(-not $run.HasExited){$peak=[Math]::Max($peak,$run.WorkingSet64)};if($timer.Elapsed.TotalSeconds -gt 600 -or $peak -gt 8GB){$run.Kill($true);$run.WaitForExit();throw 'Own shopping check exceeded bounds'}}
    Get-Content (Join-Path $folder 'stdout.log');if($run.ExitCode -ne 0){Get-Content (Join-Path $folder 'stderr.log');throw 'Shopping verification failed'}
}
if($Mode -eq 'Replay'){
    foreach($file in @('requests.bin','states.csv','final.hcity')){if((Get-FileHash (Join-Path $dir ('reference/'+$file))).Hash -ne (Get-FileHash (Join-Path $dir ('candidate/'+$file))).Hash){throw ('Frozen shopping mismatch '+$file)}}
    'PASS frozen shopping request order, complete states, save bytes and cold80'
}
