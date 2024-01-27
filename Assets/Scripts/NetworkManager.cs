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

	public void StartHost()
	{
		client.Disconnect();
		server.Close();

		Debug.Log("Starting host");

		server.Init();
		client.Init();
	}

	public void StartClient()
	{
		client.Disconnect();

		Debug.Log("Connecting client");

		client.Init();
	}

	public void StartServer()
	{
		server.Close();

		Debug.Log("Connecting server");

		server.Init();
	}
}
