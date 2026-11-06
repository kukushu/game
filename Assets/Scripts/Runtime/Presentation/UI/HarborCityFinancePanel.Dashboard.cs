using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HarborCity
{
    public sealed partial class HarborCityFinancePanel
    {
        static readonly string[] Pages={"overview","diagnosis","cashflow","chain","decisions","housing","factory","retail","employment"};
        static readonly string[] Nodes={"households","shops","factories","trade"};
        const int RowsPerPage=12;
        internal HarborCityObservationPanel Observation {get;set;}
        CityCoreIndustrySession tracked;
        readonly List<CityLogEvent> decisions=new List<CityLogEvent>();
        readonly List<MoneyPoint> history=new List<MoneyPoint>();
        int page, node, rowPage, windowDays=1, category=-1, severity, selectedIssueBuilding, selectedIssueFamily, selectedDecision=-1;
        CityFeedbackKind selectedIssueKind;
        Action pendingLayout;
        void OnNextLayout(Action change)=>pendingLayout+=change;
        double nextList;
        int listPage=-1;
        readonly List<CityBuilding> buildingRows=new List<CityBuilding>();
        readonly Dictionary<int,int> occupants=new Dictionary<int,int>(), jobs=new Dictionary<int,int>();
        List<CityFeedbackEntry> issueRows=new List<CityFeedbackEntry>();
        string listSearch="";
        int listCategory, listSeverity;
        string search="";
        double nextHistory, nextHistoryWall;
        struct MoneyPoint
        {
            public double time, retail, imports, exports, wholesale, wages, tax, localEscrow, exportEscrow;
            public double homes, shops, factories, treasury, accruedWages, inflow, outflow;
            public double Total=>homes+shops+factories+treasury+accruedWages+localEscrow;
        }
        string D(string key,params object[] args)=>text.Get("ui.dashboard."+key,args);
        void Navigate(int target)
        {
            page=target; rowPage=0; scroll=Vector2.zero;listPage=-1;
        }
        internal void Observe(CityCoreIndustrySession city)
        {
            if(tracked==city)return;
            if(tracked!=null)tracked.City.EmploymentObserved-=RecordDecision;
            tracked=city;
            history.Clear();decisions.Clear();nextHistory=nextHistoryWall=0;
            buildingRows.Clear();occupants.Clear();jobs.Clear();issueRows.Clear();
            selectedDecision=-1;selectedIssueBuilding=selectedIssueFamily=0;
            pendingLayout=null;
            category=-1;severity=0;search="";Navigate(0);
            if(city!=null)city.City.EmploymentObserved+=RecordDecision;
        }
        void RecordDecision(CityLogEvent entry)
        {
            if(decisions.Count==256)decisions.RemoveAt(0);
            decisions.Add(new CityLogEvent{type=entry.type,day=entry.day,minute=entry.minute,
                simulationSeconds=entry.simulationSeconds,citizenId=entry.citizenId,buildingId=entry.buildingId});
        }
        internal void SampleHistory()
        {
            if(tracked==null||tracked.Simulation.Fault!=null)return;
            double time=tracked.Simulation.Seconds;
            double wall=Time.realtimeSinceStartupAsDouble;
            if(time<nextHistory||wall<nextHistoryWall)return;
            history.Add(ReadMoney(tracked));
            double day=tracked.City.society.settings.secondsPerDay;
            nextHistory=time+day/12;
            nextHistoryWall=wall+1;
            while(history.Count>1&&history[1].time<time-7*day)history.RemoveAt(0);
        }
        static MoneyPoint ReadMoney(CityCoreIndustrySession city)
        {
            var model=city.City;
            var trade=model.trade;
            var tax=city.Simulation.Finance.Snapshot();
            var point=new MoneyPoint{
                time=city.Simulation.Seconds, retail=model.consumption.RetailPayments,
                imports=trade.rawReceipts,exports=trade.exportPayments,exportEscrow=trade.exportFunded-trade.exportPayments,
                tax=tax.taxCollected,treasury=city.Simulation.Finance.Money,
                inflow=trade.exportPayments+model.development.externalInvestment+tax.loanIssued+tax.developmentGrants,
                outflow=trade.rawReceipts+tax.publicWorks+tax.maintenancePaid+tax.loanRepaid
            };
            foreach(var family in model.society.families)
            {
                point.homes+=family.savings;
                point.inflow+=family.openingSavings;
                foreach(var person in family.people)point.accruedWages+=person.wageCredit;
            }
            foreach(var building in city.Entities.Buildings)ReadAccount(building,ref point);
            foreach(var building in city.Entities.RetiredBuildings)ReadAccount(building,ref point);
            return point;
        }
        static void ReadAccount(CityBuilding building,ref MoneyPoint point)
        {
            if(building is IndustrialBuilding factory)
            {
                point.factories+=factory.factory.cash;
                point.wages+=factory.factory.wageCosts;
                point.outflow+=factory.factory.productionCosts;
            }
            else if(building is CommercialBuilding shop)
            {
                point.shops+=shop.Account.Cash;
                point.wages+=shop.Account.WageCosts;
                point.wholesale+=shop.wholesaleCosts;
                point.localEscrow+=shop.Account.WholesaleFunded-shop.wholesaleCosts;
            }
        }
        void Nav(int from,int count)
        {
            int columns=Bounds.width>=760?count:Math.Min(count,3);
            for(int start=0;start<count;start+=columns)
            {
                GUILayout.BeginHorizontal();
                for(int i=start;i<Math.Min(count,start+columns);i++)
                {
                    int target=from+i;
                    if(NavButton(D(Pages[target]),page==target||target==4&&page>4))OnNextLayout(()=>Navigate(target));
                }
                GUILayout.EndHorizontal();
            }
        }
        bool NavButton(string label,bool active)
        {
            var tint=GUI.backgroundColor;
            if(active)GUI.backgroundColor=new Color(.12f,.39f,.47f);
            bool clicked=GUILayout.Button(label,GUILayout.Height(32),GUILayout.ExpandWidth(true));
            GUI.backgroundColor=tint;
            return clicked;
        }
        void Header(string label)
        {
            sectionTitle??=new GUIStyle(GUI.skin.label){fontSize=19,fontStyle=FontStyle.Bold,wordWrap=true};
            GUILayout.Space(9);
            GUILayout.Label(label,sectionTitle);
        }
        GUIStyle sectionTitle;
        void Missing(string label)
        {
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label(label);
            GUILayout.Label(D("unavailable"));
            GUILayout.EndVertical();
        }
        void EmptyTable(string label,params string[] columns)
        {
            Header(label);
            GUILayout.BeginHorizontal(GUI.skin.box);
            foreach(string column in columns)GUILayout.Label(D(column),GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Space(8);
            GUILayout.Label(D("unavailable"));
            GUILayout.Space(8);
            GUILayout.EndVertical();
        }
        void Dashboard()
        {
            switch(page)
            {
                case 0: Overview();break;
                case 1: Diagnosis();break;
                case 2: CashFlow();break;
                case 3: Chain();break;
                case 4: GUILayout.Label(D("record_help"));Decisions(false);break;
                default: DecisionDetail();break;
            }
        }
        void Overview()
        {
            Header(D("status"));
            ImpactRow("ui.economy.food_empty",alerts?.foodEmpty,alerts?.households??0,
                "ui.economy.daily_empty",alerts?.dailyEmpty,alerts?.households??0);
            ImpactRow("ui.economy.shop_cash_empty",alerts?.cashShops,shops,
                "ui.economy.factory_cash_empty",alerts?.cashFactories,factories);
            StockRow();
            if(GUILayout.Button(D("diagnose"),GUILayout.Height(32)))OnNextLayout(()=>Navigate(1));
            bool wide=Bounds.width>=960;
            if(wide)
            {
                GUILayout.BeginHorizontal();
                GUILayout.BeginVertical(GUI.skin.box,GUILayout.Width(Bounds.width*.51f));
            }
            Header(D("chain"));
            for(int i=0;i<Nodes.Length;i++)
                if(GUILayout.Button(NodeLine(i),GUILayout.Height(34))){int target=i;OnNextLayout(()=>{node=target;Navigate(3);});}
            if(wide){GUILayout.EndVertical();GUILayout.BeginVertical(GUI.skin.box);}
            Header(D("trend"));
            Trend();
            if(wide){GUILayout.EndVertical();GUILayout.EndHorizontal();}
            Missing(D("health"));
        }
        string NodeLine(int i)
        {
            var city=session.City;
            switch(i)
            {
                case 0:return D("node_homes",alerts?.households.ToString("N0")??"—",householdCash,
                    session.Simulation.Consumption.Requested.food,session.Simulation.Consumption.Requested.dailyGoods);
                case 1:return D("node_shops_data",shops,shopCash,shopFood,shopDaily);
                case 2:return D("node_factories_data",factories,factoryCash,factoryRaw,factoryFood,factoryDaily);
                default:return D("node_trade_data",city.trade.rawDelivered,city.trade.goodsExported,city.trade.exportPayments-city.trade.rawReceipts);
            }
        }
        void Chain()
        {
            Header(D("chain"));
            if(Bounds.width>=760)GUILayout.BeginHorizontal();
            for(int i=0;i<Nodes.Length;i++)
                if(NavButton((i+1)+"  "+D("node_"+Nodes[i]),node==i)){int target=i;OnNextLayout(()=>node=target);}
            if(Bounds.width>=760)GUILayout.EndHorizontal();
            GUILayout.BeginVertical(GUI.skin.box);
            Header(D("node_"+Nodes[node]));
            GUILayout.Label(NodeLine(node));
            if(node==3)
            {
                var s=session.Simulation;
                GUILayout.Label(text.Get("ui.trade.orders",s.Trade.RemainingExportOrders(ProductKind.Food),s.Design.Economy.DailyExportOrders.food,
                    s.Trade.RemainingExportOrders(ProductKind.DailyGoods),s.Design.Economy.DailyExportOrders.dailyGoods));
            }
            var rows=(Observation?.EconomyIssues??Array.Empty<CityFeedbackEntry>()).Where(e=>NodeIssue(node,e)).Take(4);
            foreach(var issue in rows)GUILayout.Label(text.Get(issue.Key)+"  ·  "+ObjectName(issue.building));
            if(GUILayout.Button(D("diagnose")))OnNextLayout(()=>Navigate(1));
            GUILayout.EndVertical();
            EmptyTable(D("forecast"),"evidence_column","impact_column");
        }
        bool NodeIssue(int index,CityFeedbackEntry issue)
        {
            var building=session.Entities.Building(issue.building);
            return index==0?building is ResidentialBuilding:index==1?building is CommercialBuilding:index==2?building is IndustrialBuilding:false;
        }
        string ObjectName(int id)
        {
            var b=session.Entities.Building(id);
            return b==null?"#"+id:D("object",text.Get("ui.inspect.use."+b.Use.ToString().ToLowerInvariant()),id,b.x,b.z);
        }
        void LocateObject(int id,int family=0)
        {
            if(session.Entities.Building(id)==null||Observation==null)return;
            IsOpen=false;
            Observation.Locate(id,family);
        }
        void LocateButton(int id,int family=0)
        {
            bool previous=GUI.enabled;
            GUI.enabled=previous&&session.Entities.Building(id)!=null&&Observation!=null;
            if(GUILayout.Button(D("locate"),GUILayout.ExpandWidth(false)))LocateObject(id,family);
            GUI.enabled=previous;
        }
        void Diagnosis()
        {
            Header(D("diagnosis"));
            GUILayout.Label(D("search"));
            GUI.SetNextControlName("EconomyIssueSearch");
            string next=GUILayout.TextField(search,80);
            if(next!=search)OnNextLayout(()=>{search=next;rowPage=0;});
            if(NavButton(D("severity"+severity),severity!=0))OnNextLayout(()=>{severity=(severity+1)%3;rowPage=0;});
            int count=Enum.GetValues(typeof(CityFeedbackKind)).Length;
            if(NavButton(category<0?D("all_categories"):text.Get("ui.feedback.kind."+((CityFeedbackKind)category).ToString().ToLowerInvariant()),category>=0))
                OnNextLayout(()=>{category=category+1==count?-1:category+1;rowPage=0;});
            var issues=Observation?.EconomyIssues??Array.Empty<CityFeedbackEntry>();
            if(Observation==null||!string.IsNullOrEmpty(Observation.FeedbackError)){Missing(D("status"));return;}
            string term=search.Trim();
            if(Event.current.type==EventType.Layout&&(listPage!=page||listSearch!=term||listCategory!=category||listSeverity!=severity||Time.realtimeSinceStartupAsDouble>=nextList))
            {
                issueRows=issues.Where(e=>(category<0||(int)e.kind==category)&&(severity==0||e.severity==(severity==1?2:1))&&
                    (term.Length==0||ObjectName(e.building).IndexOf(term,StringComparison.OrdinalIgnoreCase)>=0||
                    (e.family>0&&e.family.ToString().IndexOf(term,StringComparison.OrdinalIgnoreCase)>=0)||
                    text.Get(e.Key).IndexOf(term,StringComparison.OrdinalIgnoreCase)>=0)).ToList();
                listPage=page;listSearch=term;listCategory=category;listSeverity=severity;nextList=Time.realtimeSinceStartupAsDouble+1;
            }
            var matches=issueRows;
            if(matches.Count==0)GUILayout.Label(D("no_issues"));
            PageControls(matches.Count);
            foreach(var issue in matches.Skip(rowPage*RowsPerPage).Take(RowsPerPage))
            {
                GUILayout.BeginHorizontal(GUI.skin.box);
                string label=D(issue.severity>=2?"critical":"warning")+"  ·  "+text.Get(issue.Key)+"  ·  "+ObjectName(issue.building);
                bool selected=selectedIssueBuilding==issue.building&&selectedIssueFamily==issue.family&&selectedIssueKind==issue.kind;
                if(NavButton(label,selected))
                    OnNextLayout(()=>{selectedIssueBuilding=issue.building;selectedIssueFamily=issue.family;selectedIssueKind=issue.kind;});
                LocateButton(issue.building,issue.family);
                GUILayout.EndHorizontal();
                if(selected)
                {
                    if(issue.family>0)GUILayout.Label(D("family",issue.family));
                    GUILayout.Label(D("affected",issue.affected));
                    if(!string.IsNullOrEmpty(issue.reason))GUILayout.Label(text.Get(issue.reason));
                    if(issue.hasProduct)GUILayout.Label(D("inventory",issue.stock,issue.replenishmentNeed));
                    if(issue.kind==CityFeedbackKind.Cash)GUILayout.Label(D("cash_value",issue.cash));
                }
            }
            EmptyTable(D("causes"),"evidence_column","impact_column","event_column");
        }
        void PageControls(int count)
        {
            int pages=Math.Max(1,(count+RowsPerPage-1)/RowsPerPage);
            rowPage=Math.Min(rowPage,pages-1);
            GUILayout.BeginHorizontal();
            if(rowPage>0&&GUILayout.Button("‹",GUILayout.Width(42)))OnNextLayout(()=>rowPage--);
            GUILayout.Label(D("pages",rowPage+1,pages,count));
            if(rowPage+1<pages&&GUILayout.Button("›",GUILayout.Width(42)))OnNextLayout(()=>rowPage++);
            GUILayout.EndHorizontal();
        }
        MoneyPoint Current=>history.Count>0?history[history.Count-1]:default;
        double GameDay=>session.City.society.settings.secondsPerDay;
        int HistoryStart()
        {
            int first=0;
            double cutoff=Current.time-GameDay*windowDays;
            while(first+1<history.Count&&history[first].time<cutoff)first++;
            return first;
        }
        void CashFlow()
        {
            Header(D("cashflow"));
            if(history.Count==0){Missing(D("cashflow"));return;}
            var now=Current;
            var start=history[HistoryStart()];
            GUILayout.Label(D("coverage",(now.time-start.time)*24/GameDay));
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label(D("balances",now.homes,now.shops,now.factories,now.treasury));
            if(now.time>start.time)
            {
                FlowLine("arrow_retail",now.retail-start.retail);
                FlowLine("arrow_wholesale",now.wholesale-start.wholesale);
                FlowLine("arrow_wages",now.wages-start.wages);
                FlowLine("arrow_tax",now.tax-start.tax);
                FlowLine("arrow_import",now.imports-start.imports);
                FlowLine("arrow_export",now.exports-start.exports);
                GUILayout.Label(D("trade_net",(now.exports-start.exports)-(now.imports-start.imports)));
                if(session.City.development.enabled)
                {
                    double delta=now.Total-start.Total,net=(now.inflow-start.inflow)-(now.outflow-start.outflow);
                    GUILayout.Label(D("reconcile",start.Total,now.Total,delta,net,delta-net));
                }
                else Missing(D("reconcile_title"));
            }
            else GUILayout.Label(D("collecting"));
            GUILayout.Label(D("escrow",now.localEscrow,now.exportEscrow,now.accruedWages));
            GUILayout.EndVertical();
            Header(D("trend"));
            Trend();
            EmptyTable(D("transactions"),"event_column","object_column","amount_column");
        }
        void FlowLine(string key,double amount)
        {
            GUILayout.BeginHorizontal(GUI.skin.box);
            GUILayout.Label(D(key));
            GUILayout.Label(amount.ToString("N0"),GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();
        }
        void Trend()
        {
            if(history.Count<2){GUILayout.Label(D("collecting"));return;}
            int first=HistoryStart();
            GUILayout.Label(D("coverage",(Current.time-history[first].time)*24/GameDay));
            if(first==history.Count-1){GUILayout.Label(D("collecting"));return;}
            Plot("arrow_retail",first,p=>p.retail,new Color(.38f,.72f,1f));
            Plot("arrow_import",first,p=>p.imports,new Color(1f,.74f,.38f));
            Plot("arrow_export",first,p=>p.exports,Stable);
        }
        void Plot(string label,int first,Func<MoneyPoint,double> read,Color color)
        {
            double max=1;
            for(int i=first+1;i<history.Count;i++)max=Math.Max(max,read(history[i])-read(history[i-1]));
            GUILayout.Label(D(label)+"  ·  "+D("plot_peak",max==1?Peak(first,read):max));
            Rect area=GUILayoutUtility.GetRect(0,50,GUILayout.ExpandWidth(true));
            Fill(area,Track);
            double cutoff=Current.time-windowDays*GameDay;
            Vector2 previous=default;
            for(int i=first+1;i<history.Count;i++)
            {
                float x=(float)((history[i].time-cutoff)/(windowDays*GameDay));
                float y=(float)((read(history[i])-read(history[i-1]))/max);
                var point=new Vector2(area.x+x*area.width,area.yMax-3-y*(area.height-6));
                if(i>first+1)
                {
                    var matrix=GUI.matrix;
                    var difference=point-previous;
                    GUIUtility.RotateAroundPivot(Mathf.Atan2(difference.y,difference.x)*Mathf.Rad2Deg,previous);
                    Fill(new Rect(previous.x,previous.y,difference.magnitude,2),color);
                    GUI.matrix=matrix;
                }
                Fill(new Rect(point.x-2,point.y-2,4,4),color);
                previous=point;
            }
        }
        double Peak(int first,Func<MoneyPoint,double> read)
        {
            double peak=0;
            for(int i=first+1;i<history.Count;i++)peak=Math.Max(peak,read(history[i])-read(history[i-1]));
            return peak;
        }
        void Decisions(bool employmentOnly)
        {
            Header(D("records"));
            double since=session.Simulation.Seconds-windowDays*GameDay;
            var visible=decisions.Where(e=>e.simulationSeconds>=since).Reverse().ToList();
            if(visible.Count==0){GUILayout.Label(D("no_records"));return;}
            if(employmentOnly)PageControls(visible.Count);
            foreach(var item in visible.Skip(employmentOnly?rowPage*RowsPerPage:0).Take(RowsPerPage))
            {
                var person=session.Entities.Member(item.citizenId)?.Person;
                string result=D(item.type=="job.assigned"?"assigned":"released");
                if(GUILayout.Button(D("record",item.day,item.minute/60,person?.name??("#"+item.citizenId),result,ObjectName(item.buildingId))))
                    OnNextLayout(()=>{selectedDecision=item.citizenId;if(!employmentOnly)Navigate(8);});
            }
        }
        void DecisionDetail()
        {
            Header(D(Pages[page]));
            switch(page)
            {
                case 5: Buildings<ResidentialBuilding>();CandidateTable("housing_candidates");break;
                case 6: Buildings<IndustrialBuilding>();CandidateTable("factory_candidates");break;
                case 7: Buildings<CommercialBuilding>();CandidateTable("retail_candidates");break;
                case 8:
                    var member=session.Entities.Member(selectedDecision);
                    if(member!=null)
                    {
                        var person=member.Person;
                        var job=session.City.ResidentJob(person);
                        GUILayout.Label(D("job_state",person.name,job?.id.ToString()??"—",job?.wage.ToString()??"—",
                            D(person.atWork?"at_work":person.tripId!=0?"travel":"off_work")));
                        LocateButton(job?.buildingId??0);
                    }
                    Decisions(true);
                    CandidateTable("job_candidates");
                    break;
            }
        }
        void CandidateTable(string title)=>EmptyTable(D(title),"object_column","score_column","reason_column");
        void Buildings<T>() where T:CityBuilding
        {
            if(Event.current.type==EventType.Layout&&(listPage!=page||Time.realtimeSinceStartupAsDouble>=nextList))
            {
                buildingRows.Clear();occupants.Clear();jobs.Clear();
                foreach(var building in session.Entities.Buildings)
                    if(building is T&&!building.abandoned&&!building.burned)buildingRows.Add(building);
                if(page==5)
                    foreach(var household in session.City.society.families)
                        if(household.resident){occupants.TryGetValue(household.home,out int count);occupants[household.home]=count+1;}
                if(page==7)
                    foreach(var job in session.City.society.jobEntities)
                    {jobs.TryGetValue(job.buildingId,out int count);jobs[job.buildingId]=count+1;}
                listPage=page;nextList=Time.realtimeSinceStartupAsDouble+1;
            }
            PageControls(buildingRows.Count);
            foreach(var b in buildingRows.Skip(rowPage*RowsPerPage).Take(RowsPerPage))
            {
                bool wide=Bounds.width>=760;
                if(wide)GUILayout.BeginHorizontal(GUI.skin.box);else GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Label(ObjectName(b.id));
                if(b is IndustrialBuilding factory)GUILayout.Label(D("factory_row",D(factory.factory.product==ProductKind.Food?"food":"daily"),factory.factory.cash,factory.factory.raw,factory.Stock));
                if(b is CommercialBuilding shop)
                {
                    jobs.TryGetValue(b.id,out int count);
                    GUILayout.Label(D("shop_row",shop.Account.Cash,shop.Products.food,shop.Products.dailyGoods,count));
                }
                if(b is ResidentialBuilding home)
                {
                    occupants.TryGetValue(b.id,out int count);
                    GUILayout.Label(D("housing_row",Math.Max(0,home.housingUnits-count),count));
                    if(Observation!=null)GUILayout.Label(Observation.FacilityStatus(b));
                }
                LocateButton(b.id);
                if(wide)GUILayout.EndHorizontal();else GUILayout.EndVertical();
            }
        }
    }
}
