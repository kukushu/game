using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
namespace HarborCity
{
    // Exercises the actual IMGUI renderer with an isolated, paused test city.
    // No objects are added to the user's running city or saved scene.
    public sealed class CityDashboardVisualPreview : EditorWindow
    {
        GameObject holder;
        HarborCityGame renderer;
        CityAnalysisState state;
        Vector2 scroll;
        int page;
        Font previewFont;
        CityModel fixture;
        string baseline;
        double nextCheck;
        const BindingFlags Fields=BindingFlags.Instance|BindingFlags.NonPublic;
        void Set(string name,object value)=>typeof(HarborCityGame).GetField(name,Fields).SetValue(renderer,value);
        object Call(string name,params object[] arguments)=>typeof(HarborCityGame).GetMethod(name,Fields).Invoke(renderer,arguments);
        [MenuItem("Harbor/Preview Dashboard visuals (isolated test city)")]
        public static void Open() {var w=GetWindow<CityDashboardVisualPreview>();w.titleContent=new GUIContent("Dashboard visual check");w.minSize=new Vector2(750,700);w.Show();}
        void Prepare()
        {
            if(renderer!=null)return;
            holder=new GameObject("Dashboard isolated renderer") {hideFlags=HideFlags.HideAndDontSave};holder.SetActive(false);
            renderer=holder.AddComponent<HarborCityGame>();
            Set("dashboardPreviewOnly",true);
            var c=CityBuildingChecks.Fixture();new CityTraffic(c);c.analysis=new CityDailyAnalysis(c);
            new CityTraffic(c).Advance(660);state=c.analysis.Refresh(true);Set("city",c);
            fixture=c;baseline=JsonUtility.ToJson(c.ToSaveData());
            previewFont=Font.CreateDynamicFontFromOSFont(new[]{"Microsoft YaHei","SimHei","Arial"},18);Set("font",previewFont);
        }
        void OnGUI()
        {
            Prepare();Call("Styles");
            EditorGUILayout.HelpBox("独立测试城市 · 实际 Dashboard 绘制代码 · 不驱动或保存主游戏",MessageType.Info);
            int chosen=GUILayout.Toolbar(page,new[]{"Dashboard","工厂","商业","History","家庭","居民","时间线"});
            if(chosen!=page) {page=chosen;scroll=Vector2.zero;Set("analysisEntityId",-1);}
            GUI.color=new Color(.055f,.105f,.14f);GUI.DrawTexture(new Rect(0,65,position.width,position.height-65),Texture2D.whiteTexture);GUI.color=Color.white;
            scroll=GUILayout.BeginScrollView(scroll);
            GUILayout.BeginVertical();
            int selected=(int)typeof(HarborCityGame).GetField("analysisEntityId",Fields).GetValue(renderer);
            if(selected>=0)Call("DrawAnalysisDetail",state);
            else if(page==0)Call("DrawDashboard",state);
            else if(page==3)Call("DrawAnalysisHistory",state);
            else if(page==6)Call("DrawAnalysisTimeline",state);
            else
            {
                AnalysisEntityKind kind=page==1?AnalysisEntityKind.Factory:page==2?AnalysisEntityKind.Commercial:page==4?AnalysisEntityKind.Household:AnalysisEntityKind.Resident;
                int id=page==1 || page==2?state.businesses.FirstOrDefault(b=>b.industrial==(page==1))?.id??-1:page==4?state.households.FirstOrDefault()?.id??-1:state.residents.FirstOrDefault()?.id??-1;
                if(id<0)GUILayout.Label("该测试城市暂无此类实体。");
                else {Set("analysisEntityId",id);Set("analysisEntityKind",kind);Call("DrawAnalysisDetail",state);}
            }
            GUILayout.EndVertical();GUILayout.EndScrollView();
            if(Event.current.type==EventType.Repaint && EditorApplication.timeSinceStartup>=nextCheck)
            {
                nextCheck=EditorApplication.timeSinceStartup+1;
                CityBuildingChecks.Check(JsonUtility.ToJson(fixture.ToSaveData())==baseline,"Dashboard renderer modified isolated simulation state");
            }
        }
        void OnDisable()
        {
            if(renderer!=null)Call("ReleaseDashboardGraphics");
            if(holder!=null)DestroyImmediate(holder);
            if(previewFont!=null)DestroyImmediate(previewFont);
        }
    }
}
