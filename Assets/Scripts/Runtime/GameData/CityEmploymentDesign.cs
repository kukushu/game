using System;
namespace HarborCity
{
    [Serializable] public sealed class EmploymentDefinition
    {
        public int baseWage=95,wageStep=12,wageVariants=5,industrialBonus=20,slotsPerLevel=4;
        public int startupWorkers=4;
        public int newCityDaySeconds=120;
        public int shiftStartMinute=480,commercialShiftStartMinute=660,shiftMinutes=480,staggerMinutes=30,staggerGroups=3;
    }
    public sealed class EmploymentRules
    {
        readonly EmploymentDefinition data;
        public int SlotsPerLevel=>data.slotsPerLevel;
        public int StartupWorkers=>data.startupWorkers;
        public int NewCityDaySeconds=>data.newCityDaySeconds;
        public int ShiftMinutes=>data.shiftMinutes;
        public int ShiftStart(int citizen,bool commercial=false)=>(commercial?data.commercialShiftStartMinute:data.shiftStartMinute)+citizen%data.staggerGroups*data.staggerMinutes;
        public int Wage(int building,bool industrial)=>data.baseWage+building%data.wageVariants*data.wageStep+(industrial?data.industrialBonus:0);
        internal EmploymentRules(EmploymentDefinition definition)
        {
            if(definition==null||definition.baseWage<0||definition.wageStep<0||definition.industrialBonus<0||definition.wageVariants<1||definition.wageVariants>100||definition.slotsPerLevel<1||definition.slotsPerLevel>100||definition.shiftStartMinute<0||definition.shiftMinutes<1||definition.staggerMinutes<0||definition.staggerGroups<1||definition.staggerGroups>100||(long)definition.shiftStartMinute+definition.shiftMinutes+(long)(definition.staggerGroups-1)*definition.staggerMinutes>1440||(long)definition.baseWage+(long)definition.wageVariants*definition.wageStep+definition.industrialBonus>1000000)throw new ArgumentException("Invalid employment design data");
            if(definition.startupWorkers<0||definition.startupWorkers>1000)throw new ArgumentException("Invalid startup workforce");
            if(definition.newCityDaySeconds<60||definition.newCityDaySeconds>86400)throw new ArgumentException("Invalid new city day length");
            if(definition.commercialShiftStartMinute<0||(long)definition.commercialShiftStartMinute+definition.shiftMinutes+(long)(definition.staggerGroups-1)*definition.staggerMinutes>1440)throw new ArgumentException("Commercial shift crosses unsupported day boundary");data=Copy(definition);
        }
        static EmploymentDefinition Copy(EmploymentDefinition d)=>new EmploymentDefinition{newCityDaySeconds=d.newCityDaySeconds,startupWorkers=d.startupWorkers,baseWage=d.baseWage,wageStep=d.wageStep,wageVariants=d.wageVariants,industrialBonus=d.industrialBonus,slotsPerLevel=d.slotsPerLevel,shiftStartMinute=d.shiftStartMinute,commercialShiftStartMinute=d.commercialShiftStartMinute,shiftMinutes=d.shiftMinutes,staggerMinutes=d.staggerMinutes,staggerGroups=d.staggerGroups};
        internal EmploymentDefinition Snapshot()=>Copy(data);
    }
}
