using System;
using UnityEngine;
namespace HarborCity
{
    // UI owns only open/scroll/error state. All changes go through fiscal commands.
    public sealed class HarborCityFinancePanel
    {
        readonly CityLocalization text;CityCoreFinance finance;CityCoreUtilities services;Vector2 scroll;string message="";
        public bool IsOpen {get;set;}
        CityCoreIndustrySession session;
        CityFeedbackSummary alerts;
        bool treasury;
        double nextSample, openedAt, retailAtOpen, importsAtOpen, exportsAtOpen;
        double shopCash, factoryCash;
        long householdCash, shopFood, shopDaily, factoryFood, factoryDaily, factoryRaw;
        int shops, factories;
        Rect Bounds=>new Rect(Math.Max(10,(Screen.width-620)/2),96,Math.Min(620,Screen.width-20),Math.Max(160,Math.Min(650,Screen.height-152)));
        public HarborCityFinancePanel(CityLocalization text){this.text=text;}
        public bool BlocksInput(Vector2 screen)=>IsOpen; // A modal blocks map editing even outside its rectangle.
        enum Command {Tax,Budget,Borrow,Repay,Grant}
        void Run(Command command,int id=0,int amount=0){try{switch(command){case Command.Tax:finance.SetTax((LandUse)id,amount);break;case Command.Budget:services.SetBudget((CityServiceKind)id,amount);break;case Command.Borrow:finance.Borrow(id);break;case Command.Repay:finance.Repay(id);break;case Command.Grant:finance.GrantDevelopmentFunds();break;}message="";}catch(Exception e){message=text.Error(e);}}
        public void Open(bool showTreasury=false)
        {
            IsOpen=true;
            session=null;
            treasury=showTreasury;
        }
        public void Draw(CityCoreIndustrySession current,CityFeedbackSummary summary)
        {
            finance=current?.Simulation.Finance;
            services=current?.Simulation.Utilities;
            if(!IsOpen||current==null){IsOpen=false;session=null;alerts=null;return;}
            alerts=summary;
            if(session!=current)
            {
                session=current;
                scroll=Vector2.zero;
                message="";
                openedAt=current.Simulation.Seconds;
                retailAtOpen=current.City.consumption.RetailPayments;
                importsAtOpen=current.City.trade.rawReceipts;
                exportsAtOpen=current.City.trade.exportPayments;
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
        void Economy()
        {
            var city=session.City;
            var trade=city.trade;
            var consumption=city.consumption;
            var simulation=session.Simulation;
            GUILayout.Label(text.Get("ui.economy.stock"));
            GUILayout.Label(text.Get("ui.economy.households",householdCash));
            if(alerts!=null)GUILayout.Label(text.Get("ui.economy.shortage",alerts.householdFood,alerts.foodEmpty,alerts.dailyEmpty));
            GUILayout.Label(text.Get("ui.economy.shops",shops,shopCash,shopFood,shopDaily,alerts?.cashShops??0));
            GUILayout.Label(text.Get("ui.economy.factories",factories,factoryCash,factoryRaw,factoryFood,factoryDaily,alerts?.cashFactories??0));
            GUILayout.Space(12);
            GUILayout.Label(text.Get("ui.economy.since_open",(simulation.Seconds-openedAt)*24/city.society.settings.secondsPerDay));
            GUILayout.Label(text.Get("ui.economy.flows",consumption.RetailPayments-retailAtOpen,trade.rawReceipts-importsAtOpen,trade.exportPayments-exportsAtOpen));
            GUILayout.Space(12);
            GUILayout.Label(text.Get("ui.economy.cumulative"));
            GUILayout.Label(text.Get("ui.economy.flows",consumption.RetailPayments,trade.rawReceipts,trade.exportPayments));
            GUILayout.Label(text.Get("ui.economy.volume",consumption.Bought.food,consumption.Bought.dailyGoods,trade.rawDelivered,trade.ExportedProducts.food,trade.ExportedProducts.dailyGoods));
            GUILayout.Label(text.Get("ui.trade.orders",simulation.Trade.RemainingExportOrders(ProductKind.Food),simulation.Design.Economy.DailyExportOrders.food,simulation.Trade.RemainingExportOrders(ProductKind.DailyGoods),simulation.Design.Economy.DailyExportOrders.dailyGoods));
        }
        void Contents(int window)
        {
            // IMGUI windows have their own GUI state. Apply the HUD tint inside the window.
            var background=GUI.backgroundColor;
            GUI.backgroundColor=new Color(.16f,.24f,.30f);
            try{DrawContents();}
            finally{GUI.backgroundColor=background;}
        }
        void DrawContents()
        {
            if(GUILayout.Button(text.Get("ui.finance.close"))){IsOpen=false;return;}
            GUILayout.BeginHorizontal();
            if(GUILayout.Button(text.Get("ui.economy.overview"))&&treasury){treasury=false;scroll=Vector2.zero;nextSample=0;}
            if(GUILayout.Button(text.Get("ui.finance.title"))&&!treasury){treasury=true;scroll=Vector2.zero;}
            GUILayout.EndHorizontal();
            scroll=GUILayout.BeginScrollView(scroll);
            if(!treasury){Economy();GUILayout.EndScrollView();return;}
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
