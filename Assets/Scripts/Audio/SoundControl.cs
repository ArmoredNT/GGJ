using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class SoundControl
{
    public static float volume;

    public static void SetVolume(float value)
    {
        volume = value;
    }
    public static float GetVolume()
    {
        return volume;
    }
}
