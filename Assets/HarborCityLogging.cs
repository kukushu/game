using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace HarborCity
{
    public sealed partial class HarborCityGame
    {
        [NonSerialized] CitySimulationLog simulationLog;
        [SerializeField] string lastSimulationLogSession;
        float nextLogFlush, nextLogTraffic;
        int loggedRoadRevision=-1, loggedBuildingRevision=-1;
        string reportedLogFailure;
        readonly Dictionary<int,string> loggedTripStates=new Dictionary<int,string>();
        readonly Dictionary<int,float> loggedWaitTime=new Dictionary<int,float>();
        void StartSimulationLog(string reason)
        {
            StopSimulationLog();
            try
            {
                simulationLog=new CitySimulationLog(Path.Combine(Application.persistentDataPath,"SimulationLogs"),value=>JsonUtility.ToJson(value));
                city.logSink=simulationLog.Write;
                simulationLog.Snapshot("baseline",city);
                city.Trace("session.start",reason,new CityLogDetail {reason="Unity "+Application.unityVersion+" / schema "+CityModel.SaveFormat+" / previousSession="+lastSimulationLogSession});
                lastSimulationLogSession=simulationLog.SessionId;
                Application.logMessageReceived+=CaptureUnityLog;
                nextLogFlush=0; nextLogTraffic=0; loggedRoadRevision=loggedBuildingRevision=-1; reportedLogFailure=null;
                loggedTripStates.Clear(); loggedWaitTime.Clear();
                foreach(var t in city.traffic.trips) city.Trace("trip.resume","记录开始时已经在途",t,household:t.householdId,citizen:t.residentId,trip:t.id);
            }
            catch(Exception ex) {notice="模拟日志无法启动："+ex.Message; StopSimulationLog();}
        }
        void StopSimulationLog()
        {
            Application.logMessageReceived-=CaptureUnityLog;
            if(simulationLog==null) return;
            city?.Trace("session.end","结束当前记录会话");
            if(city!=null) {simulationLog.Snapshot("latest",city); city.logSink=null;}
            simulationLog.Dispose(); simulationLog=null;
        }
        void OnDisable() {StopSimulationLog();}
        void CaptureUnityLog(string message,string stack,LogType type)
        {
            if(type==LogType.Log) return;
            city?.Trace("unity."+type.ToString().ToLowerInvariant(),message,new CityLogDetail {reason=stack},level:type==LogType.Warning?"warning":"error");
        }
        void UpdateSimulationLog()
        {
            if(simulationLog==null) return;
            if(simulationLog.AnalysisFailure!=null && reportedLogFailure!=simulationLog.AnalysisFailure)
            {reportedLogFailure=simulationLog.AnalysisFailure; notice="城市分析报告失败，原始日志仍保留："+reportedLogFailure;}
            if(Time.realtimeSinceStartup>=nextLogFlush)
            {
                nextLogFlush=Time.realtimeSinceStartup+2; simulationLog.Flush();
                if(simulationLog.Failure!=null && reportedLogFailure!=simulationLog.Failure)
                {reportedLogFailure=simulationLog.Failure; notice="模拟日志写入失败："+reportedLogFailure;}
            }
            if(loggedRoadRevision!=city.roads.revision || loggedBuildingRevision!=city.buildingRevision)
            {
                loggedRoadRevision=city.roads.revision; loggedBuildingRevision=city.buildingRevision;
                city.Trace("network.audit","道路或建筑变化后的连通性检查",city.roads);
                foreach(var b in city.buildings.Where(b=>(int)city.UseOf(b.id)>1))
                {
                    bool connected=city.EntityRoadAccess(b.id);
                    city.Trace("building.access",connected?"建筑已连通城外入口":"建筑未连通城外入口",
                        new CityLogDetail {ids=city.AccessBuilding(b.id),x=b.x,z=b.z},building:b.id,level:connected?"info":"warning");
                }
            }
            float clock=city.traffic.clock;
            if(clock<nextLogTraffic) return; nextLogTraffic=clock+1;
            var active=new HashSet<int>();
            foreach(var t in city.traffic.trips)
            {
                active.Add(t.id);
                string state=t.status==TripStatus.Waiting?"道路中断":t.status==TripStatus.Visiting?"目的地停留":t.blocked>.1f?"排队让行":"行驶";
                loggedTripStates.TryGetValue(t.id,out string previous); loggedWaitTime.TryGetValue(t.id,out float last);
                bool waiting=state=="道路中断" || state=="排队让行";
                if(previous!=state || waiting && clock-last>=30)
                {
                    city.Trace("trip.state",state,t,household:t.householdId,citizen:t.residentId,trip:t.id,level:waiting?"warning":"info");
                    loggedTripStates[t.id]=state; loggedWaitTime[t.id]=clock;
                }
            }
            foreach(int id in loggedTripStates.Keys.Where(id=>!active.Contains(id)).ToArray()) {loggedTripStates.Remove(id); loggedWaitTime.Remove(id);}
        }
        void ExportSimulationLog()
        {
            try
            {
                if(simulationLog==null) StartSimulationLog("导出时重新启用记录；之前过程未记录");
                if(simulationLog==null) return;
                city.Trace("session.export","用户导出诊断日志");
                string target=simulationLog.Export(Path.Combine(Application.persistentDataPath,"SimulationReports"),city);
                notice="日志已导出，可用 VS Code 打开："+target;
                Debug.Log("Simulation logs exported: "+target);
                Application.OpenURL(new Uri(target+Path.DirectorySeparatorChar).AbsoluteUri);
            }
            catch(Exception ex) {notice="日志导出失败："+ex.Message;}
        }
    }
}
