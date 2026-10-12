using System;
using System.Collections.Generic;
using HarborCity;
public static class RoadKeyExperimentChecks
{
    public static void Run()
    {
        var reference=new Dictionary<long,int>();var proposed=new Dictionary<long,int>(CityRoadKeyExperiment.Instance);
        var keys=new List<long>();var random=new Random(61);
        for(int i=0;i<10000;i++)
        {
            int a=random.Next(),b=i%2==0?a+1:random.Next();long key=((long)a<<32)|(uint)b;
            keys.Add(key);reference[key]=proposed[key]=i;
        }
        foreach(long key in keys)if(reference[key]!=proposed[key] || !CityRoadKeyExperiment.Instance.Equals(key,key))throw new Exception("Road key equality/value changed");
        long x=((long)1<<32)|2,y=((long)2<<32)|1;
        if(x.GetHashCode()!=y.GetHashCode() || CityRoadKeyExperiment.Instance.Equals(x,y))throw new Exception("Reversed key must collide in default hash but remain distinct in equality");
        proposed[x]=1;proposed[y]=2;if(proposed[x]!=1 || proposed[y]!=2)throw new Exception("Direction lost");
        proposed.Remove(x);if(proposed.ContainsKey(x) || proposed[y]!=2)throw new Exception("Key removal affected reverse direction");
        Console.WriteLine("PASS: 10000 research road-key equality/value cases, reverse direction and removal (normal game still uses original comparer)");
    }
}
