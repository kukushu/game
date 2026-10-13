using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using UnityEngine;
namespace HarborCity
{
    // Called only by the explicit -harbor-validate suite, not normal game Update.
    internal static class HarborCityRoadTopologyValidation
    {
        internal static void Run(HarborCityGame game,CityModel city,CityLandscape landscape,string directory)
        {
            string saved=JsonUtility.ToJson(city.ToSaveData()),meshBefore=MeshSignature(game);
            var rebuild=typeof(HarborCityGame).GetMethod("Rebuild",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
            string oldRender,newRender;
            try
            {
                CityRoadView.LegacyGeometryForValidation=true;rebuild.Invoke(game,null);oldRender=MeshSignature(game);
                CityRoadView.LegacyGeometryForValidation=false;rebuild.Invoke(game,null);newRender=MeshSignature(game);
            }
            finally{CityRoadView.LegacyGeometryForValidation=false;}
            if(oldRender!=newRender || meshBefore!=newRender)throw new Exception("Shared road geometry changed actual mesh vertices/triangles");
            var vehiclePoints=city.traffic.trips.Select(t=>CityVehicleGeometry.PositionValue(city.roads,t,t.segment,t.progress)).ToArray();
            var topology=city.roads.GetLogicalTopology();
            foreach(var edge in city.roads.edges)
            {
                if(!topology.TryMapLegacy(edge.a,edge.b,.37f,out var position) || !topology.TryToLegacy(position,out int a,out int b,out float progress) || a!=edge.a || b!=edge.b || Math.Abs(progress-.37f)>.000001f)throw new Exception("Native logical road mapping");
                var p=position.Road.Geometry.SampleAt(position.Distance);
                float renderedSurface=CityRoadView.SurfaceHeight(city.roads,landscape,new Vector3(p.X,p.Y,p.Z),a,b);
                if(Math.Abs(renderedSurface-p.Y)>.002f)throw new Exception("Native road centreline/surface profile mismatch");
                foreach(float fraction in new[]{0f,.37f,1f})foreach(float side in new[]{-2.7f,0f,2.7f})
                {
                    var left=city.roads.Node(edge.a);var right=city.roads.Node(edge.b);var point=Vector3.Lerp(CityRoadView.Point(left),CityRoadView.Point(right),fraction);point.x+=side;
                    if(CityRoadView.SurfaceHeight(city.roads,landscape,point,edge.a,edge.b)!=CityRoadView.LegacySurfaceHeight(city.roads,landscape,point,edge.a,edge.b))throw new Exception("Shared road surface differs from legacy at cross slope/endpoints");
                }
            }
            for(int i=0;i<vehiclePoints.Length;i++)
            {
                var t=city.traffic.trips[i];var p=CityVehicleGeometry.PositionValue(city.roads,t,t.segment,t.progress);var old=vehiclePoints[i];
                if(p.x!=old.x || p.y!=old.y || p.z!=old.z)throw new Exception("Topology query changed smoothed vehicle geometry");
            }
            if(meshBefore!=MeshSignature(game) || saved!=JsonUtility.ToJson(city.ToSaveData()))throw new Exception("Topology query changed native mesh/save");
            var loaded=JsonUtility.FromJson<CitySaveData>(saved).ToCity();var rebuilt=loaded.roads.GetLogicalTopology();
            string Signature(CityRoadTopology t)=>string.Join(";",t.Roads.Select(r=>r.Id+":"+r.From+":"+r.To+":"+r.Geometry.Length+":"+string.Join(",",r.Geometry.Spans.Select(s=>s.EdgeId))));
            if(topology.IsCurrent(loaded.roads) || Signature(topology)!=Signature(rebuilt))throw new Exception("Native logical topology load");
            File.WriteAllText(Path.Combine(directory,"LogicalRoadTopology.txt"),"PASS: shared and legacy geometry produce identical active game-owned mesh vertices/triangles; projected cross slope/endpoints exact; smoothed vehicles/JSON unchanged; native load same partition. roads="+topology.Roads.Count+"; edges="+topology.LegacyEdgeCount+"; scene_mesh_sha256="+meshBefore+". Legacy traffic topology, rendering segmentation/visual rules remain.");
        }
        static string MeshSignature(HarborCityGame game)
        {
            var text=new StringBuilder();
            var filters=game.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.gameObject.activeInHierarchy).ToArray();
            if(!filters.Any(f=>f.name=="Road surface" && f.sharedMesh!=null && f.sharedMesh.isReadable))throw new Exception("Native road mesh absent from topology scene check");
            foreach(var filter in filters)
            {
                var mesh=filter.sharedMesh;if(mesh==null || !mesh.isReadable)continue;
                text.Append(filter.name).Append('|');foreach(var p in mesh.vertices)text.Append(p.x.ToString("R")).Append(',').Append(p.y.ToString("R")).Append(',').Append(p.z.ToString("R")).Append(';');
                foreach(int i in mesh.triangles)text.Append(i).Append(',');
            }
            using(var hash=SHA256.Create())return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(text.ToString()))).Replace("-","");
        }
    }
}
