param([string]$Save,[string]$Results,[int]$Steps=3,[int]$TimeoutSeconds=600,[ValidateSet("Mono","DotNet64")][string]$Runtime="Mono",[int]$WarmupSteps=0,[string]$NetworkSource='',[switch]$InspectCommuteCache,[string]$AccessSource='',[string]$DispatchSource='',[string]$WorldSource='',[switch]$NoAttribution,[switch]$InspectAdmissions,[long]$AffinityMask=0,[string]$SpatialSource='',[string]$BuildingsSource='')
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'ProjectLayout.ps1')
$root=Split-Path $PSScriptRoot -Parent
$mono='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/bin/mono.exe'
$csc='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/lib/mono/4.5/csc.exe'
if(Test-Path -LiteralPath $Results){throw 'Fresh results required'}
if($Steps -lt 1 -or $Steps -gt 10000 -or $TimeoutSeconds -gt 1200 -or $TimeoutSeconds -lt 10 -or $WarmupSteps -lt 0 -or $WarmupSteps -gt 10000){throw 'Finite limits required'}
New-Item -ItemType Directory -Path $Results|Out-Null
$target=[IO.Path]::GetFullPath((Join-Path $root $Results));$source=(Resolve-Path -LiteralPath $Save).Path
if($InspectAdmissions){
 if($Runtime -ne 'Mono' -or $WorldSource){throw 'Admission observation requires current Mono World without another World override'}
 $text=[IO.File]::ReadAllText((Join-Path $root 'Assets/Scripts/Runtime/Traffic/CityRoadCoreWorld.cs'))
 $old='=>PrepareTransport(origin,destination,amount,price,purpose,citizen,out _);'
 $new='{long before=dispatchRoutes.Preparations;var prepared=PrepareTransport(origin,destination,amount,price,purpose,citizen,out var rejection);if(global::CityStepProbe.AdmissionSampling)global::CityStepProbe.ObserveAdmission(this,origin,destination,purpose,citizen,rejection,prepared!=null,dispatchRoutes.Preparations>before);return prepared;}'
 if(([regex]::Matches($text,[regex]::Escape($old))).Count -ne 1){throw 'Admission wrapper changed'};$text=$text.Replace($old,$new)
 $old='if(Network.IsConflictJunction(reservation))reservations.Reserve(reservation,spawnMovement,v.id);return v;'
 $new='if(Network.IsConflictJunction(reservation))reservations.Reserve(reservation,spawnMovement,v.id);if(global::CityStepProbe.AdmissionSampling)global::CityStepProbe.ObserveCommitted(v);return v;'
 if(([regex]::Matches($text,[regex]::Escape($old))).Count -ne 1){throw 'Admission commit changed'};$text=$text.Replace($old,$new)
 $WorldSource=Join-Path $target 'CityRoadCoreWorld.admissions.cs';[IO.File]::WriteAllText($WorldSource,$text)
}
$sources=Get-CityModelSources $root
if($NetworkSource){
 if($Runtime -ne 'Mono'){throw 'Temporary Network override is supported only by the Mono diagnostic host'}
 $sources=@($sources|Where-Object{$_ -notlike '*CityRoadCoreNetwork.cs'})+(Resolve-Path -LiteralPath $NetworkSource).Path
}
foreach($override in @(@('CityRoadCoreAccessRouting.cs',$AccessSource),@('CityRoadCoreDispatchRoutes.cs',$DispatchSource),@('CityRoadCoreWorld.cs',$WorldSource),@('CityRoadCoreSpatial.cs',$SpatialSource),@('CityBuildings.cs',$BuildingsSource))){
 if($override[1]){if($Runtime -ne 'Mono'){throw 'Temporary source overrides require Mono'};$name=$override[0];$sources=@($sources|Where-Object{[IO.Path]::GetFileName($_) -ne $name})+(Resolve-Path -LiteralPath $override[1]).Path}
}
@{commit=(& git rev-parse HEAD);dirty=@(& git status --porcelain);inputHash=(Get-FileHash -LiteralPath $source).Hash;steps=$Steps;warmupSteps=$WarmupSteps;attributionEnabled=(-not $NoAttribution);requestedAffinity=$AffinityMask;cpu=(Get-CimInstance Win32_Processor|Select-Object -ExpandProperty Name);os=[Environment]::OSVersion.ToString();networkSource=$NetworkSource;accessSource=$AccessSource;dispatchSource=$DispatchSource;worldSource=$WorldSource;spatialSource=$SpatialSource;buildingsSource=$BuildingsSource;inspectCommuteCache=$InspectCommuteCache.IsPresent;inspectAdmissions=$InspectAdmissions.IsPresent;hostRuntime=$Runtime;probeHash=(Get-FileHash -LiteralPath (Join-Path $PSScriptRoot 'Diagnostics/CityStepProbe.cs')).Hash;sourceHashes=@($sources|ForEach-Object{@{path=$_.Substring($root.Length+1);hash=(Get-FileHash -LiteralPath $_).Hash}})}|ConvertTo-Json -Depth 5|Set-Content (Join-Path $target 'environment.json')
if($Runtime -eq 'DotNet64'){$defines=if($NoAttribution){''}else{'CITY_CORE_ATTRIBUTION'};$output=Build-CityDotNetHost $root $target (Join-Path $PSScriptRoot 'Diagnostics/CityStepProbe.cs') $defines;$hostExecutable=(Get-Command dotnet).Source}else{
$output=Join-Path $target 'steps.exe'
$flags=@('/nologo','/langversion:latest');if(-not $NoAttribution){$flags+='/define:CITY_CORE_ATTRIBUTION'}
& $mono $csc @flags ('/out:'+$output) $sources (Join-Path $PSScriptRoot 'Diagnostics/CityStepProbe.cs')
if($LASTEXITCODE -ne 0){throw 'Step probe compile failed'};$hostExecutable=$mono
}
$run=Start-Process $hostExecutable -ArgumentList ('"'+$output+'"'),('"'+$source+'"'),('"'+$target+'"'),$Steps,$WarmupSteps,$(if($InspectCommuteCache){1}else{0}),$(if($InspectAdmissions){1}else{0}) -WindowStyle Hidden -RedirectStandardOutput (Join-Path $target 'stdout.log') -RedirectStandardError (Join-Path $target 'stderr.log') -PassThru
$processHandle=$run.Handle;if($AffinityMask -lt 0){$run.Kill($true);$run.WaitForExit();throw 'Invalid affinity'};try{if($AffinityMask -gt 0){$run.ProcessorAffinity=[IntPtr]$AffinityMask};$effectiveAffinity=$run.ProcessorAffinity.ToInt64().ToString('X')}catch{if(-not $run.HasExited){$run.Kill($true);$run.WaitForExit()};throw};$timer=[Diagnostics.Stopwatch]::StartNew();$peak=0L;$reason=$null;$lastLength=0L;$lastProgress=0.0;$lastSystemCheck=-5.0
while(-not $run.WaitForExit(1000)){
 $run.Refresh();if(-not $run.HasExited){$peak=[Math]::Max($peak,$run.WorkingSet64)}
 $length=(Get-Item -LiteralPath (Join-Path $target 'stdout.log')).Length;if($length -ne $lastLength){$lastLength=$length;$lastProgress=$timer.Elapsed.TotalSeconds}
 if($timer.Elapsed.TotalSeconds -gt $TimeoutSeconds){$reason='wall-time-limit'}elseif($peak -gt 8GB){$reason='memory-limit'}elseif($timer.Elapsed.TotalSeconds-$lastProgress -gt 120){$reason='no-progress-limit'}
 if($timer.Elapsed.TotalSeconds-$lastSystemCheck -ge 5){$lastSystemCheck=$timer.Elapsed.TotalSeconds;if((Get-CimInstance Win32_OperatingSystem).FreePhysicalMemory -lt 1048576){$reason='system-free-memory-below-1GB'}}
 if($reason){$run.Kill($true);$run.WaitForExit();break}
}
$run.WaitForExit()
@{effectiveAffinityHex=$effectiveAffinity;exitCode=$run.ExitCode;wallSeconds=$timer.Elapsed.TotalSeconds;peakWorkingSetBytes=$peak;terminatedBy=$reason;timeoutSeconds=$TimeoutSeconds;note='Bounded captured-city step probe; attributionEnabled identifies instrumented versus normal model; not Unity FPS or economic-health acceptance'}|ConvertTo-Json|Set-Content (Join-Path $target 'resources.json')
Get-Content (Join-Path $target 'stdout.log')
if($reason -or $run.ExitCode -ne 0){Get-Content (Join-Path $target 'stderr.log');throw 'Step diagnosis failed/stopped; evidence retained'}
