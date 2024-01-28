using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VolumeSetter : MonoBehaviour
{
	[SerializeField] private new AudioSource audio;
    // Start is called before the first frame update
    void Start()
    {
        audio.volume = SoundControl.GetVolume();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
