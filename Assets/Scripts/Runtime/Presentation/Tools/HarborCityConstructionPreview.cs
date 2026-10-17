using System;
using HarborCity.RoadCore;
using UnityEngine;

namespace HarborCity
{
    // Cursor affordances are view state, not city entities or saved facts.
    internal sealed class HarborCityConstructionPreview:IDisposable
    {
        readonly LineRenderer start,path;readonly Material material;
        public HarborCityConstructionPreview(Material source)
        {
            material=new Material(source){color=new Color(.2f,1,.65f)};
            start=new GameObject("Construction start marker").AddComponent<LineRenderer>();
            path=new GameObject("Construction cursor preview").AddComponent<LineRenderer>();
            start.sharedMaterial=path.sharedMaterial=material;start.loop=true;start.positionCount=32;
            start.startWidth=start.endWidth=.18f;Hide();
        }
        public void Hide(){if(start!=null)start.enabled=false;if(path!=null)path.enabled=false;}
        public void Show(Point origin,bool anchored,Point[] points,bool valid,float width,Func<float,float,float> height,bool provisional=false)
        {
            start.enabled=true;material.color=!valid?new Color(1,.2f,.15f):provisional?new Color(1,.75f,.15f):new Color(.15f,1,.6f);
            for(int i=0;i<32;i++){double a=i*Math.PI/16;var p=new Point(origin.x+Math.Cos(a)*.7,origin.y,origin.z+Math.Sin(a)*.7);start.SetPosition(i,HarborCitySurfaceMesh.Visible(p,height,.28f));}
            path.enabled=anchored&&points!=null;if(!path.enabled)return;
            path.startWidth=path.endWidth=width;path.positionCount=points.Length;
            for(int i=0;i<points.Length;i++)path.SetPosition(i,HarborCitySurfaceMesh.Visible(points[i],height,.24f));
        }
        public void Dispose(){if(start!=null)UnityEngine.Object.Destroy(start.gameObject);if(path!=null)UnityEngine.Object.Destroy(path.gameObject);if(material!=null)UnityEngine.Object.Destroy(material);}
    }
}
