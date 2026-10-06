using System;
using System.Collections.Generic;
using UnityEngine;
namespace HarborCity
{
    public sealed partial class HarborCityGame
    {
        GUIStyle dashCaption,dashValue,dashNote,dashHeading,dashBadge;
        readonly Color dashSurface=new Color(.075f,.14f,.18f),dashTrack=new Color(.12f,.21f,.26f);
        readonly Color dashBlue=new Color(.45f,.69f,.81f),dashGreen=new Color(.35f,.81f,.63f),dashAmber=new Color(1,.73f,.32f),dashRed=new Color(1,.43f,.43f);
        CityHistorySample chartLastHistory;
        int chartHistoryCount=-1;
        sealed class ChartPaint
        {
            public Texture2D texture;
            public Color32[] pixels;
            public DashboardSeries first,second;
            public int width,height;
        }
        readonly Dictionary<string,ChartPaint> chartPaints=new Dictionary<string,ChartPaint>();
        void ReleaseDashboardTexture(Texture2D texture)
        {if(Application.isPlaying)Destroy(texture);else DestroyImmediate(texture);}
        void ReleaseDashboardGraphics()
        {foreach(var paint in chartPaints.Values)if(paint.texture!=null)ReleaseDashboardTexture(paint.texture);chartPaints.Clear();}
        DashboardSeries chartPopulation,chartEmployment,chartTreasury,chartCommute,chartProduction,chartSales;
        DashboardSeries sparkPopulation,sparkTreasury,sparkCommute;
        bool showAllFindings,showHistoryRows;
        void DashboardStyles()
        {
            if(dashCaption!=null)return;
            dashCaption=new GUIStyle(small) {fontSize=13,wordWrap=false,clipping=TextClipping.Clip};
            dashValue=new GUIStyle(number) {fontSize=25,wordWrap=false,clipping=TextClipping.Clip};
            dashNote=new GUIStyle(small) {fontSize=12,wordWrap=false,clipping=TextClipping.Clip};
            dashHeading=new GUIStyle(label) {fontSize=17,fontStyle=FontStyle.Bold};
            dashBadge=new GUIStyle(dashNote) {alignment=TextAnchor.MiddleCenter};
        }
        Color ToneColor(DashboardTone tone)=>tone==DashboardTone.Critical?dashRed:tone==DashboardTone.Attention?dashAmber:tone==DashboardTone.Positive?dashGreen:dashBlue;
        void Fill(Rect rect,Color color)
        {if(Event.current.type!=EventType.Repaint)return;var previous=GUI.color;GUI.color=color;GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=previous;}
        void Ink(Rect rect,string text,GUIStyle style,Color? color=null,string tooltip=null)
        {var previous=GUI.contentColor;if(color.HasValue)GUI.contentColor=color.Value;GUI.Label(rect,new GUIContent(text,tooltip),style);GUI.contentColor=previous;}
        Rect Block(float height)=>GUILayoutUtility.GetRect(0,height,GUILayout.ExpandWidth(true));
        void SectionCard(string title,string caption=null)
        {
            DashboardStyles();GUILayout.Space(9);Rect r=Block(30);Fill(r,dashSurface);
            Ink(new Rect(r.x+10,r.y+4,r.width-20,23),title,dashHeading);
            if(caption!=null)Ink(new Rect(r.x+r.width*.45f,r.y+6,r.width*.53f,20),caption,dashNote);
        }
        void StatusBadge(Rect rect,string text,DashboardTone tone)
        {DashboardStyles();Color color=ToneColor(tone);Fill(rect,new Color(color.r*.18f,color.g*.18f,color.b*.18f));Ink(rect,text,dashBadge,color);}
        void ProgressBar(Rect r,double amount,double capacity,Color color)
        {Fill(r,dashTrack);float width=(float)DashboardPresentation.Ratio(amount,capacity)*r.width;if(width>0)Fill(new Rect(r.x,r.y,width,r.height),color);}
        void ProgressMetric(string name,double amount,double capacity,DashboardTone tone=DashboardTone.Neutral,string suffix=null)
        {
            DashboardStyles();Rect r=Block(37);Ink(new Rect(r.x,r.y,115,21),name,dashCaption);
            Ink(new Rect(r.x+116,r.y,r.width-116,21),amount.ToString("0.#")+" / "+capacity.ToString("0.#")+(suffix==null?"":" · "+suffix),dashCaption);
            ProgressBar(new Rect(r.x,r.y+25,r.width,6),amount,capacity,ToneColor(tone));
        }
        void MiniBar(string name,double value,double max,DashboardTone tone,string formatted=null)
        {
            DashboardStyles();Rect r=Block(26);float labelWidth=Math.Min(125,r.width*.25f),numberWidth=Math.Min(105,r.width*.26f);
            Ink(new Rect(r.x,r.y,labelWidth,23),name,dashCaption);
            ProgressBar(new Rect(r.x+labelWidth,r.y+7,Math.Max(1,r.width-labelWidth-numberWidth-12),10),Math.Abs(value),max,ToneColor(tone));
            Ink(new Rect(r.x+r.width-numberWidth,r.y,numberWidth,23),formatted??value.ToString("0.#"),dashCaption);
        }
        void MetricCard(Rect r,string caption,string value,string note,DashboardTone tone=DashboardTone.Neutral,DashboardSeries spark=null,double amount=0,double capacity=0)
        {
            DashboardStyles();Fill(r,dashSurface);Fill(new Rect(r.x,r.y,3,r.height),ToneColor(tone));
            Ink(new Rect(r.x+11,r.y+7,r.width-19,19),caption,dashCaption);
            int valueSize=dashValue.fontSize;
            float textWidth=dashValue.CalcSize(new GUIContent(value)).x;
            if(textWidth>r.width-19)dashValue.fontSize=Math.Max(14,(int)(valueSize*(r.width-19)/textWidth));
            Ink(new Rect(r.x+11,r.y+27,r.width-19,33),value,dashValue,ToneColor(tone),value);
            dashValue.fontSize=valueSize;
            Ink(new Rect(r.x+11,r.y+62,r.width-19,19),note,dashNote,tooltip:note);
            if(spark!=null)Sparkline(new Rect(r.x+11,r.y+84,r.width-22,22),spark,ToneColor(tone),caption);
            else if(capacity>0)ProgressBar(new Rect(r.x+11,r.y+94,r.width-22,6),amount,capacity,ToneColor(tone));
        }
        Rect CardCell(Rect row,int index,int count)
        {float width=(row.width-(count-1)*8)/count;return new Rect(row.x+index*(width+8),row.y,width,row.height);}
        void FieldRow(string name,string value,DashboardTone tone=DashboardTone.Neutral)
        {DashboardStyles();Rect r=Block(25);Ink(new Rect(r.x,r.y,r.width*.36f,23),name,dashCaption);Ink(new Rect(r.x+r.width*.36f,r.y,r.width*.64f,23),value,dashCaption,ToneColor(tone));}
        void PlotPixel(ChartPaint paint,int x,int y,Color32 color)
        {if(x>=0 && x<paint.width && y>=0 && y<paint.height)paint.pixels[y*paint.width+x]=color;}
        void PlotLine(ChartPaint paint,int ax,int ay,int bx,int by,Color32 color)
        {
            int count=Math.Max(Math.Abs(bx-ax),Math.Abs(by-ay));
            for(int i=0;i<=count;i++)
            {
                double t=count==0?0:(double)i/count;int x=(int)Math.Round(ax+(bx-ax)*t),y=(int)Math.Round(ay+(by-ay)*t);
                PlotPixel(paint,x,y,color);PlotPixel(paint,x,y+1,color);
            }
        }
        void PaintSeries(ChartPaint paint,DashboardSeries series,DashboardRange range,Color color)
        {
            for(int i=0;i<series.points.Length;i++)
            {
                var p=series.points[i];if(!p.known)continue;
                int x=(int)Math.Round(range.X(p.day)*(paint.width-1)),y=(int)Math.Round(range.Y(p.value)*(paint.height-1));
                if(i>0 && series.points[i-1].known && !p.partial && !series.points[i-1].partial)
                {
                    var before=series.points[i-1];PlotLine(paint,(int)Math.Round(range.X(before.day)*(paint.width-1)),(int)Math.Round(range.Y(before.value)*(paint.height-1)),x,y,color);
                }
                Color32 dot=p.partial?dashAmber:color;
                for(int px=x-1;px<=x+1;px++)for(int py=y-1;py<=y+1;py++)PlotPixel(paint,px,py,dot);
            }
        }
        void DrawPlot(string key,Rect plot,DashboardSeries first,DashboardRange range,Color color,DashboardSeries second=null)
        {
            if(Event.current.type!=EventType.Repaint)return;
            if(!chartPaints.TryGetValue(key,out var paint)) {paint=new ChartPaint();chartPaints.Add(key,paint);}
            int width=Math.Max(2,Math.Min(2048,(int)Math.Ceiling(plot.width))),height=Math.Max(2,Math.Min(256,(int)Math.Ceiling(plot.height)));
            if(paint.texture==null || paint.width!=width || paint.height!=height)
            {
                if(paint.texture!=null)ReleaseDashboardTexture(paint.texture);
                paint.width=width;paint.height=height;paint.pixels=new Color32[width*height];
                paint.texture=new Texture2D(width,height,TextureFormat.RGBA32,false) {hideFlags=HideFlags.HideAndDontSave,filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
                paint.first=null;
            }
            if(!ReferenceEquals(paint.first,first) || !ReferenceEquals(paint.second,second))
            {
                Array.Clear(paint.pixels,0,paint.pixels.Length);PaintSeries(paint,first,range,color);
                if(second!=null)PaintSeries(paint,second,range,dashGreen);
                paint.texture.SetPixels32(paint.pixels);paint.texture.Apply(false,false);paint.first=first;paint.second=second;
            }
            GUI.DrawTexture(plot,paint.texture,ScaleMode.StretchToFill,true);
        }
        void Sparkline(Rect r,DashboardSeries series,Color color,string key)
        {
            if(series.Known<2) {Ink(r,series.Known==1?"仅 1 个日结样本":"暂无日结历史",dashNote);return;}
            DrawPlot("spark:"+key,r,series,DashboardRange.For(series),color);
        }
        void LineChart(string title,DashboardSeries first,string firstLabel,DashboardSeries second=null,string secondLabel=null,string unit="",string note=null)
        {
            DashboardStyles();Rect r=Block(190);Fill(r,dashSurface);
            Ink(new Rect(r.x+12,r.y+8,r.width-24,22),title,dashHeading);
            Ink(new Rect(r.x+12,r.y+34,r.width-24,18),firstLabel+(second==null?"":"   /   "+secondLabel)+" · "+unit,dashNote,dashBlue);
            if(first.Known+(second?.Known??0)==0)
            {
                Ink(new Rect(r.x+16,r.y+83,r.width-32,24),first.points.Length==0?"暂无已结束日历史":"这段历史没有已观测数据",dashCaption);
                Ink(new Rect(r.x+16,r.y+145,r.width-32,24),note??"不会补造数据或趋势",dashNote);
                GUILayout.Space(8);return;
            }
            var range=second==null?DashboardRange.For(first):DashboardRange.For(first,second);
            var plot=new Rect(r.x+68,r.y+62,Math.Max(1,r.width-88),94);
            for(int i=0;i<3;i++)
            {
                float y=plot.y+i*plot.height/2;Fill(new Rect(plot.x,y,plot.width,1),dashTrack);
                Ink(new Rect(r.x+8,y-8,56,18),(range.max-(range.max-range.min)*i/2).ToString("0.#"),dashNote);
            }
            if(range.min<0 && range.max>0)Fill(new Rect(plot.x,plot.yMax-(float)range.Y(0)*plot.height,plot.width,1),dashBlue);
            DrawPlot("history:"+title,plot,first,range,dashBlue,second);
            Ink(new Rect(plot.x,plot.yMax+3,90,18),"D"+range.firstDay,dashNote);
            Ink(new Rect(plot.xMax-64,plot.yMax+3,64,18),"D"+range.lastDay,dashNote);
            Ink(new Rect(r.x+12,r.y+171,r.width-24,17),note??(first.Known<2?"至少 2 个样本才形成趋势":"鼠标移入查看范围；纵轴从实际数据缩放"),dashNote);
            if(plot.Contains(Event.current.mousePosition))
            {
                double best=double.MaxValue;DashboardPoint nearest=default;
                foreach(var p in first.points)if(p.known) {double distance=Math.Abs(plot.x+range.X(p.day)*plot.width-Event.current.mousePosition.x);if(distance<best) {best=distance;nearest=p;}}
                if(best<double.MaxValue)Ink(new Rect(plot.x+4,plot.y+3,plot.width-8,21),"D"+nearest.day+" · "+firstLabel+" "+nearest.value.ToString("0.#")+(nearest.partial?"（部分日）":""),dashCaption);
            }
            GUILayout.Space(8);
        }
        DashboardSeries HistorySeries(CityAnalysisState s,Func<CityHistorySample,double> read,int days,bool industrial=false)
        {
            int start=Math.Max(0,s.history.Count-days);var points=new DashboardPoint[s.history.Count-start];
            for(int i=start;i<s.history.Count;i++) {var h=s.history[i];points[i-start]=new DashboardPoint(h.day,read(h),!industrial || h.industryKnown,industrial && h.partial);}
            return new DashboardSeries(points);
        }
        void PrepareCharts(CityAnalysisState s)
        {
            var last=s.history.Count==0?null:s.history[s.history.Count-1];
            if(chartHistoryCount==s.history.Count && ReferenceEquals(chartLastHistory,last))return;
            chartHistoryCount=s.history.Count;chartLastHistory=last;
            chartPopulation=HistorySeries(s,h=>h.population,180);chartEmployment=HistorySeries(s,h=>h.employed,180);
            chartTreasury=HistorySeries(s,h=>h.treasury,180);chartCommute=HistorySeries(s,h=>h.averageCommute,180);
            chartProduction=HistorySeries(s,h=>h.produced,180,true);chartSales=HistorySeries(s,h=>h.sold,180,true);
            sparkPopulation=HistorySeries(s,h=>h.population,30);
            sparkTreasury=HistorySeries(s,h=>h.treasury,30);sparkCommute=HistorySeries(s,h=>h.averageCommute,30);
        }
        DashboardTone FindingVisualTone(AnalysisFinding finding,CityAnalysisState s)
        {
            if(finding.code=="household_change")
            {
                var d=FindingDecision(finding,s);
                if(d!=null && d.committed && DashboardPresentation.ComparableDecision(d) && d.proposedScore>d.currentScore)return DashboardTone.Positive;
            }
            return DashboardPresentation.FindingTone(finding);
        }
        HouseholdDecisionSummary FindingDecision(AnalysisFinding finding,CityAnalysisState s)
        {return s.currentDay.households.Find(d=>d.id==finding.entityId && d.committed)??s.lastDay?.households.Find(d=>d.id==finding.entityId && d.committed);}
        DashboardTone TimelineVisualTone(CityKeyEvent e)
        {
            if(e.code=="resident.late" || e.code=="freight.blocked" || e.code=="commercial.empty")return DashboardTone.Attention;
            if(e.code=="commercial.restocked")return DashboardTone.Positive;
            if(e.code=="invariant.cash")return DashboardTone.Critical;
            if(e.code=="factory.status")
            {int arrow=e.message.LastIndexOf('→');if(arrow>=0)return DashboardPresentation.StateTone(e.message.Substring(arrow+1).Trim());}
            return DashboardTone.Neutral;
        }
        string FindingBrief(AnalysisFinding f,CityAnalysisState s)
        {
            if(f.entityKind==AnalysisEntityKind.Factory)
            {
                var report=f.title.StartsWith("昨日",StringComparison.Ordinal)?s.lastDay:s.currentDay;
                var d=report?.factories.Find(b=>b.id==f.entityId);
                if(d!=null)
                {
                    if(f.code=="raw")return d.rawBlocked.ToString("0.#")+" 工人分钟缺料受阻";
                    if(f.code=="stock")return d.stockBlocked.ToString("0.#")+" 工人分钟仓满受阻";
                    if(f.code=="cash")return "现金 "+Money(d.cash)+" · 资金受阻 "+d.fundsBlocked.ToString("0.#")+" 工人分钟";
                    if(f.code=="workers")return "观测到岗 "+d.attended+" / 已分配 "+d.assigned;
                    if(f.code=="input_transport" || f.code=="output_transport")return "长时间等待 "+(f.code=="input_transport"?d.inputWaiting:d.outputWaiting)+" 个货运任务";
                }
            }
            if(f.entityKind==AnalysisEntityKind.Commercial)
            {var b=s.Business(f.entityId);if(b!=null)return b.incomingGoods>0?"库存为 0 · 在途补货 "+b.incomingGoods:"库存为 0 · 无在途补货";}
            if(f.code=="household_change")
            {var d=FindingDecision(f,s);if(d!=null)return (d.fromHome<0?"城外":"住宅 #"+d.fromHome)+" → #"+d.toHome+(DashboardPresentation.ComparableDecision(d)?" · 评分变化 "+(d.proposedScore-d.currentScore).ToString("+0.#;-0.#;0"):d.evaluated?" · 查看候选方案":" · 未评估候选");}
            string text=f.evidence??"点击查看证据";int end=text.IndexOfAny(new[]{'。','；','\n'});return end>=0?text.Substring(0,end):text;
        }
        void FindingCard(AnalysisFinding f,CityAnalysisState s)
        {
            DashboardStyles();Rect r=Block(68);var tone=FindingVisualTone(f,s);Color color=ToneColor(tone);Fill(r,dashSurface);Fill(new Rect(r.x,r.y,4,r.height),color);
            StatusBadge(new Rect(r.x+12,r.y+11,43,22),tone==DashboardTone.Critical?"警示":tone==DashboardTone.Attention?"注意":tone==DashboardTone.Positive?"改善":"变化",tone);
            Ink(new Rect(r.x+64,r.y+9,Math.Max(1,r.width-150),24),f.title,dashHeading,color,f.title);
            Ink(new Rect(r.x+12,r.y+39,Math.Max(1,r.width-100),21),FindingBrief(f,s),dashCaption,tooltip:f.evidence);
            if(GUI.Button(new Rect(r.xMax-78,r.y+19,66,30),"查看 →",button))OpenAnalysisEntity(f.entityKind,f.entityId>=0?f.entityId:0);
            GUILayout.Space(6);
        }
    }
}
