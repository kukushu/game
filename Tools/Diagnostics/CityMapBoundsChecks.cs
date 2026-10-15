using System;
using System.Linq;
using HarborCity;
using HarborCity.RoadCore;
static class CityMapBoundsChecks
{
 static int n;static void Check(bool b,string why){n++;if(!b)throw new Exception(why);}
 static void Main(){
  Check(new CityMapState().Valid()&&new CityMapState().buildHalfSize==168,"Default map changed");
  foreach(float extent in new[]{264f,456f,792f,2048f}){var map=CityMapState.DiagnosticFlat(extent);Check(map.Valid()&&map.terrainHalfSize==extent+72,"Invalid test map");}
  foreach(float extent in new[]{0f,167f,2049f,float.NaN,float.PositiveInfinity}){bool fail=false;try{CityMapState.DiagnosticFlat(extent);}catch(ArgumentOutOfRangeException){fail=true;}Check(fail,"Invalid bound accepted");}
  Check(!new CityMapState{terrainVersion=2,buildHalfSize=264}.Valid(),"Normal map enlarged silently");
  var net=new CityUtilityNetwork();var a=new RoadNode{x=250,z=0};var b=new RoadNode{x=260,z=0};
  Check(net.Plan(a,b,out _)==null,"Default utility bounds enlarged");var planned=net.Plan(a,b,out _,264);Check(planned!=null&&planned.Valid(264)&&!planned.Valid(),"Map not authoritative in utility validation");
  Check(net.Plan(a,new RoadNode{x=265,z=0},out _,264)==null,"Out of map utility accepted");
  Check(!ZoneCell.ValidKey("250.000:0.000:0.000")&&ZoneCell.ValidKey("250.000:0.000:0.000",264),"Zoning bounds do not follow map");
  var s=CityCoreIndustrySession.CreateEmpty();s.City.map=CityMapState.DiagnosticFlat(264);s.Simulation.Growth.Zoning.SetSurface(CityScenarioBuilder.Flat);
  s.Simulation.Utilities.Build(UtilityKind.Water,new Point(250,2,0),new Point(260,2,0));s.Validate();
  var bytes=CityCoreSave.Write(s);var r=CityCoreSave.Read(bytes);r.Simulation.Growth.Zoning.SetSurface(CityScenarioBuilder.Flat);r.Validate();Check(bytes.SequenceEqual(CityCoreSave.Write(r)),"Expanded map/utility save differs");
  int cash=s.City.money;var before=CityCoreSave.Write(s);bool rejected=false;try{s.Simulation.Utilities.Build(UtilityKind.Water,new Point(260,2,0),new Point(270,2,0));}catch(InvalidOperationException){rejected=true;}Check(rejected&&cash==s.City.money&&before.SequenceEqual(CityCoreSave.Write(s)),"Failed boundary build modified city");
  Console.WriteLine("PASS "+n+" explicit map boundary and save/transaction checks");
 }
}
