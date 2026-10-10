using System.Linq;
using UnityEngine;

namespace HarborCity
{
    public sealed partial class HarborCityGame
    {
        int serviceTool;
        bool InspectingLandfill=>selected==LandUse.Empty && city.GetBuilding(inspectedHome) is LandfillBuilding;
        void ServiceToolPanel()
        {
            GUI.Label(new Rect(40,176,278,24),"市政设施",label);
            if(GUI.Button(new Rect(40,207,133,30),serviceTool==0?"● 公园":"公园",button))serviceTool=0;
            if(GUI.Button(new Rect(181,207,137,30),serviceTool==1?"● 填埋场":"填埋场",button))serviceTool=1;
            if(GUI.Button(new Rect(40,242,133,30),serviceTool==2?"● 诊所":"诊所",button))serviceTool=2;
            if(GUI.Button(new Rect(181,242,137,30),serviceTool==3?"● 小学":"小学",button))serviceTool=3;
            if(GUI.Button(new Rect(40,277,133,30),serviceTool==4?"● 消防站":"消防站",button))serviceTool=4;
            if(GUI.Button(new Rect(181,277,137,30),serviceTool==5?"● 警局":"警局",button))serviceTool=5;
            if(GUI.Button(new Rect(40,312,133,30),serviceTool==6?"● 高中":"高中",button))serviceTool=6;
            if(GUI.Button(new Rect(181,312,137,30),serviceTool==7?"● 大学":"大学",button))serviceTool=7;
            if(GUI.Button(new Rect(40,347,278,30),serviceTool==8?"● 墓地":"墓地",button))serviceTool=8;
            GUI.Label(new Rect(40,391,278,94),serviceTool==8?"墓地 · ¥4000 · 维护 ¥"+city.ServiceExpense(CityServiceKind.Healthcare,60)+" / 天\n"+CemeteryBuilding.Capacity+" 个位置；"+city.HearseFleetLimit+" 辆灵车。\n实际收取并送达才入库。\n"+placementReason:serviceTool>=6?(serviceTool==6?"高中 · ¥16000":"大学 · ¥24000")+" · 维护 ¥"+city.ServiceExpense(CityServiceKind.Education,serviceTool==6?100:160)+" / 天\n"+(serviceTool==6?city.ServiceCapacity(CityServiceKind.Education,24):city.ServiceCapacity(CityServiceKind.Education,36))+" 个学位；实际到校积累独立课程。\n需要完成前一阶段教育。\n"+placementReason:serviceTool==5?"警局 · ¥12000 · 维护 ¥"+city.ServiceExpense(CityServiceKind.Police,80)+" / 天\n"+city.PoliceFleetLimit+" 辆警车；到场后处理治安。\n需要道路、水电与排污。\n"+placementReason:serviceTool==4?"消防站 · ¥12000 · 维护 ¥"+city.ServiceExpense(CityServiceKind.Fire,80)+" / 天\n"+city.FireFleetLimit+" 辆消防车；实际到场后处置。\n需要道路、水电与排污。\n"+placementReason:serviceTool==3?"小学 · ¥10000 · 维护 ¥"+city.ServiceExpense(CityServiceKind.Education,60)+" / 天\n"+city.SchoolCapacity+" 个学位；儿童实际步行到校。\n接通服务并实际上课才增加学习。\n"+placementReason:serviceTool==2?"诊所 · ¥10000 · 维护 ¥"+city.ServiceExpense(CityServiceKind.Healthcare,80)+" / 天\n"+city.ClinicCapacity+" 个患者位置，"+city.ClinicFleetLimit+" 辆救护车；不足时步行。\n实际到院且接通服务才治疗。\n"+placementReason:serviceTool==1?"填埋场 · ¥4000\n垃圾车沿路收集，返回后才入库。\n接通水电、排污和道路后运行。\n"+placementReason:"公园 · ¥500\n沿道路点击建设。\n"+placementReason,small);
        }
        void DrawLandfillInspector(float h,Color navy)
        {
            var b=city.GetBuilding(inspectedHome) as LandfillBuilding;if(b==null)return;
            Panel(new Rect(24,120,310,h-320),navy);GUILayout.BeginArea(new Rect(38,132,282,h-345));
            GUILayout.BeginHorizontal();GUILayout.Label("填埋场 #"+b.id,title);
            if(GUILayout.Button("关闭",button,GUILayout.Width(60)))inspectedHome=-1;GUILayout.EndHorizontal();
            GUILayout.Label(city.BuildingAccess(b.id)?city.Supply(b.id).Problem:"道路未接通",small);GUILayout.Space(10);
            GUILayout.Label("已存垃圾 "+b.stored+" / "+LandfillBuilding.Capacity,label);
            int trucks=city.traffic.trips.Count(t=>t.cargoKind==CargoKind.Waste && t.home==b.id);
            GUILayout.Label("出勤车辆 "+trucks+" / "+city.GarbageFleetLimit,label);
            GUILayout.Label("在途预留 "+city.LandfillIncoming(b.id),small);
            if(GUILayout.Button(b.emptying?"停止清空 / 恢复收集":"开始清空 / 向其他填埋场转运",button))city.SetLandfillEmptying(b.id,!b.emptying);
            GUILayout.Space(10);GUILayout.Label(b.emptying?"清空期间停止收集；需要另一座可接收的填埋场。":"有库存或关联车辆时不能拆除。",small);
            GUILayout.Space(14);GUILayout.Label("全城垃圾：\n建筑内 "+city.GarbageInBuildings+"\n运输中 "+city.GarbageInTransit+"\n填埋场内 "+city.LandfillWaste,small);
            GUILayout.EndArea();
        }
        void RefreshWasteView()
        {
            foreach(var b in city.buildings.OfType<LandfillBuilding>())
            {
                if(!visuals.TryGetValue(b.id,out var view) || view==null)continue;
                foreach(var t in view.GetComponentsInChildren<Transform>())if(t.name=="Stored refuse")
                {float height=.12f+2*b.stored/LandfillBuilding.Capacity;t.localScale=new Vector3(2.5f,height,2.5f);t.localPosition=new Vector3(.8f,height/2,.7f);}
            }
        }
    }
}
