using System.Collections.Generic;
using UnityEngine;

public static class SessionProgress
{
    public static int loopCount = 0;
    public static List<CucumberScarData> cucumberScars = new List<CucumberScarData>();
    public static List<int> correctClosetOrder = new List<int>();
    public static List<Color> correctClosetColors = new List<Color>();
    [System.Serializable]
    public struct CucumberScarData
    {
        public Vector3 localPosition;
        public float missFactor;
    }




    public static void ResetSession()
    {
        loopCount = 0;
        cucumberScars.Clear();
        correctClosetOrder.Clear();
        correctClosetColors.Clear();
    }
}