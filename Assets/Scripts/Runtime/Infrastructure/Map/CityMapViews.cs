using System;

namespace HarborCity
{
    public enum CityMapView { Electricity,Water,Garbage,Noise,Pollution,Employment,Crime,LandValue }
    public sealed partial class CityModel
    {
        // Derived observations only. Viewing a map never edits simulation facts.
        public float MapIndicator(int id,CityMapView view)
        {
            var b=GetBuilding(id);if(b==null)return 0;
            switch(view)
            {
                case CityMapView.Electricity:return Supply(id).electricity?1:0;
                case CityMapView.Water:return Supply(id).water && Supply(id).sewage?1:0;
                case CityMapView.Garbage:return b is LandfillBuilding depot?Math.Clamp(depot.stored/(float)LandfillBuilding.Capacity,0,1):Math.Clamp(b.garbage/(float)GarbageWarning,0,1);
                case CityMapView.Noise:return Math.Clamp(EnvironmentAt(b.x,b.z).noise,0,1);
                case CityMapView.Pollution:return Math.Clamp(EnvironmentAt(b.x,b.z).pollution,0,1);
                case CityMapView.LandValue:return LandValue(id)/100;
                case CityMapView.Crime:return b.crime/100;
                case CityMapView.Employment:return JobCapacity(id)>0?EmployedAt(id)/(float)JobCapacity(id):0;
                default:return 0;
            }
        }
    }
}
