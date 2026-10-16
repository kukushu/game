"""Copy exact current MoveStep with observation hooks into Temp, never edit Assets.
Every replacement must match once. Branches, arithmetic and commits stay intact.
The paired disabled trace build verifies that observation changes no saved facts.
"""
from pathlib import Path
import sys
root=Path(__file__).resolve().parents[1]
source=(root/'Assets/Scripts/Runtime/Traffic/CityRoadCoreWorld.cs').read_text(encoding='utf-8-sig')
def patch(a,b):
    global source
    if source.count(a)!=1:raise RuntimeError('Source changed; review trace anchor: '+a[:90])
    source=source.replace(a,b)
patch('entries.Clear();intents.Clear();','entries.Clear();intents.Clear(); LogisticsTrace.Begin(this,reservations);')
patch('intents.Add(new Intent{vehicle=v,drivable=v.Current,cursor=v.cursor,distance=v.distance,limit=limit});',
      'if(i>0&&bucket[i-1].distance-MinimumSpacing<v.distance+Network.Get(v.Current).Speed*StepSeconds)LogisticsTrace.Block(v,bucket[i-1].vehicle.id,"following");\n'+
      'if(v.changeTarget!=0)LogisticsTrace.Shadow(this,v,occupancy[v.changeTarget]);\n'+
      'intents.Add(new Intent{vehicle=v,drivable=v.Current,cursor=v.cursor,distance=v.distance,limit=limit});')
patch('if(!reservations.CanReserve(d.To,movement,v.id))target=Math.Min(target,StopDistance(d));',
      'if(!reservations.CanReserve(d.To,movement,v.id)){LogisticsTrace.Reservation(this,v,d.To,movement);target=Math.Min(target,StopDistance(d));}')
patch('if(occupancy.TryGetValue(exit.Id,out bucket)&&bucket.Count>0&&bucket[bucket.Count-1].distance<needed)room=false;',
      'if(occupancy.TryGetValue(exit.Id,out bucket)&&bucket.Count>0&&bucket[bucket.Count-1].distance<needed){room=false;LogisticsTrace.Block(v,bucket[bucket.Count-1].vehicle.id,"exit-clearance");}')
patch('if(entries.TryGetValue(exit.Id,out reserved)&&reserved<needed)room=false;',
      'if(entries.TryGetValue(exit.Id,out reserved)&&reserved<needed){room=false;LogisticsTrace.Entry(v,exit.Id,"exit-entry-this-step");}')
patch('target=Math.Max(distance,target);time-=Math.Max(0,target-distance)/d.Speed;distance=target;',
      'target=Math.Max(distance,target);LogisticsTrace.Moved(v,target-distance);time-=Math.Max(0,target-distance)/d.Speed;distance=target;')
patch('if(next.IsConnection&&Network.IsConflictJunction(next.JunctionId)&&!reservations.CanReserve(next.JunctionId,next.Id,v.id))break;',
      'if(next.IsConnection&&Network.IsConflictJunction(next.JunctionId)&&!reservations.CanReserve(next.JunctionId,next.Id,v.id)){LogisticsTrace.Reservation(this,v,next.JunctionId,next.Id);break;}')
patch('if(occupancy.TryGetValue(exit,out exitBucket)&&exitBucket.Count>0&&exitBucket[exitBucket.Count-1].distance<Network.Get(exit).StartDistance+MinimumSpacing)break;',
      'if(occupancy.TryGetValue(exit,out exitBucket)&&exitBucket.Count>0&&exitBucket[exitBucket.Count-1].distance<Network.Get(exit).StartDistance+MinimumSpacing){LogisticsTrace.Block(v,exitBucket[exitBucket.Count-1].vehicle.id,"connection-exit-space");break;}')
patch('if(entries.TryGetValue(exit,out reserved)&&reserved<Network.Get(exit).StartDistance+MinimumSpacing)break;',
      'if(entries.TryGetValue(exit,out reserved)&&reserved<Network.Get(exit).StartDistance+MinimumSpacing){LogisticsTrace.Entry(v,exit,"connection-exit-entry");break;}')
patch('if(rear<next.StartDistance+MinimumSpacing)break;',
      'if(rear<next.StartDistance+MinimumSpacing){if(ahead!=null&&ahead.Count>0&&ahead[ahead.Count-1].distance<next.StartDistance+MinimumSpacing)LogisticsTrace.Block(v,ahead[ahead.Count-1].vehicle.id,"next-drivable-space");LogisticsTrace.Entry(v,next.Id,"next-entry-this-step");break;}')
patch('entries[next.Id]=next.StartDistance;','entries[next.Id]=next.StartDistance;LogisticsTrace.Entered(v,next.Id);')
Path(sys.argv[1]).write_text(source,encoding='utf-8')
