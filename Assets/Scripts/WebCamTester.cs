using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WebCamTester : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] private RawImage rend;

    private WebCamTexture webcamTexture;
    void Start()
    {
       
        WebCamDevice[] devices = WebCamTexture.devices;

        WebCamTexture tex = new WebCamTexture(devices[0].name);
        //rend.material.mainTexture = tex;
        rend.texture = tex;
        tex.Play();
    }
}
