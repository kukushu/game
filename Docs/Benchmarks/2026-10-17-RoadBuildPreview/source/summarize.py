import csv,json,statistics
from pathlib import Path
r=Path('Temp/RoadBuildDiagnosis-20261017')
summary=[]
for pop in [3000,10000,30000]:
 rows=list(csv.DictReader((r/str(pop)/'scopes.csv').open()))
 def vals(name,phase=None): return [float(x['ms']) for x in rows if x['name']==name and (phase is None or x['phase']==phase)]
 p=[x for x in rows if x['phase']=='preview1']
 nt=[float(x['ms']) for x in p if x['name']=='NetworkCtor']
 pv=vals('ActionPreview')
 summary.append(dict(scenario_population=pop,preview_min_ms=min(pv),preview_median_ms=statistics.median(pv),preview_max_ms=max(pv),network_builds_in_preview=len(nt),network_build_total_ms=sum(nt),network_share=sum(nt)/vals('Preview','preview1')[0],click_plan_ms=vals('ActionPlan')[0],click_check_ms=vals('ActionCheck')[0],commit_ms=vals('ActionCommit')[0],road_views_ms=vals('ActionRoadViews')[0],zone_views_ms=vals('ZoneViews','nextFrame')[0],first_post_edit_step_ms=vals('ActionFullStep','postEditStep0')[0]))
with (r/'summary.csv').open('w',newline='') as f:
 w=csv.DictWriter(f,fieldnames=summary[0].keys());w.writeheader();w.writerows(summary)
print(json.dumps(summary,indent=2))
