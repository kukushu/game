using System;using System.IO;using System.Linq;using System.Collections.Generic;using HarborCity;
// ReferenceAdvanceIndustry is generated from the frozen source, not a rewritten formula.
static class CityIndustryZeroChecks {
 static int checks,cases;static readonly Random rng=new Random(150031);
 static float Bits(int n)=>BitConverter.Int32BitsToSingle(n);
 static bool Same(float a,float b)=>BitConverter.SingleToInt32Bits(a)==BitConverter.SingleToInt32Bits(b);
 static CityModel Make(float noise,float pollution,float work,float traffic,bool development,int level){var c=new CityModel();c.development.enabled=development;var f=new IndustrialBuilding{id=1,level=level};f.factory.noise=noise;f.factory.pollution=pollution;f.factory.stepProductive=work;f.factory.stepAttendance=work;c.buildings.Add(f);c.buildings.Add(new ResidentialBuilding{id=2,heavyTraffic=traffic});c.residualIndustry.Add(new IndustrialExposure{noise=noise,pollution=pollution});return c;}
 static void Check(bool ok,string message){checks++;if(!ok)throw new Exception(message+" case="+cases);}
 static bool Equal(CityModel a,CityModel b){var x=a.Factory(1);var y=b.Factory(1);return Same(x.noise,y.noise)&&Same(x.pollution,y.pollution)&&Same(x.stepProductive,y.stepProductive)&&Same(x.stepAttendance,y.stepAttendance)&&x.status==y.status&&Same(((ResidentialBuilding)a.buildings[1]).heavyTraffic,((ResidentialBuilding)b.buildings[1]).heavyTraffic)&&a.residualIndustry.Count==b.residualIndustry.Count&&a.residualIndustry.Zip(b.residualIndustry,(p,q)=>Same(p.noise,q.noise)&&Same(p.pollution,q.pollution)).All(v=>v);}
 static void Pair(float n,float p,float w,float t,bool dev,int level,float minutes){cases++;var a=Make(n,p,w,t,dev,level);var b=Make(n,p,w,t,dev,level);a.ReferenceAdvanceIndustry(minutes);b.AdvanceIndustry(minutes);Check(Equal(a,b),"Frozen industry differs at bits "+BitConverter.SingleToInt32Bits(n)+"/"+BitConverter.SingleToInt32Bits(p)+"/"+BitConverter.SingleToInt32Bits(w)+" minutes="+minutes);}
 static float RandomFloat()=>rng.Next(3)<2?Bits(rng.Next(2)==0?0:int.MinValue):Bits(unchecked((int)((uint)rng.Next()|((uint)rng.Next(2)<<31))));
 static void Main(string[] args){float z=Bits(int.MinValue);float[] times={0,z,-1,float.NaN,float.PositiveInfinity,float.NegativeInfinity,Bits(1),.0001f,.12f,60,1000,float.MaxValue};int[] levels={int.MinValue,-3,-2,0,1,5,int.MaxValue};
  foreach(bool dev in new[]{false,true})foreach(int level in levels)foreach(float minutes in times)for(int signs=0;signs<8;signs++)Pair((signs&1)==0?0:z,(signs&2)==0?0:z,(signs&4)==0?0:z,z,dev,level,minutes);
  for(int i=0;i<10000;i++)Pair(RandomFloat(),RandomFloat(),RandomFloat(),RandomFloat(),i%2==0,levels[i%levels.Length],i%3==0?times[i%times.Length]:(float)(rng.NextDouble()*100000));
  var old=Make(z,z,z,z,false,1);var bad=Make(z,z,z,z,false,1);old.ReferenceAdvanceIndustry(.12f);bad.AdvanceIndustry(.12f);bad.Factory(1).noise=0;Check(!Equal(old,bad),"Signed-zero oracle negative control was ineffective");
  File.WriteAllText(args[0],"cases,checks,signed_zero_negative_control\n"+cases+","+checks+",detected\n");Console.WriteLine("PASS frozen industry bitwise controls: "+cases+" cases; signed-zero negative control detected");
 }
}
