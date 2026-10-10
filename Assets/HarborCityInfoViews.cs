using System.Collections.Generic;
using UnityEngine;

namespace HarborCity
{
    public sealed partial class HarborCityGame
    {
        bool showInfoViews;
        CityMapView mapView;
        GameObject infoOverlay;
        Mesh infoMesh;
        Material infoMaterial;
        float nextInfoRefresh;
        static readonly string[] mapCaptions={"供电","供水排污","垃圾","噪声","污染","岗位占用","治安","土地价值"};
        void ToggleInfoViews()
        {showInfoViews=!showInfoViews;if(showInfoViews)showEconomy=false;nextInfoRefresh=0;}
        void DrawInfoViews(Color navy)
        {
            if(!showInfoViews)return;
            Panel(new Rect(360,120,330,250),navy);GUI.Label(new Rect(376,134,230,30),"城市信息视图",title);
            if(GUI.Button(new Rect(635,135,38,28),"×",button))showInfoViews=false;
            int next=GUI.SelectionGrid(new Rect(376,174,298,116),(int)mapView,mapCaptions,3,button);
            if(next!=(int)mapView){mapView=(CityMapView)next;nextInfoRefresh=0;}
            bool nuisance=mapView==CityMapView.Garbage || mapView==CityMapView.Noise || mapView==CityMapView.Pollution || mapView==CityMapView.Crime;
            GUI.Label(new Rect(376,302,298,48),nuisance?"绿色：低  →  红色：高\n垃圾显示积压 / 填埋占用；环境来自活动记录；治安为建筑犯罪积压。":mapView==CityMapView.Employment?"浅色：没有岗位  ·  红色 → 绿色：占用比例\n显示真实岗位占用，空缺本身不代表异常。":mapView==CityMapView.LandValue?"红色 → 绿色：低 → 高土地价值\n由可达服务、环境、治安和废墟推导。":"绿色：已供应  ·  红色：尚未供应\n供水视图同时要求排污；总容量不代表接通。",small);
        }
        void RefreshInfoViews()
        {
            if(infoOverlay!=null)infoOverlay.SetActive(showInfoViews);
            if(!showInfoViews || Time.unscaledTime<nextInfoRefresh)return;
            nextInfoRefresh=Time.unscaledTime+.35f;
            if(infoOverlay==null)
            {
                infoOverlay=new GameObject("City information overlay");infoOverlay.transform.SetParent(world,false);
                infoMesh=new Mesh{name="Building indicators"};infoMesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;
                if(infoMaterial==null)infoMaterial=new Material(Resources.Load<Shader>("Zoning"));infoOverlay.AddComponent<MeshFilter>().sharedMesh=infoMesh;infoOverlay.AddComponent<CityGeneratedMesh>();
                var renderer=infoOverlay.AddComponent<MeshRenderer>();renderer.sharedMaterial=infoMaterial;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            var vertices=new List<Vector3>();var triangles=new List<int>();var colors=new List<Color>();
            bool nuisance=mapView==CityMapView.Garbage || mapView==CityMapView.Noise || mapView==CityMapView.Pollution || mapView==CityMapView.Crime;
            foreach(var b in city.buildings)
            {
                float value=city.MapIndicator(b.id,mapView);var color=nuisance?Color.Lerp(new Color(.18f,.75f,.38f),new Color(.95f,.2f,.12f),value):Color.Lerp(new Color(.95f,.2f,.12f),new Color(.18f,.75f,.38f),value);
                if(mapView==CityMapView.Employment && city.JobCapacity(b.id)==0)color=new Color(.6f,.65f,.67f);
                color.a=.72f;int start=vertices.Count;
                foreach(var corner in new[]{new Vector2(-1,-1),new Vector2(-1,1),new Vector2(1,-1),new Vector2(1,1)})
                {var p=b.Point(corner.x*(b.width/2+.12f),corner.y*(b.depth/2+.12f));vertices.Add(new Vector3(p.x,landscape.Height(p.x,p.z)+.19f,p.z));colors.Add(color);}
                triangles.AddRange(new[]{start,start+1,start+2,start+2,start+1,start+3});
            }
            infoMesh.Clear();infoMesh.SetVertices(vertices);infoMesh.SetTriangles(triangles,0);infoMesh.SetColors(colors);infoMesh.RecalculateBounds();
        }
    }
}
