using System.Collections.Generic;
using UnityEngine;

namespace HarborCity
{
    public sealed partial class HarborCityGame
    {
        int commuteFamily=-1, shownHome=-1, shownWork=-1, shownRevision=-1;
        CityRoads shownRoads;
        LineRenderer commuteLine, homeMarker, workMarker;
        int locatedTrafficEndpoint=-2, locatedTrafficTrip=-1;
        bool locatedTrafficOrigin;
        LineRenderer trafficEndpointMarker;
        bool TrafficEndpointVisible() => locatedTrafficEndpoint>=-1 && locatedTrafficTrip>=0
            && locatedTrafficTrip==inspectedTrip && selected==LandUse.Empty && inspectedHome<0
            && (locatedTrafficEndpoint==-1?city.roads?.Node(CityRoads.Entrance)!=null:locatedTrafficEndpoint<city.buildings.Count);
        Vector3 TrafficEndpointAnchor()
        {
            if(locatedTrafficEndpoint>=0) return BuildingMarker(locatedTrafficEndpoint);
            var node=city.roads.Node(CityRoads.Entrance);
            return new Vector3(node.x,Mathf.Max(node.y,landscape.Height(node.x,node.z))+.6f,node.z);
        }
        void UpdateTrafficEndpointMarker()
        {
            if(!TrafficEndpointVisible())
            {if(trafficEndpointMarker!=null) trafficEndpointMarker.positionCount=0; return;}
            Color color=locatedTrafficOrigin?new Color(.25f,1,.5f):new Color(1,.65f,.1f);
            if(trafficEndpointMarker==null) trafficEndpointMarker=CommuteRenderer("Located traffic endpoint",color,.3f);
            trafficEndpointMarker.sharedMaterial=Mat(color);
            if(locatedTrafficEndpoint>=0) MarkBuilding(trafficEndpointMarker,locatedTrafficEndpoint);
            else
            {
                Vector3 p=TrafficEndpointAnchor();
                trafficEndpointMarker.positionCount=5;
                trafficEndpointMarker.SetPositions(new[]{p+new Vector3(-2,0,-2),p+new Vector3(-2,0,2),p+new Vector3(2,0,2),p+new Vector3(2,0,-2),p+new Vector3(-2,0,-2)});
            }
        }
        void DrawTrafficEndpointLabel(float scale,Color navy)
        {
            if(!TrafficEndpointVisible() || showSimulation || help) return;
            Vector3 p=cam.WorldToScreenPoint(TrafficEndpointAnchor()+Vector3.up*.5f);
            if(p.z<=0 || p.x<0 || p.x>Screen.width || p.y<0 || p.y>Screen.height) return;
            Rect r=new Rect(p.x/scale-125,(Screen.height-p.y)/scale-42,250,36);
            string name=locatedTrafficEndpoint>=0 && city.tiles[locatedTrafficEndpoint]==0?"已拆建筑原址 #"+locatedTrafficEndpoint:EndpointName(locatedTrafficEndpoint);
            Panel(r,navy);
            Panel(new Rect(r.x,r.y,4,r.height),locatedTrafficOrigin?new Color(.25f,1,.5f):new Color(1,.65f,.1f));
            GUI.Label(new Rect(r.x+10,r.y+5,r.width-15,r.height-8),(locatedTrafficOrigin?"出发地：":"目的地：")+name,small);
        }
        Household VisibleCommuter() => city.society?.families.Find(f=>f.id==commuteFamily && f.resident && f.home==inspectedHome);
        CityResident FamilyCommuter(Household f) => f?.people.Find(p=>p.id==inspectedResident && city.Workplace(p)>=0) ?? f?.people.Find(p=>city.Workplace(p)>=0);
        int FamilyWorkplace(Household f) => city.Workplace(FamilyCommuter(f));
        bool HasWorkplace(Household f) => FamilyWorkplace(f)>=0;
        string WorkplaceName(CityResident p) => city.Workplace(p)<0?"暂无工作地点":names[city.tiles[city.Workplace(p)]]+" #"+city.Workplace(p);
        string WorkplaceName(Household f) => WorkplaceName(FamilyCommuter(f));
        void ShowWorkplace(Household family,bool locate)
        {
            commuteFamily=family.id; inspectedTrip=-1; showSimulation=false;
            if(!HasWorkplace(family)) {notice="这户目前没有有效的工作地点。"; return;}
            if(locate)
            {
                var b=city.buildings[FamilyWorkplace(family)]; focus=new Vector3(b.x,0,b.z); zoom=24; UpdateCamera();
            }
            notice="家庭 #"+family.id+"：住宅 #"+family.home+" → "+WorkplaceName(family)+"。绿色为住宅，橙色为工作地点；显示预计通勤路线。";
            UpdateCommuteView();
        }
        LineRenderer CommuteRenderer(string name,Color color,float width)
        {
            var existing=transform.Find(name);
            var line=existing!=null?existing.GetComponent<LineRenderer>():new GameObject(name).AddComponent<LineRenderer>();
            line.transform.SetParent(transform,false); line.useWorldSpace=true; line.sharedMaterial=Mat(color);
            line.startWidth=line.endWidth=width; line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows=false; return line;
        }
        Vector3 BuildingMarker(int id)
        {
            var b=city.buildings[id]; float y=landscape.Height(b.x,b.z)+1;
            if(visuals.TryGetValue(id,out var visual) && visual!=null)
                foreach(var renderer in visual.GetComponentsInChildren<Renderer>()) y=Mathf.Max(y,renderer.bounds.max.y+.4f);
            return new Vector3(b.x,y,b.z);
        }
        void MarkBuilding(LineRenderer line,int id)
        {
            var b=city.buildings[id]; Vector3 center=BuildingMarker(id);
            var points=new Vector3[5];
            for(int k=0;k<5;k++)
            {
                var p=b.Point(k%4<2?-b.width/2:b.width/2,k%4==0 || k%4==3?-b.depth/2:b.depth/2);
                points[k]=new Vector3(p.x,center.y,p.z);
            }
            line.positionCount=5; line.SetPositions(points);
        }
        void UpdateCommuteView()
        {
            var family=VisibleCommuter();
            if(selected!=LandUse.Empty || !HasWorkplace(family) || city.HousingCapacity(inspectedHome)==0)
            {
                if(commuteLine!=null) commuteLine.positionCount=0;
                if(homeMarker!=null) homeMarker.positionCount=0;
                if(workMarker!=null) workMarker.positionCount=0;
                shownHome=shownWork=-1; return;
            }
            if(commuteLine==null) commuteLine=CommuteRenderer("Household commute route",new Color(1,.72f,.15f),.23f);
            if(homeMarker==null) homeMarker=CommuteRenderer("Household home marker",new Color(.3f,1,.55f),.18f);
            if(workMarker==null) workMarker=CommuteRenderer("Household work marker",new Color(1,.55f,.1f),.23f);
            MarkBuilding(homeMarker,family.home); MarkBuilding(workMarker,FamilyWorkplace(family));
            if(shownHome==family.home && shownWork==FamilyWorkplace(family) && shownRoads==city.roads && shownRevision==city.roads.revision) return;
            shownHome=family.home; shownWork=FamilyWorkplace(family); shownRoads=city.roads; shownRevision=city.roads.revision;
            var path=city.roads.FindPath(city.AccessBuilding(family.home),city.AccessBuilding(FamilyWorkplace(family)));
            var points=new List<Vector3>();
            if(path.Count>0)
            {
                var home=city.buildings[family.home]; var work=city.buildings[FamilyWorkplace(family)];
                var anchors=new List<Vector3>{new Vector3(home.entranceX,0,home.entranceZ)};
                foreach(int node in path) anchors.Add(RoadPosition(node));
                anchors.Add(new Vector3(work.entranceX,0,work.entranceZ));
                for(int i=0;i<anchors.Count-1;i++) for(int k=0;k<=8;k++)
                {
                    Vector3 p=Vector3.Lerp(anchors[i],anchors[i+1],k/8f);
                    p.y=Mathf.Max(p.y,landscape.Height(p.x,p.z))+.4f; points.Add(p);
                }
            }
            commuteLine.positionCount=points.Count; if(points.Count>0) commuteLine.SetPositions(points.ToArray());
        }
        void DrawCommuteLabels(float scale,Color navy)
        {
            var family=VisibleCommuter();
            if(selected!=LandUse.Empty || !HasWorkplace(family) || showSimulation || help) return;
            foreach(int id in new[]{family.home,FamilyWorkplace(family)})
            {
                Vector3 p=cam.WorldToScreenPoint(BuildingMarker(id)+Vector3.up*.5f);
                if(p.z<=0 || p.x<0 || p.x>Screen.width || p.y<0 || p.y>Screen.height) continue;
                Rect r=new Rect(p.x/scale-90,(Screen.height-p.y)/scale-35,180,30);
                // Do not cover the resident list or city controls with world labels.
                if(r.x<340 || r.y<110 || r.y>Screen.height/scale-200 || r.xMax>Screen.width/scale-290) continue;
                Panel(r,navy); GUI.Label(r,id==family.home?"起点：住宅 #"+id:"目的地："+WorkplaceName(family),small);
            }
        }
    }
}
