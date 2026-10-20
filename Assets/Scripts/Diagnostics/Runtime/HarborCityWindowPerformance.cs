#if UNITY_EDITOR || CITY_TRAFFIC_WINDOW_MEASUREMENT
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Unity.Profiling;
using UnityEngine;

namespace HarborCity
{
    // Opt-in full-game measurement. Rendering, OnGUI and the normal Update all remain active.
    // The only time clamp is the endpoint of the finite test, as in the traffic benchmark.
    internal static class CityTrafficWindowProbe
    {
        internal static CityTraffic Expected;
        internal static int Steps;
        internal const int Samples=300;
        internal static void NoteStep(CityTraffic t){if(ReferenceEquals(t,Expected))Steps++;}
        internal static float Clamp(CityTraffic t,float seconds)=>ReferenceEquals(t,Expected)?t.ClampWindowSeconds(Samples-Steps,seconds):seconds;
    }
    public sealed partial class CityTraffic {internal float ClampWindowSeconds(int left,float seconds)=>Math.Max(0,Math.Min(seconds,(float)(left*FixedStepSeconds-State.remainder)));}
}
#endif
