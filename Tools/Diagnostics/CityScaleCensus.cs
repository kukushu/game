using System;using System.IO;using System.Linq;using System.Diagnostics;using HarborCity;using HarborCity.RoadCore;
static class CityScaleCensus {
 static void Main(string[] args){
  byte[] raw=File.ReadAllBytes(args[0]);long before=GC.GetTotalMemory(true);var watch=Stopwatch.StartNew();var s=CityCoreSave.Read(raw);watch.Stop();double load=watch.Elapsed.TotalMilliseconds;
  long retained=GC.GetTotalMemory(true)-before;var members=s.Entities.Members;
  int residents=members.Count(m=>m.Family.resident&&!m.Person.dead),working=members.Count(m=>m.Family.resident&&s.City.CanAttendWork(m.Person)&&m.Person.atWork&&m.Person.tripId==0&&m.Person.arrivedDay==s.City.day&&s.City.Workplace(m.Person)==m.Person.location&&s.City.ResidentJob(m.Person)!=null);
  var jobs=s.City.society.jobEntities;var vehicles=s.Transport.Vehicles;
  watch.Restart();byte[] saved=CityCoreSave.Write(s);watch.Stop();
  string header="population,members,families,buildings,jobs,occupied_jobs,at_work,active_vehicles,vehicles,waste_plants,waste_missions,raw_bytes,resaved_bytes,load_ms,save_ms,dotnet_retained_bytes";
  string row=string.Join(",",residents,members.Count,s.City.society.families.Count,s.Entities.Buildings.Count,jobs.Count,jobs.Count(j=>j.occupiedCitizenId>=0),working,s.Transport.ActiveCount,vehicles.Count,s.Entities.Buildings.Count(b=>b is LandfillBuilding),s.Simulation.Waste.Vehicles,raw.Length,saved.Length,load.ToString("F3",System.Globalization.CultureInfo.InvariantCulture),watch.Elapsed.TotalMilliseconds.ToString("F3",System.Globalization.CultureInfo.InvariantCulture),retained);
  File.WriteAllText(args[1],header+"\n"+row+"\n");Console.WriteLine(header);Console.WriteLine(row);s.Validate();Console.WriteLine("PASS read-only complete city census; memory/timing are DotNet64 cold-path diagnostics, not Unity Player FPS");
 }
}
