param([string]$Results='Temp/GoalBounds/checks',[ValidateSet('Mono','DotNet64')][string]$Runtime='DotNet64',[string]$NetworkReference='')
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
        // Force stamp rollover against deliberately stale "generation 1" data.
        // Frozen dictionary references have no stamp and run the same queries.
        var existing=seen.ToArray();foreach(var network in existing){
            // Capture a non-direct reachable answer BEFORE corrupting scratch;
            // do not let an unused opposite-curb alternative hide the first failure.
            RoadAccess rolloverFrom=null,rolloverTo=null;List<long> rolloverExpected=null;
            var candidates=network.Drivables.Where(d=>!d.IsConnection).Take(16).ToArray();
            foreach(var from in candidates){foreach(var to in candidates){
                if(from.Id==to.Id)continue;var a=new RoadAccess{lane=from.Id,distance=from.StartDistance};var b=new RoadAccess{lane=to.Id,distance=to.EndDistance};
                var answer=network.Route(a,b);if(answer==null)continue;rolloverFrom=a;rolloverTo=b;rolloverExpected=answer;break;
            }if(rolloverExpected!=null)break;}
            var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            var scratch=typeof(Network).GetField("accessSearch",flags).GetValue(network);
            var stamp=scratch?.GetType().GetField("generation",flags);
            if(stamp!=null){foreach(var name in new[]{"seen","closed"}){var cells=(uint[])scratch.GetType().GetField(name,flags).GetValue(scratch);if(cells!=null)for(int cell=0;cell<cells.Length;cell++)cells[cell]=1;}stamp.SetValue(scratch,uint.MaxValue);}
            if(rolloverExpected!=null){var actual=network.Route(rolloverFrom,rolloverTo);Check(actual!=null&&rolloverExpected.SequenceEqual(actual),"Rollover changed exact route");output?.Write(actual.Count);foreach(long id in actual)output?.Write(id);}
            // Junction search still borrows the dictionary half of the same
            // scratch. Interleave it with dense point searches, not just isolated runs.
            var junctions=network.RoadIds.SelectMany(id=>new[]{network.Settings(id).from,network.Settings(id).to}).Distinct().Take(6).ToArray();
            foreach(int from in junctions)foreach(int to in junctions){
                var route=network.Route(from,to);output?.Write(route==null?-1:route.Count);
                if(route==null)continue;foreach(long id in route)output?.Write(id);
                if(route.Count>0){network.ValidateRoute(route);Check(network.Get(route[0]).From==from&&network.Get(route[route.Count-1]).To==to,"Mixed junction scratch endpoints changed");}
            }
            Small(network);
        }
        // Addition-order/cancellation stress. Rules accept finite positive speeds;
        // do not weaken exact comparisons for tiny/huge costs or equal-cost ties.
        for(int mode=0;mode<5;mode++){
            var data=new NetworkData();for(int z=0;z<3;z++)for(int x=0;x<3;x++)data.junctions.Add(new Junction{id=1+x+3*z,position=new Point(x*30,0,z*30)});
            for(int z=0;z<3;z++)for(int x=0;x<3;x++)foreach(int offset in new[]{x<2?1:0,z<2?3:0}){
                if(offset==0)continue;int from=1+x+3*z,to=from+offset;double speed=mode==4?double.Epsilon:mode==3?12:mode==0?1e-100:mode==1?1e100:(data.roads.Count%2==0?1e-100:1e100);
                data.roads.Add(new PhysicalRoad{id=mode==3?Network.MaximumRoadId-data.roads.Count:data.nextRoad++,from=from,to=to,lanes=2,direction=RoadDirection.Both,speed=speed,points=new[]{data.junctions[from-1].position,data.junctions[to-1].position}});
            }
            data.nextJunction=10;if(mode==3)data.nextRoad=Network.MaximumRoadId+1;var net=new Network(data);var nodes=net.Drivables.Where(d=>!d.IsConnection).ToArray();
            foreach(var target in nodes.Take(4))foreach(var source in nodes)for(int repeat=0;repeat<20;repeat++)Pair(net,source.Id,source.StartDistance,new RoadAccess{lane=target.Id,distance=target.EndDistance},true);
        }
        foreach(var network in seen){builds+=network.GoalBuildCount;pruned+=network.GoalPruned;}
        foreach(var lease in leases)lease.Dispose();
        Check(builds>0&&pruned>0,"Hints never exercised; reject vacuous comparison");
        Console.WriteLine("Goal trees="+builds+" excluded expansions="+pruned);
'@
$fixture=$fixture.Replace('output?.Dispose();',$extra+"`n        output?.Dispose();")
$entry=Join-Path $dir 'ExactGoalBounds.cs';[IO.File]::WriteAllText($entry,$fixture)
$overrides=@{}
if($NetworkReference){
    $revision=(& git -C $root rev-parse --verify ($NetworkReference+'^{commit}')).Trim()
    if($LASTEXITCODE -ne 0){throw 'Unknown frozen Network reference'}
    foreach($name in @('CityRoadCoreNetwork.cs','CityRouteGoalBounds.cs')){
        $source=& git -C $root show ($revision+':Assets/Scripts/Runtime/Infrastructure/Roads/'+$name)
        if($LASTEXITCODE -ne 0){throw ('Frozen routing source unavailable: '+$name)}
        $frozen=Join-Path $dir $name;[IO.File]::WriteAllLines($frozen,$source);$overrides[$name]=$frozen
    }
    [IO.File]::WriteAllText((Join-Path $dir 'reference.txt'),$revision)
}
$answers=Join-Path $dir 'answers.bin'
if($Runtime -eq 'DotNet64'){
    $binary=Build-CityDotNetHost $root $dir $entry -SourceOverrides $overrides
    & dotnet $binary $answers | Tee-Object (Join-Path $dir 'checks.log')
}else{
    $mono='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/bin/mono.exe'
    $compiler='C:/Program Files/Unity/Hub/Editor/6000.6.3f1/Editor/Data/MonoBleedingEdge/lib/mono/4.5/csc.exe'
    $binary=Join-Path $dir 'checks.exe'
    $sources=Get-CityModelSources $root;foreach($name in $overrides.Keys){$sources=@($sources|Where-Object {[IO.Path]::GetFileName($_) -ne $name})+$overrides[$name]}
    & $mono $compiler /nologo /langversion:latest ('/out:'+$binary) $sources $entry
    if($LASTEXITCODE -ne 0){throw 'Goal bounds compile failed'}
    & $mono $binary $answers | Tee-Object (Join-Path $dir 'checks.log')
}
if($LASTEXITCODE -ne 0){throw 'Exact goal-bound comparison failed'}
(Get-FileHash -LiteralPath $answers).Hash|Set-Content (Join-Path $dir 'answers.sha256')
