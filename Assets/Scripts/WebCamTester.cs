using System.Collections;
using Unity.WebRTC;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

public class WebCamTester : MonoBehaviour
{
	private WebCamTexture cam;
	private Texture2D copyTex;

	private bool converted = false;

	void Start()
	{
		StartCoroutine(StartCo());
	}

	IEnumerator StartCo()
	{
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
			StartCoroutine(ConvertFrame());
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
			cam.Stop();
			cam = null;
		}
	}

	public Texture GetCameraTexture()
	{
		return converted ? copyTex : cam;
	}

	IEnumerator ConvertFrame()
	{
		while (true)
		{
			yield return new WaitForEndOfFrame();
			Graphics.ConvertTexture(cam, copyTex);
		}
	}
}
