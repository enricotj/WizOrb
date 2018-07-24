using UnityEngine;
using System.Collections;

public class Timer
{
    public delegate void EndCallback();

    public static void Increment(ref float t, EndCallback cb)
    {
        float oldT = t;
        Increment(ref t);
        if (oldT > 0 && t == 0)
        {
            cb();
        }
    }

    public static void Increment(ref float t)
    {
        if (t > 0)
        {
            t -= Time.deltaTime;
            if (t < 0)
            {
                t = 0;
            }
        }
    }

    public static void FixedIncrement(ref float t)
    {
        if (t > 0)
        {
            t -= Time.fixedDeltaTime;
            if (t < 0)
            {
                t = 0;
            }
        }
    }
}
