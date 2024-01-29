using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static NetworkManager2;

public class Host
{
	Dictionary<int, string> allPrompts = new(); // prompt #, prompt
	Dictionary<int, int> photographers = new(); // prompt #, player
	Dictionary<int, string> imageUrls = new(); // player, image
	int playerCount;

	public void Init(int playerCount)
	{
		this.playerCount = playerCount;
		Debug.Log(this.playerCount + " players");
	}

	public void ReceivePrompt(int playerID, string prompt)
	{
		Debug.Log(string.Format("Received prompt {0} from {1}", prompt, playerID));
		allPrompts.Add(playerID, prompt);

		if (allPrompts.Count == playerCount)
		{
			Debug.Log("All prompts in!");
			AssignPhototgraphers();
			Instance.HostSwitchScene("PhotoSelecter");
		}
	}

	public void ReceiveUrl(int playerID, string url)
	{
		Debug.Log(string.Format("Received image {0} from {1}", url, playerID));
		imageUrls.Add(playerID, url);

		if (imageUrls.Count == playerCount)
		{
			Debug.Log("All images in!");
			Instance.HostSwitchScene("NewsRoom");
			Instance.StartCo(NewsRoomTimer());
		}
	}

	IEnumerator NewsRoomTimer()
	{
		yield return null; // wait for scene to load

		for (int i = 0; i < playerCount; ++i)
		{
			PromptAndImageCombo combo = GetNextPromptAndImage();

			Instance.ServerSendToAllClients("NEWSROOM_PROMPT", combo.prompt);
			Instance.ServerSendToAllClients("NEWSROOM_IMAGE", combo.url);

			NewsMaker.instance.SetHeadline(combo.prompt);
			NewsMaker.instance.SetImage(combo.url);

			yield return new WaitForSeconds(30);
		}

		Debug.Log("Game done!");
	}

	public void AssignPhototgraphers()
	{
		foreach (var prompt in allPrompts)
		{
			int offet = UnityEngine.Random.Range(1, playerCount);

			int photographer = prompt.Key + offet;

			// wrap player id
			if (photographer >= playerCount - 1)
			{
				photographer -= playerCount;
			}

			photographers.Add(prompt.Key, photographer);

			Debug.Log("Prompt " + prompt.Key + " has photographer " + photographer);
			if (photographer != -1)
				Instance.ServerSendToClient(photographer, "PHOTOGRAPHER_PROMPT", prompt.Value);
			else
				Instance.GetClient().SetPhotoPrompt(prompt.Value);
		}
	}

	int counter = -1;
	public struct PromptAndImageCombo
	{
		public string prompt;
		public string url;
	}
	public PromptAndImageCombo GetNextPromptAndImage()
	{
		PromptAndImageCombo combo = new();
		combo.prompt = allPrompts[counter];
		combo.url = imageUrls[photographers[counter]];

		++counter;

		return combo;
	}

	public int GetPlayerCount()
	{
		return playerCount;
	}

	public void ReceiveMessage(byte[] message, int playerNum, RtcConnection con)
	{
		string s = Encoding.UTF8.GetString(message);
		DeconstructedMessage m = GetMessageType(s);

		switch (m.type)
		{
			case "PROMPT":
				ReceivePrompt(playerNum, m.message);
				break;
			case "IMAGE_URL":
				ReceiveUrl(playerNum, m.message);
				break;
		}
	}
}
