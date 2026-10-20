using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
namespace HarborCity
{
    // Explicit native visual acceptance only. Never enabled by normal Play.
    public sealed class HarborCityUICapture:MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Enable()
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-harbor-ui-capture")>=0)new GameObject("UI visual acceptance").AddComponent<HarborCityUICapture>();
        }
        IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs();int flag=Array.IndexOf(args,"-harbor-ui-capture");string output=args[flag+1];Directory.CreateDirectory(output);
            for(int i=0;i<20;i++)yield return null;
            var game=FindAnyObjectByType<HarborCityRoadCoreGame>();const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;var type=game.GetType();
            var session=(CityCoreIndustrySession)type.GetField("session",flags).GetValue(game);var before=CityCoreSave.Write(session);
            var camera=(Camera)type.GetField("cameraView",flags).GetValue(game);camera.transform.position=new Vector3(0,180,-145);camera.transform.LookAt(new Vector3(0,2,0));
            var page=type.GetField("toolPage",flags);var select=type.GetMethod("SelectPage",flags);
            foreach(var name in new[]{"None","Roads","Zones","Utilities","Trade","City","Options"})
            {
                if(name!="None")select.Invoke(game,new[]{Enum.Parse(page.FieldType,name)});
                var panel=(Rect)type.GetProperty("ToolPanelBounds",flags).GetValue(game);var status=(Rect)type.GetProperty("StatusBounds",flags).GetValue(game);
                if(panel.width>0&&panel.Overlaps(status))throw new Exception("Tool panel overlaps city status");
                yield return null;yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(output,name+".png"));
                yield return new WaitForEndOfFrame();yield return null;
            }
            if(!before.SequenceEqual(CityCoreSave.Write(session))){File.WriteAllText(Path.Combine(output,"Result.txt"),"FAIL UI navigation changed complete city state");Application.Quit(1);yield break;}
            File.WriteAllText(Path.Combine(output,"Result.txt"),"PASS native rendered overview and six tool pages; complete city state unchanged; programmatic navigation, not manual mouse acceptance");Application.Quit(0);
        }
    }
}
