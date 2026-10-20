using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Globalization;
using UnityEngine;
namespace HarborCity
{
    // Explicit Player regression with a controlled backlog, never normal Play.
    public sealed class HarborCitySpeedValidation:MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Enable(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-harbor-speed-validate")>=0)new GameObject("Speed switch validation").AddComponent<HarborCitySpeedValidation>();}
        IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs();string output=args[Array.IndexOf(args,"-harbor-speed-validate")+1];Directory.CreateDirectory(output);
            for(int i=0;i<20;i++)yield return null;
            var game=FindAnyObjectByType<HarborCityRoadCoreGame>();var session=game.ScenarioSession;
            using(var csv=new StreamWriter(Path.Combine(output,"switches.csv")))
            {
                csv.WriteLine("target_rate,wall_seconds,simulation_seconds,actual_rate,pending_before,pending_after");
                foreach(float rate in new[]{1f,3f,20f,0f,1f})
                {
                    session.Simulation.RequestTime(20);game.ScenarioRate(50);
                    yield return new WaitForSecondsRealtime(.4f);
                    game.ScenarioRate(rate);double wall=Time.realtimeSinceStartupAsDouble,clock=session.Transport.Clock,pending=session.Simulation.PendingSeconds;
                    byte[] paused=rate==0?CityCoreSave.Write(session):null;
                    yield return new WaitForSecondsRealtime(2);
                    double duration=Time.realtimeSinceStartupAsDouble-wall,advanced=session.Transport.Clock-clock;
                    csv.WriteLine(string.Join(",",new[]{(double)rate,duration,advanced,advanced/duration,pending,session.Simulation.PendingSeconds}.Select(v=>v.ToString("R",CultureInfo.InvariantCulture))));csv.Flush();
                    if(game.ScenarioCurrentRate!=rate||advanced>duration*rate+.1||rate>0&&advanced<=0||rate==0&&!paused.SequenceEqual(CityCoreSave.Write(session)))
                    {File.WriteAllText(Path.Combine(output,"Result.txt"),"FAIL speed change/pause with controlled queued debt");Application.Quit(1);yield break;}
                }
            }
            game.ScenarioRate(0);session.Validate();var bytes=CityCoreSave.Write(session);var cold=CityCoreSave.Read(bytes);
            if(!bytes.SequenceEqual(CityCoreSave.Write(cold)))throw new Exception("Speed switching changed cold save facts");
            File.WriteAllText(Path.Combine(output,"Result.txt"),"PASS actual Player 50x to 1x/3x/20x/pause/resume, deliberately seeded 20s debt per phase, no low-speed catch-up overshoot; paused full state unchanged; saved debt/cold restore exact. Not FPS measurement or physical mouse acceptance.");Application.Quit(0);
        }
    }
}
