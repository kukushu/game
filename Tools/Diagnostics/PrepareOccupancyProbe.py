"""Diagnostic hooks only; all changes stay in the disposable Unity project."""
def instrument(project, counting=False, trim=False, active_sort=False):
    path=project/'Assets/Scripts/Runtime/Traffic/CityRoadCoreWorld.cs'
    s=path.read_text(encoding='utf-8-sig')
    probe='HarborCity.OccupancyMovementProbe'
    def timed(part, text):
        return 'using('+probe+'.Measure('+probe+'.Part.'+part+')){'+text+'}'
    s=s.replace('        void Reindex()\n        {','        void Reindex()\n        {\n            using var indexProbe='+probe+'.Measure('+probe+'.Part.Reindex);')
    old='foreach(var b in occupancy.Values)b.Clear();ActiveCount=0;awaitingRestarts=0;reservations.Reset(Network);'
    assert old in s
    s=s.replace(old,timed('ClearBuckets','foreach(var b in occupancy.Values)b.Clear();')+timed('ResetReservations','ActiveCount=0;awaitingRestarts=0;reservations.Reset(Network);'))
    start=s.index('        void Reindex()');end=s.index('        public void Step()',start)
    reindex=s[start:end]
    reindex=reindex.replace('foreach(var v in data.vehicles)', 'using var actorProbe='+probe+'.Measure('+probe+'.Part.RegisterActors);foreach(var v in data.vehicles)')
    reindex=reindex.replace('if(v.status==VehicleStatus.Active&&(!EndpointExists',probe+'.Actor(v);if(v.status==VehicleStatus.Active&&(!EndpointExists')
    reindex=reindex.replace('foreach(var b in occupancy.Values)b.Sort(LaneOccupantOrder.Instance);',timed('SortBuckets','foreach(var b in occupancy.Values){'+probe+'.Bucket(b.Count);b.Sort(LaneOccupantOrder.Instance);}'))
    s=s[:start]+reindex+s[end:]
    start=s.index('        internal void MoveStep(');end=s.index('        internal bool EndpointExists',start);move=s[start:end]
    i=move.index('{')+1;move=move[:i]+'\n            using var moveProbe='+probe+'.Measure('+probe+'.Part.Move);'+move[i:]
    move=move.replace('if(indexDirty)Reindex();',timed('EnsureIndex','if(indexDirty)Reindex();'))
    old='if(Network.HasParallelLanes)EditEpoch+=LaneChanges.Choose(Network,data.vehicles,occupancy,(int)(timeline.Tick%int.MaxValue));'
    assert old in move
    move=move.replace(old,timed('ChooseChanges','if(Network.HasParallelLanes){int started=LaneChanges.Choose(Network,data.vehicles,occupancy,(int)(timeline.Tick%int.MaxValue));EditEpoch+=started;'+probe+'.Started(started);}'))
    move=move.replace('entries.Clear();intents.Clear();',timed('ClearBuffers','entries.Clear();intents.Clear();'))
    a=move.index('                    foreach(var pair in occupancy)');b=move.index('                    intents.Sort(',a)
    move=move[:a]+timed('GatherIntents',move[a:b])+move[b:]
    move=move.replace('intents.Sort(IntentOrder.Instance);',timed('SortIntents','intents.Sort(IntentOrder.Instance);'))
    a=move.index('                    for(int index=0;');b=move.index('\n                }\n#if UNITY_5_3_OR_NEWER',a)
    move=move[:a]+timed('AdvanceIntents',move[a:b])+move[b:]
    a=move.index('                foreach(var intent in intents)');b=move.index('                EditEpoch++;',a)
    commit=move[a:b]
    commit=commit.replace('var v=intent.vehicle;v.CommitPosition', 'var v=intent.vehicle;var before=IndexProbeBefore(v);v.CommitPosition')
    commit=commit.replace('if(v.cursor!=v.Plan.segments.Count-1', 'IndexProbeAfter(before,v);if(v.cursor!=v.Plan.segments.Count-1')
    move=move[:a]+timed('CommitPositions',commit)+move[b:]
    s=s[:start]+move+s[end:]
    # Claim calculation mirrors Reindex solely to count semantic changes. It is
    # enabled only in the untimed census, never in the timing or stock variants.
    helper='''
        (int junction,long movement) IndexProbeClaim(Vehicle v) {
            var d=Network.Get(v.Current);int j=d.JunctionId;
            if(j==0&&v.distance<EntryReservationEnd(d)-1e-9)j=d.From;
            if(!d.IsConnection&&v.distance>StopDistance(d)+1e-9)j=d.To;
            return j!=0&&Network.IsConflictJunction(j)?(j,v.changeTarget!=0?0:Movement(Network,v.Plan.segments,v.cursor,d,j)):(0,0);
        }
        HarborCity.OccupancyMovementProbe.Before IndexProbeBefore(Vehicle v) {
            if(!HarborCity.OccupancyMovementProbe.Counting)return default;
            var c=IndexProbeClaim(v);return HarborCity.OccupancyMovementProbe.Capture(v,c.junction,c.movement);
        }
        void IndexProbeAfter(HarborCity.OccupancyMovementProbe.Before before,Vehicle v) {
            if(!HarborCity.OccupancyMovementProbe.Counting)return;
            var c=IndexProbeClaim(v);HarborCity.OccupancyMovementProbe.Committed(before,v,c.junction,c.movement);
        }
'''
    if counting:
        needle='        void IndexDeparture(Vehicle v)\n        {';assert needle in s
        s=s.replace(needle,needle+'\n            '+probe+'.Departure();')
        needle='if(retained<data.vehicles.Count)data.vehicles.RemoveRange(retained,data.vehicles.Count-retained);';assert needle in s
        s=s.replace(needle,probe+'.Retired(data.vehicles.Count-retained);'+needle)
        changes=project/'Assets/Scripts/Runtime/Traffic/CityRoadCoreLaneChanges.cs'
        c=changes.read_text(encoding='utf-8-sig');needle='bucket.Insert(Slot(bucket,d),v.shadowOccupant);return true;';assert needle in c
        changes.write_text(c.replace(needle,'bucket.Insert(Slot(bucket,d),v.shadowOccupant);HarborCity.OccupancyMovementProbe.ShadowAdded(target);return true;'),encoding='utf-8')
        s=s.replace('        internal bool EndpointExists',helper+'        internal bool EndpointExists',1)
    else:
        s=s.replace(probe+'.Actor(v);','').replace(probe+'.Bucket(b.Count);','').replace(probe+'.Started(started);','')
        s=s.replace('var before=IndexProbeBefore(v);','').replace('IndexProbeAfter(before,v);','')
    if trim:
        s=s.replace('foreach(var b in occupancy.Values)b.Clear();','foreach(var b in occupancy.Values)if(b.Count>0)b.Clear();')
        s=s.replace('b.Sort(LaneOccupantOrder.Instance);','if(b.Count>1)b.Sort(LaneOccupantOrder.Instance);')
    if active_sort:
        # Recomputed during this very rebuild, not a persistent membership cache.
        # Births and LaneChanges outside Reindex therefore cannot leave stale refs.
        s=s.replace('        void Reindex()','        readonly List<List<LaneOccupant>> indexProbeLiveBuckets=new List<List<LaneOccupant>>();\n        void Reindex()',1)
        s=s.replace('using var indexProbe='+probe+'.Measure('+probe+'.Part.Reindex);','using var indexProbe='+probe+'.Measure('+probe+'.Part.Reindex);indexProbeLiveBuckets.Clear();')
        a=s.index('        void Reindex()');b=s.index('        public void Step()',a);r=s[a:b]
        for old,new in [('bucket.Add(placement.occupant);','if(bucket.Count==0)indexProbeLiveBuckets.Add(bucket);bucket.Add(placement.occupant);'),('b.Add(v.sourceOccupant);','if(b.Count==0)indexProbeLiveBuckets.Add(b);b.Add(v.sourceOccupant);'),('shadow.Add(v.shadowOccupant);','if(shadow.Count==0)indexProbeLiveBuckets.Add(shadow);shadow.Add(v.shadowOccupant);')]:
            assert old in r;r=r.replace(old,new)
        r=r.replace('foreach(var b in occupancy.Values){','foreach(var b in indexProbeLiveBuckets){')
        s=s[:a]+r+s[b:]
    path.write_text(s,encoding='utf-8')
