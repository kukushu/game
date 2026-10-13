using UnityEngine;
using UnityEngine.Rendering;

namespace HarborCity
{
    public sealed partial class HarborCityGame
    {
        LineRenderer buildBoundary;
        void EnsureBuildBoundary()
        {
            var sea=transform.Find("Sea");
            if(sea!=null)sea.localScale=new Vector3(CityMapState.TerrainHalfSize*8,.3f,CityMapState.TerrainHalfSize*8);
            if(buildBoundary==null)
            {
                buildBoundary=new GameObject("Buildable map boundary").AddComponent<LineRenderer>();
                buildBoundary.transform.SetParent(transform,false);
                buildBoundary.sharedMaterial=Mat(new Color(.95f,.8f,.3f));
                buildBoundary.startWidth=buildBoundary.endWidth=.24f;
                buildBoundary.shadowCastingMode=ShadowCastingMode.Off;buildBoundary.receiveShadows=false;
                buildBoundary.loop=true;buildBoundary.useWorldSpace=true;
            }
            const int samples=192;float half=CityModel.BuildHalfSize;
            var corners=new[]{new Vector2(-half,-half),new Vector2(half,-half),new Vector2(half,half),new Vector2(-half,half)};
            buildBoundary.positionCount=samples*4;
            for(int side=0;side<4;side++)for(int i=0;i<samples;i++)
            {
                var p=Vector2.Lerp(corners[side],corners[(side+1)%4],i/(float)samples);
                buildBoundary.SetPosition(side*samples+i,new Vector3(p.x,Mathf.Max(CityLandscape.SeaLevel,landscape.Height(p.x,p.y))+.16f,p.y));
            }
            buildBoundary.enabled=selected!=LandUse.Empty;
        }
    }
}
