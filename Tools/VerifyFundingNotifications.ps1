param([string]$Results='Temp/MillionScaleExecution/funding',[switch]$TargetedOnly)
$ErrorActionPreference='Stop';$root=Split-Path $PSScriptRoot -Parent;. (Join-Path $PSScriptRoot 'ProjectLayout.ps1')
$out=Join-Path $root $Results;if(Test-Path -LiteralPath $out){throw 'Fresh output required'};New-Item -ItemType Directory -Path $out|Out-Null
$frozen=@{};foreach($relative in @('Core/CityEntities.cs','Residents/CityCoreResidents.cs','Industry/CityIndustry.cs','Economy/CityShopAccount.cs','Residents/CityResidents.cs','Residents/CityHouseholds.cs','Core/CityCoreSave.cs','Industry/CityCoreIndustrySession.cs')){$name=[IO.Path]::GetFileName($relative);$path=Join-Path $out $name;$source=& git -C $root show ('791942f:Assets/Scripts/Runtime/Simulation/'+$relative);if($LASTEXITCODE -ne 0){throw 'Frozen source unavailable'};[IO.File]::WriteAllLines($path,$source);$frozen[$name]=$path}
$save=Join-Path (Split-Path $root -Parent) 'test/Temp/CityScenarios/regenerated30000-20261017/city.hcity'
$originalSave=$save;$orderSave=Export-CityMemberOrderFixture $root $save $out
foreach($version in @('old','new')){$overrides=@{};$defines='FUNDING';if($version -eq 'old'){$overrides=$frozen;$defines=''};$cases=@('funding');if(!$TargetedOnly){$cases+=@('growth')};foreach($case in $cases){$dir=Join-Path $out ($version+'/'+$case);New-Item -ItemType Directory -Path $dir -Force|Out-Null;$entry=Join-Path $PSScriptRoot $(if($case -eq 'funding'){'Diagnostics/CityFundingChecks.cs'}else{'Diagnostics/CityGrowthCandidateChecks.cs'});if($version -eq 'new' -and $case -eq 'growth'){
 $session=[IO.File]::ReadAllText((Join-Path $root 'Assets/Scripts/Runtime/Simulation/Industry/CityCoreIndustrySession.cs'))
 if(([regex]::Matches($session,'Entities.Simulation=Simulation;')).Count -ne 1){throw 'Session observer anchor changed'}
 $session=[regex]::Replace($session,'(public sealed class CityCoreIndustrySession\s*\{)','$1 internal static long DiagnosticFundingNotifications;')
 $session=$session.Replace('Entities.Simulation=Simulation;','Entities.Simulation=Simulation;Entities.FundingEligibilityChanged+=m=>{DiagnosticFundingNotifications++;if(!ReferenceEquals(Entities.Member(m.Person.id),m)||City.ResidentJob(m.Person)==null)throw new InvalidOperationException("Diagnostic stale funding member");};')
 $observed=Join-Path $dir 'CityCoreIndustrySession.observed.cs';[IO.File]::WriteAllText($observed,$session);$overrides=@{'CityCoreIndustrySession.cs'=$observed}
 $body=[IO.File]::ReadAllText($entry);$anchor='File.WriteAllLines(Path.Combine(args[1],"states.csv"),rows);';if(!$body.Contains($anchor)){throw 'Growth result anchor changed'}
 $body=$body.Replace($anchor,$anchor+'Console.WriteLine("fundingObserverNotifications="+CityCoreIndustrySession.DiagnosticFundingNotifications);');$entry=Join-Path $dir 'ObservedGrowth.cs';[IO.File]::WriteAllText($entry,$body)
}
$entry=New-CityOrderWitnessEntry $root $entry $dir;$save=if($version -eq 'old'){$originalSave}else{$orderSave};$binary=Build-CityDotNetHost -ProjectRoot $root -OutputDirectory $dir -Entry $entry -Defines $defines -SourceOverrides $overrides;if($case -eq 'funding'){& dotnet $binary $dir}else{& dotnet $binary $save $dir};if($LASTEXITCODE -ne 0){throw ($version+' '+$case+' failed')}}}
foreach($relative in @('funding/states.csv','funding/requests.csv')){if([IO.File]::ReadAllText((Join-Path $out ('old/'+$relative))) -cne [IO.File]::ReadAllText((Join-Path $out ('new/'+$relative)))){throw ('Full state/order differs: '+$relative)}}
if((Get-FileHash -LiteralPath (Join-Path $out 'old/funding/final.hcity.witness')).Hash -ne (Get-FileHash -LiteralPath (Join-Path $out 'new/funding/final.hcity.witness')).Hash){throw 'Canonical saved facts/order differ'}
if(!$TargetedOnly -and [IO.File]::ReadAllText((Join-Path $out 'old/growth/states.csv')) -cne [IO.File]::ReadAllText((Join-Path $out 'new/growth/states.csv'))){throw 'Growth/30k states differ'}
Write-Output 'PASS frozen791942f funding notifications: exact canonical city/request facts and saved order, targeted and cold tests'

if(!$TargetedOnly){$dir=Join-Path $out 'maintenance';New-Item -ItemType Directory -Path $dir|Out-Null;& dotnet (Join-Path $out 'new/funding/bin/Release/net10.0/CityModelHost.dll') $dir $orderSave;if($LASTEXITCODE -ne 0){throw 'Funding maintenance count/cost failed'}}
