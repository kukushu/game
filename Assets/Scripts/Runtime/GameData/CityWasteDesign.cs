using System;
namespace HarborCity
{
    [Serializable] public sealed class WasteDefinition
    {
        public int constructionPrice=4000,capacity=3200,trucks=3,payload=8,processingPerDay=256;
        public double dispatchIntervalSeconds=2;
        public int residentialPerPersonDay=1,workMinutesPerUnit=240;
    }
    public sealed class WasteRules
    {
        readonly WasteDefinition d;
        public int ConstructionPrice=>d.constructionPrice;public int Capacity=>d.capacity;public int Trucks=>d.trucks;public int Payload=>d.payload;
        public int ProcessingPerDay=>d.processingPerDay;public int ResidentialRate=>d.residentialPerPersonDay;public int WorkMinutesPerUnit=>d.workMinutesPerUnit;
        public long DispatchTicks {get;}
        internal WasteRules(WasteDefinition value)
        {
            if(value==null||value.constructionPrice<0||value.constructionPrice>10000000||value.capacity<1||value.capacity>LandfillBuilding.Capacity||value.trucks<1||value.trucks>1000||value.payload<1||value.payload>value.capacity||value.processingPerDay<1||value.processingPerDay>100000||value.residentialPerPersonDay<0||value.residentialPerPersonDay>100||value.workMinutesPerUnit<1||value.dispatchIntervalSeconds<.05||value.dispatchIntervalSeconds>3600||double.IsNaN(value.dispatchIntervalSeconds))throw new ArgumentException("Invalid waste design");
            d=new WasteDefinition{constructionPrice=value.constructionPrice,capacity=value.capacity,trucks=value.trucks,payload=value.payload,processingPerDay=value.processingPerDay,residentialPerPersonDay=value.residentialPerPersonDay,workMinutesPerUnit=value.workMinutesPerUnit,dispatchIntervalSeconds=value.dispatchIntervalSeconds};
            DispatchTicks=(long)Math.Ceiling(d.dispatchIntervalSeconds/RoadCore.SimulationClock.StepSeconds-1e-9);
        }
        internal WasteDefinition Snapshot()=>new WasteDefinition{constructionPrice=d.constructionPrice,capacity=d.capacity,trucks=d.trucks,payload=d.payload,processingPerDay=d.processingPerDay,residentialPerPersonDay=d.residentialPerPersonDay,workMinutesPerUnit=d.workMinutesPerUnit,dispatchIntervalSeconds=d.dispatchIntervalSeconds};
    }
}
