using System;
using UnityEngine;
using System.Net.WebSockets;
using System.Text;
using System.Threading;

public class NetworkManager : MonoBehaviour
{
	private ClientWebSocket webSocket = null;

	public void DebugConnect()
	{
		Connect();
	}

	private async void Connect()
	{
		Debug.Log("TESTPACKET!!!!!");

		webSocket = new ClientWebSocket();
		try
		{
			await webSocket.ConnectAsync(new Uri("wss://newsgame.onrender.com"), CancellationToken.None);
			await SendTestPacket();
		}
		catch (Exception ex)
		{
			Debug.Log("WebSocket connection exception: " + ex.ToString());
		}
	}

	private async System.Threading.Tasks.Task SendTestPacket()
	{
		if (webSocket.State == WebSocketState.Open)
		{
			var encoded = Encoding.UTF8.GetBytes("TEST");
			var buffer = new ArraySegment<Byte>(encoded, 0, encoded.Length);
			await webSocket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
		}
	}
}
