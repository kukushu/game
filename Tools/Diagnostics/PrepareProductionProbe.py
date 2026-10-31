"""Add observation to a disposable Temp project; preserve the business statements."""
from pathlib import Path
import sys
root, project = (Path(p).resolve() for p in sys.argv[1:3])
assert project.is_relative_to(root / 'Temp'), 'Only a disposable project may be patched'
mode=sys.argv[3] if len(sys.argv)>3 else 'detail'
assert mode in ('coarse','off','detail','census')
layout='selective'
def replace(text, old, new):
    assert text.count(old) == 1, f'Expected one source anchor: {old[:100]}'
    return text.replace(old, new)
target=project/'Assets/Scripts/Diagnostics/Runtime/ProductionPhaseProbe.cs'
target.write_text((root/'Tools/Diagnostics/ProductionPhaseProbe.cs').read_text(encoding='utf-8').replace('const string Layout="deep"', 'const string Layout="'+layout+'"'),encoding='utf-8')
p=project/'Assets/Scripts/Runtime/Simulation/Industry/CityProductionSystem.cs'
s=p.read_text(encoding='utf-8-sig')
s=replace(s,'payroll.BeginStep();float minute=', 'using(ProductionPhaseProbe.Measure(ProductionPhaseProbe.Part.PayrollBegin)){payroll.BeginStep();}float minute=')
a=s.index('            if(city.logSink!=null)');b=s.index('            city.AdvanceIndustry(minutes);',a)
s=s[:a]+'            using(ProductionPhaseProbe.Measure(ProductionPhaseProbe.Part.Workers)){\n'+s[a:b]+'            }\n'+s[b:]
s=replace(s,'city.AdvanceIndustry(minutes);','using(ProductionPhaseProbe.Measure(ProductionPhaseProbe.Part.Industry)){city.AdvanceIndustry(minutes);}')
p.write_text(s,encoding='utf-8')
p=project/'Assets/Scripts/Runtime/Simulation/Industry/CityIndustry.cs';s=p.read_text(encoding='utf-8-sig');a=s.index('        public void AdvanceIndustry(float minutes)');b=s.index('        public bool ValidIndustry()',a);body=s[a:b];original=body.replace('public void AdvanceIndustry(float minutes)','void ProbeOriginalAdvanceIndustry(float minutes)')
body=replace(body,'            float noiseKeep=', '            var coefficients=ProductionPhaseProbe.Measure(ProductionPhaseProbe.Part.Coefficients,true);float noiseKeep=')
body=replace(body,'            foreach(var old in residualIndustry)', '            coefficients.Dispose();ProductionPhaseProbe.ResidualCount(residualIndustry.Count);using(ProductionPhaseProbe.Measure(ProductionPhaseProbe.Part.Residual,true)){\n            foreach(var old in residualIndustry)')
body=replace(body,'            foreach(var b in buildings)', '            }\n            foreach(var b in buildings)')
body=replace(body,'                var f=(b as IndustrialBuilding)?.factory;', '                ProductionPhaseProbe.Building();var f=(b as IndustrialBuilding)?.factory;')
body=replace(body,'                    // Normalised worker-equivalents from actual processing, not job occupancy.', '                    var before=ProductionPhaseProbe.BeforeFactory(f);using(ProductionPhaseProbe.Measure(ProductionPhaseProbe.Part.FactoryEnvironment,true)){\n                    // Normalised worker-equivalents from actual processing, not job occupancy.')
body=replace(body,'                    string state=', '                    }using(ProductionPhaseProbe.Measure(ProductionPhaseProbe.Part.FactoryState,true)){string state=')
body=replace(body,'                    f.stepAttendance=f.stepProductive=0;', '                    f.stepAttendance=f.stepProductive=0;}ProductionPhaseProbe.AfterFactory(f,before);')
# Count/timestamp only the original Supply reads, not an extra supply computation.
body=body.replace('Supply(b.id)', 'ProductionPhaseProbe.Supply(this,b.id)')
body=replace(body,'                if(UseOf(b.id)!=LandUse.Residential) continue;', '                using var homeScope=ProductionPhaseProbe.Measure(ProductionPhaseProbe.Part.HomeGateAndDecay,true);if(UseOf(b.id)!=LandUse.Residential) continue;')
body=replace(body,'var home=(ResidentialBuilding)b;home.heavyTraffic=home.heavyTraffic*trafficKeep+0f;', 'var home=(ResidentialBuilding)b;float homeBefore=home.heavyTraffic;home.heavyTraffic=home.heavyTraffic*trafficKeep+0f;ProductionPhaseProbe.Home(homeBefore,home.heavyTraffic);')
body=replace(body,'            if(minutes<=0) return;', '            if(!ProductionPhaseProbe.DeepRequested){ProbeOriginalAdvanceIndustry(minutes);return;}if(minutes<=0) return;')
p.write_text(s[:a]+body+original+s[b:],encoding='utf-8')
p=project/'Assets/Scripts/Diagnostics/Runtime/HarborCityScenarioWindowProbe.cs';s=p.read_text(encoding='utf-8-sig')
s=replace(s,'File.WriteAllBytes(Path.Combine(output,"fixed-start.hcity"),CityCoreSave.Write(city));','File.WriteAllBytes(Path.Combine(output,"fixed-start.hcity"),CityCoreSave.Write(city));ProductionPhaseProbe.Reset(output);')
s=replace(s,'for(int i=0;i<steps;i++){yield return null;long before=', 'for(int i=0;i<steps;i++){yield return null;ProductionPhaseProbe.BeginStep(city.Simulation.Tick+1);long before=')
s=replace(s,'clock.Stop();long bytes=supported?', 'clock.Stop();ProductionPhaseProbe.EndStep();long bytes=supported?')
s=replace(s,'long measuredEnd=city.Simulation.Tick;IndexOracle(city);','ProductionPhaseProbe.Write(output);long measuredEnd=city.Simulation.Tick;IndexOracle(city);')
s=replace(s,'"PASS Unity fixed-work complete state and cold20; scripted one step/frame only");Application.Quit(0);','"PASS Unity fixed-work complete state and cold20; scripted one step/frame only");yield return new WaitForEndOfFrame();var screenshot=ScreenCapture.CaptureScreenshotAsTexture();try{File.WriteAllBytes(Path.Combine(output,"fixed-window.png"),screenshot.EncodeToPNG());}finally{UnityEngine.Object.Destroy(screenshot);}yield return null;Application.Quit(0);')
p.write_text(s,encoding='utf-8')
print('Prepared Temp-only production observation:', project)
