using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkManager : MonoBehaviour
{
	public const ushort serverPort = 7777;

	ClientBehaviour client;
	ServerBehaviour server;

	private void Awake()
	{
		client = GetComponent<ClientBehaviour>();
		server = GetComponent<ServerBehaviour>();
	}

	public void StartServer()
	{
		Disconnect();

		Debug.Log("Starting server");

		server.Init();
		client.Init();
	}

	public void Disconnect()
	{
		client.Disconnect();
		server.Close();
	}
}
