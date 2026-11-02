using System;using System.Collections.Generic;using HarborCity.RoadCore;using UnityEngine;using UnityEngine.Rendering;
namespace HarborCity
{
    internal sealed class HarborCityLayeredBuildings:IDisposable
    {
        sealed class Item {internal int Id;internal CityDisplayCell Cell;internal LandUse Use;internal Matrix4x4 Matrix;internal Bounds Bounds;internal float Size;}
        sealed class Node {internal int Level;internal CityDisplayCell Key;internal Bounds Bounds;internal float Size;internal int Count,Mode;internal bool Valid;internal readonly Dictionary<CityDisplayCell,Node> Children=new Dictionary<CityDisplayCell,Node>();internal readonly HashSet<int> Ids=new HashSet<int>();internal readonly Dictionary<LandUse,Mesh> Meshes=new Dictionary<LandUse,Mesh>();}
        readonly CityEntities entities;readonly CityPresentationJournal journal;readonly HarborCityBuildingViews owner;readonly HarborCityDisplayCamera query=new HarborCityDisplayCamera();readonly Mesh cube,roof;
        readonly Dictionary<int,Item> items=new Dictionary<int,Item>();readonly Dictionary<CityDisplayCell,Node> roots=new Dictionary<CityDisplayCell,Node>();readonly HashSet<Node> dirty=new HashSet<Node>();readonly HashSet<int> near=new HashSet<int>();
        internal int Represented,Batched,Summary;internal double IndexMs;
        internal HarborCityLayeredBuildings(HarborCityBuildingViews owner,CityEntities entities)
        {this.owner=owner;this.entities=entities;journal=entities.ObserveBuildings();var go=GameObject.CreatePrimitive(PrimitiveType.Cube);cube=go.GetComponent<MeshFilter>().sharedMesh;UnityEngine.Object.Destroy(go);roof=new Mesh{name="Building footprint template"};roof.vertices=new[]{new Vector3(-.5f,.5f,-.5f),new Vector3(-.5f,.5f,.5f),new Vector3(.5f,.5f,.5f),new Vector3(.5f,.5f,-.5f)};roof.triangles=new[]{0,1,2,0,2,3};roof.RecalculateNormals();roof.RecalculateBounds();foreach(var b in entities.Buildings)Update(b.id);Rebuild(true);journal.Clear();}
        Node Leaf(CityDisplayCell key)
        {var parent=key.Parent;var top=parent.Parent;if(!roots.TryGetValue(top,out var root))roots.Add(top,root=new Node{Key=top,Level=2});if(!root.Children.TryGetValue(parent,out var middle))root.Children.Add(parent,middle=new Node{Key=parent,Level=1});if(!middle.Children.TryGetValue(key,out var leaf))middle.Children.Add(key,leaf=new Node{Key=key});return leaf;}
        void Mark(CityDisplayCell key,Item item,int delta){var root=roots[key.Parent.Parent];var middle=root.Children[key.Parent];Invalidate(root,item,delta);Invalidate(middle,item,delta);Invalidate(middle.Children[key],item,delta);}
        void Invalidate(Node node,Item item,int delta){if(node.Count==0&&delta>0)node.Bounds=item.Bounds;else if(delta>0)node.Bounds.Encapsulate(item.Bounds);node.Count+=delta;node.Size=Mathf.Max(node.Size,item.Size);node.Valid=false;dirty.Add(node);}
        void Update(int id)
        {
            if(items.TryGetValue(id,out var old)){Leaf(old.Cell).Ids.Remove(id);Mark(old.Cell,old,-1);items.Remove(id);}
            var b=entities.Building(id);if(b==null)return;var matrix=Matrix4x4.TRS(new Vector3(b.x,b.groundY+1.5f,b.z),Quaternion.Euler(0,b.yaw,0),new Vector3(b.width,3,b.depth));var bounds=new Bounds(matrix.GetColumn(3),Vector3.zero);for(int i=0;i<8;i++)bounds.Encapsulate(matrix.MultiplyPoint3x4(new Vector3((i&1)==0?-.5f:.5f,(i&2)==0?-.5f:.5f,(i&4)==0?-.5f:.5f)));
            var cell=CityDisplayCell.At(new Point(b.x,b.groundY,b.z));var item=new Item{Id=id,Cell=cell,Use=b.Use,Matrix=matrix,Bounds=bounds,Size=Mathf.Max(b.width,b.depth,3)};items.Add(id,item);owner.UpdatePrepared(id,item.Use,item.Matrix);Leaf(cell).Ids.Add(id);Mark(cell,item,1);
        }
        void Collect(Node node,List<Item> result){if(node.Level==0){foreach(int id in node.Ids)result.Add(items[id]);}else foreach(var child in node.Children.Values)Collect(child,result);}
        readonly List<Node> completed=new List<Node>();
        void Build(Node node)
        {
            var records=new List<Item>();Collect(node,records);var replacements=new Dictionary<LandUse,Mesh>();
            try{
                var groups=new Dictionary<LandUse,List<CombineInstance>>();foreach(var item in records){if(!groups.TryGetValue(item.Use,out var group))groups.Add(item.Use,group=new List<CombineInstance>());group.Add(new CombineInstance{mesh=node.Level==0?cube:roof,transform=item.Matrix});}
                foreach(var group in groups){var mesh=new Mesh{name="Buildings "+node.Level+" / "+group.Key,indexFormat=IndexFormat.UInt32};replacements.Add(group.Key,mesh);mesh.CombineMeshes(group.Value.ToArray(),true,true);}
            }catch{foreach(var mesh in replacements.Values)UnityEngine.Object.Destroy(mesh);throw;}
            foreach(var mesh in node.Meshes.Values)UnityEngine.Object.Destroy(mesh);node.Meshes.Clear();foreach(var pair in replacements)node.Meshes.Add(pair.Key,pair.Value);
            node.Valid=true;
        }
        void Rebuild(bool cold=false)
        {
            completed.Clear();long start=System.Diagnostics.Stopwatch.GetTimestamp();
            // Changed leaves are immediate. Stale parents are never drawn: the
            // current child batches bridge their bounded asynchronous preparation.
            foreach(var node in dirty)if(cold||node.Level==0){Build(node);completed.Add(node);}
            foreach(var node in completed)dirty.Remove(node);completed.Clear();
            if(!cold)foreach(var node in dirty){if((System.Diagnostics.Stopwatch.GetTimestamp()-start)*1000.0/System.Diagnostics.Stopwatch.Frequency>=1)break;Build(node);completed.Add(node);}
            foreach(var node in completed)dirty.Remove(node);
        }
        internal void Sync(Camera camera,HarborCityDisplaySettings settings)
        {
            long at=System.Diagnostics.Stopwatch.GetTimestamp();foreach(int id in journal.Ids)Update(id);journal.Clear();Rebuild();IndexMs=(System.Diagnostics.Stopwatch.GetTimestamp()-at)*1000.0/System.Diagnostics.Stopwatch.Frequency;
            query.Update(camera);near.Clear();Represented=Batched=Summary=0;foreach(var root in roots.Values)Visit(root,settings);owner.RetireExcept(near);
        }
        void Visit(Node node,HarborCityDisplaySettings settings)
        {
            var padded=node.Bounds;padded.Expand(settings.edgePadding*2);if(node.Count==0||!query.Visible(padded))return;float pixels=query.Pixels(node.Bounds,node.Size);bool far=node.Valid&&node.Level>0&&pixels<(node.Mode==2?settings.buildingSummaryExit:settings.buildingSummaryEnter);
            if(far){node.Mode=2;Draw(node);Summary+=node.Count;Represented+=node.Count;return;}
            if(node.Level>0){node.Mode=1;foreach(var child in node.Children.Values)Visit(child,settings);return;}
            bool individual=pixels>=(node.Mode==0?settings.buildingNearExit:settings.buildingNearEnter);node.Mode=individual?0:1;
            bool ready=individual;if(individual&&node.Ids.Contains(owner.Selected))owner.Prepare(owner.Selected,items[owner.Selected].Use,items[owner.Selected].Matrix,settings); if(individual)foreach(int id in node.Ids){near.Add(id);if(!owner.Prepare(id,items[id].Use,items[id].Matrix,settings))ready=false;}
            // A whole leaf changes representation atomically; pending individuals
            // stay disabled until the last one is ready, with the batch covering it.
            if(ready){foreach(int id in node.Ids){near.Add(id);owner.Activate(id,true);}}else{foreach(int id in node.Ids)owner.Activate(id,false);Draw(node);Batched+=node.Count;}
            Represented+=node.Count;
        }
        void Draw(Node node){foreach(var pair in node.Meshes)Graphics.DrawMesh(pair.Value,Matrix4x4.identity,owner.ForUse(pair.Key),0);}
        internal bool Matrix(int id,out Matrix4x4 matrix){if(items.TryGetValue(id,out var item)){matrix=item.Matrix;return true;}matrix=default;return false;}
        internal int Pick(Ray ray){int found=0;float best=float.PositiveInfinity;foreach(var root in roots.Values)Pick(root,ray,ref found,ref best);return found;}
        void Pick(Node node,Ray ray,ref int found,ref float best){if(node.Count==0||!node.Bounds.IntersectRay(ray,out float distance)||distance>best)return;if(node.Level>0){foreach(var child in node.Children.Values)Pick(child,ray,ref found,ref best);return;}foreach(int id in node.Ids)if(items[id].Bounds.IntersectRay(ray,out distance)&&distance<best){best=distance;found=id;}}
        internal void AssertExact()
        {
            int total=0;foreach(var root in roots.Values)total+=root.Count;if(total!=entities.Buildings.Count||total!=items.Count)throw new InvalidOperationException("Building summary count differs");
            foreach(var b in entities.Buildings){if(!items.TryGetValue(b.id,out var item)||item.Use!=b.Use||Vector3.Distance(item.Matrix.GetColumn(3),new Vector3(b.x,b.groundY+1.5f,b.z))>.001f)throw new InvalidOperationException("Building display identity/pose differs");}
        }
        public void Dispose(){entities.ReleaseBuildings(journal);foreach(var root in roots.Values)Release(root);UnityEngine.Object.Destroy(roof);items.Clear();roots.Clear();}
        void Release(Node node){foreach(var mesh in node.Meshes.Values)UnityEngine.Object.Destroy(mesh);foreach(var child in node.Children.Values)Release(child);}
    }
}
