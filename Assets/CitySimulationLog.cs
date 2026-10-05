using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace HarborCity
{
    [Serializable] public sealed class CityLogEvent
    {
        public int schema=1;
        public long seq;
        public string session, utc, type, level, message;
        public int day, householdId=-1, citizenId=-1, jobId=-1, buildingId=-1, tripId=-1, roadRevision;
        public float minute, simulationSeconds;
        [NonSerialized] public object data;
    }
    [Serializable] public sealed class CityLogDetail
    {
        public int origin=-1, destination=-1, previous=-1, amount, count;
        public float minutes, earned, x, z;
        public string reason;
        public List<int> ids;
        public List<HouseholdOption> options;
        public HouseholdOption currentOption, selectedOption;
    }
    public sealed partial class CityModel
    {
        [NonSerialized] public Action<CityLogEvent> logSink;
        public void Trace(string type,string message,object data=null,int household=-1,int citizen=-1,int job=-1,int building=-1,int trip=-1,string level="info")
        {
            if(logSink==null && analysis==null) return;
            var entry=new CityLogEvent {type=type,message=message,data=data,householdId=household,citizenId=citizen,
                jobId=job,buildingId=building,tripId=trip,level=level,day=day,minute=society==null?0:ResidentMinute,
                simulationSeconds=traffic?.clock??0,roadRevision=roads?.revision??0};
            if(analysis!=null) try {analysis.ObserveEvent(entry);} catch(Exception ex) {analysis.Fail(ex);}
            // Observability must never break a simulation transaction.
            try {logSink?.Invoke(entry);} catch { }
        }
    }

    // Append-only disk journal. No Unity dependency, no simulation RNG or saved gameplay state.
    public sealed class CitySimulationLog : IDisposable
    {
        readonly Func<object,string> json;
        readonly Dictionary<string,int> counts=new Dictionary<string,int>();
        StreamWriter events, readable, daily;
        readonly long segmentBytes;
        long bytes, sequence;
        int segment;
        bool closed;
        CityModel observedCity;
        CityDailyAnalysis derived;
        public string DirectoryPath {get; private set;}
        public string SessionId {get; private set;}
        public string Failure {get; private set;}
        public string AnalysisFailure => derived?.Failure;
        public CitySimulationLog(string root,Func<object,string> serialize,long rotateBytes=8*1024*1024)
        {
            json=serialize; segmentBytes=rotateBytes;
            SessionId=DateTime.UtcNow.ToString("yyyyMMdd-HHmmss",CultureInfo.InvariantCulture)+"-"+Guid.NewGuid().ToString("N");
            DirectoryPath=Path.Combine(root,SessionId); Directory.CreateDirectory(DirectoryPath);
            try
            {
                Rotate(); daily=Writer("daily.csv");
                daily.WriteLine("day,population,households,employed,unemployed,averageCommute,wages,rent,maintenance,arrived,moved,left,treasuryError,savingsError,factoryWages,externalWages");
                File.WriteAllText(Path.Combine(DirectoryPath,"README.md"),
                    "# 城市模拟诊断日志\n\n用 VS Code 打开本文件夹。events-*.jsonl 每行一个事件，按文件名和 seq 排序；readable-*.log 是中文摘要，daily.csv 是每日结算。\n\n"+
                    "理解玩法请先打开 Analysis/latest-city.md，再看 Analysis/day-*-factories.md 与 day-*-households.md。对应 day-*.json 是确定性分析快照；in-progress-* 是尚未结束的区间。原始事件用于追查证据，继续完整保留。\n\n"+
                    "每个事件含 session、seq、UTC 时间、day、minute、simulationSeconds、type、level，以及 householdId/citizenId/jobId/buildingId/tripId 和 roadRevision；-1 表示不适用（出行 data.origin/destination 的 -1 表示城外）。data 是结构化事件数据。\n\n"+
                    "baseline.json 是开始记录时的完整城市；latest.json 是最近导出/结束时的完整城市；summary.json 包含事件计数和写入错误。加载城市或脚本重载会开启新会话，不把不同城市的 ID 混在一起。\n\n"+
                    "搜索 household.decision 查看全部候选、拒绝原因和评分；citizen.pay 查看出勤和实际收入；trip. 查看出行；city.day 查看日账；unity. 查看警告和异常。关联同一 householdId/citizenId/tripId 可追踪因果链。\n\n"+
                    "日志从启用时开始，不补造历史。不会逐帧保存位置；出行状态按变化记录，持续阻塞每 30 模拟秒记录一次。文件每约 8 MB 分段，不自动删除历史。正常运行每 2 秒刷新，异常退出可能丢失最后未刷新的少量记录；磁盘写满等错误会停止写入并在游戏提示。日志不是逐帧回放格式。\n",new UTF8Encoding(false));
            }
            catch {Dispose(); throw;}
        }
        StreamWriter Writer(string name) => new StreamWriter(new FileStream(Path.Combine(DirectoryPath,name),FileMode.CreateNew,FileAccess.Write,FileShare.Read),new UTF8Encoding(false));
        void Rotate()
        {
            events?.Dispose(); readable?.Dispose(); segment++; bytes=0;
            events=Writer("events-"+segment.ToString("D4")+".jsonl"); readable=Writer("readable-"+segment.ToString("D4")+".log");
        }
        public void Write(CityLogEvent e)
        {
            if(closed || Failure!=null) return;
            try
            {
                e.seq=++sequence; e.session=SessionId; e.utc=DateTime.UtcNow.ToString("o",CultureInfo.InvariantCulture);
                string header=json(e), payload=e.data==null?"null":json(e.data);
                string line=header.Substring(0,header.Length-1)+",\"data\":"+payload+"}";
                if(bytes>=segmentBytes) Rotate();
                events.WriteLine(line); bytes+=Encoding.UTF8.GetByteCount(line)+1;
                readable.WriteLine(e.seq+" | D"+e.day+" "+e.minute.ToString("F1",CultureInfo.InvariantCulture)+"m | "+e.level+" | "+e.type+
                    " | H="+e.householdId+" C="+e.citizenId+" J="+e.jobId+" B="+e.buildingId+" T="+e.tripId+" | "+(e.message??"").Replace("\r","\\r").Replace("\n","\\n"));
                counts.TryGetValue(e.type,out int count); counts[e.type]=count+1;
                if(e.type=="city.day" && e.data is HouseholdDay d)
                    daily.WriteLine(string.Join(",",new[]{d.day.ToString(),d.population.ToString(),d.households.ToString(),d.employed.ToString(),d.unemployed.ToString(),
                        d.averageCommute.ToString("R",CultureInfo.InvariantCulture),d.wages.ToString(),d.rent.ToString(),d.maintenance.ToString(),d.arrived.ToString(),d.moved.ToString(),d.left.ToString(),
                        (d.closingTreasury-d.openingTreasury-d.rent+d.maintenance).ToString(),(d.closingSavings-d.openingSavings-d.wages+d.rent+d.living+d.travel+d.movingCosts).ToString(),d.factoryWages.ToString(),d.externalWages.ToString()}));
            }
            catch(Exception ex) {Failure=ex.Message;}
        }
        public void Flush()
        {
            if(closed) return;
            try {events?.Flush(); readable?.Flush(); daily?.Flush();} catch(Exception ex) {Failure=ex.Message;}
        }
        public void Snapshot(string name,object city)
        {
            if(closed) return;
            try
            {
                string path=Path.Combine(DirectoryPath,name+".json"), temporary=path+".tmp";
                File.WriteAllText(temporary,json(city is CityModel modelData?modelData.ToSaveData():city),new UTF8Encoding(false));
                if(File.Exists(path)) File.Replace(temporary,path,null); else File.Move(temporary,path);
                if(name=="baseline" && city is CityModel model && model.society!=null && derived==null)
                {observedCity=model; derived=new CityDailyAnalysis(model,WriteAnalysis); model.analysis=derived;}
                if(name=="latest") derived?.PublishPartial();
            }
            catch(Exception ex) {Failure=ex.Message;}
        }
        void WriteAnalysis(CityAnalysisReport report,bool partial)
        {
            string root=Path.Combine(DirectoryPath,"Analysis"); Directory.CreateDirectory(root);
            string stem=partial?"in-progress":"day-"+report.day.ToString("D6",CultureInfo.InvariantCulture);
            File.WriteAllText(Path.Combine(root,stem+".json"),json(report),new UTF8Encoding(false));
            File.WriteAllText(Path.Combine(root,stem+"-city.md"),CityDailyAnalysis.RenderCity(report),new UTF8Encoding(false));
            File.WriteAllText(Path.Combine(root,stem+"-factories.md"),CityDailyAnalysis.RenderFactories(report),new UTF8Encoding(false));
            File.WriteAllText(Path.Combine(root,stem+"-households.md"),CityDailyAnalysis.RenderHouseholds(report),new UTF8Encoding(false));
            if(!partial) File.WriteAllText(Path.Combine(root,"latest-city.md"),CityDailyAnalysis.RenderCity(report),new UTF8Encoding(false));
        }
        [Serializable] sealed class Summary { public string session, failure, analysisFailure; public long records; public List<string> types=new List<string>(); public List<int> counts=new List<int>(); }
        public void SummaryFile()
        {
            var s=new Summary {session=SessionId,failure=Failure??"",analysisFailure=AnalysisFailure??"",records=sequence};
            foreach(var p in counts) {s.types.Add(p.Key); s.counts.Add(p.Value);} Snapshot("summary",s);
        }
        public string Export(string root,object city)
        {
            if(closed) throw new InvalidOperationException("日志会话已关闭");
            Snapshot("latest",city); Flush(); SummaryFile();
            if(Failure!=null) throw new IOException("日志写入失败："+Failure);
            string target=Path.Combine(root,SessionId+"-export-"+Guid.NewGuid().ToString("N")); Directory.CreateDirectory(target);
            foreach(string file in Directory.GetFiles(DirectoryPath,"*",SearchOption.AllDirectories))
            {
                string destination=Path.Combine(target,file.Substring(DirectoryPath.Length).TrimStart(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(destination)); File.Copy(file,destination);
            }
            return target;
        }
        public void Dispose()
        {
            if(closed) return;
            Flush(); SummaryFile(); closed=true;
            if(observedCity!=null && observedCity.analysis==derived) observedCity.analysis=null;
            foreach(var writer in new[]{events,readable,daily}) try {writer?.Dispose();} catch(Exception ex) {Failure=ex.Message;}
        }
    }
}
