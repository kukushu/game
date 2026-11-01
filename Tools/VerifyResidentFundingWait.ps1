param([string]$Results='Temp/ResidentFunding/checks',[ValidateSet('Controls','Replay')][string]$Mode='Controls',[string]$Save='', [ValidateRange(1,5000)][int]$Steps=3000,[string]$Reference='1aa2f4d')
$ErrorActionPreference='Stop';. (Join-Path $PSScriptRoot 'ProjectLayout.ps1');$root=Split-Path $PSScriptRoot -Parent;$out=[IO.Path]::GetFullPath((Join-Path $root $Results));if(Test-Path $out){throw 'Fresh results required'};New-Item -ItemType Directory $out|Out-Null
if($Mode -eq 'Replay'){$Save=(Resolve-Path $Save).Path}
foreach($variant in @('polling','guard','funding')){
 $dir=Join-Path $out $variant;New-Item -ItemType Directory $dir|Out-Null;$flags='';$overrides=@{}
 if($variant -eq 'polling'){$body=(& git -C $root show ($Reference+':Assets/Scripts/Runtime/Simulation/Residents/CityCoreResidents.cs')) -join "`n";if($LASTEXITCODE -ne 0){throw 'Frozen Residents missing'};$frozen=Join-Path $dir 'Residents.frozen.cs';[IO.File]::WriteAllText($frozen,$body);$overrides['CityCoreResidents.cs']=$frozen}
 else{$flags='CITY_RESIDENT_GUARD_SCHEDULE';if($variant -eq 'funding'){$flags+=';CITY_RESIDENT_FUNDING_SCHEDULE'}}
 $binary=Build-CityDotNetHost $root $dir (Join-Path $PSScriptRoot 'Diagnostics/CityResidentFundingWaitChecks.cs') $flags -SourceOverrides $overrides
 $arguments=@(('"'+$binary+'"'),$Mode.ToLowerInvariant(),('"'+$dir+'"'));if($Mode -eq 'Replay'){$arguments+=@(('"'+$Save+'"'),$Steps)}
 $p=Start-Process (Get-Command dotnet).Source -ArgumentList $arguments -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $dir 'stdout.log') -RedirectStandardError (Join-Path $dir 'stderr.log');$clock=[Diagnostics.Stopwatch]::StartNew()
 while(-not $p.WaitForExit(1000)){$p.Refresh();if($clock.Elapsed.TotalSeconds -gt 600 -or $p.WorkingSet64 -gt 8GB){$p.Kill($true);$p.WaitForExit();throw 'Own funding check exceeded bounds'}}
 Get-Content (Join-Path $dir 'stdout.log');if($p.ExitCode -ne 0){Get-Content (Join-Path $dir 'stderr.log');throw ($variant+' check failed')}
}
foreach($file in $(if($Mode -eq 'Controls'){@('controls.csv')}else{@('states.csv','requests.bin','final.hcity')})){
 $expected=(Get-FileHash (Join-Path $out ('polling/'+$file))).Hash;foreach($variant in @('guard','funding')){if((Get-FileHash (Join-Path $out ($variant+'/'+$file))).Hash -ne $expected){throw ($variant+' mismatch '+$file)}}
}
'PASS frozen polling / existing guard / funding wait: exact paired results'
