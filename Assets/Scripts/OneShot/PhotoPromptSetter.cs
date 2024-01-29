using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class PhotoPromptSetter : MonoBehaviour
{
	private void Start()
	{
		GetComponent<TMP_Text>().text = NetworkManager2.Instance.GetClient().GetPhotoPrompt();
	}
}
