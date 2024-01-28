using System;
using UnityEngine;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using Unity.WebRTC;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;
using TMPro;

public class NetworkManager : MonoBehaviour
{
	public static NetworkManager Instance { get; private set; }

	const string defaultLobbyServerAddr = "wss://newsgame.onrender.com";

	[SerializeField] string customLobbyServerAddr = defaultLobbyServerAddr;
	[SerializeField] bool useCustomAddr = false;

	[SerializeField] TMP_InputField lobbyInput;

	private ClientWebSocket webSocket = null;

	RTCPeerConnection rtcConnection;
	RTCDataChannel sendChannel;

	bool isHost = false;
	int lobbySize = 0;
	string hostCode;

	#region Init
	private void Awake()
	{
		Instance = this;
	}

	private async Task InitWebSocket()
	{
		if (webSocket != null)
			await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Restarted", CancellationToken.None);

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

	private void InitRtc()
	{
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
	}
	#endregion

	#region Scene
	// TODO: Make a scene loader class
	private void OnSceneLoad()
	{
		GameObject[] hostOnlyObjects = GameObject.FindGameObjectsWithTag("HostOnly");
		foreach (GameObject hostObject in hostOnlyObjects)
		{
			hostObject.SetActive(isHost);
		}
	}

	private void GoToLobby()
	{
		var op = SceneManager.LoadSceneAsync("Beta_Lobby");
		op.completed += (x) =>
		{
			OnSceneLoad();
		};
	}
	#endregion

	#region Host
	public async void HostStartGame()
	{
		Debug.Log("Starting game and connecting peers");
		await ConnectP2P();
	}

	public void CreateLobbyDebug()
	{
		CreateLobby();
	}

	private async Task ConnectP2P()
	{
		// Tell the server to start
		await SendObjectToServer(new LobbyStartPacket(hostCode));

		// Get number of player to connect to
		LobbySizePacket packet = new(0);
		await ReceiveObjectFromServer(64, packet);

		lobbySize = packet.size;

		InitRtc();

		sendChannel = rtcConnection.CreateDataChannel("sendChannel");
		sendChannel.OnOpen = () =>
		{
			Debug.Log("Open");
		};

		StartCoroutine(HandshakeHost());
	}

	private IEnumerator HandshakeHost()
	{
		// Create offer
		var op1 = rtcConnection.CreateOffer();
		yield return op1;
		RTCSessionDescription desc = op1.Desc;
		var op2 = rtcConnection.SetLocalDescription(ref desc);
		yield return op2;

		// Send offer description
		Task send = SendObjectToServer(new RtcOfferPacket(hostCode, 0, desc.type, desc.sdp));
		yield return new WaitUntil(() => send.IsCompleted);

		// Wait for answer
		RtcAnswerPacket packet = new RtcAnswerPacket("", RTCSdpType.Offer, "");
		Task receive = ReceiveObjectFromServer(2048, packet);
		yield return new WaitUntil(() => receive.IsCompleted);

		RTCSessionDescription desc2 = new RTCSessionDescription();
		desc2.type = packet.rtcType;
		desc2.sdp = packet.sdp;
		rtcConnection.SetRemoteDescription(ref desc2);

		Debug.Log("RTC Connected");

		//var op3 = remoteConnection.SetRemoteDescription(ref op1.desc);
		//yield return op3;
		//var op4 = remoteConnection.CreateAnswer();
		//yield return op4;
		//var op5 = remoteConnection.setLocalDescription(op4.desc);
		//yield return op5;
		//var op6 = rtcConnection.setRemoteDescription(op4.desc);
		//yield return op6;
	}

	private async void CreateLobby()
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
				hostCode = packet.code;

				isHost = true;

				GoToLobby();
			}
			else
			{
				Debug.Log("Failed to create lobby");
			}
		}
	}
	#endregion

	#region Client
	public void ConnectClientDebug()
	{
		ConnectClient();
	}

	private async void ConnectClient()
	{
		await InitWebSocket();

		hostCode = lobbyInput.text;
		Debug.Log("Connection to lobby: " + hostCode);
		await SendObjectToServer(new LobbyConnectRequest(hostCode));

		// Wait for confirmation
		LobbyConnectResponse packet = new();
		await ReceiveObjectFromServer(64, packet);

		if (packet.type == LobbyPacketType.connectRes)
		{
			if (packet.success)
			{
				Debug.Log("Sucessfully joined lobbby!");
				GoToLobby();
			}
			else
			{
				Debug.Log("Failed to join lobby");
				return;
			}
		}

		// Start client lobby loop
		ClientLobbyLoop();
	}

	[Serializable]
	struct SdpThing
	{
		public RTCSdpType type;
		public string sdp;
	}

	private async void ClientLobbyLoop()
	{
		while (true)
		{
			// Wait for confirmation
			RtcOfferPacket packet = new(hostCode, 0, RTCSdpType.Offer, "");
			await ReceiveObjectFromServer(2048, packet);

			switch (packet.type)
			{
				case LobbyPacketType.rtcOffer:
					RtcOfferPacket rtcPacket = packet;

					RTCSessionDescription desc = new();
					desc.type = rtcPacket.rtcType;
					desc.sdp = rtcPacket.sdp;

					Debug.Log("Connecting to host");

					InitRtc();
					StartCoroutine(ClientHandshake(desc));
					break;
			}
		}
	}

	// After receiving offer from server
	private IEnumerator ClientHandshake(RTCSessionDescription desc)
	{
		yield return rtcConnection.SetRemoteDescription(ref desc);
		var op1 = rtcConnection.CreateAnswer();
		yield return op1;

		RTCSessionDescription desc2 = op1.Desc;
		yield return rtcConnection.SetLocalDescription(ref desc2);

		// Send answer to server
		Task send = SendObjectToServer(new RtcAnswerPacket(hostCode, desc2.type, desc2.sdp));
		yield return new WaitUntil(() => send.IsCompleted);
	}
	#endregion

	private async Task ReceiveObjectFromServer(int bufferLength, object obj)
	{
		var recvBuffer = new byte[bufferLength];
		await webSocket.ReceiveAsync(recvBuffer, CancellationToken.None);

		Debug.Log(Encoding.UTF8.GetString(recvBuffer));

		JsonUtility.FromJsonOverwrite(Encoding.UTF8.GetString(recvBuffer), obj);
	}

	private async Task SendObjectToServer(object obj)
	{
		var encoded = Encoding.UTF8.GetBytes(JsonUtility.ToJson(obj));
		var buffer = new ArraySegment<byte>(encoded, 0, encoded.Length);
		await webSocket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
	}
}
