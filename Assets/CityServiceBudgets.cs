using System;

namespace HarborCity
{
    public enum CityServiceKind { Electricity,Water,Garbage,Healthcare,Education,Fire,Police }
    public sealed partial class CityModel
    {
        // The manual confirms budgets change capacity and service resources.
        // Range and linear response are provisional until original-game comparison.
        public int ServiceBudget(CityServiceKind kind)=>kind==CityServiceKind.Electricity?development.electricityBudget:
            kind==CityServiceKind.Water?development.waterBudget:kind==CityServiceKind.Healthcare?development.healthcareBudget:kind==CityServiceKind.Education?development.educationBudget:kind==CityServiceKind.Fire?development.fireBudget:kind==CityServiceKind.Police?development.policeBudget:development.garbageBudget;
        public bool SetServiceBudget(CityServiceKind kind,int percent)
        {
            if(!development.enabled || !Enum.IsDefined(typeof(CityServiceKind),kind) || percent<50 || percent>150)return false;
            int previous=ServiceBudget(kind);if(previous==percent)return true;
            if(kind==CityServiceKind.Electricity)development.electricityBudget=percent;
            else if(kind==CityServiceKind.Water)development.waterBudget=percent;else if(kind==CityServiceKind.Healthcare)development.healthcareBudget=percent;else if(kind==CityServiceKind.Education)development.educationBudget=percent;else if(kind==CityServiceKind.Fire)development.fireBudget=percent;else if(kind==CityServiceKind.Police)development.policeBudget=percent;else development.garbageBudget=percent;
            Recalculate();Trace("economy.service_budget","市政服务预算调整",new CityLogDetail{previous=previous,amount=percent,reason=kind.ToString()});return true;
        }
        public int ServiceCapacity(CityServiceKind kind,int standard)=>development.enabled?(int)Math.Floor(standard*ServiceBudget(kind)/100.0):standard;
        public int ServiceExpense(CityServiceKind kind,int standard)=>development.enabled?(int)Math.Ceiling(standard*ServiceBudget(kind)/100.0):standard;
        public int GarbageFleetLimit=>Math.Max(1,ServiceCapacity(CityServiceKind.Garbage,LandfillBuilding.Trucks));
    }
}
