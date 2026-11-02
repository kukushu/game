using UnityEngine;
namespace HarborCity
{
    // Test-only camera input. Has no route, time, transport or city write access.
    [DefaultExecutionOrder(-100)]
    public sealed class HarborCityDisplayCameraProbe:MonoBehaviour
    {
        HarborCityRoadCoreGame host;string mode;float start;
        internal void Configure(HarborCityRoadCoreGame value,string preset){host=value;mode=preset;start=Time.realtimeSinceStartup;}
        void Update()
        {
            if(host==null||host.ScenarioSession==null)return;var camera=host.ScenarioCamera;float half=host.ScenarioSession.City.map.buildHalfSize;float distance=mode=="near"?25:mode=="middle"?150:half*2.6f;Vector3 target=new Vector3(0,0,0);
            if(mode=="sweep"){float cycle=Mathf.PingPong((Time.realtimeSinceStartup-start)*.25f,1);distance=Mathf.Lerp(25,half*2.6f,cycle);target.x=Mathf.Sin(Time.realtimeSinceStartup*.7f)*half*.6f;target.z=Mathf.Cos(Time.realtimeSinceStartup*.7f)*half*.6f;}
            camera.farClipPlane=Mathf.Max(3000,distance+half*3);camera.transform.position=target+new Vector3(0,distance,-distance*.35f);camera.transform.LookAt(target);
        }
    }
}
