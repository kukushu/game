param([string]$Results='Temp/UnifiedRouting/checks',[string]$CitySave='',[int]$Steps=4000)
$ErrorActionPreference='Stop';. (Join-Path $PSScriptRoot 'ProjectLayout.ps1');$root=Split-Path $PSScriptRoot -Parent;$dir=Join-Path $root $Results
if(Test-Path $dir){throw 'Fresh output required'};New-Item -ItemType Directory $dir|Out-Null
$fixture=[IO.File]::ReadAllText((Join-Path $PSScriptRoot 'Diagnostics/CityFromLaneChecks.cs'));$a=$fixture.IndexOf('    static void Pair(');$b=$fixture.IndexOf('    static Network Grid(',$a)
$fixture=$fixture.Substring(0,$a)+"    static void Pair(Network n,long lane,double distance,RoadAccess target,bool both){HarborCity.UnifiedRoutingChecks.Pair(n,lane,distance,target,both);}`n"+$fixture.Substring($b)
$fixture=$fixture.Replace('static void Main(string[] args)','static void Fixtures(string[] args)').Replace('var n=Grid(seed);','var n=Grid(seed);HarborCity.UnifiedRoutingChecks.Junctions(n);')
$fixture=$fixture.Replace('Console.WriteLine("PASS "+checks+" checks / "+queries+" exact old FromLane queries: route, terminal bits, cost bits, one-way, multilane, disconnected, zero connection, ties and boundaries");','')
$entry='static void Main(string[] args){Fixtures(new string[0]);HarborCity.UnifiedRoutingChecks.Replay(args[0],args[1],args[2]);HarborCity.UnifiedRoutingChecks.Pressure(args[3]);if(args.Length>4)HarborCity.UnifiedRoutingChecks.Continue(args[4],args[5],int.Parse(args[6]));Console.WriteLine(HarborCity.UnifiedRoutingChecks.Summary());}'
$fixture=$fixture.Insert($fixture.LastIndexOf('}'),$entry);$file=Join-Path $dir 'Entry.cs';[IO.File]::WriteAllText($file,$fixture)
$mono='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/bin/mono.exe';$csc='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/lib/mono/4.5/csc.exe'
& $mono $csc /nologo /langversion:latest ('/out:'+(Join-Path $dir 'checks.exe')) (Get-CityModelSources $root) (Join-Path $PSScriptRoot 'Diagnostics/CityRoutingDijkstraOracle.cs') (Join-Path $PSScriptRoot 'Diagnostics/CityUnifiedRoutingChecks.cs') $file
if($LASTEXITCODE -ne 0){throw 'Unified routing compile failed'}
$arguments=@((Join-Path $root 'Temp/RecoverySearch-20261025/native-after/window/pending.hcity'),(Join-Path $root 'Docs/Performance/RecoveryRouteSharing-20261025/queries-new.csv'),(Join-Path $dir 'replay.csv'),(Join-Path $dir 'pressure.csv'));if($CitySave){$arguments+=@((Resolve-Path $CitySave).Path,(Join-Path $dir 'city'),$Steps)}
& $mono (Join-Path $dir 'checks.exe') @arguments | Tee-Object (Join-Path $dir 'checks.log')
if($LASTEXITCODE -ne 0){throw 'Unified routing checks failed'}
