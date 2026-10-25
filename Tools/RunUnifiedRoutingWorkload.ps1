param([Parameter(Mandatory)][string]$Save,[Parameter(Mandatory)][string]$Results,[string]$Reference='',[ValidateSet('formal','astar-no-sharing','astar-sharing')][string]$Mode='astar-sharing',[int]$Steps=4000)
$ErrorActionPreference='Stop';. (Join-Path $PSScriptRoot 'ProjectLayout.ps1');$root=Split-Path $PSScriptRoot -Parent;$dir=Join-Path $root $Results
if(Test-Path $dir){throw 'Fresh output required'};New-Item -ItemType Directory $dir|Out-Null;$overrides=@{}
if($Reference){foreach($source in Get-CityModelSources $root){$relative=$source.Substring($root.Length+1).Replace('\','/');$text=(& git show ($Reference+':'+$relative)) -join "`n";if($LASTEXITCODE -ne 0){throw ('Reference model source missing '+$relative)};$file=Join-Path $dir ([IO.Path]::GetFileName($source));[IO.File]::WriteAllText($file,$text);$overrides[[IO.Path]::GetFileName($source)]=$file}}
$binary=Build-CityDotNetHost -ProjectRoot $root -OutputDirectory $dir -Entry (Join-Path $PSScriptRoot 'Diagnostics/CityUnifiedRoutingWorkload.cs') -SourceOverrides $overrides
& dotnet $binary (Resolve-Path $Save).Path $dir $Mode $Steps;if($LASTEXITCODE -ne 0){throw 'Fixed-step routing workload failed'}
