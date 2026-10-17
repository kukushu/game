using System;
using System.IO;
using System.Diagnostics;
using HarborCity;
static class CityBuildingOverlapChecks
{
 static void Main(string[] args)
 {
  var a=new ResidentialBuilding{x=0,z=0,yaw=0,width=10,depth=6};var b=new ResidentialBuilding{x=10,z=0,yaw=0,width=10,depth=6};
  if(a.Overlaps(b))throw new Exception("Touching rectangles overlap");b.x=9;if(!a.Overlaps(b))throw new Exception("Interior overlap missed");b.x=100;if(a.Overlaps(b))throw new Exception("Distant rectangles overlap");
  int yes=0,no=0;var rng=new Random(302319);
  using(var output=new BinaryWriter(File.Create(args[0])))for(int i=0;i<500000;i++)
  {
   a.x=(float)((rng.NextDouble()-.5)*5000);a.z=(float)((rng.NextDouble()-.5)*5000);a.yaw=(float)((rng.NextDouble()-.5)*1440);a.width=(float)(.02+rng.NextDouble()*80);a.depth=(float)(.02+rng.NextDouble()*80);
   b.x=a.x+(float)((rng.NextDouble()-.5)*100);b.z=a.z+(float)((rng.NextDouble()-.5)*100);b.yaw=i%4==0?a.yaw:(float)((rng.NextDouble()-.5)*1440);b.width=(float)(.02+rng.NextDouble()*80);b.depth=(float)(.02+rng.NextDouble()*80);
   if(i%7==0){a.x=a.z=a.yaw=b.yaw=0;b.x=(a.width+b.width)/2-.015f;b.z=0;}if(i%13==0){b.x=a.x;b.z=a.z;}
   bool hit=a.Overlaps(b);if(hit)yes++;else no++;output.Write(hit);
  }
  for(int i=0;i<10000;i++)a.Overlaps(b);var timer=new Stopwatch();long before=GC.GetAllocatedBytesForCurrentThread();timer.Start();for(int i=0;i<100000;i++){a.yaw=i%360;b.x=i%60;if(a.Overlaps(b))yes++;}timer.Stop();long allocated=GC.GetAllocatedBytesForCurrentThread()-before;
  Console.WriteLine("PASS 500000 deterministic oriented-rectangle cases plus touching/interior/distant checks; yes="+yes+" no="+no+"; 100000 warmed calls bytes="+allocated+" ms="+timer.Elapsed.TotalMilliseconds.ToString("F3"));
 }
}
