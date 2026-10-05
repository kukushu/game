using UnityEngine;
using UnityEngine.InputSystem;

namespace HarborCity
{
    public sealed partial class HarborCityGame
    {
        CityBuilding previewLot;
        bool previewValid;
        string placementReason="";
        HousingKind housingChoice=HousingKind.Apartment;
        Mesh placementMesh;
        readonly Vector3[] placementVertices=new Vector3[49];
        LineRenderer placementFront;

        void BuildingHeight(CityBuilding lot,out float low,out float high)
        {
            low=float.MaxValue; high=float.MinValue;
            for(int z=0;z<=6;z++) for(int x=0;x<=6;x++)
            {
                var p=lot.Point((x/6f-.5f)*lot.width,(z/6f-.5f)*lot.depth);
                float h=landscape.Height(p.x,p.z); low=Mathf.Min(low,h); high=Mathf.Max(high,h);
            }
        }
        void ResetBuildingPreview()
        {
            previewLot=null; previewValid=false; placementReason="";
            if(cursor!=null) cursor.gameObject.SetActive(false);
        }
        void DrawBuildingPreview(CityBuilding lot,Color color)
        {
            // Reuse one terrain-conforming mesh while the mouse moves, rather than
            // allocating a new GameObject/mesh or drawing every possible frontage.
            if(placementMesh==null)
            {
                if(cursor!=null) {cursor.gameObject.SetActive(false); Destroy(cursor.gameObject);}
                cursor=landscape.Surface("Placement",transform,new Vector3(lot.x,0,lot.z),lot.width,lot.depth,.18f,Mat(color),lot.yaw).transform;
                placementMesh=cursor.GetComponent<MeshFilter>().sharedMesh; placementMesh.MarkDynamic();
                placementFront=new GameObject("Road-facing frontage").AddComponent<LineRenderer>();
                placementFront.transform.SetParent(cursor,false); placementFront.useWorldSpace=true;
                placementFront.startWidth=placementFront.endWidth=.1f; placementFront.positionCount=2;
                placementFront.sharedMaterial=Mat(new Color(.95f,.95f,.85f));
                placementFront.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                placementFront.receiveShadows=false;
            }
            for(int z=0;z<=6;z++) for(int x=0;x<=6;x++)
            {
                var p=lot.Point((x/6f-.5f)*lot.width,(z/6f-.5f)*lot.depth);
                placementVertices[z*7+x]=transform.InverseTransformPoint(new Vector3(p.x,landscape.Height(p.x,p.z)+.18f,p.z));
            }
            placementMesh.vertices=placementVertices; placementMesh.RecalculateNormals(); placementMesh.RecalculateBounds();
            cursor.GetComponent<MeshRenderer>().sharedMaterial=Mat(color);
            for(int i=0;i<2;i++)
            {
                var p=lot.Point((i==0?-.35f:.35f)*lot.width,-lot.depth/2);
                placementFront.SetPosition(i,new Vector3(p.x,landscape.Height(p.x,p.z)+.22f,p.z));
            }
            cursor.gameObject.SetActive(true);
        }
        void HandleBuildingInput(Mouse mouse,Ray ray,bool blocked)
        {
            bool build=selected>=LandUse.Residential && selected<=LandUse.Park;
            ResetBuildingPreview();
            if(blocked || (!build && selected!=LandUse.Bulldoze) || !landscape.Raycast(ray,out var hit)) return;
            int existing=-1;
            if(selected==LandUse.Bulldoze)
            {
                existing=city.PickBuilding(hit.x,hit.z);
                if(existing<0) return;
                previewLot=city.GetBuilding(existing); previewValid=true;
            }
            else
            {
                // Do not filter by existing buildings: overlaps must stay visible
                // as a red preview with the authoritative CanBuild rejection.
                previewLot=city.RoadsidePreview(hit.x,hit.z,selected,housingChoice,out placementReason);
                if(previewLot==null) return;
                previewValid=city.CanBuild(previewLot,landscape.Height,out placementReason);
            }
            DrawBuildingPreview(previewLot,selected==LandUse.Bulldoze?palette[8]:previewValid?new Color(.2f,.85f,.35f):new Color(.95f,.25f,.2f));
            // One click commits this exact temporary pose; holding the button does
            // not repeatedly build/reject buildings as the continuous preview moves.
            if(!mouse.leftButton.wasPressedThisFrame) return;
            if(selected==LandUse.Bulldoze)
            {
                if(city.DemolishBuilding(existing)) {DrawLot(existing); notice="已拆除建筑，原有车辆会重新处理目的地。"; ResetBuildingPreview();}
                else notice="资金不足，无法拆除。";
            }
            else
            {
                int id=city.PlaceBuilding(previewLot,selected,landscape.Height,out string error);
                if(id>=0)
                {
                    DrawLot(id); notice="已建设沿路"+names[(int)selected]+" #"+id+"。";
                    // The next click would overlap this building; update immediately.
                    previewValid=city.CanBuild(previewLot,landscape.Height,out placementReason);
                    DrawBuildingPreview(previewLot,new Color(.95f,.25f,.2f));
                }
                else {notice=error; city.Trace("building.rejected",error,new CityLogDetail {x=previewLot.x,z=previewLot.z,reason=selected.ToString()},level:"warning");}
            }
        }
    }
}
