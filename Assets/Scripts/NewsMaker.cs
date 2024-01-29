using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NewsMaker : MonoBehaviour
{
	public static NewsMaker instance;

	[SerializeField] TMP_Text headline;
	[SerializeField] TMP_Text presenter;
	[SerializeField] ImageHandler imageHandler;

	private void Awake()
	{
		instance = this;
	}

	public void SetHeadline(string headline)
	{
		this.headline.text = headline;
	}

	public void SetImage(string url)
	{
		imageHandler.OnURLEntered(url);
	}

	public void SetPresenter(string presenter)
	{
		this.presenter.text = presenter;
	}
}
