using System;using System.Collections;using System.IO;using System.Linq;using UnityEngine;
namespace HarborCity
{
    internal static class HarborCityPresentationChecks
    {
        internal static IEnumerator Run(Material material,string output)
        {
            if(HarborCityRoadCoreGame.SimulationDiagnosticArgument("-harbor-legacy-views")||HarborCityRoadCoreGame.SimulationDiagnosticArgument("-logFile")||!HarborCityRoadCoreGame.SimulationDiagnosticArgument("-harbor-city-scenario-window"))throw new Exception("Display-only switch changed simulation launch mode");
            var city=CityCoreIndustrySession.Create();var settings=new HarborCityDisplaySettings();var go=new GameObject("Display test camera");var camera=go.AddComponent<Camera>();camera.enabled=false;camera.farClipPlane=10000;camera.pixelRect=new Rect(0,0,1440,900);var views=new HarborCityBuildingViews(material);
            try{
                camera.transform.position=new Vector3(0,20,-10);camera.transform.LookAt(Vector3.zero);
                for(int i=0;i<20;i++){views.Sync(city.Entities,camera,settings);yield return null;}
                views.AssertDisplayIndex();int initialCreated=views.Created;
                for(int i=0;i<10;i++)views.Sync(city.Entities,camera,settings);
                if(views.Created!=initialCreated)throw new Exception("Unchanged static building frames allocated new individuals");
                var home=city.Entities.Building(3);views.Select(home.id);if(views.Pick(new Ray(new Vector3(home.x,30,home.z),Vector3.down))!=3)throw new Exception("Near building identity differs");
                camera.transform.position=new Vector3(0,2500,-500);camera.transform.LookAt(Vector3.zero);views.Sync(city.Entities,camera,settings);yield return null;
                if(views.Instance(3)!=null||views.Pick(new Ray(new Vector3(home.x,30,home.z),Vector3.down))!=3)throw new Exception("Far building selection depends on permanent GO");
                views.Select(3);var before=CityCoreSave.Write(city);for(int i=0;i<12;i++){camera.transform.position+=new Vector3(20,0,0);views.Sync(city.Entities,camera,settings);views.AssertDisplayIndex();}
                if(!before.SequenceEqual(CityCoreSave.Write(city)))throw new Exception("Paused camera changed city state");
                var building=new ResidentialBuilding{id=city.City.nextBuildingId,x=64,z=64,width=140,depth=12};city.Entities.AddBuilding(building);views.Sync(city.Entities,camera,settings);views.AssertDisplayIndex();
                if(views.Pick(new Ray(new Vector3(125,30,64),Vector3.down))!=building.id)throw new Exception("Cross-cell footprint not selectable");
                var replacement=new ResidentialBuilding{id=building.id,x=-80,z=64,width=140,depth=12};city.Entities.ReplaceBuilding(replacement);views.Sync(city.Entities,camera,settings);views.AssertDisplayIndex();
                if(views.Pick(new Ray(new Vector3(125,30,64),Vector3.down))==building.id||views.Pick(new Ray(new Vector3(-140,30,64),Vector3.down))!=building.id)throw new Exception("Replacement left a stale proxy");
                replacement.x+=512;bool caught=false;try{views.AssertDisplayIndex();}catch(InvalidOperationException){caught=true;}replacement.x-=512;if(!caught)throw new Exception("Independent building oracle failed negative control");
                city.Entities.RemoveBuilding(building.id);views.Sync(city.Entities,camera,settings);views.AssertDisplayIndex();if(views.Pick(new Ray(new Vector3(-140,30,64),Vector3.down))==building.id)throw new Exception("Deleted building remains selectable");
                var restored=CityCoreSave.Read(CityCoreSave.Write(city));views.Sync(restored.Entities,camera,settings);views.AssertDisplayIndex();views.Select(3);
                File.WriteAllText(Path.Combine(output,"PresentationLifecycleResult.txt"),"PASS near/far exclusive coverage; static frame reuse; stable-ID selection without GO; paused complete-save equality; cross-cell AABB; replacement; deletion; independent missed-notification negative control; cold city rebind.\n");
            }finally{views.Dispose();UnityEngine.Object.Destroy(go);}
        }
    }
}
