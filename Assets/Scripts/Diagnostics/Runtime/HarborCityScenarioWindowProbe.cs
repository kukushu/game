using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Unity.Profiling;
using UnityEngine;
namespace HarborCity
{
    // Observes the normal host clock and views. Never advances or repairs the city.
    public sealed class HarborCityScenarioWindowProbe:MonoBehaviour
    {
        struct Sample {public double seconds,ms;public long bytes,tick;public int steps,active,views;}
        HarborCityRoadCoreGame host;string savePath,output;float rate;
        public void Configure(HarborCityRoadCoreGame game,string input,string result,float speed)
        {host=game;savePath=input;output=result;rate=speed;StartCoroutine(Measure());}
        IEnumerator Measure()
        {
            Directory.CreateDirectory(output);QualitySettings.vSyncCount=0;Application.targetFrameRate=60;
            yield return null;yield return null;
            host.ScenarioRate(rate);double warm=Time.realtimeSinceStartupAsDouble;
            while(Time.realtimeSinceStartupAsDouble-warm<8)yield return null;
            var city=host.ScenarioSession;long startTick=city.Simulation.Tick;
            var frames=new List<double>(5000);var rows=new List<Sample>(5000);
            var gc=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC Allocated In Frame",1);
            double start=Time.realtimeSinceStartupAsDouble,last=start;long allocated=0;int gcSamples=0,peak=0,maxSteps=0;
            int gen0=GC.CollectionCount(0),gen1=GC.CollectionCount(1),gen2=GC.CollectionCount(2);
            while(Time.realtimeSinceStartupAsDouble-start<60)
            {
                yield return null;double now=Time.realtimeSinceStartupAsDouble,ms=(now-last)*1000;last=now;frames.Add(ms);
                long bytes=gc.Valid&&gc.Count>0?gc.LastValue:-1;if(bytes>=0){allocated+=bytes;gcSamples++;}
                peak=Math.Max(peak,city.Transport.ActiveCount);maxSteps=Math.Max(maxSteps,host.ScenarioSteps);
                rows.Add(new Sample{seconds=now-start,ms=ms,bytes=bytes,tick=city.Simulation.Tick,steps=host.ScenarioSteps,active=city.Transport.ActiveCount,views=host.ScenarioVehicleViews});
            }
            int endGen0=GC.CollectionCount(0),endGen1=GC.CollectionCount(1),endGen2=GC.CollectionCount(2);
            double elapsed=last-start;long endTick=city.Simulation.Tick;host.ScenarioRate(0);gc.Dispose();
            frames.Sort();double p95=frames[(int)Math.Ceiling(frames.Count*.95)-1],p99=frames[(int)Math.Ceiling(frames.Count*.99)-1];
            string hash;using(var sha=SHA256.Create())hash=BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(savePath))).Replace("-","").ToLowerInvariant();
            File.WriteAllLines(Path.Combine(output,"frames.csv"),new[]{"real_seconds,frame_ms,gc_bytes,tick,fixed_steps,active_vehicles,vehicle_views"}.Concat(rows.Select(r=>string.Join(",",r.seconds.ToString("R",CultureInfo.InvariantCulture),r.ms.ToString("R",CultureInfo.InvariantCulture),r.bytes,r.tick,r.steps,r.active,r.views))));
            File.WriteAllText(Path.Combine(output,"summary.csv"),"input_sha256,rate,real_seconds,start_tick,end_tick,sim_ratio,fps,p95_ms,p99_ms,max_ms,gc_bytes_per_frame,gc_samples,gen0,gen1,gen2,peak_vehicles,max_steps_frame,building_views,population\n"+string.Join(",",hash,rate,elapsed,startTick,endTick,(endTick-startTick)*.05/elapsed,frames.Count/elapsed,p95,p99,frames.Last(),gcSamples==frames.Count?(double)allocated/gcSamples:-1,gcSamples,endGen0-gen0,endGen1-gen1,endGen2-gen2,peak,maxSteps,host.ScenarioBuildingViews,city.City.population));
            // Validation and disk output are outside the measured window.
            try{city.Validate();File.WriteAllBytes(Path.Combine(output,"final.hcity"),CityCoreSave.Write(city));File.WriteAllText(Path.Combine(output,"Result.txt"),"PASS normal host visible Player, loaded captured flat mature city; not mouse construction acceptance. GC pauses not measured.\n");}
            catch(Exception e){File.WriteAllText(Path.Combine(output,"Result.txt"),e.ToString());Application.Quit(1);yield break;}
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"window.png"));yield return new WaitForEndOfFrame();yield return null;Application.Quit(0);
        }
    }
}
