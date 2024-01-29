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
        // Capture the current frame as a photo
        Texture2D photo = new Texture2D(webcamTexture.width, webcamTexture.height);
        photo.SetPixels(webcamTexture.GetPixels());
        photo.Apply();

        previewImage2.texture = photo;

        string fileName = "CORRUPTED_" + Random.Range(10000, 99999).ToString() + "_DO_NOT_OPEN.png";
        
        SaveTextureToFile(photo, fileName);
    }
    
    void SaveTextureToFile(Texture2D texture, string filename)
    {
        // Convert the texture to PNG format
        byte[] bytes = texture.EncodeToPNG();

        // Specify the file path
        string filePath = System.IO.Path.Combine(Application.persistentDataPath, filename);

        // Write the PNG data to a file
        System.IO.File.WriteAllBytes(filePath, bytes);

        Debug.Log("Texture saved as PNG: " + filePath);
    }
    
}
