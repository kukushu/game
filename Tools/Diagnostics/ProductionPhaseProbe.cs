using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Collections.Generic;
namespace HarborCity {
// Disposable test-project observer only. No business decisions, writes, or saved data.
internal static class ProductionPhaseProbe {
 internal const string Layout="deep";
 internal static bool DeepRequested=>enabled&&(mode=="detail"||mode=="census");
 internal enum Part { PayrollBegin,Workers,Industry,Coefficients,Residual,FactoryEnvironment,FactoryState,HomeGateAndDecay,Supply }
 static string mode="off";static bool enabled;static bool fine=>enabled&&mode=="detail";static bool census=>enabled&&mode=="census";
 static readonly long[] ticks=new long[9],calls=new long[9];static long[] previous=new long[9];
 static readonly List<string> steps=new List<string>();static long tick;
 static long buildings,factories,homes,residual,environmentZero,environmentChanged,stateChanged,attendanceZero,productiveZero,homeZero,homeChanged;
 internal readonly struct Scope:IDisposable {
  readonly bool active;readonly Part part;readonly long began;
  internal Scope(Part p,bool detail){active=enabled&&(!detail||fine);part=p;began=active?Stopwatch.GetTimestamp():0;}
  public void Dispose(){if(active){ticks[(int)part]+=Stopwatch.GetTimestamp()-began;calls[(int)part]++;}}
 }
 internal static Scope Measure(Part p,bool detail=false)=>new Scope(p,detail);
 internal static void Reset(string output){var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-harbor-production-profile");mode=i<0?"off":args[i+1];if(mode!="off"&&mode!="coarse"&&mode!="detail"&&mode!="census")throw new ArgumentException("Unknown production probe mode");if(Layout=="coarse"&&(mode=="detail"||mode=="census"))throw new InvalidOperationException("Detailed observation requires a deep-layout Player");enabled=mode!="off";Array.Clear(ticks,0,9);Array.Clear(calls,0,9);previous=new long[9];buildings=factories=homes=residual=environmentZero=environmentChanged=stateChanged=attendanceZero=productiveZero=homeZero=homeChanged=0;steps.Clear();steps.Add("tick,phase,ms");File.WriteAllText(Path.Combine(output,"production-mode.txt"),mode);File.WriteAllText(Path.Combine(output,"production-layout.txt"),Layout);}
 internal static void BeginStep(long value){tick=value;}
 internal static void EndStep(){if(!enabled)return;for(int k=0;k<9;k++){steps.Add(tick+","+(Part)k+","+N((ticks[k]-previous[k])*1000.0/Stopwatch.Frequency));previous[k]=ticks[k];}}
 internal static void Building(){if(census)buildings++;}
 internal static void ResidualCount(int count){if(census)residual+=count;}
 internal readonly struct FactoryBefore {
  internal readonly float noise,pollution;internal readonly string status;
  internal FactoryBefore(FactoryState f){noise=f.noise;pollution=f.pollution;status=f.status;}
 }
 internal static FactoryBefore BeforeFactory(FactoryState f){if(!census)return default;factories++;if(f.noise==0&&f.pollution==0&&f.stepProductive==0)environmentZero++;if(f.stepAttendance==0)attendanceZero++;if(f.stepProductive==0)productiveZero++;return new FactoryBefore(f);}
 static bool Same(float a,float b)=>BitConverter.SingleToInt32Bits(a)==BitConverter.SingleToInt32Bits(b);
 internal static void AfterFactory(FactoryState f,FactoryBefore before){if(!census)return;if(!Same(f.noise,before.noise)||!Same(f.pollution,before.pollution))environmentChanged++;if(f.status!=before.status)stateChanged++;}
 internal static void Home(float before,float after){if(!census)return;homes++;if(before==0)homeZero++;if(!Same(before,after))homeChanged++;}
 internal static BuildingSupply Supply(CityModel city,int id){using(Measure(Part.Supply,true))return city.Supply(id);}
 static string N(double value)=>value.ToString("R",CultureInfo.InvariantCulture);
 internal static void Write(string output){enabled=false;var rows=new List<string>{"phase,total_ms,calls"};for(int k=0;k<9;k++)rows.Add((Part)k+","+N(ticks[k]*1000.0/Stopwatch.Frequency)+","+calls[k]);File.WriteAllLines(Path.Combine(output,"production-total.csv"),rows);File.WriteAllLines(Path.Combine(output,"production-steps.csv"),steps);
  File.WriteAllLines(Path.Combine(output,"production-counts.csv"),new[]{"metric,value","buildings,"+buildings,"factories,"+factories,"homes,"+homes,"residual,"+residual,"environment_zero,"+environmentZero,"environment_changed,"+environmentChanged,"state_changed,"+stateChanged,"attendance_zero,"+attendanceZero,"productive_zero,"+productiveZero,"home_zero,"+homeZero,"home_changed,"+homeChanged});}
}
}
