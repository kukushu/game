using System;
using UnityEngine;
namespace HarborCity
{
    // The overview samples existing accounts. All writes go through fiscal commands.
    public sealed partial class HarborCityFinancePanel
    {
        readonly CityLocalization text;CityCoreFinance finance;CityCoreUtilities services;Vector2 scroll;string message="";
        public bool IsOpen {get;set;}
        CityCoreIndustrySession session;
        CityFeedbackSummary alerts;
        bool treasury;
        GUIStyle metricValue;
        internal bool IsEditingSearch {get;private set;}
        static readonly Color Alert=new Color(1f,.52f,.45f);
        static readonly Color Stable=new Color(.45f,.85f,.77f);
        static readonly Color Track=new Color(.19f,.27f,.33f);
        double nextSample;
        double shopCash, factoryCash;
        long householdCash, shopFood, shopDaily, factoryFood, factoryDaily, factoryRaw;
        int shops, factories;
        Rect Bounds
        {
            get
            {
                float width=Math.Min(1440,Screen.width-24);
                return new Rect((Screen.width-width)/2,24,width,Screen.height-48);
            }
        }
        public HarborCityFinancePanel(CityLocalization text){this.text=text;}
        public bool BlocksInput(Vector2 screen)=>IsOpen; // A modal blocks map editing even outside its rectangle.
        enum Command {Tax,Budget,Borrow,Repay,Grant}
        void Run(Command command,int id=0,int amount=0){try{switch(command){case Command.Tax:finance.SetTax((LandUse)id,amount);break;case Command.Budget:services.SetBudget((CityServiceKind)id,amount);break;case Command.Borrow:finance.Borrow(id);break;case Command.Repay:finance.Repay(id);break;case Command.Grant:finance.GrantDevelopmentFunds();break;}message="";}catch(Exception e){message=text.Error(e);}}
        public void Open(bool showTreasury=false)
        {
            IsOpen=true;
            session=null;
            treasury=showTreasury;
            Navigate(0);
        }
        public void Draw(CityCoreIndustrySession current,CityFeedbackSummary summary)
        {
            finance=current?.Simulation.Finance;
            services=current?.Simulation.Utilities;
            if(!IsOpen||current==null){IsOpen=false;IsEditingSearch=false;session=null;alerts=null;pendingLayout=null;return;}
            alerts=summary;
            if(session!=current)
            {
                session=current;
                scroll=Vector2.zero;
                message="";
                nextSample=0;
            }
            if(!treasury&&Time.realtimeSinceStartupAsDouble>=nextSample)
            {
                SampleBalances();
                nextSample=Time.realtimeSinceStartupAsDouble+1;
            }
            var previous=GUI.color;var content=GUI.contentColor;GUI.color=new Color(.09f,.12f,.14f,1);GUI.DrawTexture(Bounds,Texture2D.whiteTexture);GUI.color=previous;GUI.contentColor=Color.white;GUI.ModalWindow(93441,Bounds,Contents,text.Get("ui.economy.title"));GUI.contentColor=content;
        }
        void SampleBalances()
        {
            shopCash=factoryCash=0;
            householdCash=shopFood=shopDaily=factoryFood=factoryDaily=factoryRaw=0;
            shops=factories=0;
            foreach(var family in session.City.society.families)
                if(family.resident)householdCash+=family.savings;
            foreach(var building in session.Entities.Buildings)
            {
                if(building.abandoned||building.burned)continue;
                if(building is CommercialBuilding shop)
                {
                    shops++;
                    shopCash+=shop.Account.Cash;
                    shopFood+=shop.Products.food;
                    shopDaily+=shop.Products.dailyGoods;
                }
                else if(building is IndustrialBuilding factory)
                {
                    factories++;
                    factoryCash+=factory.factory.cash;
                    factoryFood+=factory.Products.food;
                    factoryDaily+=factory.Products.dailyGoods;
                    factoryRaw+=factory.factory.raw;
                }
            }
        }
        static void Fill(Rect rect,Color color)
        {
            var previous=GUI.color;
            GUI.color=color;
            GUI.DrawTexture(rect,Texture2D.whiteTexture);
            GUI.color=previous;
        }
        void Metric(Rect rect,string label,string value,Color color,float fraction=-1)
        {
            Fill(rect,new Color(.10f,.16f,.20f));
            Fill(new Rect(rect.x,rect.y,3,rect.height),color);
            GUI.Label(new Rect(rect.x+10,rect.y+5,rect.width-20,32),label);
            metricValue??=new GUIStyle(GUI.skin.label){fontSize=24,fontStyle=FontStyle.Bold,wordWrap=false};
            metricValue.normal.textColor=color;
            GUI.Label(new Rect(rect.x+10,rect.y+37,rect.width-20,30),value,metricValue);
            if(fraction<0)return;
            var bar=new Rect(rect.x+10,rect.yMax-9,Math.Max(0,rect.width-20),4);
            Fill(bar,Track);
            bar.width*=Mathf.Clamp01(fraction);
            Fill(bar,color);
        }
        void ImpactRow(string left,int? affected,int total,string right,int? otherAffected,int otherTotal)
        {
            var row=GUILayoutUtility.GetRect(0,76,GUILayout.ExpandWidth(true));
            float width=(row.width-6)/2;
            Impact(new Rect(row.x,row.y,width,row.height),left,affected,total);
            Impact(new Rect(row.x+width+6,row.y,width,row.height),right,otherAffected,otherTotal);
            GUILayout.Space(5);
        }
        void Impact(Rect rect,string label,int? affected,int total)
        {
            Metric(rect,text.Get(label),affected.HasValue?$"{affected.Value:N0} / {total:N0}":"—",
                affected>0?Alert:affected.HasValue?Stable:Track,
                affected.HasValue&&total>0?(float)affected.Value/total:-1);
        }
        void StockRow()
        {
            var row=GUILayoutUtility.GetRect(0,72,GUILayout.ExpandWidth(true));
            float width=(row.width-6)/2;
            Metric(new Rect(row.x,row.y,width,row.height),text.Get("ui.economy.shop_food"),
                shops>0?shopFood.ToString("N0"):"—",shops>0?(shopFood==0?Alert:Stable):Track);
            Metric(new Rect(row.x+width+6,row.y,width,row.height),text.Get("ui.economy.shop_daily"),
                shops>0?shopDaily.ToString("N0"):"—",shops>0?(shopDaily==0?Alert:Stable):Track);
        }
        void Contents(int window)
        {
            // IMGUI windows have their own GUI state. Apply the HUD tint inside the window.
            var background=GUI.backgroundColor;
            GUI.backgroundColor=new Color(.16f,.24f,.30f);
            try{DrawContents();IsEditingSearch=IsOpen&&!treasury&&page==1&&GUI.GetNameOfFocusedControl()=="EconomyIssueSearch";}
            finally{GUI.backgroundColor=background;}
        }
        void DrawContents()
        {
            if(Event.current.type==EventType.Layout)
            {
                var change=pendingLayout;
                pendingLayout=null;
                change?.Invoke();
            }
            if(GUILayout.Button(text.Get("ui.finance.close"))){IsOpen=false;return;}
            GUILayout.BeginHorizontal();
            if(GUILayout.Button(text.Get("ui.economy.overview"))&&treasury)OnNextLayout(()=>{treasury=false;Navigate(0);nextSample=0;});
            if(GUILayout.Button(text.Get("ui.finance.title"))&&!treasury)OnNextLayout(()=>{treasury=true;scroll=Vector2.zero;});
            GUILayout.EndHorizontal();
            if(!treasury)
            {
                Nav(0,5);
                if(page>=4)Nav(5,4);
                GUILayout.BeginHorizontal();
                if(NavButton(D("one_day"),windowDays==1))OnNextLayout(()=>{windowDays=1;rowPage=0;});
                if(NavButton(D("seven_days"),windowDays==7))OnNextLayout(()=>{windowDays=7;rowPage=0;});
                GUILayout.EndHorizontal();
            }
            scroll=GUILayout.BeginScrollView(scroll);
            if(!treasury){Dashboard();GUILayout.EndScrollView();return;}
            GUILayout.Label(text.Get("ui.finance.summary",finance.UnlimitedFunds?"∞":finance.Money.ToString(),finance.LastTax,finance.LastMaintenance,finance.LastLoanPayment,finance.Debt));
            GUILayout.Label(text.Get("ui.finance.upkeep_rate",finance.MaintenanceRate,finance.MaintenanceAccrued.ToString("F1")));
            GUILayout.Label(text.Get("ui.finance.assessment_note",finance.UncollectedAssessment));
            foreach(var use in new[]{LandUse.Residential,LandUse.Commercial,LandUse.Industrial})
            {GUILayout.BeginHorizontal();GUILayout.Label(text.Get("ui.finance.zone."+use.ToString().ToLowerInvariant())+" "+finance.Tax(use)+"%");if(GUILayout.Button("−",GUILayout.Width(50)))Run(Command.Tax,(int)use,Math.Max(0,finance.Tax(use)-1));if(GUILayout.Button("+",GUILayout.Width(50)))Run(Command.Tax,(int)use,Math.Min(29,finance.Tax(use)+1));GUILayout.EndHorizontal();}
            foreach(var kind in new[]{CityServiceKind.Electricity,CityServiceKind.Water})
            {GUILayout.BeginHorizontal();GUILayout.Label(text.Get("ui.finance.service."+kind.ToString().ToLowerInvariant())+" "+finance.Budget(kind)+"%");if(GUILayout.Button("−",GUILayout.Width(50)))Run(Command.Budget,(int)kind,Math.Max(50,finance.Budget(kind)-10));if(GUILayout.Button("+",GUILayout.Width(50)))Run(Command.Budget,(int)kind,Math.Min(150,finance.Budget(kind)+10));GUILayout.EndHorizontal();}
            foreach(var offer in finance.Offers)
            {
                GUILayout.Label(text.Get("ui.finance.loan_offer",text.Get("ui.finance.loan."+offer.Id),offer.Principal,offer.Total,offer.Days));int active=finance.ActiveLoanId(offer.Id);
                if(active==0){if(GUILayout.Button(text.Get("ui.finance.borrow")))Run(Command.Borrow,offer.Id);}
                else if(GUILayout.Button(text.Get("ui.finance.repay",finance.Remaining(active))))Run(Command.Repay,active);
            }
            if(Debug.isDebugBuild&&GUILayout.Button(text.Get("ui.finance.grant")))Run(Command.Grant);
            GUILayout.Label(message);GUILayout.EndScrollView();
        }
    }
}
