using UnityEngine;

namespace HarborCity
{
    public sealed partial class HarborCityGame
    {
        bool showEconomy;
        int economyTab;
        void DrawDemand(float x,float y)
        {
            var values=new[]{city.residentialDemand,city.commercialDemand,city.industrialDemand};var captions=new[]{"住宅","商业","工业"};
            for(int n=0;n<3;n++)
            {
                float left=x+n*72;GUI.Label(new Rect(left,y,70,24),captions[n]+" "+values[n],small);
                Panel(new Rect(left,y+25,64,7),new Color(.2f,.27f,.29f));Panel(new Rect(left,y+25,64*values[n]/100f,7),palette[n+2]);
            }
        }
        void EconomyPanel(Color navy)
        {
            if(!showEconomy || !city.development.enabled) return;
            Panel(new Rect(360,120,330,490),navy);GUI.Label(new Rect(376,135,220,28),"城市财政",title);
            if(GUI.Button(new Rect(635,135,38,28),"×",button)) showEconomy=false;
            economyTab=GUI.Toolbar(new Rect(376,174,298,27),economyTab,new[]{"税率","服务预算"},button);
            var state=city.development;
            if(economyTab==0)
            {
                state.residentialTax=TaxSlider(207,"住宅",state.residentialTax);
                state.commercialTax=TaxSlider(249,"商业",state.commercialTax);
                state.industrialTax=TaxSlider(291,"工业",state.industrialTax);
                GUI.Label(new Rect(376,346,298,66),"上日税收 ¥"+city.income+" / 当前公共维护 ¥"+city.upkeep+"\n普通建筑由区域生长，建设不花城市资金。\n私营住宅不计入政府维护支出。",small);
            }
            else
            {
                BudgetSlider(207,"电力",CityServiceKind.Electricity);
                BudgetSlider(249,"水务",CityServiceKind.Water);
                BudgetSlider(291,"垃圾",CityServiceKind.Garbage);
                BudgetSlider(333,"医疗",CityServiceKind.Healthcare);
                BudgetSlider(375,"教育",CityServiceKind.Education);
                BudgetSlider(417,"消防",CityServiceKind.Fire);
                BudgetSlider(473,"警察",CityServiceKind.Police);
                GUI.Label(new Rect(376,529,298,66),"当前公共维护 ¥"+city.upkeep+"\n供电容量 "+city.power+" / 供水容量 "+city.water+"\n垃圾车 "+city.GarbageFleetLimit+" · 消防车 "+city.FireFleetLimit+" · 小学学位 "+city.SchoolCapacity,small);
            }
        }
        void BudgetSlider(float y,string caption,CityServiceKind kind)
        {
            int value=city.ServiceBudget(kind);GUI.Label(new Rect(376,y,298,24),caption+"预算  "+value+"%",small);
            int next=Mathf.RoundToInt(GUI.HorizontalSlider(new Rect(376,y+25,298,15),value,50,150)/5)*5;
            if(next!=value && city.SetServiceBudget(kind,next))notice=caption+"预算调整为 "+next+"%；容量立即更新，维护费用下次日结扣除。";
        }
        int TaxSlider(float y,string caption,int value)
        {
            GUI.Label(new Rect(376,y,298,25),caption+"税率  "+value+"%",small);
            int next=Mathf.RoundToInt(GUI.HorizontalSlider(new Rect(376,y+26,298,18),value,0,29));
            if(next!=value) {notice=caption+"税率调整为 "+next+"%，下次日结生效。";city.Trace("economy.tax_changed",notice,new CityLogDetail{previous=value,amount=next,reason=caption});}
            return next;
        }
    }
}
