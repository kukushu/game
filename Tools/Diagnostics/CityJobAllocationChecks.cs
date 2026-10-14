using System;
using HarborCity;
public static class CityJobAllocationChecks
{
    public static void Main()
    {
        var city=CityIntegratedScaleFixture.Create(3000).City;int id=city.society.jobEntities[0].id;
        for(int i=0;i<100;i++)city.Job(id);
        long start=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<10000;i++)if(city.Job(id)==null)throw new Exception("Missing job");
        long bytes=GC.GetAllocatedBytesForCurrentThread()-start;
        if(bytes!=0)throw new Exception("Indexed job queries allocate "+bytes+" bytes");
        Console.WriteLine("PASS 10000 indexed CityModel.Job calls allocate zero bytes");
    }
}
