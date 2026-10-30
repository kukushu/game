"""Prepare only the existing disposable Player probe, retaining the 8 MiB budget."""
from pathlib import Path
import sys
root=Path(sys.argv[1]).resolve();project=root/'Temp/ResidentScheduling/Unity';capacity=int(sys.argv[2])
assert capacity in (4096,16384)
w=project/'Assets/Scripts/Runtime/Simulation/Services/CityCoreWaste.cs'
s=w.read_text(encoding='utf-8-sig');needle='state.nextDispatchTick=time.Tick+rules.DispatchTicks;if(plants.Count==0)return;'
assert s.count(needle)==1
w.write_text(s.replace(needle,needle+'using var cacheObservation=WasteCacheProbe.Begin(world,time.Tick);'),encoding='utf-8')
n=project/'Assets/Scripts/Runtime/Infrastructure/Roads/CityRoadCoreNetwork.cs';s=n.read_text(encoding='utf-8-sig')
assert s.count('const int SharedRouteCapacity=4096;')==1
n.write_text(s.replace('const int SharedRouteCapacity=4096;',f'const int SharedRouteCapacity={capacity};'),encoding='utf-8')
(project/'Assets/Scripts/Diagnostics/Runtime/WasteCacheProbe.cs').write_text((root/'Tools/Diagnostics/WasteCacheProbe.cs').read_text(encoding='utf-8-sig'),encoding='utf-8')
d=project/'Assets/Scripts/Diagnostics/Runtime/HarborCityResidentWindowProbe.cs';s=d.read_text(encoding='utf-8-sig');needle='File.WriteAllText(Path.Combine(output,"index-work.csv")'
assert s.count(needle)==1
d.write_text(s.replace(needle,'WasteCacheProbe.Write(output);'+needle),encoding='utf-8')
print('Prepared Temp-only exact-cache experiment:',capacity,'entries; existing 8 MiB budget unchanged')
