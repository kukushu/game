param([string]$Results='Temp/UnifiedResidents/checks',[ValidateSet('Controls','Replay')][string]$Mode='Controls',[string]$Save='', [ValidateRange(1,10000)][int]$Steps=3000,[string]$Reference='e2429c5')
$ErrorActionPreference='Stop';. (Join-Path $PSScriptRoot 'ProjectLayout.ps1');$root=Split-Path $PSScriptRoot -Parent;$out=[IO.Path]::GetFullPath((Join-Path $root $Results));if(Test-Path $out){throw 'Fresh output required'};New-Item -ItemType Directory $out|Out-Null
if($Mode -eq 'Replay'){$Save=(Resolve-Path $Save).Path}
foreach($variant in @('frozen','events')){
 $flags='';$overrides=@{}
 $entries=if($Mode -eq 'Controls'){@('CityResidentFundingWaitChecks','CityResidentGuardSchedulingChecks','CityResidentTemporalChecks')}else{@('CityResidentFundingWaitChecks')}
 foreach($entry in $entries){
  $dir=Join-Path $out ($variant+'/'+$entry);New-Item -ItemType Directory $dir -Force|Out-Null
  if($variant -eq 'frozen'){$body=(& git -C $root show ($Reference+':Assets/Scripts/Runtime/Simulation/Residents/CityCoreResidents.cs')) -join "`n";if($LASTEXITCODE -ne 0){throw 'Frozen Residents missing'};$source=Join-Path $dir 'Residents.frozen.cs';[IO.File]::WriteAllText($source,$body);$overrides['CityCoreResidents.cs']=$source;$flags='CITY_RESIDENT_POLLING_REFERENCE'}
  $binary=Build-CityDotNetHost $root $dir (Join-Path $PSScriptRoot ('Diagnostics/'+$entry+'.cs')) $flags -SourceOverrides $overrides
  $arguments=@(('"'+$binary+'"'));if($entry -eq 'CityResidentFundingWaitChecks'){$arguments+=@($Mode.ToLowerInvariant(),('"'+$dir+'"'));if($Mode -eq 'Replay'){$arguments+=@(('"'+$Save+'"'),$Steps)}}else{$arguments+=('"'+$dir+'"')}
  $p=Start-Process (Get-Command dotnet).Source -ArgumentList $arguments -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $dir 'stdout.log') -RedirectStandardError (Join-Path $dir 'stderr.log');$clock=[Diagnostics.Stopwatch]::StartNew()
  while(-not $p.WaitForExit(1000)){$p.Refresh();if($clock.Elapsed.TotalSeconds -gt 900 -or $p.WorkingSet64 -gt 8GB){$p.Kill($true);$p.WaitForExit();throw 'Own unified checks exceeded bounds'}}
  Get-Content (Join-Path $dir 'stdout.log');if($p.ExitCode -ne 0){Get-Content (Join-Path $dir 'stderr.log');throw ($variant+'/'+$entry+' failed')}
 }
}
foreach($entry in $entries){foreach($file in $(if($Mode -eq 'Controls'){@('controls.csv')}else{@('states.csv','requests.bin','final.hcity')})){
 if((Get-FileHash (Join-Path $out ('frozen/'+$entry+'/'+$file))).Hash -ne (Get-FileHash (Join-Path $out ('events/'+$entry+'/'+$file))).Hash){throw ('Unified/frozen mismatch '+$entry+'/'+$file)}
}}
'PASS frozen polling vs default unified events: exact state/order/results and negative controls'
