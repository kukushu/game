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
        float nextLogFlush,nextLogTraffic,nextAnalysisRefresh;
        int loggedRoadRevision=-1,loggedBuildingRevision=-1;
        string reportedLogFailure;
        readonly Dictionary<int,string> loggedTripStates=new Dictionary<int,string>();
        readonly Dictionary<int,float> loggedWaitTime=new Dictionary<int,float>();
        void StartObservation(string reason)
        {
            StopObservation();
            city.analysis=new CityDailyAnalysis(city);
            Application.logMessageReceived+=CaptureUnityLog;
            nextAnalysisRefresh=0;
            RefreshAnalysis();
        }
        void StopObservation()
        {
            StopDebugTrace();
            Application.logMessageReceived-=CaptureUnityLog;
            if(city!=null)city.analysis=null;
        }
        void OnDisable()
        {
            // Unity's default hot-reload serialization loses derived buildings.
            // Preserve the same concrete DTO used by normal saves instead.
            if(Application.isPlaying && city!=null && city.Valid())reloadCityJson=JsonUtility.ToJson(city.ToSaveData());
            StopObservation();ReleaseDashboardGraphics();
        }
        void RefreshAnalysis()
        {
            if(city?.analysis==null)return;
            try {city.analysis.Refresh(true);}
            catch(Exception ex) {city.analysis.Fail(ex);}
            if(city.analysis.Failure!=null && reportedLogFailure!=city.analysis.Failure)
            {reportedLogFailure=city.analysis.Failure;notice="城市分析失败："+reportedLogFailure;}
        }
        void StartDebugTrace()
        {
            if(simulationLog!=null)return;
            try
            {
                simulationLog=new CitySimulationLog(Path.Combine(Application.persistentDataPath,"SimulationLogs"),value=>JsonUtility.ToJson(value));
                city.logSink=simulationLog.Write;simulationLog.Snapshot("baseline",city);
                city.Trace("session.start","开发者主动开启 Debug Trace；此前原始事件未记录");
                nextLogFlush=nextLogTraffic=0;loggedRoadRevision=loggedBuildingRevision=-1;reportedLogFailure=null;
                loggedTripStates.Clear();loggedWaitTime.Clear();
                foreach(var t in city.traffic.trips)city.Trace("trip.resume","开启时已在途",t,household:t.householdId,citizen:t.residentId,trip:t.id);
            }
            catch(Exception ex) {notice="Debug Trace 无法启动："+ex.Message;StopDebugTrace();}
        }
        void StopDebugTrace()
        {
            if(simulationLog==null)return;
            city?.Trace("session.end","关闭 Debug Trace");
            if(city!=null) {simulationLog.Snapshot("latest",city);city.logSink=null;}
            simulationLog.Dispose();simulationLog=null;
        }
        void CaptureUnityLog(string message,string stack,LogType type)
        {
            if(type==LogType.Log)return;
            city?.Trace("unity."+type.ToString().ToLowerInvariant(),message,new CityLogDetail {reason=stack},level:type==LogType.Warning?"warning":"error");
        }
        void UpdateObservation()
        {
            if(Time.realtimeSinceStartup>=nextAnalysisRefresh)
            {nextAnalysisRefresh=Time.realtimeSinceStartup+.25f;RefreshAnalysis();}
            UpdateDebugTrace();
        }
        void UpdateDebugTrace()
        {
            if(simulationLog==null)return;
            if(Time.realtimeSinceStartup>=nextLogFlush)
            {
                nextLogFlush=Time.realtimeSinceStartup+2;simulationLog.Flush();
                if(simulationLog.Failure!=null && reportedLogFailure!=simulationLog.Failure)
                {reportedLogFailure=simulationLog.Failure;notice="Debug Trace 写入失败："+reportedLogFailure;}
            }
            if(loggedRoadRevision!=city.roads.revision || loggedBuildingRevision!=city.buildingRevision)
            {
                loggedRoadRevision=city.roads.revision;loggedBuildingRevision=city.buildingRevision;
                city.Trace("network.audit","道路或建筑变化后的连通性检查",city.roads);
                foreach(var b in city.buildings)
                    city.Trace("building.access",city.EntityRoadAccess(b.id)?"建筑已连通":"建筑未连通",new CityLogDetail {ids=city.AccessBuilding(b.id),x=b.x,z=b.z},building:b.id);
            }
            float clock=city.traffic.clock;
            if(clock<nextLogTraffic)return;nextLogTraffic=clock+1;
            var active=new HashSet<int>();
            foreach(var t in city.traffic.trips)
            {
                active.Add(t.id);
                string state=t.status==TripStatus.Waiting?"道路中断":t.status==TripStatus.Visiting?"目的地停留":t.blocked>.1f?"排队让行":"行驶";
                loggedTripStates.TryGetValue(t.id,out string previous);loggedWaitTime.TryGetValue(t.id,out float last);
                bool waiting=state=="道路中断" || state=="排队让行";
                if(previous!=state || waiting && clock-last>=30)
                {
                    city.Trace("trip.state",state,t,household:t.householdId,citizen:t.residentId,trip:t.id,level:waiting?"warning":"info");
                    loggedTripStates[t.id]=state;loggedWaitTime[t.id]=clock;
                }
            }
            foreach(int id in loggedTripStates.Keys.Where(id=>!active.Contains(id)).ToArray()) {loggedTripStates.Remove(id);loggedWaitTime.Remove(id);}
        }
        void ExportSimulationLog()
        {
            if(simulationLog==null) {notice="先在 Debug 页主动开启 Debug Trace；不会补写此前事件。";return;}
            try {notice="Debug Trace 已导出："+simulationLog.Export(Path.Combine(Application.persistentDataPath,"SimulationReports"),city);}
            catch(Exception ex) {notice="Trace 导出失败："+ex.Message;}
        }
        void ExportAnalysis()
        {
            try
            {
                RefreshAnalysis();var state=city.analysis?.State;if(state==null)return;
                string directory=Path.Combine(Application.persistentDataPath,"SimulationReports");Directory.CreateDirectory(directory);
                string path=Path.Combine(directory,"analysis-day-"+city.day+"-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".json");
                File.WriteAllText(path,JsonUtility.ToJson(state,true));notice="结构化分析已导出："+path;
            }
            catch(Exception ex) {notice="分析导出失败："+ex.Message;}
        }
    }
}
