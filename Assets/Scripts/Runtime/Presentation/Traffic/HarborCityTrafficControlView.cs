using System.Collections.Generic;
using UnityEngine;
namespace HarborCity
{
    public sealed partial class HarborCityGame
    {
        readonly Dictionary<(int node,int from),Renderer> trafficControlViews=new Dictionary<(int,int),Renderer>();
        CityRoads controlViewRoads;
        int controlViewRevision=-1;
        void UpdateTrafficControlViews()
        {
            if(city==null || landscape==null)return;
            if(controlViewRoads!=city.roads || controlViewRevision!=city.roads.revision)
            {
                foreach(var r in trafficControlViews.Values)if(r!=null){r.gameObject.SetActive(false);Destroy(r.gameObject);}trafficControlViews.Clear();
                controlViewRoads=city.roads;controlViewRevision=city.roads.revision;
                foreach(var node in city.roads.nodes)
                {
                    if(city.roads.Neighbors(node.id).Count<3)continue;
                    bool signals=city.roads.HasSignals(node.id);
                    foreach(var from in city.roads.Neighbors(node.id))
                    {
                        if(!city.roads.CanTravel(from,node.id) || !signals && !node.stopFrom.Contains(from))continue;
                        var arm=city.roads.Node(from);var incoming=new Vector3(node.x-arm.x,0,node.z-arm.z).normalized;
                        var right=Vector3.Cross(Vector3.up,incoming);var position=new Vector3(node.x,node.y,node.z)-incoming*Mathf.Min(1.5f,CityRoads.Length(node,arm)*.5f)+right*(city.roads.EdgeWidth(from,node.id)/2+.25f);
                        position.y=CityRoadView.SurfaceHeight(city.roads,landscape,position,node.id,from)+.65f;
                        var obj=GameObject.CreatePrimitive(PrimitiveType.Cube);obj.name=(signals?"Signal":"Stop sign")+" "+node.id+"/"+from;obj.transform.SetParent(transform,false);obj.transform.position=position;obj.transform.localScale=new Vector3(.24f,.35f,.24f);
                        Destroy(obj.GetComponent<Collider>());trafficControlViews[(node.id,from)]=obj.GetComponent<Renderer>();
                    }
                }
            }
            foreach(var pair in trafficControlViews)
            {
                bool signals=city.roads.HasSignals(pair.Key.node);var aspect=city.roads.Signal(pair.Key.node,pair.Key.from,city.traffic.clock);
                pair.Value.sharedMaterial=Mat(!signals || aspect==SignalAspect.Red?new Color(.92f,.17f,.12f):aspect==SignalAspect.Amber?new Color(1,.72f,.1f):new Color(.15f,.85f,.25f));
            }
        }
    }
}
