using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HarborCity
{
    public sealed partial class HarborCityGame
    {
        Mesh zoningMesh;
        GameObject zoningOverlay;
        CityModel drawnZoneCity;
        CityRoads drawnZoneRoads;
        int drawnZoneRoadRevision=-1,drawnZoneRevision=-1,drawnZoneBuildings=-1;
        int drawnZoneDevelopment=-1;
        int zoneTool,zoneBrush;
        bool eraseZones,zoneDragging;
        Vector2 zoneAnchor;
        ZoneCell zoneHover;
        LineRenderer zoneOutline;
        static bool ZoningTool(LandUse use)=>CityZoningState.ZoneUse(use);

        bool HandleZoningInput(Mouse mouse,Ray ray,bool blocked)
        {
            bool active=ZoningTool(selected);
            if(!active || blocked || !landscape.Raycast(ray,out var hit))
            {zoneHover=null;if(!mouse.leftButton.isPressed || !active || mouse.rightButton.isPressed) zoneDragging=false;RefreshZoningOverlay();return active;}
            var cells=city.ZoningCells(landscape.Height);zoneHover=city.PickZone(hit.x,hit.z,landscape.Height);
            var use=eraseZones?LandUse.Empty:selected;
            if(zoneTool==1)
            {
                if(mouse.leftButton.wasPressedThisFrame) {zoneAnchor=new Vector2(hit.x,hit.z);zoneDragging=true;}
                if(mouse.leftButton.wasReleasedThisFrame && zoneDragging)
                {
                    float x0=Mathf.Min(zoneAnchor.x,hit.x),x1=Mathf.Max(zoneAnchor.x,hit.x),z0=Mathf.Min(zoneAnchor.y,hit.z),z1=Mathf.Max(zoneAnchor.y,hit.z);
                    var chosen=cells.Where(c=>c.pose.x>=x0 && c.pose.x<=x1 && c.pose.z>=z0 && c.pose.z<=z1).ToList();
                    if(chosen.Count==0 && zoneHover!=null) chosen.Add(zoneHover);
                    ReportZonePaint(city.PaintZones(chosen,use));zoneDragging=false;
                }
            }
            else if(zoneHover!=null && mouse.leftButton.isPressed)
            {
                IEnumerable<ZoneCell> chosen;
                if(zoneTool==2) chosen=cells.Where(c=>c.block==zoneHover.block && c.side==zoneHover.side && c.use==zoneHover.use).ToList();
                else
                {
                    float radius=zoneBrush==0?.05f:CityZoningState.CellSize*1.8f;
                    chosen=zoneBrush==0?(IEnumerable<ZoneCell>)new[]{zoneHover}:cells.Where(c=>Vector2.Distance(new Vector2(c.pose.x,c.pose.z),new Vector2(hit.x,hit.z))<=radius).ToList();
                }
                ReportZonePaint(city.PaintZones(chosen,use));
            }
            RefreshZoningOverlay();DrawZoneOutline(hit);return true;
        }
        void ReportZonePaint(int count)
        {
            if(count>0) notice=(eraseZones?"取消分区":"划定"+names[(int)selected]+"用途")+"："+count+" 格。";
        }
        void RefreshZoningOverlay()
        {
            bool visible=ZoningTool(selected) || selected==LandUse.Road;
            if(zoneOutline!=null) zoneOutline.positionCount=0;
            if(zoningOverlay!=null) zoningOverlay.SetActive(visible);
            if(!visible) return;
            if(zoningOverlay==null)
            {
                zoningOverlay=new GameObject("Roadside zoning");zoningOverlay.transform.SetParent(transform,false);
                zoningMesh=new Mesh{name="Roadside planning cells"};zoningMesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;zoningMesh.MarkDynamic();
                zoningOverlay.AddComponent<MeshFilter>().sharedMesh=zoningMesh;
                var renderer=zoningOverlay.AddComponent<MeshRenderer>();
                var shader=Resources.Load<Shader>("Zoning");renderer.sharedMaterial=new Material(shader!=null?shader:Shader.Find("Universal Render Pipeline/Unlit"));
                renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
            }
            if(drawnZoneCity==city && drawnZoneRoads==city.roads && drawnZoneRoadRevision==city.roads.revision
                && drawnZoneRevision==city.zoning.revision && drawnZoneBuildings==city.buildingRevision && drawnZoneDevelopment==city.development.revision) return;
            var vertices=new List<Vector3>();var indices=new List<int>();var colors=new List<Color>();
            foreach(var cell in city.ZoningCells(landscape.Height))
            {
                if(!cell.available) continue;
                float half=CityZoningState.CellSize/2-.055f;int start=vertices.Count;
                foreach(var corner in new[]{cell.pose.Point(-half,-half),cell.pose.Point(-half,half),cell.pose.Point(half,half),cell.pose.Point(half,-half)})
                {
                    vertices.Add(transform.InverseTransformPoint(new Vector3(corner.x,landscape.Height(corner.x,corner.z)+.12f,corner.z)));
                    colors.Add(cell.use==LandUse.Empty?new Color(.75f,.86f,.9f,.24f):new Color(palette[(int)cell.use].r,palette[(int)cell.use].g,palette[(int)cell.use].b,.68f));
                }
                indices.AddRange(new[]{start,start+1,start+2,start,start+2,start+3});
            }
            zoningMesh.Clear();zoningMesh.SetVertices(vertices);zoningMesh.SetColors(colors);zoningMesh.SetTriangles(indices,0);zoningMesh.RecalculateBounds();
            drawnZoneCity=city;drawnZoneRoads=city.roads;drawnZoneRoadRevision=city.roads.revision;drawnZoneRevision=city.zoning.revision;drawnZoneBuildings=city.buildingRevision;
            drawnZoneDevelopment=city.development.revision;
        }
        void ZoningToolPanel()
        {
            GUI.Label(new Rect(40,177,278,28),names[(int)selected]+" · 沿路分区",label);
            int next=GUI.Toolbar(new Rect(40,211,278,28),zoneTool,new[]{"画笔","框选","填充"},button);
            if(next!=zoneTool) zoneDragging=false;zoneTool=next;
            eraseZones=GUI.Toggle(new Rect(40,245,135,25),eraseZones,"取消分区",button);
            if(zoneTool==0) zoneBrush=GUI.Toolbar(new Rect(182,245,136,25),zoneBrush,new[]{"小","大"},button);
            GUI.Label(new Rect(40,280,278,85),"左键拖动规划土地；分区不收建筑费。\n接通道路并提供水电后逐步生长。\n"+(zoneHover==null?"把鼠标移到沿路格子上。":zoneHover.occupiedBuilding>=0?"已有建筑 #"+zoneHover.occupiedBuilding+"；改变用途不会立即拆楼。":"当前用途："+names[(int)zoneHover.use])+ (zoneDragging?"\n松开左键完成框选。":""),small);
        }
        void DrawZoneOutline(Vector3 hit)
        {
            if(zoneHover==null && !zoneDragging) return;
            if(zoneOutline==null)
            {
                zoneOutline=new GameObject("Zoning selection").AddComponent<LineRenderer>();zoneOutline.transform.SetParent(transform,false);
                zoneOutline.sharedMaterial=Mat(new Color(1f,.94f,.6f));zoneOutline.useWorldSpace=true;zoneOutline.startWidth=zoneOutline.endWidth=.07f;
                zoneOutline.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;zoneOutline.receiveShadows=false;
            }
            var points=new List<RoadNode>();
            if(zoneDragging)
            {
                points.Add(new RoadNode{x=zoneAnchor.x,z=zoneAnchor.y});points.Add(new RoadNode{x=zoneAnchor.x,z=hit.z});
                points.Add(new RoadNode{x=hit.x,z=hit.z});points.Add(new RoadNode{x=hit.x,z=zoneAnchor.y});
            }
            else
            {
                float h=CityZoningState.CellSize/2;points.Add(zoneHover.pose.Point(-h,-h));points.Add(zoneHover.pose.Point(-h,h));
                points.Add(zoneHover.pose.Point(h,h));points.Add(zoneHover.pose.Point(h,-h));
            }
            var vertices=new List<Vector3>();
            for(int k=0;k<4;k++) for(int n=0;n<8;n++)
            {var p=CityRoads.Lerp(points[k],points[(k+1)%4],n/8f);vertices.Add(new Vector3(p.x,landscape.Height(p.x,p.z)+.2f,p.z));}
            vertices.Add(vertices[0]);zoneOutline.positionCount=vertices.Count;zoneOutline.SetPositions(vertices.ToArray());
        }
    }
}
