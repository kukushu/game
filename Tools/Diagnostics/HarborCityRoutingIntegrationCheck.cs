using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using HarborCity;

// Copied only into the disposable research project. Plain startup has no -harbor
// arguments: this exercises the actual normal-Play default and Save/Load handlers.
public sealed class HarborCityRoutingIntegrationCheck:MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        if(!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("HARBOR_ROUTING_INTEGRATION_OUTPUT")))
        {
            Application.runInBackground=true;
            new GameObject("Routing integration check").AddComponent<HarborCityRoutingIntegrationCheck>();
        }
    }
    IEnumerator Start()
    {
        yield return null;yield return null;
        string output=Environment.GetEnvironmentVariable("HARBOR_ROUTING_INTEGRATION_OUTPUT");Directory.CreateDirectory(output);int code=0;
        try{Run(output);}catch(Exception e){File.WriteAllText(Path.Combine(output,"Result.txt"),"FAIL "+e);Debug.LogException(e);code=1;}
        Application.Quit(code);
    }
    static void Check(bool ok,string message){if(!ok)throw new InvalidOperationException(message);}
    static void Run(string output)
    {
        Check(Application.companyName=="HarborCityIntegration"&&Application.productName=="LaneRoadBatching20261024","Must isolate persistent test files from user saves");
        var host=FindAnyObjectByType<HarborCityRoadCoreGame>();Check(host!=null&&host.ScenarioSession!=null,"No normal game session");host.ScenarioRate(0);
        Check(host.ScenarioSession.Transport.DepotLaneRoutingEnabled&&!host.ScenarioSession.Transport.DepotJunctionRoutingEnabled,"Normal Play did not select lane routing alone");
        var flags=BindingFlags.Instance|BindingFlags.NonPublic;
        string save=(string)typeof(HarborCityRoadCoreGame).GetProperty("SavePath",flags).GetValue(host);
        Check(Path.GetFullPath(save).StartsWith(Path.GetFullPath(Application.persistentDataPath)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase),"Test save escaped isolated profile");
        Directory.CreateDirectory(Path.GetDirectoryName(save));File.Copy(Environment.GetEnvironmentVariable("HARBOR_ROUTING_INTEGRATION_INPUT"),save,true);
        var initial=host.ScenarioSession;typeof(HarborCityRoadCoreGame).GetMethod("Load",flags).Invoke(host,null);
        var city=host.ScenarioSession;Check(!ReferenceEquals(initial,city),"Normal Load failed");
        Check(city.Transport.DepotLaneRoutingEnabled&&!city.Transport.DepotJunctionRoutingEnabled,"Normal Load lost routing selection");
        Check(host.ScenarioRoadRenderers>0&&host.ScenarioRoadRenderers<host.ScenarioRoadViews,"Loaded city did not batch its complete road network");
        for(int i=0;i<200&&(i<20||city.Transport.DepotRoutes.TreeBuilds==0);i++){Check(city.Simulation.Advance(.05,1,double.PositiveInfinity)==1,"Fixed step lost");city.Transport.AssertSafety();Check(city.Simulation.Waste.BalanceError==0,"Waste not conserved");}
        Check(city.Transport.DepotRoutes.TreeBuilds>0&&city.Transport.JunctionRoutes.Builds==0,"Real garbage dispatch did not use lane trees exclusively");
        byte[] before=CityCoreSave.Write(city);typeof(HarborCityRoadCoreGame).GetMethod("Save",flags).Invoke(host,null);
        Check(before.SequenceEqual(File.ReadAllBytes(save)),"Normal Save handler failed");
        typeof(HarborCityRoadCoreGame).GetMethod("Load",flags).Invoke(host,null);var loaded=host.ScenarioSession;
        Check(!ReferenceEquals(city,loaded)&&loaded.Transport.DepotLaneRoutingEnabled&&!loaded.Transport.DepotJunctionRoutingEnabled,"Save/Load did not preserve active algorithm");
        Check(before.SequenceEqual(CityCoreSave.Write(loaded)),"Normal Save/Load changed authoritative city facts");
        for(int i=0;i<20;i++)
        {
            Check(city.Simulation.Advance(.05,1,double.PositiveInfinity)==1&&loaded.Simulation.Advance(.05,1,double.PositiveInfinity)==1,"Continuation lost fixed steps");
            city.Transport.AssertSafety();loaded.Transport.AssertSafety();Check(city.Simulation.Waste.BalanceError==0&&loaded.Simulation.Waste.BalanceError==0,"Continuation waste balance");
        }
        city.Validate();loaded.Validate();Check(CityCoreSave.Write(city).SequenceEqual(CityCoreSave.Write(loaded)),"Cold versus warm cache full continuation mismatch");
        File.WriteAllText(Path.Combine(output,"Result.txt"),"PASS native plain normal startup selects lane trees, junction disabled; current real city Load and batched roads; real garbage queries use lane trees; normal Save/Load exact city facts and selection; 20 warm/cold fixed continuation steps exact; actor safety and waste conservation. No mouse or FPS claim.\n");
        File.WriteAllText(Path.Combine(output,"views.csv"),"logical_drivables,road_renderers,lane_tree_builds,junction_tree_builds\n"+string.Join(",",host.ScenarioRoadViews,host.ScenarioRoadRenderers,loaded.Transport.DepotRoutes.TreeBuilds,loaded.Transport.JunctionRoutes.Builds));
    }
}
