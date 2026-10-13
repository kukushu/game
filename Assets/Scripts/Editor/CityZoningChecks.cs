using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HarborCity
{
    public static class CityZoningChecks
    {
        [MenuItem("Harbor/Validate zoning save")]
        public static void Validate()
        {
            var c=CityModel.Create();c.SetEntranceHeight(CityBuildingChecks.Flat);
            var plan=c.roads.Plan(c,c.roads.Node(CityRoads.Entrance),CityBuildingChecks.P(25,1.5f),CityBuildingChecks.Flat);
            CityBuildingChecks.Check(c.CommitRoad(plan),"Zoning fixture road");
            var cells=c.ZoningCells(CityBuildingChecks.Flat);c.PaintZones(cells.Where(x=>x.side==1),LandUse.Residential);
            var copy=CityBuildingChecks.Copy(c);
            CityBuildingChecks.Check(copy.Valid() && copy.zoning.designations.Count==c.zoning.designations.Count
                && copy.ZoningCells(CityBuildingChecks.Flat).Count(x=>x.use==LandUse.Residential)==c.zoning.designations.Count,"Native JsonUtility zoning round trip");
            var shader=Resources.Load<Shader>("Zoning");
            CityBuildingChecks.Check(shader!=null && !ShaderUtil.ShaderHasError(shader),"Included URP zoning shader");
            CityBuildingChecks.Result("CityZoningChecks","PASS: native zoning JSON, map configuration and shader import");
        }
        // Run in a disposable validation project; never reset a user's open scene.
        public static void RenderFixture()
            =>Render(false);
        public static void RenderDevelopedCity()
            =>Render(true);
        public static void BuildValidationPlayer()
        {
            if(!Application.isBatchMode) throw new InvalidOperationException("Validation build requires isolated batch mode");
            string output=Path.GetFullPath(Path.Combine(Application.dataPath,"../Builds/CS1Validation/HarborCity.exe"));Directory.CreateDirectory(Path.GetDirectoryName(output));
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
            PlayerSettings.defaultScreenWidth=1440;PlayerSettings.defaultScreenHeight=900;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/SampleScene.unity"},locationPathName=output,target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
            if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded) throw new InvalidOperationException("Standalone validation build failed: "+report.summary.result);
        }
        static void Render(bool grow)
        {
            if(!Application.isBatchMode) throw new InvalidOperationException("RenderFixture requires isolated batch mode");
            Validate();EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var game=new GameObject("Zoning validation city").AddComponent<HarborCityGame>();
            const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
            var type=typeof(HarborCityGame);type.GetMethod("Start",flags).Invoke(game,null);
            var city=(CityModel)type.GetField("city",flags).GetValue(game);
            var land=(CityLandscape)type.GetField("landscape",flags).GetValue(game);
            var road=city.roads.Plan(city,city.roads.Node(CityRoads.Entrance),new RoadNode{x=28,z=1.5f,y=land.Height(28,1.5f)},land.Height);
            CityBuildingChecks.Check(road.Valid && city.CommitRoad(road),"Terrain road fixture: "+road.error);
            var cells=city.ZoningCells(land.Height);
            city.PaintZones(cells.Where(x=>x.pose.x<-22),LandUse.Residential);
            city.PaintZones(cells.Where(x=>x.pose.x>=-22 && x.pose.x<2),LandUse.Commercial);
            city.PaintZones(cells.Where(x=>x.pose.x>=2),LandUse.Industrial);
            if(grow)
            {
                foreach(var service in new[]{LandUse.Power,LandUse.Water})
                {
                    var lot=city.RoadsidePreview(service==LandUse.Power?-47:-40,-5,service,HousingKind.Apartment,out string error);
                    CityBuildingChecks.Check(city.PlaceBuilding(lot,service,land.Height,out error)>0,error);
                }
                HarborCityStandaloneValidation.ConnectSupplies(city,land.Height);
                var traffic=(CityTraffic)type.GetField("traffic",flags).GetValue(game);traffic.Advance(600);
                CityBuildingChecks.Check(city.population>0 && city.Employed>0 && city.Valid(),"Terrain city grows residents, jobs and real transport");
                CityBuildingChecks.Check(CityBuildingChecks.Copy(city).Valid(),"Developed city native JSON round trip");
            }
            type.GetField("selected",flags).SetValue(game,LandUse.Residential);
            type.GetField("focus",flags).SetValue(game,new Vector3(-10,0,1.5f));type.GetField("zoom",flags).SetValue(game,48f);
            type.GetMethod("Rebuild",flags).Invoke(game,null);type.GetMethod("RefreshZoningOverlay",flags).Invoke(game,null);type.GetMethod("UpdateCamera",flags).Invoke(game,null);
            type.GetMethod("RefreshDevelopmentView",flags).Invoke(game,null);
            var camera=(Camera)type.GetField("cam",flags).GetValue(game);
            var target=new RenderTexture(1280,800,24);target.Create();camera.targetTexture=target;
            UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera,new UnityEngine.Rendering.Universal.UniversalRenderPipeline.SingleCameraRequest{destination=target});
            RenderTexture.active=target;var image=new Texture2D(1280,800,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,800),0,0);image.Apply();
            string output=Path.GetFullPath(Path.Combine(Application.dataPath,"../Library/CS1Validation"));
            Directory.CreateDirectory(output);File.WriteAllBytes(Path.Combine(output,grow?"DevelopedCity.png":"ZoningPreview.png"),image.EncodeToPNG());
            File.WriteAllText(Path.Combine(output,"City.json"),JsonUtility.ToJson(city.ToSaveData(),true));
            File.WriteAllText(Path.Combine(output,"Result.txt"),"Population="+city.population+"; employed="+city.Employed+"; jobs="+city.jobs+"; buildings="+city.buildings.Count+"; income="+city.income+"; trafficCompleted="+city.traffic.completed+"; valid="+city.Valid());
            camera.targetTexture=null;RenderTexture.active=null;UnityEngine.Object.DestroyImmediate(image);target.Release();UnityEngine.Object.DestroyImmediate(target);
            CityBuildingChecks.Result("CityZoningRender","PASS: terrain road and R/C/I overlay rendered; this is a fixture, not interactive gameplay acceptance");
        }
    }
}
