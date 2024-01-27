using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Timer : MonoBehaviour
{
    [SerializeField] float maxTime = 60;
    private float currentTime;

    [SerializeField] private TextMeshProUGUI _text;
    // Start is called before the first frame update
    void Start()
    {
        currentTime = maxTime;
        _text.text = currentTime.ToString();
    }

    // Update is called once per frame
    void Update()
    {
        if (currentTime >= 0) currentTime -= Time.deltaTime;
        else ;
        _text.text = ((int)currentTime).ToString();
    }
}
