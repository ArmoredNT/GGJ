using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LobbyCodeUi : MonoBehaviour
{
    TMP_Text text;

	private void Awake()
	{
		text = GetComponent<TMP_Text>();
	}

	void Start()
    {
        text.text = NetworkManager2.Instance.GetLobbyCode();
    }
}
