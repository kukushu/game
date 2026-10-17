using System;
using System.Collections;
using System.IO;
using System.Linq;
using HarborCity;
using HarborCity.RoadCore;
using UnityEngine;
public sealed class HarborCityVehicleViewValidation:MonoBehaviour
{
 static int checks;
 static void Check(bool ok,string reason){checks++;if(!ok)throw new Exception(reason);}
 static Vector3 V(Point p)=>new Vector3((float)p.x,(float)p.y,(float)p.z);
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
 static void Boot(){var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-harbor-vehicle-view-check");if(i>=0)new GameObject("Vehicle view checks").AddComponent<HarborCityVehicleViewValidation>().StartCoroutine(Run(args[i+1]));}
 static IEnumerator Run(string output)
 {
  yield return null;Directory.CreateDirectory(output);HarborCityVehicleViews view=null;Material material=null;
  try
  {
   material=new Material(Resources.Load<Shader>("RoadCore")){color=Color.yellow};view=new HarborCityVehicleViews(material);
   var world=CityRoadCoreFixtures.Create(3000,false);byte[] before=SaveCodec.Write(world);view.Sync(world);var instances=world.Vehicles.ToDictionary(v=>v.id,v=>view.Instance(v.id));
   Check(view.Count==3000&&view.Created==3000&&view.Pooled==0,"Active displays limited or missing");
   foreach(var v in world.Vehicles){var car=view.Instance(v.id);Check(car!=null&&car.gameObject.activeSelf,"Missing display");Check(car.position==V(world.Position(v))+Vector3.up*.4f,"Display changed position");var d=world.Network.Get(v.Current);var direction=V(d.Geometry.Sample(v.distance+.1))-V(d.Geometry.Sample(Math.Max(0,v.distance-.1)));if(direction.sqrMagnitude>1e-8)Check(car.rotation==Quaternion.LookRotation(direction),"Display changed rotation");Check(car.localScale==new Vector3(.65f,.5f,1)&&car.GetComponent<Renderer>().sharedMaterial==material&&car.GetComponent<BoxCollider>()!=null,"Cube appearance/collider changed");}
   for(int i=0;i<20;i++)view.Sync(world);Check(view.Created==3000&&view.Reused==0&&instances.All(p=>view.Instance(p.Key)==p.Value),"Unchanged sync rebuilt objects");Check(before.SequenceEqual(SaveCodec.Write(world)),"Observation changed transport facts");
   world.Advance(.05);before=SaveCodec.Write(world);view.Sync(world);Check(before.SequenceEqual(SaveCodec.Write(world)),"Moving display changed facts");foreach(var v in world.Vehicles)Check(view.Instance(v.id).position==V(world.Position(v))+Vector3.up*.4f,"Moving transform stale");
   var shortWorld=new World();int a=shortWorld.AddJunction(new Point()),b=shortWorld.AddJunction(new Point(15,0,0));shortWorld.AddRoad(a,b,new[]{shortWorld.JunctionPosition(a),shortWorld.JunctionPosition(b)},true);int origin=shortWorld.AddDepot(a,100),destination=shortWorld.AddDepot(b,0);var old=shortWorld.Dispatch(origin,destination);view.Sync(shortWorld);var reused=view.Instance(old.id);int created=view.Created;
   for(int i=0;i<12;i++)
   {
    shortWorld.Advance(2);before=SaveCodec.Write(shortWorld);view.Sync(shortWorld);Check(view.Count==0&&view.Pooled==1&&!reused.gameObject.activeSelf,"Retired display not parked");Check(before.SequenceEqual(SaveCodec.Write(shortWorld)),"Retirement changed settlement");
    var next=shortWorld.Dispatch(origin,destination);Check(next!=null,"Synthetic next trip rejected");before=SaveCodec.Write(shortWorld);view.Sync(shortWorld);Check(view.Instance(next.id)==reused&&view.Pooled==0&&view.Created==created,"New stable ID did not reuse display");Check(before.SequenceEqual(SaveCodec.Write(shortWorld)),"Reuse changed task or transport");Check(reused.name=="Freight "+next.id&&reused.gameObject.activeSelf,"Reuse kept stale display identity");
   }
   Check(view.Reused==12,"Reuse count wrong");view.Clear();Check(view.Count==0&&view.Pooled==0,"Clear retained IDs");view.Sync(null);view.Dispose();Check(material!=null,"Borrowed material destroyed");
   File.WriteAllText(Path.Combine(output,"Result.txt"),"PASS "+checks+" native view lifecycle checks; all 3000 actors shown, stable IDs/position/cube/material/collider, repeated sync, actual settlement and new-task object reuse, world rebind/clear; full transport save unchanged. No city FPS claim.\n");
  }
  catch(Exception e){File.WriteAllText(Path.Combine(output,"Result.txt"),e.ToString());Application.Quit(1);yield break;}
  finally{view?.Dispose();if(material!=null)Destroy(material);}
  yield return null;Application.Quit(0);
 }
}
