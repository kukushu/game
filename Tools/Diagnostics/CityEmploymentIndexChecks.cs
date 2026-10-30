using System;using System.IO;using System.Linq;using System.Collections.Generic;using System.Security.Cryptography;using HarborCity;
static class CityEmploymentIndexChecks {
 static int checks=0;
 static void Oracle(CityCoreIndustrySession s){
#if EMPLOYMENT_INDEX
  s.Entities.ValidateEmploymentIndex();var expectedByWork=s.Entities.Members.GroupBy(m=>s.City.Workplace(m.Person)).ToDictionary(g=>g.Key,g=>g.ToArray());var found=new List<CityMember>();foreach(var b in s.Entities.Buildings){s.Entities.CollectEmployees(b.id,found);expectedByWork.TryGetValue(b.id,out var expected);if(!found.SequenceEqual(expected??Array.Empty<CityMember>()))throw new Exception("Employment index order/membership differs");checks++;}
#endif
 }
 static string Hash(CityCoreIndustrySession s)=>Convert.ToHexString(SHA256.HashData(CityCoreSave.Write(s)));
 static void Measure(string output,string save){var s=CityCoreSave.Read(File.ReadAllBytes(save));s.Transport.DepotLaneRoutingEnabled=true;s.Simulation.Growth.Zoning.SetSurface(CityScenarioBuilder.Flat);for(int i=0;i<2400;i++)s.Advance(.05);
  long retained=GC.GetTotalMemory(true),allocated=GC.GetAllocatedBytesForCurrentThread();var times=new double[600];var watch=new System.Diagnostics.Stopwatch();for(int i=0;i<times.Length;i++){watch.Restart();if(s.Advance(.05)!=1)throw new Exception("Lost fixed step");watch.Stop();times[i]=watch.Elapsed.TotalMilliseconds;}allocated=GC.GetAllocatedBytesForCurrentThread()-allocated;Array.Sort(times);
  File.WriteAllLines(Path.Combine(output,"maintenance.csv"),new[]{"steps,mean_ms,p95_ms,p99_ms,max_ms,allocated_bytes,retained_bytes,final_sha256",string.Join(",",600,times.Average().ToString("R",System.Globalization.CultureInfo.InvariantCulture),times[569].ToString("R",System.Globalization.CultureInfo.InvariantCulture),times[593].ToString("R",System.Globalization.CultureInfo.InvariantCulture),times[599].ToString("R",System.Globalization.CultureInfo.InvariantCulture),allocated,retained,Hash(s))});Console.WriteLine("PASS real30k maintenance measurement; model only, not Player/FPS");}
 static void Main(string[] args){if(args.Length>1){Measure(args[0],args[1]);return;}var s=CityCoreIndustrySession.Create();var p=s.Entities.Members[0].Person;int jobId=p.jobId;var job=s.City.ResidentJob(p);var rows=new List<string>{"case,sha256"};Oracle(s);
  // Direct supported scalar writes invalidate even between the two sides of a binding.
  p.jobId=-1;Oracle(s);p.jobId=jobId;Oracle(s);job.occupiedCitizenId=-1;Oracle(s);job.occupiedCitizenId=p.id;Oracle(s);int workplace=job.buildingId;job.buildingId=2;Oracle(s);job.buildingId=workplace;Oracle(s);
  for(int i=0;i<100;i++){s.City.ReleaseJob(p);Oracle(s);if(!s.City.AssignJob(p,jobId))throw new Exception("Reassignment fixture failed");Oracle(s);}rows.Add("reassign,"+Hash(s));
  s.City.GetBuilding(1).level=2;s.City.SyncJobs();Oracle(s);rows.Add("slots,"+Hash(s));
  var family=s.Entities.Members[0].Family;var added=new CityResident{id=s.City.society.nextCitizenId,age=30,canWork=true,skill=3,location=family.home};s.Entities.AddResident(family.id,added);int vacancy=s.City.society.jobEntities.First(j=>j.occupiedCitizenId<0).id;if(!s.City.AssignJob(added,vacancy))throw new Exception("Added employment fixture failed");Oracle(s);
  s.Entities.RemoveResident(added.id);Oracle(s);added.jobId=jobId;Oracle(s);rows.Add("remove,"+Hash(s));
  var cold=CityCoreSave.Read(CityCoreSave.Write(s));Oracle(cold);if(Hash(cold)!=Hash(s))throw new Exception("Cold index state differs");
  for(int i=0;i<1200;i++){s.Advance(.05);cold.Advance(.05);if(i%100==99){Oracle(s);Oracle(cold);if(Hash(cold)!=Hash(s))throw new Exception("Cold continuation differs");rows.Add("tick-"+i+","+Hash(s));}}
  File.WriteAllLines(Path.Combine(args[0],"employment.csv"),rows);Console.WriteLine("PASS employment direct binding writes, release/reassign, slots, removal, original order and cold1200; checks="+checks);
 }
}
