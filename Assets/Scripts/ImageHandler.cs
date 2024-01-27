using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.PackageManager.UI;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

class SavedImage
{
    private Texture _image;
    private Vector2 _position;
    private float _scale;

    public SavedImage(Vector2 pos, float size)
    {
       
        _position = pos;
        _scale = size;
    }

    public void SetTexture(Texture texture)
    {
        _image = texture;
    }

}

public class ImageHandler : MonoBehaviour
{

    [SerializeField] private Image sampleMask;
    [SerializeField] private RawImage rawImage;
    [SerializeField] private RawImage screen;

    private SavedImage image;

    private void Start()
    {
        image = new SavedImage(rawImage.transform.position, 1);
    }

    public void OnURLEntered(string url)
    {
        StartCoroutine(Stootz(url));
    }
    
    IEnumerator Stootz(string url)
    {
        using (var uwr = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET))
        {
            uwr.downloadHandler = new DownloadHandlerTexture();
            yield return uwr.SendWebRequest();
            //GetComponent<Renderer>().material.mainTexture
            Texture texture = DownloadHandlerTexture.GetContent(uwr);
            rawImage.texture = texture;
            image.SetTexture(texture);
            rawImage.SetNativeSize();
            print(rawImage.uvRect);
        }
    }
}
