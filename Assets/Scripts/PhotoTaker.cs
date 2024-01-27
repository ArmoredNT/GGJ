using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Net;

public class PhotoTaker : MonoBehaviour
{
    public RawImage previewImage;
    public RawImage previewImage2;

    private WebCamTexture webcamTexture;

    void Start()
    {
        // Start the camera
        StartCamera();
        
    }
    void StartCamera()
    {
        webcamTexture = new WebCamTexture();
        previewImage.texture = webcamTexture;
        previewImage.rectTransform.sizeDelta = new Vector2(webcamTexture.width, webcamTexture.height);
        webcamTexture.Play();
    }
    public void CapturePhoto()
    {
        print("khgjtfvujutfv");
        // Capture the current frame as a photo
        Texture2D photo = new Texture2D(webcamTexture.width, webcamTexture.height);
        photo.SetPixels(webcamTexture.GetPixels());
        photo.Apply();

        // Display the captured photo (you can save it or perform other actions)
        previewImage2.texture = photo;
    }
    
}
