using System.Collections.Generic;

public static class SessionProgress
{
    public static int loopCount = 0;

    public static List<float> cucumberMarksX = new List<float>();

    public static void ResetSession()
    {
        loopCount = 0;
        cucumberMarksX.Clear();
    }
}