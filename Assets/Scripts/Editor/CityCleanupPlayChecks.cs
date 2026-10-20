using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace HarborCity
{
    // Actual Play-mode launch in an isolated project, not a manual Start call.
    [InitializeOnLoad] public static class CityCleanupPlayChecks
    {
        const string Active="Harbor.CleanupPlay.Active",Frames="Harbor.CleanupPlay.Frames";
        static CityCleanupPlayChecks(){EditorApplication.update+=Observe;}
        public static void Run()
        {
            if(!Application.isBatchMode)throw new Exception("Use an isolated batch project");
            CityCodeLayoutChecks.Run();EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            SessionState.SetInt(Frames,0);SessionState.SetBool(Active,true);EditorApplication.EnterPlaymode();
        }
        static void Observe()
        {
            if(!SessionState.GetBool(Active,false))return;
            try
            {
                int frames=SessionState.GetInt(Frames,0);
                if(!EditorApplication.isPlaying){if(frames<30)return;SessionState.SetBool(Active,false);EditorApplication.Exit(0);return;}
                SessionState.SetInt(Frames,++frames);if(frames!=30)return;
                var games=UnityEngine.Object.FindObjectsByType<HarborCityRoadCoreGame>(FindObjectsSortMode.None);
                if(games.Length!=1)throw new Exception("Normal Play must launch exactly one game entry");
                var s=(CityCoreIndustrySession)typeof(HarborCityRoadCoreGame).GetField("session",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(games[0]);
                if(s==null||s.City.buildings.Count!=80||s.City.power<=0||s.Transport.Clock<=0)throw new Exception("Normal Play did not initialize and advance the real template city");
                s.Validate();var restored=CityCoreSave.Read(CityCoreSave.Write(s));restored.Validate();
                string output=Path.GetFullPath(Path.Combine(Application.dataPath,"../Library/LegacyCleanupPlay"));Directory.CreateDirectory(output);File.WriteAllText(Path.Combine(output,"Result.txt"),"PASS actual normal Play: exactly one RoadCore game entry, 80 facilities, real fixed-step advancement, city validation and cold save read\n");
                Debug.Log("PASS actual normal Play entry, real template city, advancing fixed steps and cold save read");EditorApplication.ExitPlaymode();
            }
            catch(Exception e){SessionState.SetBool(Active,false);Debug.LogException(e);EditorApplication.Exit(1);}
        }
    }
}
