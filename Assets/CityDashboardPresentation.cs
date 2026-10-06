using System;
using System.Collections.Generic;
namespace HarborCity
{
    // Display-only math. Never reads or writes CityModel.
    public enum DashboardTone { Neutral, Positive, Attention, Critical }
    public struct DashboardPoint
    {
        public int day;
        public double value;
        public bool known,partial;
        public DashboardPoint(int d,double v,bool observed=true,bool incomplete=false)
        {day=d;value=v;known=observed && Finite(v);partial=incomplete;}
        static bool Finite(double v)=>!double.IsNaN(v) && !double.IsInfinity(v);
    }
    public sealed class DashboardSeries
    {
        public readonly DashboardPoint[] points;
        public int Known {get;private set;}
        public DashboardSeries(DashboardPoint[] data)
        {points=data??Array.Empty<DashboardPoint>();foreach(var p in points)if(p.known)Known++;}
        public string Delta()
        {
            DashboardPoint first=default,last=default;bool found=false;
            foreach(var p in points)if(p.known) {if(!found)first=p;last=p;found=true;}
            if(Known<2)return "历史不足";
            double change=last.value-first.value;
            return "D"+first.day+"–"+last.day+"  "+(change>0?"+":"")+change.ToString("0.#");
        }
    }
    public struct DashboardRange
    {
        public double min,max;
        public int firstDay,lastDay;
        public double Y(double value)=>Math.Max(0,Math.Min(1,(value-min)/(max-min)));
        public double X(int day)=>lastDay==firstDay?.5:Math.Max(0,Math.Min(1,(double)(day-firstDay)/(lastDay-firstDay)));
        public static DashboardRange For(params DashboardSeries[] series)
        {
            double low=0,high=0;int first=int.MaxValue,last=int.MinValue;
            foreach(var s in series)foreach(var p in s.points)
            {
                first=Math.Min(first,p.day);last=Math.Max(last,p.day);
                if(p.known) {low=Math.Min(low,p.value);high=Math.Max(high,p.value);}
            }
            if(high==low)high=low+1;
            else {double pad=(high-low)*.06;high+=pad;if(low<0)low-=pad;}
            return new DashboardRange {min=low,max=high,firstDay=first==int.MaxValue?0:first,lastDay=last==int.MinValue?0:last};
        }
    }
    public static class DashboardPresentation
    {
        public static double Ratio(double value,double capacity)=>capacity>0?Math.Max(0,Math.Min(1,value/capacity)):0;
        public static bool ComparableDecision(HouseholdDecisionSummary d)=>d!=null && d.evaluated && d.fromHome>=0
            && string.IsNullOrEmpty(d.currentRejection) && string.IsNullOrEmpty(d.proposedRejection);
        public static DashboardTone FindingTone(AnalysisFinding f)
        {
            switch(f.code)
            {
                case "cash":case "invariant.cash":return DashboardTone.Critical;
                case "raw":case "stock":case "input_transport":case "output_transport":case "late":case "freight":
                case "commercial_empty":case "unemployment":case "rent":return DashboardTone.Attention;
                default:return DashboardTone.Neutral;
            }
        }
        public static DashboardTone StateTone(string state)
        {
            if(state=="资金耗尽" || state=="加工资金不足")return DashboardTone.Critical;
            if(state=="生产中" || state=="有库存")return DashboardTone.Positive;
            if(state!=null && (state.Contains("缺料") || state.Contains("仓满") || state.Contains("断") || state.Contains("缺货")))return DashboardTone.Attention;
            return DashboardTone.Neutral;
        }
    }
    // Aggregates only the supplied observation snapshot; never touches gameplay objects.
    public sealed class ResidencePresentation
    {
        public readonly List<HouseholdAnalysisState> families=new List<HouseholdAnalysisState>();
        public int employed,workforce,unemployed,contractRent;
        public float commute;
        public ResidencePresentation(CityAnalysisState state,int home)
        {
            foreach(var h in state.households)
            {
                if(!h.resident || h.home!=home)continue;
                families.Add(h);employed+=h.employed;contractRent+=h.rent;commute+=h.commute;
                foreach(var p in h.members)if(p.canWork) {workforce++;if(p.jobId<=0)unemployed++;}
            }
            families.Sort((a,b)=>a.unit!=b.unit?a.unit.CompareTo(b.unit):a.id.CompareTo(b.id));
        }
        public HouseholdAnalysisState Selected(int id)=>families.Find(h=>h.id==id);
        public static int Unemployed(HouseholdAnalysisState h)
        {int count=0;foreach(var p in h.members)if(p.canWork && p.jobId<=0)count++;return count;}
        // A visual cue, explicitly labelled as a total, not a simulation rule.
        public static bool LongCommute(HouseholdAnalysisState h)=>h.commute>=120;
    }
}
