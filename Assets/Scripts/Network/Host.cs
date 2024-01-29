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
	Dictionary<int, int> presenters = new(); // prompt #, player
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
			AssignPhototgraphersAndPresenters();
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
			PromptData combo = GetNextPromptData();

			Instance.ServerSendToAllClients("NEWSROOM_PROMPT", combo.prompt);
			Instance.ServerSendToAllClients("NEWSROOM_IMAGE", combo.url);
			Instance.ServerSendToAllClients("NEWSROOM_PRESENTER", combo.presenter);

			NewsMaker.instance.SetHeadline(combo.prompt);
			NewsMaker.instance.SetImage(combo.url);
			NewsMaker.instance.SetPresenter(combo.presenter);

			yield return new WaitForSeconds(30);
		}

		Debug.Log("Game done!");
	}

	public void AssignPhototgraphersAndPresenters()
	{
		int offset = UnityEngine.Random.Range(1, playerCount);

		foreach (var prompt in allPrompts)
		{
			int photographer = prompt.Key + offset;
			int presenter = prompt.Key + offset + 1;

			// wrap player id
			while (photographer >= playerCount - 1)
			{
				photographer -= playerCount;
			}
			while (presenter >= playerCount - 1)
			{
				presenter -= playerCount;
			}

			photographers.Add(prompt.Key, photographer);
			presenters.Add(prompt.Key, photographer);

			Debug.Log("Prompt " + prompt.Key + " has photographer " + photographer + " and presenter " + presenter);
			if (photographer != -1)
				Instance.ServerSendToClient(photographer, "PHOTOGRAPHER_PROMPT", prompt.Value);
			else
				Instance.GetClient().SetPhotoPrompt(prompt.Value);
		}
	}

	int counter = -1;
	public struct PromptData
	{
		public string prompt;
		public string url;
		public string presenter;
	}
	public PromptData GetNextPromptData()
	{
		PromptData combo = new();
		combo.prompt = allPrompts[counter];
		combo.url = imageUrls[photographers[counter]];
		combo.presenter = Instance.GetPlayerName(presenters[counter]);

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
