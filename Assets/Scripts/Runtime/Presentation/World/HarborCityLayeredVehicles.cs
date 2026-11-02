using System;using System.Collections.Generic;using HarborCity.RoadCore;using UnityEngine;
namespace HarborCity
{
    internal sealed class HarborCityLayeredVehicles:IDisposable
    {
        sealed class Pose {internal int Version;internal Matrix4x4 Matrix;internal Quaternion Rotation;internal bool Near;internal int DrawFrame=-1,SeenFrame=-1;}
        readonly HarborCityVehicleViews owner;readonly CityVehicleDisplayIndex index;readonly World world;readonly HarborCityDisplayCamera query=new HarborCityDisplayCamera();readonly HarborCityDisplayBatch batch;
        readonly Dictionary<int,Pose> poses=new Dictionary<int,Pose>();readonly HashSet<int> near=new HashSet<int>();readonly List<int> stale=new List<int>();
        readonly HashSet<CityVehicleDisplayIndex.Node> visibleSummaries=new HashSet<CityVehicleDisplayIndex.Node>();
        readonly Dictionary<CityVehicleDisplayIndex.Node,bool> summaries=new Dictionary<CityVehicleDisplayIndex.Node,bool>();
        internal long PoseSamples;internal int Represented,SummaryVehicles,SummaryRegions,Batched;internal double IndexMs;
        internal HarborCityLayeredVehicles(HarborCityVehicleViews owner,World world,Material[] materials){this.owner=owner;this.world=world;index=new CityVehicleDisplayIndex(world);batch=new HarborCityDisplayBatch(materials);}
        internal void Sync(Camera camera,HarborCityDisplaySettings settings)
        {
            long start=System.Diagnostics.Stopwatch.GetTimestamp();index.Sync();IndexMs=(System.Diagnostics.Stopwatch.GetTimestamp()-start)*1000.0/System.Diagnostics.Stopwatch.Frequency;
            query.Update(camera);near.Clear();visibleSummaries.Clear();batch.Begin();Represented=SummaryVehicles=SummaryRegions=Batched=0;
            foreach(var root in index.Roots.Values)Visit(root,settings);
            owner.RetireExcept(near);stale.Clear();foreach(var pair in poses)if(pair.Value.SeenFrame!=Time.frameCount)stale.Add(pair.Key);foreach(int id in stale)poses.Remove(id);
            // Modes for removed tree nodes must not retain old cities/empty cells.
            if(summaries.Count>index.Records.Count+64)summaries.Clear();batch.Draw();
        }
        void Visit(CityVehicleDisplayIndex.Node node,HarborCityDisplaySettings settings)
        {
            float size=64*(node.Level==2?16:node.Level==1?4:1);var bounds=new Bounds(new Vector3((node.Key.X+.5f)*size,(float)((index.MinY+index.MaxY)*.5),(node.Key.Z+.5f)*size),new Vector3(size+2*settings.edgePadding,(float)(index.MaxY-index.MinY),size+2*settings.edgePadding));
            if(!query.Visible(bounds))return;float pixels=query.Pixels(bounds,1);bool was=summaries.TryGetValue(node,out bool prior)&&prior;bool summary=pixels<(was?settings.vehicleSummaryExit:settings.vehicleSummaryEnter);summaries[node]=summary;
            if(summary){visibleSummaries.Add(node);SummaryRegions++;SummaryVehicles+=node.Count;Represented+=node.Count;for(int p=0;p<4;p++)if(node.Purposes[p]>0){float radius=Mathf.Clamp(Mathf.Sqrt(node.Purposes[p])*settings.densityCountScale,settings.vehicleDensityMinRadius,size*settings.vehicleDensityMaxFraction);var at=bounds.center+new Vector3((p-1.5f)*radius*2,0,0);at.y=(float)index.MaxY+2;batch.Add(p,Matrix4x4.TRS(at,Quaternion.identity,new Vector3(radius*1.6f,.12f,radius*1.6f)));}return;}
            if(node.Level>0){foreach(var child in node.Children.Values)Visit(child,settings);return;}
            foreach(int id in node.Ids){var record=index.Records[id];var actor=record.Actor;if(!poses.TryGetValue(id,out var pose))poses.Add(id,pose=new Pose{Version=-1,Rotation=Quaternion.identity});
                pose.SeenFrame=Time.frameCount;
                if(pose.Version!=record.Version){PoseSamples++;var point=world.Position(actor);if(actor.status==VehicleStatus.Active){var geometry=world.Network.Get(actor.Current).Geometry;var direction=V(geometry.Sample(actor.distance+.1))-V(geometry.Sample(Math.Max(0,actor.distance-.1)));if(direction.sqrMagnitude>1e-8)pose.Rotation=Quaternion.LookRotation(direction);}pose.Matrix=Matrix4x4.TRS(V(point)+Vector3.up*.4f,pose.Rotation,new Vector3(.65f,.5f,1));pose.Version=record.Version;}
                var actual=new Bounds(pose.Matrix.GetColumn(3),Vector3.one*2);var padded=actual;padded.Expand(settings.edgePadding*2);if(!query.Visible(padded))continue;float projected=query.Pixels(actual,1);pose.Near=projected>=(pose.Near?settings.vehicleNearExit:settings.vehicleNearEnter);
                Transform transform=null;bool individual=pose.Near&&owner.Prepare(id,actor.Task.purpose,settings,out transform);if(individual){near.Add(id);transform.SetPositionAndRotation(pose.Matrix.GetColumn(3),pose.Rotation);}else{batch.Add(record.Purpose,pose.Matrix);Batched++;}pose.DrawFrame=Time.frameCount;Represented++;
            }
        }
        static Vector3 V(Point p)=>new Vector3((float)p.x,(float)p.y,(float)p.z);
        internal void AssertExact()
        {
            index.AssertExact();
            foreach(var pair in index.Records){var actor=pair.Value.Actor;var bounds=new Bounds(V(world.Position(actor))+Vector3.up*.4f,Vector3.one*2);if(!query.Visible(bounds))continue;
                var cell=pair.Value.Cell;var root=index.Roots[cell.Parent.Parent];var middle=root.Children[cell.Parent];var leaf=middle.Children[cell];bool summary=visibleSummaries.Contains(root)||visibleSummaries.Contains(middle)||visibleSummaries.Contains(leaf);
                bool exact=poses.TryGetValue(pair.Key,out var pose)&&pose.DrawFrame==Time.frameCount;
                if(summary==exact||(!summary&&!exact))throw new InvalidOperationException("Visible vehicle has missing/duplicate representation "+pair.Key);
                if(exact&&owner.Instance(pair.Key)!=null&&Vector3.Distance(owner.Instance(pair.Key).position,bounds.center)>.001f)throw new InvalidOperationException("Individual vehicle pose differs");
            }
        }
        public void Dispose(){index.Dispose();batch.Dispose();poses.Clear();summaries.Clear();}
    }
}
