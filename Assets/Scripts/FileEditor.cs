using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;


public class FileEditor : MonoBehaviour
{
    public RawImage rawImage;

    public string imageUrl = "https://www.bing.com/images/search?view=detailV2&ccid=UbZCrTHu&id=B73F8E7B11CF6009F4E57DEC28871A03D72276B9&thid=OIP.UbZCrTHu5Am87wRekeyg9AHaEK&mediaurl=https%3a%2f%2fassets1.cbsnewsstatic.com%2fhub%2fi%2fr%2f2020%2f02%2f08%2f84802117-8197-4d8a-aa12-6b1f5b7feb89%2fthumbnail%2f620x349%2f698758e3f633af94a9318ce1ede3c177%2faasuspect.jpg%23&cdnurl=https%3a%2f%2fth.bing.com%2fth%2fid%2fR.51b642ad31eee409bcef045e91eca0f4%3frik%3duXYi1wMahyjsfQ%26pid%3dImgRaw%26r%3d0&exph=349&expw=620&q=Richard+belden&simid=608033903684564596&FORM=IRPRST&ck=D0B9961F9586F768F04190296D8FE976&selectedIndex=3&itb=0&ajaxhist=0&ajaxserp=0";
    void Start()
    {
        //StartCoroutine(LoadImageFromURL(imageUrl));
        StartCoroutine(Stoot());
    }

    IEnumerator LoadImageFromURL(string url)
    {
        using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(url))
        {
            
            yield return www.SendWebRequest();
            
            if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("Error: " + www.error);
            }
            else
            {
                // Get the downloaded texture
                Texture2D texture = DownloadHandlerTexture.GetContent(www);
                print(texture);

                // Apply the texture to the RawImage component
                rawImage.texture = texture;
            }
        }
    }
    
    IEnumerator Stoot()
    {
        using (var uwr = new UnityWebRequest("https://www.gouletpens.com/cdn/shop/files/goulet-logo--stacked_ce165c8e-6104-4220-8ef5-d12a37ce1f70_500x.jpg?v=1613190309", UnityWebRequest.kHttpVerbGET))
        {
            uwr.downloadHandler = new DownloadHandlerTexture();
            yield return uwr.SendWebRequest();
            //GetComponent<Renderer>().material.mainTexture
            rawImage.texture = DownloadHandlerTexture.GetContent(uwr);
        }
    }
}
