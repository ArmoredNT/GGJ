using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class AudioScript : MonoBehaviour
{
    [SerializeField] private Slider slider;
    [SerializeField] private AudioClip clip;
    [SerializeField] AudioSource source;

    // Start is called before the first frame update
    void Start()
    {
        slider.maxValue = 1;
        slider.minValue = 0;

        slider.value = 0.5f;
    }

    // Update is called once per frame
    void Update()
    {
        source.volume = slider.value;
    }
}
