using System;
using System.Linq;
using System.Collections.Generic;

namespace HarborCity
{
    [Serializable] public sealed class FactoryReport {public FactoryState state; public int goods;}
    [Serializable] public sealed class FactoryState
    {
        public const int RawCapacity=24, GoodsCapacity=24;
        public const float LabourPerItem=60;
        public const int StartingCash=1200, RawPrice=8, SalePrice=45, ProcessingCost=2;
        public double cash, capital, salesRevenue, rawCosts, wageCosts, productionCosts, unpaidWages,taxCosts;
        public int sold;
        public double Profit => salesRevenue-rawCosts-wageCosts-productionCosts-taxCosts;
        public double CashError => cash-(capital+salesRevenue-rawCosts-wageCosts-productionCosts-taxCosts);
        public int raw, produced, consumed, imported, shipped, discardedBatches;
        public bool processing;
        public float progress;
        public double attendanceMinutes, productiveMinutes;
        // Derived, saved activity histories. Noise fades quickly; pollution dissipates more slowly.
        public float noise, pollution;
        public string status="缺工";
        [NonSerialized] public float stepAttendance, stepProductive;
    }
    [Serializable] public sealed class IndustrialExposure {public float x,z,noise,pollution;}
    [Serializable] public sealed class LocalEnvironment
    {
        public float noise, pollution, heavyTraffic;
    }
    public sealed partial class CityModel
    {
        
