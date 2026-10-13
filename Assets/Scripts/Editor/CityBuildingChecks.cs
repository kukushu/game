using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
namespace HarborCity
{
    public static class CityBuildingChecks
    {
        internal static float Flat(float x,float z)=>1;
        internal static RoadNode P(float x,float z)=>new RoadNode {x=x,z=z,y=1};
        internal static void Check(bool ok,string message) {if(!ok)throw new Exception(message);}
        internal static void Result(string name,string message)
        {Directory.CreateDirectory("Temp");File.WriteAllText("Temp/"+name+".txt",message);Debug.Log(message);}
        internal static CityModel Copy(CityModel c)=>JsonUtility.FromJson<CitySaveData>(JsonUtility.ToJson(c.ToSaveData())).ToCity();
        internal static bool Equivalent(object a,object b)
        {
            if(a==null || b==null) return a==b;
            if(a.GetType()!=b.GetType()) return false;
            // JsonUtility's double parser may move the final binary digit.
            // All integer, enum, string and float state still compares exactly.
            if(a is double number) return Math.Abs(number-(double)b)<1e-8;
            var type=a.GetType();if(type.IsPrimitive || type.IsEnum || a is string) return a.Equals(b);
            if(a is System.Collections.IEnumerable values)
            {
                var left=values.Cast<object>().ToList();var right=((System.Collections.IEnumerable)b).Cast<object>().ToList();
                return left.Count==right.Count && left.Select((value,index)=>Equivalent(value,right[index])).All(ok=>ok);
            }
            return type.GetFields(BindingFlags.Public|BindingFlags.Instance).Where(field=>!field.IsNotSerialized)
                .All(field=>Equivalent(field.GetValue(a),field.GetValue(b)));
        }
        internal static int Build(CityModel c,float x,float z,LandUse use,HousingKind housing=HousingKind.Apartment)
        {var p=c.RoadsidePreview(x,z,use,housing,out string error);Check(p!=null,error);int id=c.PlaceBuilding(p,use,Flat,out error);Check(id>0,error);return id;}
        internal static CityModel Fixture()
        {
            var c=CityModel.Create();c.SetEntranceHeight(Flat);c.nextBuildingId=101;
            Check(c.CommitRoad(c.roads.Plan(c,c.roads.Node(CityRoads.Entrance),P(45,1.5f),Flat)),"Fixture road");
            Build(c,-42.137f,7,LandUse.Residential);c.nextBuildingId+=7;
            Build(c,-30.137f,7,LandUse.Residential,HousingKind.Villa);
            Build(c,-20,7,LandUse.Commercial);Build(c,20,7,LandUse.Industrial);
            Build(c,-40,-5,LandUse.Power);Build(c,-30,-5,LandUse.Water);Build(c,-20,-5,LandUse.Park);
            return c;
        }
        [MenuItem("Harbor/Validate roadside buildings")]
        public static void Validate()
        {
            var c=Fixture();var home=c.buildings.OfType<ResidentialBuilding>().First();var f=c.buildings.OfType<IndustrialBuilding>().Single();var shop=c.buildings.OfType<CommercialBuilding>().Single();
            home.applications=3;home.vacantDays=9;home.interestedFamilies.Add(23);home.heavyTraffic=.3f;
            f.goodsStock=7;f.factory.raw=2;f.factory.processing=true;f.factory.consumed=1;f.factory.progress=17;shop.stock=11;
            c.buildings.Reverse();var copy=Copy(c);
            Check(copy.buildings.All(b=>b.GetType()==c.GetBuilding(b.id).GetType()) && Equivalent(copy.ToSaveData(),c.ToSaveData()),"All native subtype fields and exact poses");
            Check(copy.Valid() && copy.GetBuilding(home.id) is ResidentialBuilding && copy.GetBuilding(f.id) is IndustrialBuilding && copy.GetBuilding(shop.id) is CommercialBuilding,"Native JSON concrete types");
            Check(copy.Residence(home.id).applications==3 && copy.Residence(home.id).vacantDays==9 && copy.Residence(home.id).interestedFamilies.SequenceEqual(home.interestedFamilies),"Native JSON residential payload");
            Check(copy.Factory(f.id).processing && copy.Factory(f.id).progress==17 && copy.Factory(f.id).raw==2 && copy.Goods(f.id)==7 && copy.Goods(shop.id)==11,"Native JSON industrial/commercial payload");
            Check(copy.buildings.Select(b=>b.id).SequenceEqual(c.buildings.Select(b=>b.id)) && copy.nextBuildingId==c.nextBuildingId && copy.GetBuilding(home.id).entranceX==home.entranceX && copy.BuildingAccess(home.id),"Native JSON sparse IDs and entrances");
            var game=UnityEngine.Object.FindAnyObjectByType<HarborCityGame>();
            if(game!=null)
            {
                var live=(CityModel)typeof(HarborCityGame).GetField("city",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(game);
                Check(live.Valid(),"Live city invalid");
                var visuals=(System.Collections.Generic.Dictionary<int,GameObject>)typeof(HarborCityGame).GetField("visuals",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(game);
                foreach(var pair in visuals) {var b=live.GetBuilding(pair.Key);Check(b!=null && Math.Abs(pair.Value.transform.position.x-b.x)<.001f && Math.Abs(pair.Value.transform.position.z-b.z)<.001f && Quaternion.Angle(pair.Value.transform.rotation,Quaternion.Euler(0,b.yaw,0))<.01f,"Building visual pose");}
            }
            Result("CityBuildingChecks","PASS: native DTO JSON, all six concrete types, apartment/villa payloads, stocks, exact frontage, sparse IDs; live visuals "+(game!=null?"checked":"not running"));
        }
        [MenuItem("Harbor/Validate entity architecture and native saves")]
        public static void ValidateAll()
        {Validate();CityHouseholdChecks.Validate();CityTrafficChecks.Validate();CityRoadChecks.Validate();}
    }
}
