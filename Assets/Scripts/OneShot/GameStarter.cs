using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameStarter : MonoBehaviour
{
	public void StartGame()
	{
		NetworkManager2.Instance.HostStartGame();
	}
}
