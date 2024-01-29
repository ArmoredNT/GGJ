using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

class SavedImage
{
	private Texture _image;
	private Vector2 _position;
	private float _scale;

	public SavedImage()
	{

	}

	public void SetTexture(Texture texture)
	{
		_image = texture;
	}

	public Texture GetTexture()
	{
		return _image;
	}

	public void SetScale(float size)
	{
		_scale = size;
	}
	public float GetScale()
	{
		return _scale;
	}

	public void SetPos(Vector2 pos)
	{
		_position = pos;
	}
	public Vector2 GetPos()
	{
		return _position;
	}

}

public class ImageHandler : MonoBehaviour
{

	[SerializeField] private Image sampleMask;
	[SerializeField] private RawImage rawImage;
	//[SerializeField] private RawImage screen;

	private SavedImage image;

	private bool called;

	private string url;

	private void Start()
	{
		image = new SavedImage();
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

			if (!texture)
			{
				ImageFailed();
			}
			else
			{
				rawImage.gameObject.SetActive(true);

				rawImage.texture = texture;
				image.SetTexture(texture);

				// Fit aspect ratio
				float aspect = (float)texture.width / texture.height;

				Vector3 scale = rawImage.gameObject.transform.localScale;
				scale.x = aspect;
				rawImage.gameObject.transform.localScale = scale;

				this.url = url;
			}
		}
	}

	void ImageFailed()
	{
		Debug.LogError("Image failed");
	}

	public void Scaling(float num)
	{
		rawImage.transform.localScale = new Vector3(num, num, 1);
	}

	public void EndEdit()
	{
		if (!called)
		{
			called = true;
			image.SetScale(rawImage.transform.localScale.x);
			image.SetPos(rawImage.transform.localPosition);

			if (!NetworkManager2.Instance.GetIsHost())
			{
				NetworkManager2.Instance.ClientSendToServer("IMAGE_URL", url);
			}
			else
			{
				NetworkManager2.Instance.GetHost().ReceiveUrl(-1, url);
			}
		}
		// screen.texture = image.GetTexture();
		// screen.SetNativeSize();
		// screen.transform.localScale = new Vector3(image.GetScale(), image.GetScale(), 1);
		// screen.transform.localPosition = image.GetPos();
	}

	public void LoadNextScene()
	{
		SceneManager.LoadScene("NewsRoom");
	}
}
