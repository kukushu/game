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
        public int ServiceCapacity(CityServiceKind kind,int standard)=>development.enabled?(int)Math.Floor(standard*ServiceBudget(kind)/100.0):standard;
        public int ServiceExpense(CityServiceKind kind,int standard)=>development.enabled?(int)Math.Ceiling(standard*ServiceBudget(kind)/100.0):standard;
    }
}
