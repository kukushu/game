param([string]$Results='Temp/ScaleArchitecture/wake-queue')
$ErrorActionPreference='Stop';$root=Split-Path $PSScriptRoot -Parent
$out=Join-Path $root $Results
if(Test-Path -LiteralPath $out){throw 'Fresh test output required'}
New-Item -ItemType Directory -Path $out|Out-Null
# No Unity/model source: test only the proposed derived queue primitive under .NET64.
$items=@('CityWakeQueue.cs','CityWakeQueueChecks.cs')
foreach($name in $items){Copy-Item -LiteralPath (Join-Path $PSScriptRoot ('Diagnostics/'+$name)) -Destination (Join-Path $out $name)}
$sdkVersion=(& dotnet --version).Trim();$framework='net'+([version]($sdkVersion -split '-')[0]).Major+'.0'
[IO.File]::WriteAllText((Join-Path $out 'checks.csproj'),('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>{0}</TargetFramework><ImplicitUsings>disable</ImplicitUsings><Nullable>disable</Nullable></PropertyGroup></Project>' -f $framework))
dotnet run --project (Join-Path $out 'checks.csproj') -c Release | Tee-Object (Join-Path $out 'Result.txt')
if($LASTEXITCODE -ne 0){throw 'Wake queue primitive tests failed'}
