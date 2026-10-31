// Test-only exact binary witness/export. Never linked into Unity runtime.
internal static class CityOrderWire {
 internal static int[] Order(HarborCity.CityCoreIndustrySession s)=>s.Entities.Members.Select(m=>m.Person.id).ToArray();
 internal static byte[] Envelope(HarborCity.CityCoreIndustrySession s,bool current,int[] order=null,bool overrideOrder=false){
  s.Validate();var data=s.SnapshotBusiness();if(data.version!=8&&data.version!=9)throw new System.Exception("Unknown witness schema");
  var fields=data.GetType().GetFields(System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Instance).Where(f=>f.Name!="memberOrder").ToDictionary(f=>f.Name);var names=fields.Keys.ToList();if(current)names.Add("memberOrder");names.Sort(System.StringComparer.Ordinal);
  var put=typeof(HarborCity.CityCoreSave).GetMethod("Put",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
  using(var stream=new System.IO.MemoryStream())using(var w=new System.IO.BinaryWriter(stream)){
   w.Write(0x48434932);w.Write(current?18:17);w.Write(true);w.Write(data.GetType().FullName);w.Write(names.Count);
   foreach(string name in names){w.Write(name);System.Type type;object value;if(name=="memberOrder"){type=typeof(int[]);value=overrideOrder?order:Order(s);}else if(name=="version"){type=typeof(int);value=current?9:8;}else{type=fields[name].FieldType;value=fields[name].GetValue(data);}put.Invoke(null,new object[]{w,type,value});}
   byte[] core=HarborCity.RoadCore.SaveCodec.Write(s.Transport);w.Write(core.Length);w.Write(core);byte[] payload=stream.ToArray();return payload.Concat(System.Security.Cryptography.SHA256.HashData(payload)).ToArray();
  }
 }
 internal static byte[] Witness(HarborCity.CityCoreIndustrySession s){using(var stream=new System.IO.MemoryStream())using(var w=new System.IO.BinaryWriter(stream)){w.Write(Envelope(s,false));var order=Order(s);w.Write(order.Length);foreach(int id in order)w.Write(id);return stream.ToArray();}}
 internal static void WriteNative(string path,byte[] bytes){System.IO.File.WriteAllBytes(path,bytes);if(path.EndsWith(".hcity",System.StringComparison.OrdinalIgnoreCase))System.IO.File.WriteAllBytes(path+".witness",Witness(HarborCity.CityCoreSave.Read(bytes)));}
}
