using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;


public class FileEditor : MonoBehaviour
{
    public RawImage rawImage;

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
            rawImage.texture = DownloadHandlerTexture.GetContent(uwr);
            rawImage.SetNativeSize();
            print(rawImage.uvRect);
        }
    }
    
}