        void InitializeEnvironmentPreferences(Household h)
        {
            h.noiseSensitivity=.2f+(h.id*37%101)/125f;
            h.pollutionSensitivity=.2f+(h.id*53%101)/125f;
            h.trafficSensitivity=.2f+(h.id*71%101)/125f;
        }
        public FactoryState Factory(int id) => (GetBuilding(id) as IndustrialBuilding)?.factory;
        public bool JobFunded(CityJob job)
        {
            if(job==null)return false;
            if(entities!=null&&development.enabled){var b=GetBuilding(job.buildingId);return b is IndustrialBuilding factory?factory.factory.cash>0:b is CommercialBuilding shop&&shop.Account.Cash>0;}
            return development.enabled||Factory(job.buildingId)==null||Factory(job.buildingId).cash>0;
        }
        public int ExpectedJobWage(CityJob job)
        {
            if(job==null) return 0;
            var f=Factory(job.buildingId);
            return f==null || development.enabled?job.wage:(int)Math.Min(job.wage,Math.Floor(f.cash/Math.Max(1,EmployedAt(job.buildingId))));
        }
        // Employer cash becomes a saved citizen credit immediately. Day-end transfers
        // whole units to savings and carries fractions, never rounding into new money.
        public void PayAttendance(CityResident person,float minutes)
        {
            double due=(double)minutes/480*ResidentWage(person);
            var f=Factory(Workplace(person));
            if(f==null || development.enabled) {person.earnedWages+=(float)due; return;}
            double paid=Math.Min(f.cash,due);
            f.cash-=paid; f.wageCosts+=paid; f.unpaidWages+=due-paid;
            person.wageCredit+=paid;
        }
        public int TransferEmploymentPay(CityResident person)
        {int pay=(int)Math.Floor(person.wageCredit);person.wageCredit-=pay;person.wagesPaid+=pay;return pay;}
        public void RecordFactorySale(int id,int amount,int trip,int buyer=-1,bool notify=true,double unitPrice=FactoryState.SalePrice)
        {
            var f=Factory(id); if(f==null || amount<=0) return;
            double revenue=amount*unitPrice;
            f.cash+=revenue; f.salesRevenue+=revenue; f.sold+=amount;
            if(notify){ObserveAnalysisSale(id,buyer);Trace("factory.sale","商品实际交付；买方简化外部经营资金支付货款",f,building:id,trip:trip);}
        }
        // Called only with the minutes actually credited by the resident attendance system.
        public void ProcessFactoryWork(CityResident person,float minutes,float actualAttendance=-1,ProductionRules rules=null)
        {
            int id=Workplace(person); var f=Factory(id);
            float attendance=actualAttendance<0?minutes:actualAttendance;
            if(f==null || attendance<=0 || !person.canWork || !person.atWork || person.tripId!=0 || person.location!=id || person.arrivedDay!=day) return;
            f.attendanceMinutes+=attendance; f.stepAttendance+=attendance;
            double productiveBefore=f.productiveMinutes; float rawLost=0, stockLost=0, fundsLost=Math.Max(0,attendance-minutes);
            if(development.enabled && (GetBuilding(id).burning || GetBuilding(id).burned || !Supply(id).Complete)) {f.status=BuildingConditionLabel(id);return;}
            while(minutes>.000001f)
            {
                if(Inventory(id).Stock>=FactoryState.GoodsCapacity) {stockLost=minutes; break;}
                if(!f.processing)
                {
                    if(f.raw==0) {rawLost=minutes; break;}
                    if(!development.enabled||rules!=null)
                    {
                        double cost=rules?.UnitProcessingCost??FactoryState.ProcessingCost;if(f.cash<cost) {fundsLost+=minutes; break;}
                        f.cash-=cost; f.productionCosts+=cost;
                    }
                    f.raw--; f.consumed++; f.processing=true; f.progress=0;
                    Trace("factory.input","领用一份原料开始加工",f,building:id,citizen:person.id,job:person.jobId,household:person.householdId);
                }
                float rate=FactoryProcessingRate(id);
                float required=rules?.LabourMinutesPerItem??FactoryState.LabourPerItem;float work=Math.Min(minutes,(required-f.progress)/rate);
                f.progress+=work*rate; f.productiveMinutes+=work; f.stepProductive+=work; minutes-=work;
                if(f.progress>=required-.00001f)
                {
                    Inventory(id).Stock++; f.produced++; f.progress=0; f.processing=false;
                    Trace("factory.produced","实际劳动完成一件商品",f,building:id);
                }
                else break;
            }
            ObserveAnalysisLabour(id,person.id,attendance,(float)(f.productiveMinutes-productiveBefore),rawLost,stockLost,fundsLost);
        }
        static float Falloff(float distance,float radius) => Math.Max(0,1-distance/radius);
        public LocalEnvironment EnvironmentAt(int home)
        {
            var target=Residence(home);if(target==null)return new LocalEnvironment();
            var result=EnvironmentAt(target.x,target.z);result.heavyTraffic=target.heavyTraffic;return result;
        }
        public LocalEnvironment EnvironmentAt(float x,float z)
        {
            var result=new LocalEnvironment();
            foreach(var b in buildings)
            {
                var f=(b as IndustrialBuilding)?.factory; if(f==null) continue;
                float distance=(float)Math.Sqrt((b.x-x)*(b.x-x)+(b.z-z)*(b.z-z));
                result.noise+=f.noise*Falloff(distance,24);
                result.pollution+=f.pollution*Falloff(distance,45);
            }
            foreach(var old in residualIndustry)
            {
                float distance=(float)Math.Sqrt((old.x-x)*(old.x-x)+(old.z-z)*(old.z-z));
                result.noise+=old.noise*Falloff(distance,24);result.pollution+=old.pollution*Falloff(distance,45);
            }
            return result;
        }
        public double retiredFactoryWages;
        public List<IndustrialExposure> residualIndustry=new List<IndustrialExposure>();
        public void AdvanceIndustry(float minutes)
        {
            if(minutes<=0) return;
            float noiseKeep=(float)Math.Exp(-minutes/60), pollutionKeep=(float)Math.Exp(-minutes/720), trafficKeep=(float)Math.Exp(-minutes/120);
            foreach(var old in residualIndustry) {old.noise*=noiseKeep;old.pollution*=pollutionKeep;}
            residualIndustry.RemoveAll(e=>e.noise<.00001f && e.pollution<.00001f);
            foreach(var b in buildings)
            {
                var f=(b as IndustrialBuilding)?.factory;
                if(f!=null)
                {
                    // Normalised worker-equivalents from actual processing, not job occupancy.
                    float activity=f.stepProductive/minutes/4;
                    f.noise=f.noise*noiseKeep+activity*(1-noiseKeep);
                    f.pollution=f.pollution*pollutionKeep+activity*(1-pollutionKeep)/(development.enabled?1+.3f*(b.level-1):1);
                    string state=b.abandoned?"已废弃":development.enabled && !Supply(b.id).Complete?Supply(b.id).Problem:!development.enabled && f.cash<=0?"资金耗尽":!development.enabled && !f.processing && f.raw>0 && f.cash<FactoryState.ProcessingCost?"加工资金不足":f.stepAttendance==0?"缺工":Inventory(b.id).Stock>=FactoryState.GoodsCapacity?"成品仓满":!f.processing && f.raw==0?"缺料":"生产中";
                    if(state!=f.status) {f.status=state; Trace("factory.status",state,f,building:b.id);}
                    f.stepAttendance=f.stepProductive=0;
                }
                if(UseOf(b.id)!=LandUse.Residential) continue;
                float exposure=0;
                foreach(var t in traffic.trips)
                {
                    if(t.purpose<TripPurpose.Delivery || t.status==TripStatus.Visiting) continue;
                    var a=roads.Node(t.Current); var end=roads.Node(t.Next); if(a==null || end==null) continue;
                    float x=a.x+(end.x-a.x)*t.progress, z=a.z+(end.z-a.z)*t.progress;
                    float distance=(float)Math.Sqrt((x-b.x)*(x-b.x)+(z-b.z)*(z-b.z));
                    exposure+=Falloff(distance,12);
                }
                var home=(ResidentialBuilding)b; home.heavyTraffic=home.heavyTraffic*trafficKeep+exposure*(1-trafficKeep);
            }
        }
        public bool ValidIndustry()
        {
            if(!FinanceNumber(retiredFactoryWages) || residualIndustry==null || residualIndustry.Any(e=>e==null || !FinitePosition(e.x) || !FinitePosition(e.z) || !IndustryNumber(e.noise) || !IndustryNumber(e.pollution))) return false;
            double employerPayroll=entities==null?retiredFactoryWages:entities.RetiredBuildings.OfType<IndustrialBuilding>().Sum(b=>b.factory.wageCosts)+entities.RetiredBuildings.OfType<CommercialBuilding>().Sum(b=>b.Account.WageCosts);
            foreach(var b in buildings)
            {
                if(b is ResidentialBuilding residential && !IndustryNumber(residential.heavyTraffic)) return false;
                var f=(b as IndustrialBuilding)?.factory;
                if(UseOf(b.id)==LandUse.Industrial && (f==null)) return false;
                if(f==null) continue;
                employerPayroll+=f.wageCosts;
                if((f.sold<0 || Math.Abs(f.CashError)>.0001)) return false;
                foreach(double n in new[]{f.cash,f.capital,f.salesRevenue,f.rawCosts,f.wageCosts,f.productionCosts,f.unpaidWages,f.taxCosts}) if(!FinanceNumber(n)) return false;
                if(f.raw<0 || f.raw>FactoryState.RawCapacity || f.produced<0 || f.consumed<0 || f.imported<0 || f.shipped<0 || f.discardedBatches<0
                    || f.consumed!=f.produced+(f.processing?1:0)+f.discardedBatches
                    || f.progress<0 || f.progress>=(entities!=null&&development.enabled?entities.Production.LabourMinutesPerItem:FactoryState.LabourPerItem) || !f.processing && f.progress!=0
                    || !IndustryNumber(f.progress) || !FinanceNumber(f.attendanceMinutes) || !FinanceNumber(f.productiveMinutes)
                    || !IndustryNumber(f.noise) || !IndustryNumber(f.pollution) || f.productiveMinutes>f.attendanceMinutes+.1f) return false;
            }
            foreach(var h in society.families)
                foreach(float n in new[]{h.noiseSensitivity,h.pollutionSensitivity,h.trafficSensitivity}) if(!IndustryNumber(n) || n>1) return false;
            if(entities!=null&&development.enabled)employerPayroll+=buildings.OfType<CommercialBuilding>().Sum(b=>b.Account.WageCosts);
            if(Math.Abs(employerPayroll-society.retiredWagesPaid-society.families.SelectMany(h=>h.people).Sum(p=>p.wagesPaid+p.wageCredit))>.0001) return false;
            return true;
        }
        static bool IndustryNumber(float n) => !float.IsNaN(n) && !float.IsInfinity(n) && n>=0;
        static bool FinanceNumber(double n) => !double.IsNaN(n) && !double.IsInfinity(n) && n>=0;
    }
}

