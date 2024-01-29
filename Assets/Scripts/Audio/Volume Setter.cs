using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VolumeSetter : MonoBehaviour
{
	[SerializeField] private AudioSource audio1;
    // Start is called before the first frame update
    void Start()
    {
        audio1.volume = SoundControl.GetVolume();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
