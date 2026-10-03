using System;
using System.Linq;

namespace HarborCity
{
    [Serializable] public sealed class FactoryReport {public FactoryState state; public int goods;}
    [Serializable] public sealed class FactoryState
    {
        public const int RawCapacity=24, GoodsCapacity=24;
        public const float LabourPerItem=60;
        public const int StartingCash=1200, RawPrice=8, SalePrice=45, ProcessingCost=2;
        public bool financeInitialized;
        public bool capitalFromTreasury;
        public double cash, capital, salesRevenue, rawCosts, wageCosts, productionCosts, unpaidWages;
        public int sold;
        public double Profit => salesRevenue-rawCosts-wageCosts-productionCosts;
        public double CashError => cash-(capital+salesRevenue-rawCosts-wageCosts-productionCosts);
        public int raw, produced, consumed, imported, shipped, discardedBatches;
        public bool initialized, processing;
        public float progress, attendanceMinutes, productiveMinutes;
        // Derived, saved activity histories. Noise fades quickly; pollution dissipates more slowly.
        public float noise, pollution;
        public string status="缺工";
        [NonSerialized] public float stepAttendance, stepProductive;
    }
    [Serializable] public sealed class LocalEnvironment
    {
        public float noise, pollution, heavyTraffic;
    }
    public sealed partial class CityModel
    {
        public void EnableIndustry()
        {
            if(society==null) return;
            foreach(var b in buildings) if(tiles[b.id]==4)
            {
                if(b.factory==null) b.factory=new FactoryState();
                b.factory.initialized=true;
                if(!b.factory.financeInitialized && version<7)
                {
                    b.factory.financeInitialized=true;
                    b.factory.cash=b.factory.capital=FactoryState.StartingCash;
                    Trace("factory.capital","旧厂一次性期初经营资金；不是销售收入，不自动补款",b.factory,building:b.id);
                }
            }
            if(version>=7) return;
            if(version<6) foreach(var h in society.families) InitializeEnvironmentPreferences(h);
            version=7;
            Trace("industry.migrated","启用工厂现金收支；保留库存、行程和旧版已挣工资");
        }
        void InitializeEnvironmentPreferences(Household h)
        {
            h.noiseSensitivity=.2f+(h.id*37%101)/125f;
            h.pollutionSensitivity=.2f+(h.id*53%101)/125f;
            h.trafficSensitivity=.2f+(h.id*71%101)/125f;
        }
        public FactoryState Factory(int id) => buildings!=null && id>=0 && id<tiles.Length && tiles[id]==4?buildings[id].factory:null;
        public bool JobFunded(CityJob job) => job!=null && (Factory(job.buildingId)==null || Factory(job.buildingId).cash>0);
        public int ExpectedJobWage(CityJob job)
        {
            if(job==null) return 0;
            var f=Factory(job.buildingId);
            return f==null?job.wage:(int)Math.Min(job.wage,Math.Floor(f.cash/Math.Max(1,EmployedAt(job.buildingId))));
        }
        // Employer cash becomes a saved citizen credit immediately. Day-end transfers
        // whole units to savings and carries fractions, never rounding into new money.
        public void PayAttendance(CityResident person,float minutes)
        {
            double due=(double)minutes/480*ResidentWage(person);
            var f=Factory(Workplace(person));
            if(f==null) {person.earnedWages+=(float)due; return;}
            double paid=Math.Min(f.cash,due);
            f.cash-=paid; f.wageCosts+=paid; f.unpaidWages+=due-paid;
            person.factoryWageCredit+=paid;
        }
        public void RecordFactorySale(int id,int amount,int trip,int buyer=-1)
        {
            var f=Factory(id); if(f==null || amount<=0) return;
            double revenue=amount*FactoryState.SalePrice;
            f.cash+=revenue; f.salesRevenue+=revenue; f.sold+=amount;
            ObserveAnalysisSale(id,buyer);
            Trace("factory.sale","商品实际交付；买方简化外部经营资金支付货款",f,building:id,trip:trip);
        }
        // Called only with the minutes actually credited by the resident attendance system.
        public void ProcessFactoryWork(CityResident person,float minutes,float actualAttendance=-1)
        {
            int id=Workplace(person); var f=Factory(id);
            float attendance=actualAttendance<0?minutes:actualAttendance;
            if(f==null || attendance<=0 || !person.canWork || !person.atWork || person.tripId!=0 || person.location!=id || person.arrivedDay!=day) return;
            f.attendanceMinutes+=attendance; f.stepAttendance+=attendance;
            float productiveBefore=f.productiveMinutes, rawLost=0, stockLost=0, fundsLost=Math.Max(0,attendance-minutes);
            while(minutes>.000001f)
            {
                if(traffic.stock[id]>=FactoryState.GoodsCapacity) {stockLost=minutes; break;}
                if(!f.processing)
                {
                    if(f.raw==0) {rawLost=minutes; break;}
                    if(f.cash<FactoryState.ProcessingCost) {fundsLost+=minutes; break;}
                    f.cash-=FactoryState.ProcessingCost; f.productionCosts+=FactoryState.ProcessingCost;
                    f.raw--; f.consumed++; f.processing=true; f.progress=0;
                    Trace("factory.input","领用一份原料开始加工",f,building:id,citizen:person.id,job:person.jobId,household:person.householdId);
                }
                float work=Math.Min(minutes,FactoryState.LabourPerItem-f.progress);
                f.progress+=work; f.productiveMinutes+=work; f.stepProductive+=work; minutes-=work;
                if(f.progress>=FactoryState.LabourPerItem-.00001f)
                {
                    traffic.stock[id]++; f.produced++; f.progress=0; f.processing=false;
                    Trace("factory.produced","实际劳动完成一件商品",f,building:id);
                }
                else break;
            }
            ObserveAnalysisLabour(id,person.id,attendance,f.productiveMinutes-productiveBefore,rawLost,stockLost,fundsLost);
        }
        static float Falloff(float distance,float radius) => Math.Max(0,1-distance/radius);
        public LocalEnvironment EnvironmentAt(int home)
        {
            var result=new LocalEnvironment();
            if(home<0 || home>=buildings.Count) return result;
            var target=buildings[home]; result.heavyTraffic=target.heavyTraffic;
            foreach(var b in buildings)
            {
                var f=b.factory; if(f==null || !f.initialized) continue;
                float distance=(float)Math.Sqrt((b.x-target.x)*(b.x-target.x)+(b.z-target.z)*(b.z-target.z));
                result.noise+=f.noise*Falloff(distance,24);
                result.pollution+=f.pollution*Falloff(distance,45);
            }
            return result;
        }
        public void AdvanceIndustry(float minutes)
        {
            if(version<6 || minutes<=0) return;
            float noiseKeep=(float)Math.Exp(-minutes/60), pollutionKeep=(float)Math.Exp(-minutes/720), trafficKeep=(float)Math.Exp(-minutes/120);
            foreach(var b in buildings)
            {
                var f=b.factory;
                if(f!=null && f.initialized)
                {
                    // Normalised worker-equivalents from actual processing, not job occupancy.
                    float activity=f.stepProductive/minutes/4;
                    f.noise=f.noise*noiseKeep+activity*(1-noiseKeep);
                    f.pollution=f.pollution*pollutionKeep+activity*(1-pollutionKeep);
                    string state=tiles[b.id]!=4?"已拆除":f.cash<=0?"资金耗尽":!f.processing && f.raw>0 && f.cash<FactoryState.ProcessingCost?"加工资金不足":f.stepAttendance==0?"缺工":traffic.stock[b.id]>=FactoryState.GoodsCapacity?"成品仓满":!f.processing && f.raw==0?"缺料":"生产中";
                    if(state!=f.status) {f.status=state; Trace("factory.status",state,f,building:b.id);}
                    f.stepAttendance=f.stepProductive=0;
                }
                if(tiles[b.id]!=2) continue;
                float exposure=0;
                foreach(var t in traffic.trips)
                {
                    if(t.purpose<TripPurpose.Delivery || t.status==TripStatus.Visiting) continue;
                    var a=roads.Node(t.Current); var end=roads.Node(t.Next); if(a==null || end==null) continue;
                    float x=a.x+(end.x-a.x)*t.progress, z=a.z+(end.z-a.z)*t.progress;
                    float distance=(float)Math.Sqrt((x-b.x)*(x-b.x)+(z-b.z)*(z-b.z));
                    exposure+=Falloff(distance,12);
                }
                b.heavyTraffic=b.heavyTraffic*trafficKeep+exposure*(1-trafficKeep);
            }
        }
        public bool ValidIndustry()
        {
            double employerPayroll=0;
            foreach(var b in buildings)
            {
                if(!IndustryNumber(b.heavyTraffic)) return false;
                var f=b.factory;
                if(tiles[b.id]==4 && (f==null || !f.initialized)) return false;
                if(f==null) continue;
                employerPayroll+=f.wageCosts;
                if(version>=7 && (!f.financeInitialized || f.sold<0 || Math.Abs(f.CashError)>.0001)) return false;
                foreach(double n in new[]{f.cash,f.capital,f.salesRevenue,f.rawCosts,f.wageCosts,f.productionCosts,f.unpaidWages}) if(!FinanceNumber(n)) return false;
                if(f.raw<0 || f.raw>FactoryState.RawCapacity || f.produced<0 || f.consumed<0 || f.imported<0 || f.shipped<0 || f.discardedBatches<0
                    || f.consumed!=f.produced+(f.processing?1:0)+f.discardedBatches
                    || f.progress<0 || f.progress>=FactoryState.LabourPerItem || !f.processing && f.progress!=0
                    || !IndustryNumber(f.progress) || !IndustryNumber(f.attendanceMinutes) || !IndustryNumber(f.productiveMinutes)
                    || !IndustryNumber(f.noise) || !IndustryNumber(f.pollution) || f.productiveMinutes>f.attendanceMinutes+.1f) return false;
            }
            foreach(var h in society.families)
                foreach(float n in new[]{h.noiseSensitivity,h.pollutionSensitivity,h.trafficSensitivity}) if(!IndustryNumber(n) || n>1) return false;
            if(version>=7 && Math.Abs(employerPayroll-society.families.SelectMany(h=>h.people).Sum(p=>p.totalFactoryWagesPaid+p.factoryWageCredit))>.0001) return false;
            return true;
        }
        static bool IndustryNumber(float n) => !float.IsNaN(n) && !float.IsInfinity(n) && n>=0;
        static bool FinanceNumber(double n) => !double.IsNaN(n) && !double.IsInfinity(n) && n>=0;
    }
}
