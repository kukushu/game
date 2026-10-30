"""Temp-only guard shadow. No actual candidate controls execution."""
from pathlib import Path
import sys
root=Path(sys.argv[1]);out=Path(sys.argv[2]);out.mkdir(parents=True,exist_ok=True)
p=root/'Assets/Scripts/Runtime/Simulation/Residents/CityCoreResidents.cs';s=p.read_text(encoding='utf-8-sig')
s=s.replace('readonly CityModel city;readonly World transport;','readonly CityModel city;readonly World transport;ResidentWakeShadow wakeShadow;')
s=s.replace('if(!Enabled)return;PruneEstimates();','if(!Enabled)return;PruneEstimates();using var wakeFrame=(wakeShadow??(wakeShadow=new ResidentWakeShadow(entities,transport))).Begin();')
s=s.replace('var p=member.Person;var h=member.Family;if(!h.resident', 'using var wakeObservation=wakeShadow.Observe(member);var p=member.Person;var h=member.Family;if(!h.resident')
s=s.replace('if(!returning&&!leaving)continue;p.retryAt=', 'if(!returning&&!leaving)continue;wakeShadow.Request(p.id);p.retryAt=')
assert s.count('new ResidentWakeShadow(')==s.count('wakeShadow.Observe(')==s.count('wakeShadow.Request(')==1
(out/'CityCoreResidents.shadow.cs').write_text(s,encoding='utf-8')
# Put shared usings before all concatenated namespaces; no alternative model implementation.
parts=[]
for name in ('CityWakeQueue.cs','ResidentWakeShadow.cs'):
 s=(root/'Tools/Diagnostics'/name).read_text(encoding='utf-8-sig')
 s=s[s.index('\n')+1:];parts.append(s)
body=(root/'Tools/Diagnostics/ResidentWakeReplay.cs').read_text(encoding='utf-8-sig').split('\n',1)[1]
(out/'ShadowSupport.cs').write_text('using System;using System.IO;using System.Linq;using System.Security.Cryptography;using System.Collections.Generic;using HarborCity;using HarborCity.RoadCore;\n'+'\n'.join(parts)+'\n'+body,encoding='utf-8')
