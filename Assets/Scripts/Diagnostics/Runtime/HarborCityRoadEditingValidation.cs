using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using HarborCity.RoadCore;
using UnityEngine;
namespace HarborCity
{
    // Native acceptance only. Runs in the actual growing city, not a second engine.
    public static class HarborCityRoadEditingValidation
    {
        static void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);}
        public static IEnumerator Run(CityCoreIndustrySession session,HarborCityRoadEditor editor,Func<float,float,float> height,string path,Action<Exception> finished)
        {
            int id=0;double upgradeMs=0,reverseMs=0;int buildCost=0,upgradeCost=0;
            try
            {
                var world=session.Transport;var prior=new HashSet<int>(world.Network.RoadIds);var points=new Point[9];for(int i=0;i<points.Length;i++){float x=-125+i*6;points[i]=new Point(x,height(x,55),55);}
                Check(session.Construction.Commit(world.PlanConnectedRoad(points,lanes:1),out buildCost),"Native extra street commit failed");id=world.Network.RoadIds.Single(r=>!prior.Contains(r));Check(id!=1,"Native editor still targets first road");
                editor.Bind(session);editor.Begin();var geometry=world.Network.Centreline(id);editor.Pick(geometry.Sample(geometry.Length/2));Check(editor.Selected==id,"Native selected road ID differs from actual street");
                var positions=world.Vehicles.ToDictionary(v=>v.id,v=>world.Position(v));var tasks=world.Vehicles.ToDictionary(v=>v.id,v=>v.Task.id);int money=session.City.money;var timer=Stopwatch.StartNew();Check(editor.Upgrade(2),"Native selected widening failed");timer.Stop();upgradeMs=timer.Elapsed.TotalMilliseconds;upgradeCost=money-session.City.money;Check(world.Network.Settings(id).lanes==2&&upgradeCost>0,"Native widened street did not alter actual lanes/pay");
                var callback=editor.Changed;bool refreshed=false;var network=world.Network;money=session.City.money;
                try{editor.Changed=()=>refreshed=true;Check(editor.Upgrade(2)&&!refreshed&&ReferenceEquals(network,world.Network)&&session.City.money==money,"No-op editor action rebuilt views/network or charged cash");}finally{editor.Changed=callback;}
                foreach(var v in world.Vehicles)Check(tasks[v.id]==v.Task.id&&Point.Distance(positions[v.id],world.Position(v))<1e-7,"Editing quiet street moved unrelated real traffic");
                timer.Restart();Check(editor.ToggleDirection()&&editor.Reverse(),"Native one-way/reverse tool failed");timer.Stop();reverseMs=timer.Elapsed.TotalMilliseconds;var settings=world.Network.Settings(id);Check(settings.direction==RoadDirection.Backward&&world.Network.Route(settings.from,settings.to)==null&&world.Network.Route(settings.to,settings.from)!=null,"Native direction only changed appearance");
                var loaded=CityCoreSave.Read(CityCoreSave.Write(session));loaded.Simulation.Growth.Zoning.SetSurface(height);for(int i=0;i<40;i++){session.Advance(.05);loaded.Advance(.05);}Check(CityCoreSave.Write(session).SequenceEqual(CityCoreSave.Write(loaded)),"Native edited-city continuation differs");session.Validate();
            }
            catch(Exception e){File.WriteAllText(Path.Combine(path,"RoadEditingResult.txt"),"FAIL "+e);finished(e);yield break;}
            yield return null;yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(path,"RoadEditing.png"));yield return null;
            try
            {
                int money=session.City.money;var timer=Stopwatch.StartNew();Check(editor.Remove(),"Native selected road removal failed");timer.Stop();Check(session.City.money==money-40&&!session.Transport.Network.ContainsRoad(id)&&editor.Selected==0,"Native removal charged incorrectly or retained stale selection");var bytes=CityCoreSave.Write(session);Check(!editor.Reverse()&&bytes.SequenceEqual(CityCoreSave.Write(session)),"Missing selection modified city");session.Validate();
                File.WriteAllText(Path.Combine(path,"road-editing.csv"),"road_id,build_cost,upgrade_cost,upgrade_tool_ms,direction_tool_ms,remove_tool_ms,save_equal\n"+string.Join(",",id,buildCost,upgradeCost,upgradeMs.ToString("F4",System.Globalization.CultureInfo.InvariantCulture),reverseMs.ToString("F4",System.Globalization.CultureInfo.InvariantCulture),timer.Elapsed.TotalMilliseconds.ToString("F4",System.Globalization.CultureInfo.InvariantCulture),true));
                File.WriteAllText(Path.Combine(path,"RoadEditingResult.txt"),"PASS native actual growing city: non-first street selected/highlighted, paid widening, one-way/reverse actual reachability, unrelated traffic unchanged, exact in-flight edited save continuation, selected removal and stale-selection rejection. Tool timings include view callbacks. Programmatic tool commands; no manual mouse click acceptance claimed.");editor.Clear();finished(null);
            }
            catch(Exception e){File.WriteAllText(Path.Combine(path,"RoadEditingResult.txt"),"FAIL "+e);finished(e);}
        }
    }
}
