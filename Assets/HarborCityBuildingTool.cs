using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HarborCity
{
    public sealed partial class HarborCityGame
    {
        CityRoads zoningRoads;
        int zoningRevision=-1, zoningBuildings=-1;
        List<CityBuilding> zoningLots=new List<CityBuilding>();
        GameObject zoningOverlay;
        CityBuilding previewLot;
        LandUse previewUse;
        HousingKind housingChoice=HousingKind.Apartment;
        HousingKind zoningHousing;
        LandUse zoningUse;

        void BuildingHeight(CityBuilding lot,out float low,out float high)
        {
            low=float.MaxValue; high=float.MinValue;
            for(int z=0;z<=6;z++) for(int x=0;x<=6;x++)
            {
                var p=lot.Point((x/6f-.5f)*lot.width,(z/6f-.5f)*lot.depth);
                float h=landscape.Height(p.x,p.z); low=Mathf.Min(low,h); high=Mathf.Max(high,h);
            }
        }
        void RefreshZoning()
        {
            if(zoningRoads==city.roads && zoningRevision==city.roads.revision && zoningBuildings==city.buildingRevision && zoningHousing==housingChoice && zoningUse==selected) return;
            zoningHousing=housingChoice; zoningUse=selected;
            zoningRoads=city.roads; zoningRevision=city.roads.revision; zoningBuildings=city.buildingRevision;
            zoningLots.Clear();
            if(zoningOverlay!=null) { zoningOverlay.SetActive(false); Destroy(zoningOverlay); }
            zoningOverlay=new GameObject("Roadside zoning"); zoningOverlay.transform.SetParent(transform,false);
            var vertices=new List<Vector3>(); var indices=new List<int>();
            foreach(var lot in city.RoadsideLots())
            {
                if(selected==LandUse.Residential && city.society!=null)
                {
                    lot.housing=housingChoice; lot.width=housingChoice==HousingKind.Villa?5.9f:2.9f;
                    lot.depth=5.9f;
                    var centre=lot.Point(0,1.5f); lot.x=centre.x; lot.z=centre.z;
                    // Entrance remains at the original road frontage.
                    if(zoningLots.Exists(other=>lot.Overlaps(other))) continue;
                }
                if(!city.CanBuild(lot,landscape.Height,out _)) continue;
                zoningLots.Add(lot);
                for(int edge=0;edge<4;edge++)
                {
                    var a=lot.Point(edge<2 ? -lot.width/2:lot.width/2,edge==0 || edge==3 ? -lot.depth/2:lot.depth/2);
                    var b=lot.Point(edge==0 || edge==3 ? -lot.width/2:lot.width/2,edge<2 ? lot.depth/2:-lot.depth/2);
                    Vector3 start=new Vector3(a.x,0,a.z), end=new Vector3(b.x,0,b.z);
                    Vector3 normal=Vector3.Cross(Vector3.up,(end-start).normalized)*.025f;
                    int n=vertices.Count;
                    foreach(var point in new[]{start-normal,start+normal,end-normal,end+normal})
                        vertices.Add(new Vector3(point.x,landscape.Height(point.x,point.z)+.13f,point.z));
                    indices.AddRange(new[]{n,n+1,n+2,n+2,n+1,n+3});
                }
            }
            var mesh=new Mesh { name="Available roadside lots",indexFormat=UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.SetVertices(vertices); mesh.SetTriangles(indices,0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            zoningOverlay.AddComponent<MeshFilter>().sharedMesh=mesh;
            zoningOverlay.AddComponent<MeshRenderer>().sharedMaterial=Mat(new Color(.72f,.83f,.65f));
            zoningOverlay.AddComponent<CityGeneratedMesh>();
        }
        void HandleBuildingInput(Mouse mouse,Ray ray,bool blocked)
        {
            bool build=selected>=LandUse.Residential && selected<=LandUse.Park;
            if(build) RefreshZoning();
            if(zoningOverlay!=null) zoningOverlay.SetActive(build);
            cursor.gameObject.SetActive(false);
            if(blocked || (!build && selected!=LandUse.Bulldoze) || !landscape.Raycast(ray,out var hit)) return;
            int existing=city.PickBuilding(hit.x,hit.z);
            CityBuilding lot=null;
            if(selected==LandUse.Bulldoze && existing>=0) lot=city.buildings[existing];
            else if(build && existing<0) lot=zoningLots.Find(b=>b.Contains(hit.x,hit.z));
            if(lot==null) return;
            if(previewLot!=lot || previewUse!=selected)
            {
                Destroy(cursor.gameObject);
                cursor=landscape.Surface("Placement",transform,new Vector3(lot.x,0,lot.z),lot.width,lot.depth,.18f,Mat(palette[(int)selected]),lot.yaw).transform;
                previewLot=lot; previewUse=selected;
            }
            cursor.gameObject.SetActive(true);
            if(!mouse.leftButton.isPressed) return;
            if(selected==LandUse.Bulldoze)
            {
                if(city.DemolishBuilding(existing)) { DrawLot(existing); notice="已拆除建筑，原有车辆会重新处理目的地。"; }
                else notice="资金不足，无法拆除。";
            }
            else
            {
                int id=city.PlaceBuilding(lot,selected,landscape.Height,out string error);
                if(id>=0) { DrawLot(id); notice="已划分沿路"+names[(int)selected]+"地块 #"+id+"。"; }
                else {notice=error; city.Trace("building.rejected",error,new CityLogDetail {x=lot.x,z=lot.z,reason=selected.ToString()},level:"warning");}
            }
        }
    }
}
