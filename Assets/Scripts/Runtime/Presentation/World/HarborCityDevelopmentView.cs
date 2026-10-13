using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HarborCity
{
    public sealed partial class HarborCityGame
    {
        readonly Dictionary<int,Transform> constructionViews=new Dictionary<int,Transform>();
        readonly Dictionary<int,int> drawnBuildingLevels=new Dictionary<int,int>();
        readonly Dictionary<int,bool> drawnBuildingConditions=new Dictionary<int,bool>();
        CityModel constructionCity;
        void RefreshDevelopmentView()
        {
            foreach(var b in city.buildings)
                if(!visuals.ContainsKey(b.id) || !drawnBuildingLevels.TryGetValue(b.id,out int drawnLevel) || drawnLevel!=b.level || (drawnBuildingConditions.TryGetValue(b.id,out bool abandoned)?abandoned!=b.abandoned:b.abandoned))
                {
                    DrawLot(b.id);drawnBuildingConditions[b.id]=b.abandoned;drawnBuildingLevels[b.id]=b.level;
                    if(b.abandoned)foreach(var renderer in visuals[b.id].GetComponentsInChildren<MeshRenderer>())renderer.sharedMaterial=Mat(new Color(.42f,.43f,.4f));
                }
            if(constructionCity!=city)
            {
                foreach(var view in constructionViews.Values) if(view!=null) Destroy(view.gameObject);
                constructionViews.Clear();constructionCity=city;
            }
            var active=city.development.projects.Select(p=>p.id).ToHashSet();
            foreach(int id in constructionViews.Keys.Where(id=>!active.Contains(id)).ToList())
            {if(constructionViews[id]!=null)Destroy(constructionViews[id].gameObject);constructionViews.Remove(id);}
            foreach(var project in city.development.projects)
            {
                if(!constructionViews.TryGetValue(project.id,out var view) || view==null)
                {
                    var b=project.building.pose;view=new GameObject("Private construction #"+project.id).transform;view.SetParent(world,false);
                    view.position=new Vector3(b.x,landscape.Height(b.x,b.z)+.1f,b.z);view.rotation=Quaternion.Euler(0,b.yaw,0);
                    Box("Construction footprint",new Vector3(0,.06f,0),new Vector3(b.width,.12f,b.depth),new Color(.55f,.5f,.35f),view);
                    Box("Scaffold",new Vector3(0,.7f,0),new Vector3(b.width-.3f,1.4f,b.depth-.3f),new Color(.64f,.64f,.58f),view);
                    Box("Crane",new Vector3(b.width/2,1.6f,0),new Vector3(.12f,3.2f,.12f),new Color(.94f,.76f,.24f),view);
                    Box("Crane arm",new Vector3(.5f,3.1f,0),new Vector3(3,.12f,.12f),new Color(.94f,.76f,.24f),view);
                    constructionViews[project.id]=view;
                }
            }
        }
    }
}
