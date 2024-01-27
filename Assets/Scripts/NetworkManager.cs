using System;
using UnityEngine;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using Unity.WebRTC;
using System.Collections;
using Unity.RenderStreaming;
using System.Threading.Tasks;

public class NetworkManager : MonoBehaviour
{
	const string defaultLobbyServerAddr = "wss://newsgame.onrender.com";

	[SerializeField] string customLobbyServerAddr = defaultLobbyServerAddr;
	[SerializeField] bool useCustomAddr = false;

	private ClientWebSocket webSocket = null;

	RTCPeerConnection rtcConnection;
	RTCDataChannel sendChannel;

	[SerializeField] string debugLobbyCode;

	public void DebugConnect()
	{
		Connect();
	}

	private async void Connect()
	{
		await InitWebSocket();

		// Connect P2P
		rtcConnection = new RTCPeerConnection();
		rtcConnection.OnDataChannel = (channel) =>
		{
			Debug.Log("Data channel");
		};

		rtcConnection.OnIceCandidate = e =>
		{
			if (!string.IsNullOrEmpty(e.Candidate))
				rtcConnection.AddIceCandidate(e);
		};

		sendChannel = rtcConnection.CreateDataChannel("sendChannel");
		sendChannel.OnOpen = () =>
		{
			Debug.Log("Open");
		};

		StartCoroutine(Handshake());
	}

	private async Task InitWebSocket()
	{
		// Create websocket connection to the main server
		webSocket = new ClientWebSocket();
		try
		{
			await webSocket.ConnectAsync(new Uri(useCustomAddr ? customLobbyServerAddr : defaultLobbyServerAddr), CancellationToken.None);
		}
		catch (Exception ex)
		{
			Debug.Log("WebSocket connection exception: " + ex.ToString());
		}
	}

	IEnumerator Handshake()
	{
		// Create offer
		var op1 = rtcConnection.CreateOffer();
		yield return op1;
		RTCSessionDescription desc = op1.Desc;
		var op2 = rtcConnection.SetLocalDescription(ref desc);
		yield return op2;

		// Send offer description

		// Wait for answer



		//var op3 = remoteConnection.SetRemoteDescription(ref op1.desc);
		//yield return op3;
		//var op4 = remoteConnection.CreateAnswer();
		//yield return op4;
		//var op5 = remoteConnection.setLocalDescription(op4.desc);
		//yield return op5;
		//var op6 = rtcConnection.setRemoteDescription(op4.desc);
		//yield return op6;
	}

	private async Task SendTestPacket()
	{
		if (webSocket.State == WebSocketState.Open)
		{
			var encoded = Encoding.UTF8.GetBytes("TEST PACKET!!!!");
			var buffer = new ArraySegment<byte>(encoded, 0, encoded.Length);
			await webSocket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
		}
	}

	public void StartHostDebug()
	{
		StartHost();
	}

	private async void StartHost()
	{
		await InitWebSocket();

		// Send a message to the main server to create a lobby
		await SendObjectToServer(new LobbyPacket(LobbyPacketType.request));

		// Wait for confirmation
		LobbyPacketResponse packet = new();
		await ReceiveObjectFromServer(64, packet);

		if (packet.type == LobbyPacketType.response)
		{
			if (packet.success)
			{
				Debug.Log("Sucessfully created a lobbby!");
				Debug.Log(packet.code);
			}
			else
			{
				Debug.Log("Failed to create lobby");
			}
		}
	}

	private async Task SendObjectToServer(object obj)
	{
		var encoded = Encoding.UTF8.GetBytes(JsonUtility.ToJson(obj));
		var buffer = new ArraySegment<byte>(encoded, 0, encoded.Length);
		await webSocket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
	}

	private async Task ReceiveObjectFromServer(int bufferLength, object obj)
	{
		var recvBuffer = new byte[bufferLength];
		await webSocket.ReceiveAsync(recvBuffer, CancellationToken.None);

		JsonUtility.FromJsonOverwrite(Encoding.UTF8.GetString(recvBuffer), obj);
	}
}
