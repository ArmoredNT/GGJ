using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Host
{
	Dictionary<int, string> allPrompts = new(); // prompt #, prompt
	Dictionary<int, int> photographers = new(); // prompt id, player
	Dictionary<int, string> imageUrls = new(); // prompt id, image
	int playerCount;

	public void Init(int playerCount)
	{
		this.playerCount = playerCount;
		Debug.Log(this.playerCount + " players");
	}

	public void AddPrompt(int playerID, string prompt)
	{
		allPrompts.Add(playerID, prompt);

		if (allPrompts.Count == playerCount)
		{
			Debug.Log("All prompts in!");
			NetworkManager2.Instance.HostAllPromptsDone();
		}
	}

	public void AddUrl(int playerID, string url)
	{
		imageUrls.Add(playerID, url);

		if (imageUrls.Count == playerCount)
		{
			Debug.Log("All images in!");
			NetworkManager2.Instance.HostAllImagesDone();
		}
	}

	public void AssignPhototgraphers()
	{
		foreach (var prompt in allPrompts)
		{
			int offet = Random.Range(1, playerCount);

			int photographer = prompt.Key + offet;

			// wrap player id
			if (photographer >= playerCount - 1)
			{
				photographer -= playerCount;
			}

			photographers.Add(prompt.Key, photographer);

			Debug.Log("Prompt " + prompt.Key + " has photographer " + photographer);
			if (photographer != -1)
				NetworkManager2.Instance.SendDataToClient(photographer, "PHOTOGRAPHER:" + prompt.Value);
			else
				NetworkManager2.Instance.SetPhotoPrompt(prompt.Value);
		}
	}

	int confirmCounter = 0;
	public void OnConfirmImagePrompt()
	{
		confirmCounter++;

		Debug.Log(confirmCounter);
		Debug.Log(playerCount);
		if (confirmCounter == playerCount - 1)
		{
			NetworkManager2.Instance.HostLoadIntoPhotos();
		}
	}
	
	int loadedNewsRoomCounter = 0;
	public void ClientLoadedNewsRoom()
	{
		loadedNewsRoomCounter++;

		Debug.Log(confirmCounter);
		Debug.Log(playerCount);
		if (loadedNewsRoomCounter == playerCount - 1)
		{
			NetworkManager2.Instance.AllClientsInNewsRoom();
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
		combo.url = imageUrls[counter];

		++counter;

		return combo;
	}

	public int GetPlayerCount()
	{
		return playerCount;
	}
}
