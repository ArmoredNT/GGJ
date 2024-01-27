using System;
using UnityEngine;
using WebSocketSharp;

public class NetworkManager : MonoBehaviour
{
	// Port to connect to on the server
	// MAKE SURE THIS MATCHES ON THE SERVER!!!
	const ushort serverPort = 80;
	[SerializeField] string ip = "127.0.0.1";

	WebSocket ws;

	public void Connect()
	{
		ws = new WebSocket(string.Format("ws://{0}:{1}", ip, serverPort));
		ws.Connect();

		Debug.Log(string.Format("Connecting to ws://{0}:{1}", ip, serverPort));

		ws.OnOpen += OnOpen;
	}

	private void OnOpen(object sender, EventArgs e)
	{
		Debug.Log("Open");
		ws.Send("TEST");
	}
}