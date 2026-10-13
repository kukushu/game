using UnityEngine;
using System.Collections.Generic;

namespace HarborCity
{
    public sealed partial class HarborCityGame
    {
        int inspectedResident=-1;
        bool followResident;
        LineRenderer residentMarker, residentRoute;
        sealed class PopulationRow
        {
            public Household family;
            public CityResident person;
            public ResidentActivity activity;
        }
        void OpenPopulationPanel()
        {
            showSimulation=true; simulationTab=5;
            analysisEntityId=-1;
            simulationScroll=Vector2.zero; simulationTyping=false;
        }
        string PopulationPlace(int id) => city.GetBuilding(id)==null?"无":city.UseOf(id)==LandUse.Empty?"已拆建筑 #"+id:EndpointName(id);
        void SelectPopulationResident(PopulationRow row,bool follow)
        {
            selected=LandUse.Empty; inspectedHome=row.family.home; inspectedResident=row.person.id;
            inspectedTrip=-1; commuteFamily=-1; followResident=follow;
            showSimulation=false; simulationTyping=false; householdListScroll=Vector2.zero;
            focus=new Vector3(row.activity.x,0,row.activity.z); zoom=24; UpdateCamera(); UpdateResidentView();
            notice="已选中 "+row.person.name+"（居民 #"+row.person.id+"），青色标记显示当前位置。";
        }
        void UpdateResidentView()
        {
            var h=city.society?.families.Find(f=>f.resident && f.home==inspectedHome && f.people!=null && f.people.Exists(p=>p.id==inspectedResident));
            if(selected!=LandUse.Empty || h==null)
            {
                if(residentMarker!=null) residentMarker.positionCount=0;
                if(residentRoute!=null) residentRoute.positionCount=0;
                followResident=false; return;
            }
            var person=h.people.Find(p=>p.id==inspectedResident); var a=city.ObserveResident(h,person);
            if(residentMarker==null) residentMarker=CommuteRenderer("Resident location",Color.cyan,.2f);
            if(residentRoute==null) residentRoute=CommuteRenderer("Resident journey",Color.cyan,.12f);
            if(!a.located) {residentMarker.positionCount=residentRoute.positionCount=0; followResident=false; return;}
            Vector3 p=a.building>=0?BuildingMarker(a.building):new Vector3(a.x,landscape.Height(a.x,a.z)+.6f,a.z);
            var actualTrip=traffic.State.trips.Find(t=>t.id==person.tripId && t.residentId==person.id);
            if(actualTrip!=null && a.located) p=TrafficPoint(actualTrip,actualTrip.segment,actualTrip.progress)+Vector3.up*.4f;
            residentMarker.positionCount=5;
            residentMarker.SetPositions(new[]{p+Vector3.left*.6f,p+Vector3.forward*.6f,p+Vector3.right*.6f,p+Vector3.back*.6f,p+Vector3.left*.6f});
            residentRoute.positionCount=a.route.Count;
            for(int i=0;i<a.route.Count;i++) {var n=a.route[i]; residentRoute.SetPosition(i,new Vector3(n.x,Mathf.Max(n.y,landscape.Height(n.x,n.z))+.35f,n.z));}
            if(followResident && !showSimulation && !help && !draggingView)
            {focus=new Vector3(a.x,0,a.z); UpdateCamera();}
        }
    }
}
