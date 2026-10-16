"""Only two observation hooks in a Temp copy; no product source edits."""
from pathlib import Path
import sys
root=Path(__file__).resolve().parents[1]
s=(root/'Assets/Scripts/Runtime/Traffic/CityRoadCoreWorld.cs').read_text(encoding='utf-8-sig')
for a,b in [
 ('reservations.Reserve(reservation,spawnMovement,v.id);return v;',
  'reservations.Reserve(reservation,spawnMovement,v.id);ImportObservation.Depart(this,v);return v;'),
 ('v.status=VehicleStatus.Delivered;ActiveCount--;}',
  'v.status=VehicleStatus.Delivered;ActiveCount--;ImportObservation.Arrive(this,v,Clock+StepSeconds);}')]:
    if s.count(a)!=1:raise RuntimeError('Review changed source anchor: '+a)
    s=s.replace(a,b)
Path(sys.argv[1]).write_text(s,encoding='utf-8')
