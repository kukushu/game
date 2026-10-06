using System;
using HarborCity;
public static class DashboardVisualChecks
{
    static int checks;
    static void Check(bool yes,string why) {if(!yes)throw new Exception("Dashboard visuals: "+why);checks++;}
    static DashboardSeries Series(params DashboardPoint[] points)=>new DashboardSeries(points);
    public static void Run()
    {
        var empty=Series();var range=DashboardRange.For(empty);
        Check(empty.Known==0 && range.max>range.min && !double.IsNaN(range.Y(0)),"Empty histories have finite coordinates");
        Check(empty.Delta()=="历史不足","No trend is claimed for an empty history");
        var zero=Series(new DashboardPoint(4,0),new DashboardPoint(5,0));range=DashboardRange.For(zero);
        Check(zero.Known==2 && range.Y(0)==0 && range.max>0,"Observed zero remains data and flat zero does not divide by zero");
        var flat=Series(new DashboardPoint(4,25),new DashboardPoint(5,25));range=DashboardRange.For(flat);
        Check(range.Y(25)>0 && range.Y(25)<=1 && flat.Delta().EndsWith("  0"),"Constant nonzero values render without an invented increase");
        var negative=Series(new DashboardPoint(10,-800),new DashboardPoint(11,-400));range=DashboardRange.For(negative);
        Check(range.min<=-800 && range.max>=0 && range.Y(-800)<range.Y(-400),"Negative treasury preserves sign, ordering and a zero reference");
        var mixed=Series(new DashboardPoint(10,-100),new DashboardPoint(12,100));range=DashboardRange.For(mixed);
        Check(range.Y(-100)<range.Y(0) && range.Y(0)<range.Y(100),"Cash passing through zero uses one consistent axis");
        var days=Series(new DashboardPoint(1,10),new DashboardPoint(10,20),new DashboardPoint(30,30));range=DashboardRange.For(days);
        Check(range.X(1)==0 && range.X(30)==1 && Math.Abs(range.X(10)-9.0/29)<1e-9,"X positions use actual day distances, not row indexes");
        var one=Series(new DashboardPoint(13,4));range=DashboardRange.For(one);
        Check(range.X(13)==.5 && one.Delta()=="历史不足","One sample is a point, not an invented time trend");
        var gaps=Series(new DashboardPoint(1,6),new DashboardPoint(2,999,false),new DashboardPoint(3,0),new DashboardPoint(4,2,true,true));range=DashboardRange.For(gaps);
        Check(gaps.Known==3 && range.max<999 && !gaps.points[1].known && gaps.points[2].known,"Unobserved industrial days remain gaps; observed zero stays valid");
        Check(gaps.points[3].partial && !gaps.points[2].partial,"Partial-day points retain coverage flags for disconnected rendering");
        var invalid=Series(new DashboardPoint(1,double.NaN),new DashboardPoint(2,double.PositiveInfinity));range=DashboardRange.For(invalid);
        Check(invalid.Known==0 && range.max>range.min,"Invalid numeric samples cannot poison chart ranges");
        Check(DashboardPresentation.Ratio(0,0)==0 && DashboardPresentation.Ratio(10,0)==0,"Absent capacity never divides by zero");
        Check(DashboardPresentation.Ratio(18,24)==.75 && DashboardPresentation.Ratio(40,24)==1 && DashboardPresentation.Ratio(-1,24)==0,"Bars preserve real ratios and clip only their display extent");
        Check(DashboardPresentation.FindingTone(new AnalysisFinding {code="cash"})==DashboardTone.Critical,"Confirmed financing finding has warning emphasis");
        Check(DashboardPresentation.FindingTone(new AnalysisFinding {code="raw"})==DashboardTone.Attention,"Raw bottleneck gets attention without inventing severity thresholds");
        Check(DashboardPresentation.FindingTone(new AnalysisFinding {code="new_unknown_cause",severity=100000})==DashboardTone.Neutral,"Unknown finding remains neutral even with a large ranking score");
        Check(DashboardPresentation.StateTone("生产中")==DashboardTone.Positive && DashboardPresentation.StateTone("缺工")==DashboardTone.Neutral,"Only explicit positive/current states get positive emphasis");
        Check(!DashboardPresentation.ComparableDecision(new HouseholdDecisionSummary {evaluated=true,fromHome=-1,currentScore=-10000,proposedScore=61}),"A migrant's invalid current-option sentinel never becomes a huge claimed improvement");
        Check(!DashboardPresentation.ComparableDecision(new HouseholdDecisionSummary {evaluated=true,fromHome=3,currentRejection="租金不可负担"}) && !DashboardPresentation.ComparableDecision(new HouseholdDecisionSummary {evaluated=false,fromHome=3}),"Rejected or unassessed options cannot imply comparable utility gains");
        Check(DashboardPresentation.ComparableDecision(new HouseholdDecisionSummary {evaluated=true,fromHome=3,currentScore=72,proposedScore=81}),"Two valid evaluated home options permit a truthful score comparison");
        var c=TestCity.Create();new CityTraffic(c);c.analysis=new CityDailyAnalysis(c);var s=c.analysis.Refresh(true);
        string saved=TestCity.Snapshot(c);
        foreach(var f in s.findings)DashboardPresentation.FindingTone(f);
        DashboardRange.For(Series(new DashboardPoint(s.live.day,s.live.treasury)));
        DashboardPresentation.Ratio(s.live.employed,s.live.workforce);
        Check(TestCity.Snapshot(c)==saved,"Display math never writes simulation or saved state");
        var residence=new ResidencePresentation(s,s.housing.Count>0?s.housing[0].id:-1);
        Check(TestCity.Snapshot(c)==saved,"Residence aggregation only observes the snapshot");
        var rs=new CityAnalysisState();
        var child=new ResidentAnalysisState {canWork=false,jobId=-1};
        var worker=new ResidentAnalysisState {canWork=true,jobId=8};
        var jobless=new ResidentAnalysisState {canWork=true,jobId=-1};
        var a=new HouseholdAnalysisState {id=7,home=3,unit=1,resident=true,rent=24,employed=1,commute=126};a.members.AddRange(new[]{child,worker,jobless});
        var b=new HouseholdAnalysisState {id=6,home=3,unit=0,resident=true,rent=38};
        rs.households.AddRange(new[]{a,b,new HouseholdAnalysisState {id=8,home=3,resident=false,rent=999},new HouseholdAnalysisState {id=9,home=4,resident=true,rent=999}});
        residence=new ResidencePresentation(rs,3);
        Check(residence.families.Count==2 && residence.contractRent==62 && residence.employed==1 && residence.commute==126,"Summary includes only resident households in this home and uses their contracted rents");
        Check(residence.workforce==2 && residence.unemployed==1 && ResidencePresentation.Unemployed(a)==1,"Children are not labelled unemployed");
        Check(residence.families[0].id==6 && rs.households[0].id==7 && residence.Selected(8)==null,"Display sorting leaves source order intact and departed selection is discarded");
        Check(new ResidencePresentation(rs,99).families.Count==0 && ResidencePresentation.LongCommute(a),"Empty homes remain empty; a high total commute has an explicit presentation cue");
        Console.WriteLine("PASS: "+checks+" dashboard chart/visual semantics checks");
    }
}
