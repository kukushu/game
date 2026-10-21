using System;using System.IO;using System.Diagnostics;using System.Security.Cryptography;using HarborCity;
static class CityDepotFrozenBaseline {
 static void Main(string[] a){var s=CityCoreSave.Read(File.ReadAllBytes(a[0]));s.Simulation.Growth.Zoning.SetSurface(CityScenarioBuilder.Flat);for(int i=0;i<200;i++)s.Simulation.Advance(.05,1,double.PositiveInfinity);var w=Stopwatch.StartNew();for(int i=0;i<int.Parse(a[2]);i++)s.Simulation.Advance(.05,1,double.PositiveInfinity);w.Stop();s.Validate();byte[] b=CityCoreSave.Write(s);File.WriteAllBytes(a[1],b);using(var h=SHA256.Create())Console.WriteLine("Frozen 4ac850c runtime: steps="+a[2]+" ms="+w.Elapsed.TotalMilliseconds+" SHA256="+BitConverter.ToString(h.ComputeHash(b)).Replace("-",""));}
}
