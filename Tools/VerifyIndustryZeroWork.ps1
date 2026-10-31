param([string]$Results='Temp/MillionScaleExecution/ms015',[switch]$ControlsOnly,[switch]$Growth,[string]$CandidateReference='104caa1')
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
. (Join-Path $PSScriptRoot 'ProjectLayout.ps1')
$out=Join-Path $root $Results
if(Test-Path -LiteralPath $out){throw 'Fresh output required'}
New-Item -ItemType Directory -Path $out|Out-Null
$source=& git -C $root show 'd83c2dfe8ca7ba7c6923ccf304755234c1df061c:Assets/Scripts/Runtime/Simulation/Industry/CityIndustry.cs'
if($LASTEXITCODE -ne 0){throw 'Frozen MS014 baseline missing'}
$frozen=Join-Path $out 'CityIndustry.baseline.cs';[IO.File]::WriteAllLines($frozen,$source)
$candidate=Join-Path $root 'Assets/Scripts/Runtime/Simulation/Industry/CityIndustry.cs'
if($CandidateReference){
 $candidate=Join-Path $out 'CityIndustry.candidate.cs';$candidateSource=& git -C $root show ($CandidateReference+':Assets/Scripts/Runtime/Simulation/Industry/CityIndustry.cs')
 if($LASTEXITCODE -ne 0){throw 'Frozen industry experiment missing'}
 [IO.File]::WriteAllLines($candidate,$candidateSource)
}
if($ControlsOnly){
 $body=$source -join "`n";$a=$body.IndexOf('        public void AdvanceIndustry(float minutes)');$b=$body.IndexOf('        public bool ValidIndustry()',$a)
 if($a -lt 0 -or $b -lt 0){throw 'Frozen method anchor missing'}
 $reference=$body.Substring($a,$b-$a).Replace('public void AdvanceIndustry(float minutes)','public void ReferenceAdvanceIndustry(float minutes)')
 $entry=Join-Path $out 'Controls.cs';[IO.File]::WriteAllText($entry,[IO.File]::ReadAllText((Join-Path $PSScriptRoot 'Diagnostics/CityIndustryZeroChecks.cs'))+"`nnamespace HarborCity {public sealed partial class CityModel {"+$reference+'}}')
 $binary=Build-CityDotNetHost -ProjectRoot $root -OutputDirectory $out -Entry $entry -SourceOverrides @{'CityIndustry.cs'=$candidate}
 & dotnet $binary (Join-Path $out 'controls.csv');if($LASTEXITCODE -ne 0){throw 'Industry bitwise controls failed'}
 $mono='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/bin/mono.exe';$compiler='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/lib/mono/4.5/csc.exe';$exe=Join-Path $out 'MonoControls.exe'
 $modelSources=@(Get-CityModelSources $root | Where-Object {[IO.Path]::GetFileName($_) -ne 'CityIndustry.cs'})+$candidate
 & $mono $compiler /nologo /langversion:latest ('/out:'+$exe) $modelSources $entry
 if($LASTEXITCODE -ne 0){throw 'Mono industry controls compile failed'}
 & $mono $exe (Join-Path $out 'controls-mono.csv');if($LASTEXITCODE -ne 0){throw 'Mono bitwise controls failed'}
 return
}
$save=Join-Path $root 'Temp/MillionScaleExecution/ms012-paired/fixture-export/same-input-order18.hcity'
foreach($version in @('old','new')){
 $dir=Join-Path $out $version;New-Item -ItemType Directory -Path $dir|Out-Null
 $override=@{'CityIndustry.cs'=$candidate};if($version -eq 'old'){$override['CityIndustry.cs']=$frozen}
 $entry=if($Growth){'CityGrowthCandidateChecks.cs'}else{'ResidentWakeReplay.cs'}
 $binary=Build-CityDotNetHost -ProjectRoot $root -OutputDirectory $dir -Entry (Join-Path $PSScriptRoot ('Diagnostics/'+$entry)) -Defines 'CITY_RESIDENT_GUARD_SCHEDULE;CITY_RESIDENT_TIME_SCHEDULE' -SourceOverrides $override
 & dotnet $binary $save $dir | Tee-Object (Join-Path $dir 'result.txt');if($LASTEXITCODE -ne 0){throw ($version+' industry replay failed')}
}
foreach($name in $(if($Growth){@('states.csv')}else{@('states.csv','requests.csv','final.hcity')})){
 if((Get-FileHash -LiteralPath (Join-Path $out ('old/'+$name))).Hash -ne (Get-FileHash -LiteralPath (Join-Path $out ('new/'+$name))).Hash){throw ('Industry changed full result: '+$name)}
}
Write-Output 'PASS frozen industry vs candidate: full paired state and cold continuation'
