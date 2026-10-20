using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace HarborCity
{
    public static class CityCodeLayoutChecks
    {
        public static void Run()
        {
            if(!Application.isBatchMode)throw new Exception("Use an isolated batch project");
            var assembly=typeof(CityCoreIndustrySession).Assembly;
            foreach(string name in new[]{"HarborCityGame","CityTraffic","CityRoads","TrafficTrip","TrafficState","CitySaveData","CityRoadTopology","CityDailyAnalysis"})
                if(assembly.GetType("HarborCity."+name)!=null)throw new Exception("Old runtime is still compiled: "+name);
            if(typeof(CityModel).GetField("roads")!=null||typeof(CityModel).GetField("traffic")!=null)throw new Exception("Old mutable world still attached to CityModel");
            int scenes=0,prefabs=0;
            foreach(string guid in AssetDatabase.FindAssets("t:Scene",new[]{"Assets"}))
            {
                var scene=EditorSceneManager.OpenScene(AssetDatabase.GUIDToAssetPath(guid));foreach(var root in scene.GetRootGameObjects())CheckMissing(root);scenes++;
            }
            foreach(string guid in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets"}))
            {
                var root=PrefabUtility.LoadPrefabContents(AssetDatabase.GUIDToAssetPath(guid));try{CheckMissing(root);prefabs++;}finally{PrefabUtility.UnloadPrefabContents(root);}
            }
            foreach(var type in new[]{typeof(HarborCityRoadCoreGame),typeof(CityLandscape),typeof(HarborCityRoadViews)})
            {
                bool found=false;foreach(string guid in AssetDatabase.FindAssets(type.Name+" t:MonoScript",new[]{"Assets"}))
                    if(AssetDatabase.LoadAssetAtPath<MonoScript>(AssetDatabase.GUIDToAssetPath(guid))?.GetClass()==type)found=true;
                if(!found)throw new Exception("Current MonoScript type missing: "+type.Name);
            }
            Debug.Log("PASS current runtime types only; "+scenes+" scenes / "+prefabs+" prefabs have no missing scripts; current MonoScript GUIDs resolve");
        }
        static void CheckMissing(GameObject root)
        {
            foreach(var t in root.GetComponentsInChildren<Transform>(true))if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)!=0)throw new Exception("Missing script: "+t.name);
        }
    }
}
