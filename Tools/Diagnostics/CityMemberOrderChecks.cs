using System;using System.IO;using System.Linq;using System.Collections.Generic;using System.Security.Cryptography;using HarborCity;
static class CityMemberOrderChecks {
 static int checks;static void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}
 static string Hash(CityCoreIndustrySession s)=>Convert.ToHexString(SHA256.HashData(CityOrderWire.Witness(s)));
 static void RoundTrip(string name,CityCoreIndustrySession s,List<string> rows,bool baselineDefect){
  int[] before=CityOrderWire.Order(s);byte[] bytes=CityCoreSave.Write(s);var cold=CityCoreSave.Read(bytes);bool same=before.SequenceEqual(CityOrderWire.Order(cold));
#if ORDER_PERSISTED
  Check(same,"Member sequence changed on cold read: "+name);Check(bytes.SequenceEqual(CityCoreSave.Write(cold)),"Current save roundtrip changed bytes");
#else
  Check(same!=baselineDefect,"Baseline order defect expectation changed: "+name);Check(CityOrderWire.Envelope(s,false).SequenceEqual(bytes),"Canonical witness lost a saved fact");
#endif
  for(int i=0;i<240;i++){s.Advance(.05);cold.Advance(.05);}bool equal=Hash(s)==Hash(cold);
#if ORDER_PERSISTED
  Check(equal,"Full cold continuation differs: "+name);
#else
  Check(equal!=baselineDefect,"Baseline continuation expectation changed: "+name);
#endif
  rows.Add(name+","+string.Join(";",before)+","+string.Join(";",CityOrderWire.Order(cold))+","+equal+","+Hash(s)+","+Hash(cold));
 }
 static void Main(string[] args){var rows=new List<string>{"case,warm_order,cold_order,equal,warm_sha256,cold_sha256"};var s=CityCoreIndustrySession.Create();var family=new Household{id=s.City.society.nextId};s.Entities.AddFamily(family);s.Entities.MoveResident(1,family.id);s.Entities.MoveResident(1,1);RoundTrip("move-back",s,rows,true);
  s=CityCoreIndustrySession.Create();((ResidentialBuilding)s.City.GetBuilding(3)).housingUnits=2;family=new Household{id=s.City.society.nextId,home=3,unit=1,resident=true};s.Entities.AddFamily(family);s.Entities.MoveResident(2,family.id);s.Entities.MoveResident(4,family.id);RoundTrip("interleaved",s,rows,true);
#if ORDER_PERSISTED
  var originalOrder=CityOrderWire.Order(s);var handed=new CityCoreIndustrySession(s.City,s.Simulation.CalendarOffset,s.Transport.Snapshot(),s.Simulation.ClockSnapshot);Check(CityOrderWire.Order(handed).SequenceEqual(originalOrder),"Interleaved handover regrouped members");
#endif
  s=CityCoreIndustrySession.Create();s.City.society.families[0].people.Reverse();
#if ORDER_PERSISTED
  s=CityCoreSave.Read(CityOrderWire.Envelope(s,true,new[]{4,3,2,1},true));
#else
  s=CityCoreSave.Read(CityOrderWire.Envelope(s,false));
#endif
  Check(CityOrderWire.Order(s).SequenceEqual(new[]{4,3,2,1}),"Reader sorted by ID");RoundTrip("non-ID-order",s,rows,false);
#if ORDER_PERSISTED
  Check(CityCoreSave.DefaultFileName=="harbor-city-core-v18.hcity","Save path may overwrite an older default file");
  s=CityCoreIndustrySession.Create();var h=s.Entities.Members[0].Family;for(int i=0;i<1100;i++){var added=new CityResident{id=s.City.society.nextCitizenId,age=20,location=3};s.Entities.AddResident(h.id,added);s.Entities.RemoveResident(added.id);}RoundTrip("compaction",s,rows,false);
  foreach(var order in new int[][]{null,new int[0],new[]{1,1,3,4},new[]{1,2,3,99},new[]{1,2,3,4,5}}){bool rejected=false;try{CityCoreSave.Read(CityOrderWire.Envelope(s,true,order,true));}catch(ArgumentException){rejected=true;}catch(InvalidDataException){rejected=true;}Check(rejected,"Malformed member order accepted");}
  byte[] before=CityCoreSave.Write(s);bool bad=false;try{new CityCoreIndustrySession(s.City,s.Simulation.CalendarOffset,s.Transport.Snapshot(),s.Simulation.ClockSnapshot,memberOrder:new[]{1,1,3,4});}catch(ArgumentException){bad=true;}Check(bad&&before.SequenceEqual(CityCoreSave.Write(s)),"Rejected constructor changed original city");
  var replacement=new CityCoreIndustrySession(s.City,s.Simulation.CalendarOffset,s.Transport.Snapshot(),s.Simulation.ClockSnapshot);Check(CityOrderWire.Order(replacement).SequenceEqual(CityOrderWire.Order(s)),"Same-model handover lost sequence");
  bool oldRejected=false;try{CityCoreSave.Read(CityOrderWire.Envelope(replacement,false));}catch(InvalidDataException){oldRejected=true;}Check(oldRejected,"Runtime accepted old format");
#endif
  File.WriteAllLines(Path.Combine(args[0],"order-results.csv"),rows);Console.WriteLine("PASS member-order tests="+checks+"; baseline known failures are separate rows, not claimed restored");}
}
