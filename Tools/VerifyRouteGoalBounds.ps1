param([string]$Results='Temp/GoalBounds/checks',[ValidateSet('Mono','DotNet64')][string]$Runtime='DotNet64')
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'ProjectLayout.ps1')
$root=Split-Path $PSScriptRoot -Parent
$dir=[IO.Path]::GetFullPath((Join-Path $root $Results))
if(Test-Path -LiteralPath $dir){throw 'Fresh results required'}
New-Item -ItemType Directory -Path $dir|Out-Null
# Reuse the exact route/terminal/cost-bit oracle and road-edit fixtures. Disable
# pair caching on BOTH sides so a cached unpruned answer cannot mask a bad hint.
$fixture=[IO.File]::ReadAllText((Join-Path $PSScriptRoot 'Diagnostics/CityFromLaneChecks.cs'))
$fixture=$fixture.Replace('static int checks,queries;', 'static readonly HashSet<Network> seen=new HashSet<Network>();static readonly List<IDisposable> leases=new List<IDisposable>();static long builds,pruned;static int checks,queries;')
$needle='queries++;var expected=Old(n,lane,distance,target,both,out double a);var actual='
if(-not $fixture.Contains($needle)){throw 'Fixture changed: review exact oracle injection'}
$fixture=$fixture.Replace($needle,'if(seen.Add(n))leases.Add(n.BeginGoalBatch());n.RouteSharingEnabled=false;n.GoalBoundsEnabled=false;queries++;var expected=Old(n,lane,distance,target,both,out double a);n.GoalBoundsEnabled=true;var actual=')
$extra=@'
        // Addition-order/cancellation stress. Rules accept finite positive speeds;
        // do not weaken exact comparisons for tiny/huge costs or equal-cost ties.
        for(int mode=0;mode<3;mode++){
            var data=new NetworkData();for(int z=0;z<3;z++)for(int x=0;x<3;x++)data.junctions.Add(new Junction{id=1+x+3*z,position=new Point(x*30,0,z*30)});
            for(int z=0;z<3;z++)for(int x=0;x<3;x++)foreach(int offset in new[]{x<2?1:0,z<2?3:0}){
                if(offset==0)continue;int from=1+x+3*z,to=from+offset;double speed=mode==0?1e-100:mode==1?1e100:(data.roads.Count%2==0?1e-100:1e100);
                data.roads.Add(new PhysicalRoad{id=data.nextRoad++,from=from,to=to,lanes=2,direction=RoadDirection.Both,speed=speed,points=new[]{data.junctions[from-1].position,data.junctions[to-1].position}});
            }
            data.nextJunction=10;var net=new Network(data);var nodes=net.Drivables.Where(d=>!d.IsConnection).ToArray();
            foreach(var target in nodes.Take(4))foreach(var source in nodes)for(int repeat=0;repeat<20;repeat++)Pair(net,source.Id,source.StartDistance,new RoadAccess{lane=target.Id,distance=target.EndDistance},true);
        }
        foreach(var network in seen){builds+=network.GoalBuildCount;pruned+=network.GoalPruned;}
        foreach(var lease in leases)lease.Dispose();
        Check(builds>0&&pruned>0,"Hints never exercised; reject vacuous comparison");
        Console.WriteLine("Goal trees="+builds+" excluded expansions="+pruned);
'@
$fixture=$fixture.Replace('output?.Dispose();',$extra+"`n        output?.Dispose();")
$entry=Join-Path $dir 'ExactGoalBounds.cs';[IO.File]::WriteAllText($entry,$fixture)
if($Runtime -eq 'DotNet64'){
    $binary=Build-CityDotNetHost $root $dir $entry
    & dotnet $binary | Tee-Object (Join-Path $dir 'checks.log')
}else{
    $mono='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/bin/mono.exe'
    $compiler='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/lib/mono/4.5/csc.exe'
    $binary=Join-Path $dir 'checks.exe'
    & $mono $compiler /nologo /langversion:latest ('/out:'+$binary) (Get-CityModelSources $root) $entry
    if($LASTEXITCODE -ne 0){throw 'Goal bounds compile failed'}
    & $mono $binary | Tee-Object (Join-Path $dir 'checks.log')
}
if($LASTEXITCODE -ne 0){throw 'Exact goal-bound comparison failed'}
