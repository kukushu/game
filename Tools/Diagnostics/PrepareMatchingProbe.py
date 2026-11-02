"""Add bounded operation counts to a disposable model host, never Assets.

Uses the existing full-state replay. Counts are diagnostic, not Player timings.
Optional frozen directory contains the three changed files; all other sources
must remain identical to the baseline. Every text anchor must match exactly once.
"""
from pathlib import Path
import sys

root, target = (Path(p).resolve() for p in sys.argv[1:3])
if not target.is_relative_to(root / 'Temp'):
    raise RuntimeError('Probe output must be inside project Temp')
target.mkdir(parents=True, exist_ok=True)
frozen = Path(sys.argv[3]).resolve() if len(sys.argv) > 3 else None
paths = {
    'CityJobs.cs': 'Residents/CityJobs.cs',
    'CityEntities.cs': 'Core/CityEntities.cs',
    'CityFreightCandidates.cs': 'Industry/CityFreightCandidates.cs',
    'CityCorePopulation.cs': 'Residents/CityCorePopulation.cs',
}

def patch(text, old, new):
    if text.count(old) != 1:
        raise RuntimeError('Review changed diagnostic anchor: ' + old[:100])
    return text.replace(old, new)

for name, relative in paths.items():
    source = frozen / name if frozen and (frozen / name).exists() else root / 'Assets/Scripts/Runtime/Simulation' / relative
    text = source.read_text(encoding='utf-8-sig')
    if name == 'CityJobs.cs':
        text = patch(text, 'public bool AssignJob(CityResident p,int jobId)\n        {',
                     'public bool AssignJob(CityResident p,int jobId)\n        { MatchingProbe.Assignments++;')
    if name == 'CityFreightCandidates.cs':
        text = patch(text, 'lanes.Clear();network=', 'MatchingProbe.Rebuilds++;lanes.Clear();network=')
        text = patch(text, 'internal void Collect(RoadAccess source,List<CommercialBuilding> output)\n        {',
                     'internal void Collect(RoadAccess source,List<CommercialBuilding> output)\n        { MatchingProbe.Collections++;')
    if name == 'CityCorePopulation.cs':
        text = patch(text, 'bool FindJobs()\n        {', 'bool FindJobs()\n        { MatchingProbe.Rounds++;')
        text = patch(text, 'var p=member.Person;var h=member.Family;', 'MatchingProbe.Members++;var p=member.Person;var h=member.Family;')
        text = patch(text, 'continue;CityJob best=null;', 'continue;MatchingProbe.Seekers++;CityJob best=null;')
        text = patch(text, 'CityJob candidate=null;foreach(var job in pair.Value)',
                     'MatchingProbe.Slots+=pair.Value.Count;CityJob candidate=null;foreach(var job in pair.Value)')
        text = patch(text, 'candidates.Sort(JobCandidate.Compare);',
                     'candidates.Sort(JobCandidate.Compare);MatchingProbe.Candidates+=candidates.Count;')
    (target / name).write_text(text, encoding='utf-8')

entry = (root / 'Tools/Diagnostics/CityFreightFundingReplay.cs').read_text(encoding='utf-8-sig')
entry = patch(entry, 'var restored=CityCoreSave.Read(final);',
              'MatchingProbe.Write(args[1]+".workload.csv");var restored=CityCoreSave.Read(final);')
entry += '''
static class MatchingProbe {
 internal static long Assignments,Rebuilds,Collections,Rounds,Members,Seekers,Slots,Candidates;
 internal static void Write(string path){File.WriteAllText(path,
 "assignments,freight_rebuilds,freight_collections,matching_rounds,member_visits,eligible_seekers,slot_visits,candidates_sorted\\n"+
 string.Join(",",Assignments,Rebuilds,Collections,Rounds,Members,Seekers,Slots,Candidates)+"\\n");}
}
'''
(target / 'MatchingReplay.cs').write_text(entry, encoding='utf-8')
