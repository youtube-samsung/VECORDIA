using System.Collections.Generic;
using UnityEngine;

public static class SessionProgress
{
    public static int loopCount = 0;


    public struct ScarData
    {
        public Vector3 position;
        public float missFactor; 
    }

    public static List<ScarData> cucumberScars = new List<ScarData>();
    public static List<int> correctClosetOrder = new List<int>();
    public static List<Color> correctClosetColors = new List<Color>();

    public static void ResetSession()
    {
        loopCount = 0;
        cucumberScars.Clear();
        correctClosetOrder.Clear();
        correctClosetColors.Clear();
    }
}