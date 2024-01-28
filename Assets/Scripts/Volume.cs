using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public static class Volume
{
    static float volume;
    

    public static float ChangeVol(int VolIn)
    {
        volume = VolIn;
        return VolIn;
    }
}
