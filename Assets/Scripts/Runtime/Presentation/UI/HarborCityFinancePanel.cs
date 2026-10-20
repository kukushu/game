using System;
using UnityEngine;
namespace HarborCity
{
    // UI owns only open/scroll/error state. All changes go through fiscal commands.
    public sealed class HarborCityFinancePanel
    {
        readonly CityLocalization text;CityCoreFinance finance;CityCoreUtilities services;Vector2 scroll;string message="";
        public bool IsOpen {get;set;}
        Rect Bounds=>new Rect(Math.Max(10,(Screen.width-620)/2),96,Math.Min(620,Screen.width-20),Math.Max(160,Math.Min(650,Screen.height-152)));
        public HarborCityFinancePanel(CityLocalization text){this.text=text;}
        public bool BlocksInput(Vector2 screen)=>IsOpen; // A modal blocks map editing even outside its rectangle.
        enum Command {Tax,Budget,Borrow,Repay,Grant}
        void Run(Command command,int id=0,int amount=0){try{switch(command){case Command.Tax:finance.SetTax((LandUse)id,amount);break;case Command.Budget:services.SetBudget((CityServiceKind)id,amount);break;case Command.Borrow:finance.Borrow(id);break;case Command.Repay:finance.Repay(id);break;case Command.Grant:finance.GrantDevelopmentFunds();break;}message="";}catch(Exception e){message=text.Error(e);}}
        public void Draw(CitySimulation current)
        {if(finance!=current?.Finance)message="";finance=current?.Finance;services=current?.Utilities;if(finance==null){IsOpen=false;return;}if(!IsOpen)return;var previous=GUI.color;var content=GUI.contentColor;GUI.color=new Color(.09f,.12f,.14f,1);GUI.DrawTexture(Bounds,Texture2D.whiteTexture);GUI.color=previous;GUI.contentColor=Color.white;GUI.ModalWindow(93441,Bounds,Contents,text.Get("ui.finance.title"));GUI.contentColor=content;}
        void Contents(int window)
        {
            if(GUILayout.Button(text.Get("ui.finance.close"))){IsOpen=false;return;}
            GUILayout.Label(text.Get("ui.finance.summary",finance.UnlimitedFunds?"∞":finance.Money.ToString(),finance.LastTax,finance.LastMaintenance,finance.LastLoanPayment,finance.Debt));
            GUILayout.Label(text.Get("ui.finance.upkeep_rate",finance.MaintenanceRate,finance.MaintenanceAccrued.ToString("F1")));
            GUILayout.Label(text.Get("ui.finance.assessment_note",finance.UncollectedAssessment));
            scroll=GUILayout.BeginScrollView(scroll);
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
