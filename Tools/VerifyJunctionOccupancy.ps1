param([switch]$ControlsOnly,[string]$Save,[string]$Results='Temp/JunctionOccupancy/replay',[int]$Steps=3000,[string]$Reference='baab467')
$ErrorActionPreference='Stop';. (Join-Path $PSScriptRoot 'ProjectLayout.ps1');$root=Split-Path $PSScriptRoot -Parent
$out=Join-Path $root $Results;if(Test-Path $out){throw 'Fresh output required'};New-Item -ItemType Directory $out|Out-Null
if($ControlsOnly){$binary=Build-CityDotNetHost $root $out (Join-Path $PSScriptRoot 'Diagnostics/CityJunctionIndexChecks.cs') ''; & dotnet $binary;if($LASTEXITCODE -ne 0){throw 'Junction controls failed'};return}
$inputPath=(Resolve-Path -LiteralPath $Save).Path
foreach($variant in @('frozen','incremental')){
 $dir=Join-Path $out $variant;New-Item -ItemType Directory $dir|Out-Null;$overrides=@{}
 if($variant -eq 'frozen'){foreach($name in @('CityRoadCoreWorld','CityRoadCoreOccupancy','CityRoadCoreLaneChanges','CityRoadCoreReservations')){$source=(& git -C $root show ($Reference+':Assets/Scripts/Runtime/Traffic/'+$name+'.cs')) -join "`n";if($LASTEXITCODE -ne 0){throw 'Missing frozen traffic source'};$path=Join-Path $dir ($name+'.frozen.cs');[IO.File]::WriteAllText($path,$source);$overrides[($name+'.cs')]=$path}}
 $binary=Build-CityDotNetHost $root $dir (Join-Path $PSScriptRoot 'Diagnostics/CityResidentFundingWaitChecks.cs') 'JUNCTION_OCCUPANCY_TRACE' -SourceOverrides $overrides
 $p=Start-Process (Get-Command dotnet).Source -ArgumentList ('"'+$binary+'"'),'replay',('"'+$dir+'"'),('"'+$inputPath+'"'),$Steps -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $dir 'stdout.log') -RedirectStandardError (Join-Path $dir 'stderr.log');$timer=[Diagnostics.Stopwatch]::StartNew()
 while(-not $p.WaitForExit(1000)){$p.Refresh();if($timer.Elapsed.TotalSeconds -gt 900 -or $p.WorkingSet64 -gt 8GB){$p.Kill($true);$p.WaitForExit();throw 'Own replay exceeded bounds'}}
 Get-Content (Join-Path $dir 'stdout.log');if($p.ExitCode -ne 0){Get-Content (Join-Path $dir 'stderr.log');throw 'Junction replay failed'}
}
foreach($name in @('states.csv','requests.bin','final.hcity','physical-trace.sha256')){if((Get-FileHash (Join-Path $out ('frozen/'+$name))).Hash -ne (Get-FileHash (Join-Path $out ('incremental/'+$name))).Hash){throw ('Exact frozen/incremental mismatch: '+$name)}}
'PASS every-Tick vehicle positions/routes/status and physical+temporary claim trace; complete state/request results/save/cold80 identical'
