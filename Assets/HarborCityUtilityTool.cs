using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HarborCity
{
    public sealed partial class HarborCityGame
    {
        int powerTool,waterTool;
        bool eraseUtility;
        RoadNode utilityStart;
        LandUse lastUtilitySelection;
        int lastUtilityTool=-1;
        GameObject utilityView;
        LineRenderer utilityPreview;
        CityModel utilityViewCity;
        CityUtilityNetwork drawnPower,drawnWater;
        int powerRevision=-1,waterRevision=-1;
        readonly List<GameObject> utilityObjects=new List<GameObject>();
        Material utilityCoverageMaterial;
        LandUse SelectedFacility=>selected==LandUse.Water && waterTool==2?LandUse.Sewage:selected==LandUse.Park && serviceTool==1?LandUse.Landfill:selected==LandUse.Park && serviceTool==2?LandUse.Clinic:selected==LandUse.Park && serviceTool==3?LandUse.ElementarySchool:selected==LandUse.Park && serviceTool==4?LandUse.FireHouse:selected==LandUse.Park && serviceTool==5?LandUse.PoliceStation:selected==LandUse.Park && serviceTool==6?LandUse.HighSchool:selected==LandUse.Park && serviceTool==7?LandUse.University:selected==LandUse.Park && serviceTool==8?LandUse.Cemetery:selected;
        bool UtilityToolActive=>selected==LandUse.Power && powerTool==1 || selected==LandUse.Water && waterTool==1;
        UtilityKind SelectedUtility=>selected==LandUse.Power?UtilityKind.Electricity:UtilityKind.Water;
        void UtilityToolPanel()
        {
            GUI.Label(new Rect(40,174,278,23),selected==LandUse.Power?"电力建设":"供水与排污",label);
            if(selected==LandUse.Power)
            {if(GUI.Button(new Rect(40,205,133,30),powerTool==0?"● 电站":"电站",button))powerTool=0;
                if(GUI.Button(new Rect(181,205,137,30),powerTool==1?"● 输电线":"输电线",button))powerTool=1;}
            else
            {for(int i=0;i<3;i++)if(GUI.Button(new Rect(40+i*94,205,90,30),(waterTool==i?"● ":"")+new[]{"水塔","水管","排水口"}[i],button))waterTool=i;}
            if(UtilityToolActive)
            {
                eraseUtility=GUI.Toggle(new Rect(40,246,278,26),eraseUtility,"拆除当前管线 · ¥40",button);
                GUI.Label(new Rect(40,283,278,80),eraseUtility?"点击管线拆除。建筑与道路不会被此工具拆掉。":
                    "点击起点，再点击终点；交叉点自动连接。右键取消起点。\n"+(selected==LandUse.Power?"输电线需接近已供电建筑，连接远处电网。":"水塔与排水口要接入同一管网；住宅须在管线覆盖内。"),small);
            }
            else GUI.Label(new Rect(40,246,278,110),SelectedFacility==LandUse.Sewage?"沿水岸附近道路建设排水口，接电后连接水管。\n¥2500 · 维护 ¥65 / 天\n"+placementReason:
                "沿道路点击建设设施。设施与管线分别建设。\n"+placementReason,small);
        }
        bool HandleUtilityInput(Mouse mouse,Ray ray,bool blocked)
        {
            RefreshUtilityView();int tool=selected==LandUse.Power?powerTool:waterTool;
            if(lastUtilitySelection!=selected || lastUtilityTool!=tool)utilityStart=null;
            lastUtilitySelection=selected;lastUtilityTool=tool;
            if(utilityPreview!=null)utilityPreview.positionCount=0;
            if(!UtilityToolActive){utilityStart=null;return false;}
            if(mouse.rightButton.wasPressedThisFrame)utilityStart=null;
            if(blocked || !landscape.Raycast(ray,out var hit))return true;
            var net=city.UtilityNetwork(SelectedUtility);var point=net.Snap(hit.x,hit.z);
            if(eraseUtility)
            {utilityStart=null;var edge=net.Pick(hit.x,hit.z);if(edge!=null && mouse.leftButton.wasPressedThisFrame)
                notice=city.RemoveUtility(SelectedUtility,edge.id)?"已拆除管线。":"无法拆除管线。";return true;}
            if(utilityStart!=null)
            {
                var plan=net.Plan(utilityStart,point,out string error);
                if(utilityPreview==null)utilityPreview=NewUtilityLine("Utility preview",transform,.16f);
                utilityPreview.sharedMaterial=Mat(plan==null?new Color(.9f,.2f,.2f):new Color(.25f,.95f,.7f));utilityPreview.positionCount=2;
                utilityPreview.SetPosition(0,UtilityPosition(utilityStart,SelectedUtility));utilityPreview.SetPosition(1,UtilityPosition(point,SelectedUtility));
                if(mouse.leftButton.wasPressedThisFrame)
                {if(city.BuildUtility(SelectedUtility,utilityStart,point,out error)){notice="管线已连接。";utilityStart=point;}else notice=error;}
            }
            else if(mouse.leftButton.wasPressedThisFrame)utilityStart=point;
            return true;
        }
        Vector3 UtilityPosition(RoadNode node,UtilityKind kind)=>new Vector3(node.x,landscape.Height(node.x,node.z)+(kind==UtilityKind.Electricity?4:.22f),node.z);
        LineRenderer NewUtilityLine(string name,Transform parent,float width)
        {var line=new GameObject(name).AddComponent<LineRenderer>();line.transform.SetParent(parent,false);line.useWorldSpace=true;
            line.startWidth=line.endWidth=width;line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;return line;}
        void RefreshUtilityView()
        {
            if(utilityView==null){utilityView=new GameObject("Utility networks");utilityView.transform.SetParent(world,false);}
            bool pipes=selected==LandUse.Water;
            if(utilityViewCity==city && drawnPower==city.utilities.electricity && drawnWater==city.utilities.water
                && powerRevision==drawnPower.revision && waterRevision==drawnWater.revision)
            {foreach(var obj in utilityObjects)if(obj.name.StartsWith("Water"))obj.SetActive(pipes);return;}
            foreach(var obj in utilityObjects){obj.SetActive(false);Destroy(obj);}utilityObjects.Clear();
            utilityViewCity=city;drawnPower=city.utilities.electricity;drawnWater=city.utilities.water;powerRevision=drawnPower.revision;waterRevision=drawnWater.revision;
            foreach(var kind in new[]{UtilityKind.Electricity,UtilityKind.Water})
            {
                var net=city.UtilityNetwork(kind);
                foreach(var edge in net.edges)
                {
                    var line=NewUtilityLine(kind+" line #"+edge.id,utilityView.transform,kind==UtilityKind.Water?.2f:.045f);
                    line.sharedMaterial=Mat(kind==UtilityKind.Water?new Color(.25f,.8f,.95f):new Color(.3f,.32f,.34f));
                    var a=net.Node(edge.a);var b=net.Node(edge.b);int steps=Mathf.Max(1,Mathf.CeilToInt(CityRoads.Length(a,b)/2));line.positionCount=steps+1;
                    for(int i=0;i<=steps;i++)line.SetPosition(i,UtilityPosition(CityRoads.Lerp(a,b,i/(float)steps),kind));
                    line.gameObject.SetActive(kind!=UtilityKind.Water || pipes);utilityObjects.Add(line.gameObject);
                    if(kind==UtilityKind.Water)DrawPipeCoverage(net,a,b,pipes);
                }
                if(kind==UtilityKind.Electricity)foreach(var node in net.nodes)
                {
                    if(!net.edges.Exists(e=>e.a==node.id || e.b==node.id))continue;
                    var pos=UtilityPosition(node,kind);pos.y-=2;
                    var pole=Box("Electricity pole",pos,new Vector3(.12f,4,.12f),new Color(.58f,.53f,.4f),utilityView.transform);utilityObjects.Add(pole);
                }
            }
        }
        void DrawPipeCoverage(CityUtilityNetwork net,RoadNode a,RoadNode b,bool visible)
        {
            if(utilityCoverageMaterial==null)utilityCoverageMaterial=new Material(Resources.Load<Shader>("Zoning"));
            var obj=new GameObject("Water coverage");obj.transform.SetParent(utilityView.transform,false);
            var vertices=new List<Vector3>();var triangles=new List<int>();var colors=new List<Color>();
            float length=CityRoads.Length(a,b),nx=-(b.z-a.z)/length,nz=(b.x-a.x)/length;
            int steps=Mathf.Max(1,Mathf.CeilToInt(length/2));
            for(int i=0;i<=steps;i++)
            {
                var p=CityRoads.Lerp(a,b,i/(float)steps);
                for(int side=-1;side<=1;side+=2)
                {float x=p.x+nx*CityModel.PipeReach*side,z=p.z+nz*CityModel.PipeReach*side;
                    vertices.Add(new Vector3(x,landscape.Height(x,z)+.13f,z));colors.Add(new Color(.2f,.65f,.95f,.19f));}
                if(i>0){int j=i*2;triangles.AddRange(new[]{j-2,j-1,j,j,j-1,j+1});}
            }
            var mesh=new Mesh{name="Pipe coverage strip"};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.SetColors(colors);mesh.RecalculateBounds();
            obj.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=obj.AddComponent<MeshRenderer>();renderer.sharedMaterial=utilityCoverageMaterial;
            renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;obj.AddComponent<CityGeneratedMesh>();obj.SetActive(visible);utilityObjects.Add(obj);
        }
    }
}
