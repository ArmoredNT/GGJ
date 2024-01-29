using System.Collections;
using Unity.VisualScripting;
using Unity.WebRTC;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.UI;

public class WebCamTester : MonoBehaviour
{
	private WebCamTexture cam;
	private Texture2D copyTex;

	[SerializeField] RawImage testImage;

	private bool converted = false;

	bool shouldConvertFrame = false;

	void Start()
	{
		StartCoroutine(StartCo());
	}

	IEnumerator StartCo()
	{
		Debug.Log("WEBCAM START");

		WebCamDevice[] devices = WebCamTexture.devices;

		if (devices.Length > 0)
		{
			for (int i = 0; i < devices.Length; i++)
			{
				Debug.Log(devices[i].name);
			}

			cam = new WebCamTexture(devices[0].name);
			cam.Play();
		}
		else
		{
			Debug.LogError("No webcam available");
		}

		yield return new WaitUntil(() => cam.didUpdateThisFrame);

		var supportedFormat = WebRTC.GetSupportedGraphicsFormat(SystemInfo.graphicsDeviceType);
		if (cam.graphicsFormat != supportedFormat)
		{
			copyTex = new Texture2D(cam.width, cam.height, supportedFormat, TextureCreationFlags.None);
			// StartCoroutine(ConvertFrame());
			shouldConvertFrame = true;
			converted = true;
		}
		else
		{
			converted = false;
		}
	}

	private void OnDestroy()
	{
		if (cam != null)
		{
			Debug.Log("STOP CAMERA");
			cam.Stop();
			cam = null;
		}
	}

	public Texture GetCameraTexture()
	{
		testImage.texture = converted ? copyTex : cam;
		return converted ? copyTex : cam;
	}

	private void Update()
	{
		if (shouldConvertFrame)
		{
			Graphics.ConvertTexture(cam, copyTex);
		}
	}
}
