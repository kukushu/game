param([string]$Results='Temp/RecoveryAStar/model',[string]$Save='Temp/RecoverySearch-20261025/native-after/window/pending.hcity',[string]$Queries='Docs/Performance/RecoveryRouteSharing-20261025/queries-new.csv')
$ErrorActionPreference='Stop';. (Join-Path $PSScriptRoot 'ProjectLayout.ps1');$root=Split-Path $PSScriptRoot -Parent
$dir=Join-Path $root $Results;if(Test-Path $dir){throw 'Fresh output required'};New-Item -ItemType Directory $dir|Out-Null
$sources=Get-CityModelSources $root
$extra=@((Join-Path $PSScriptRoot 'Diagnostics/RecoveryAStar.cs'),(Join-Path $PSScriptRoot 'Diagnostics/RecoveryAStarReplay.cs'))
$mono='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/bin/mono.exe';$csc='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/lib/mono/4.5/csc.exe'
$fixture=[IO.File]::ReadAllText((Join-Path $PSScriptRoot 'Diagnostics/CityFromLaneChecks.cs'))
$a=$fixture.IndexOf('    static void Pair(');$b=$fixture.IndexOf('    static Network Grid(', $a)
$fixture=$fixture.Substring(0,$a)+"    static void Pair(Network n,long lane,double distance,RoadAccess target,bool both){HarborCity.RecoveryAStarReplay.CheckPair(n,lane,distance,target,both);}`n"+$fixture.Substring($b)
$fixture=$fixture.Replace('static void Main(string[] args)','static void Fixtures(string[] args)')
$entry=@'
    static void Main(string[] args){Fixtures(new string[0]);Console.WriteLine(HarborCity.RecoveryAStarReplay.Checks());var city=HarborCity.CityCoreSave.Read(File.ReadAllBytes(args[0]));HarborCity.RecoveryAStarReplay.Run(city.Transport.Network,args[1],args[2]);Console.WriteLine(HarborCity.RecoveryAStarReplay.Checks());}
'@
$fixture=$fixture.Insert($fixture.LastIndexOf('}'),$entry);$fixture=$fixture.Replace('Console.WriteLine("PASS "+checks+" checks / "+queries+" exact old FromLane queries: route, terminal bits, cost bits, one-way, multilane, disconnected, zero connection, ties and boundaries");','')
$file=Join-Path $dir 'Entry.cs';[IO.File]::WriteAllText($file,$fixture)
& $mono $csc /nologo /langversion:latest ('/out:'+(Join-Path $dir 'check.exe')) $sources $extra $file;if($LASTEXITCODE -ne 0){throw 'AStar research compile failed'}
& $mono (Join-Path $dir 'check.exe') (Join-Path $root $Save) (Join-Path $root $Queries) $dir | Tee-Object (Join-Path $dir 'checks.log');if($LASTEXITCODE -ne 0){throw 'AStar research failed'}
@{source=(git rev-parse HEAD);dirty=@(git status --porcelain);saveHash=(Get-FileHash (Join-Path $root $Save)).Hash;queryHash=(Get-FileHash (Join-Path $root $Queries)).Hash;note='Shadow queries only; runtime routing unchanged. h=0 strict control, h>0 differences classified, never asserted byte-equivalent.'}|ConvertTo-Json|Set-Content (Join-Path $dir 'environment.json')
