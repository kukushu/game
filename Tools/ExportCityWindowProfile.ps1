param([Parameter(Mandatory)][string]$Capture,[int]$TimeoutSeconds=300,[double]$MaxMemoryGB=8)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$folder=(Resolve-Path -LiteralPath $Capture).Path
if(-not(Test-Path -LiteralPath (Join-Path $folder 'window.raw'))){throw 'Missing raw Player capture'}
if(Test-Path -LiteralPath (Join-Path $folder 'hierarchy.csv')){throw 'Export exists; original evidence is preserved'}
Copy-Item -LiteralPath (Join-Path $root 'Assets/Scripts/Editor/CityWindowProfilerExport.cs') -Destination (Join-Path $root 'Temp/RoadCoreUnity/Assets/Scripts/Editor/CityWindowProfilerExport.cs')
Copy-Item -LiteralPath (Join-Path $root 'Assets/Scripts/Editor/CityWindowProfilerExport.cs.meta') -Destination (Join-Path $root 'Temp/RoadCoreUnity/Assets/Scripts/Editor/CityWindowProfilerExport.cs.meta')
$env:HARBOR_WINDOW_CAPTURE=$folder
$p=Start-Process 'C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Unity.exe' -ArgumentList '-batchmode','-quit','-projectPath',('"'+(Join-Path $root 'Temp/RoadCoreUnity')+'"'),'-executeMethod','HarborCity.CityWindowProfilerExport.Run','-logFile',('"'+(Join-Path $folder 'export.log')+'"') -WindowStyle Hidden -PassThru
$watch=[Diagnostics.Stopwatch]::StartNew();$peak=0L;$reason=$null
while(-not $p.WaitForExit(1000)){$p.Refresh();if($p.HasExited){break};$peak=[Math]::Max($peak,$p.WorkingSet64);if($watch.Elapsed.TotalSeconds -gt $TimeoutSeconds){$reason='timeout'}elseif($p.WorkingSet64 -gt $MaxMemoryGB*1GB){$reason='memory-limit'}elseif((Get-CimInstance Win32_OperatingSystem -Property FreePhysicalMemory).FreePhysicalMemory -lt 768*1024){$reason='low-system-memory'};if($reason){$p.Kill($true);$p.WaitForExit();break}}
@{wallSeconds=$watch.Elapsed.TotalSeconds;peakWorkingSetBytes=$peak;exitCode=$p.ExitCode;stopped=$reason}|ConvertTo-Json|Set-Content (Join-Path $folder 'export-resources.json')
if($reason -or $p.ExitCode -ne 0){Get-Content (Join-Path $folder 'export.log') -Tail 12;throw 'Bounded raw export failed; evidence kept'}
& python (Join-Path $PSScriptRoot 'PerformanceData.py') record-export --capture $folder
if($LASTEXITCODE -ne 0){throw 'Profiler export exists, but its sharing receipt could not be recorded; original files preserved'}
Get-Item (Join-Path $folder 'hierarchy.csv'),(Join-Path $folder 'threads.csv')|Select-Object Name,Length
