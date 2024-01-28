using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PhotoPrompt : MonoBehaviour
{
	private void Awake()
	{
		GetComponent<TMP_Text>().text = NetworkManager2.Instance.GetPhotoPrompt();
	}
}
