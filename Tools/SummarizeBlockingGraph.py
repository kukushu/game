"""Read-only summary for the single-blocker stalled snapshot diagnostic."""
import collections,json,sys
from pathlib import Path
source=Path(sys.argv[1]);out=Path(sys.argv[2])
if out.exists():raise SystemExit("Fresh evidence path required")
d=json.loads(source.read_text(encoding="utf-8-sig"));edges=d["edges"]
a={e["from"]:e["to"] for e in edges}
if len(a)!=len(edges):raise SystemExit("Multiple blockers per vehicle: single-successor cycle analysis is inapplicable; raw graph preserved")
done=set();cycles=[]
for start in a:
    path=[];seen={};v=start
    while v not in done and v not in seen and v in a:
        seen[v]=len(path);path.append(v);v=a[v]
    if v in seen:cycles.append(path[seen[v]:])
    done.update(path)
summary=dict(physicalClaims=d["claims"],reasons=dict(collections.Counter(e["reason"] for e in edges)),cycles=cycles,cycleLengths=[len(c) for c in cycles],dependencyNodes=len(a),unblockedNodes=d["active"]-len(a),note="Necessary blockers reconstructed at a single snapshot. Cycles show waiting dependencies, not proof of a specific code defect. This tool refuses ambiguous multi-blocker graphs.")
out.write_text(json.dumps(summary,ensure_ascii=False,indent=2),encoding="utf-8")
print({k:v for k,v in summary.items() if k!="cycles"})
