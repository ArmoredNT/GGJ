using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.WebRTC;
using UnityEngine;

public class Client
{
	int playerId = -1;
	string photoPrompt = "";

	public void ReceiveMessage(byte[] message)
	{
		string s = Encoding.UTF8.GetString(message);

		var m = NetworkManager2.GetMessageType(s);
		Debug.Log(m.type);
		Debug.Log(m.message);

		switch (m.type)
		{
			case "SCENE":
				NetworkManager2.Instance.ClientSetScene(m.message);
				break;
			case "PHOTOGRAPHER_PROMPT":
				photoPrompt = m.message;
				break;
			case "NEWSROOM_PROMPT":
				NewsMaker.instance.SetHeadline(m.message);
				break;
			case "NEWSROOM_PRESENTER":
				NewsMaker.instance.SetPresenter(m.message);
				break;
			case "NEWSROOM_IMAGE":
				NewsMaker.instance.SetImage(m.message);
				break;
		}
	}

	public int GetPlayerId()
	{
		return playerId;
	}

	public void SetPlayerId(int playerId)
	{
		this.playerId = playerId;
	}

	public string GetPhotoPrompt()
	{
		return photoPrompt;
	}

	public void SetPhotoPrompt(string prompt)
	{
		photoPrompt = prompt;
	}
}
