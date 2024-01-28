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

class RtcConnection
{
	public RTCPeerConnection rtcConnection;
	public RTCDataChannel sendChannel;
	public bool sendOpen = false;

	public void Close()
	{
		sendChannel?.Close();
		rtcConnection?.Close();
	}

	public bool NotFullyConnected()
	{
		return rtcConnection.ConnectionState == RTCPeerConnectionState.Connecting
			|| rtcConnection.ConnectionState == RTCPeerConnectionState.New
			|| !sendOpen;
	}
}

[RequireComponent(typeof(DontDestroy))]
public class NetworkManager2 : MonoBehaviour
{
	public static NetworkManager2 Instance { get; private set; }

	const string defaultLobbyServerAddr = "wss://newsgame.onrender.com";

	[SerializeField] string customLobbyServerAddr = defaultLobbyServerAddr;
	[SerializeField] bool useCustomAddr = false;

	[SerializeField] TMP_InputField lobbyInput;

	private ClientWebSocket webSocket = null;

	RtcConnection[] connections;
	Host host;

	bool isHost = false;
	string hostCode;
	string photoPrompt;

	int clientPlayerNum = -1;

	#region Init
	private void Awake()
	{
		Instance = this;
	}

	private void OnDestroy()
	{
		Disconnect();
	}

	private void Disconnect()
	{
		if (connections != null)
			foreach (var connection in connections)
			{
				connection.Close();
			}

		connections = null;
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

	private RtcConnection InitRtc(int playerNum = -1)
	{
		RtcConnection connection = new();

		connection.rtcConnection = new RTCPeerConnection();

		connection.rtcConnection.OnIceCandidate = e =>
		{
			Debug.Log(e.Candidate);
			if (!string.IsNullOrEmpty(e.Candidate))
			{
				if (playerNum == -1)
					SendICE(e, clientPlayerNum);
				else
					SendICE(e, playerNum);
			}
		};

		connection.rtcConnection.OnIceConnectionChange = state =>
		{
			Debug.Log("ICE " + state);
		};

		connection.rtcConnection.OnConnectionStateChange = state =>
		{
			Debug.Log("State " + state);
		};

		connection.rtcConnection.OnNegotiationNeeded = () =>
		{
			Debug.Log("Negotiation needed!");
		};

		RTCConfiguration rtcConfiguration = new();
		rtcConfiguration.iceServers = new RTCIceServer[] { new() {
			urls = new string[]
			{
				"stun:stun.relay.metered.ca:80"
			}
		},
		new() {
			urls = new string[]
			{
				"stun:stun.l.google.com:19302"
			}
		},
		new() {
			urls = new string[]
			{
				"turn:standard.relay.metered.ca:80"
			},
			username = "bb975cabc169e1c48aa23c54",
			credential = "eAFvAGwqqhORmq+x"
		},
		new() {
			urls = new string[]
			{
				"turn:standard.relay.metered.ca:80?transport=tcp"
			},
			username = "bb975cabc169e1c48aa23c54",
			credential = "eAFvAGwqqhORmq+x"
		},
		new() {
			urls = new string[]
			{
				"turn:standard.relay.metered.ca:443"
			},
			username = "bb975cabc169e1c48aa23c54",
			credential = "eAFvAGwqqhORmq+x"
		},
		new() {
			urls = new string[]
			{
				"turns:standard.relay.metered.ca:443?transport=tcp"
			},
			username = "bb975cabc169e1c48aa23c54",
			credential = "eAFvAGwqqhORmq+x"
		}};
		rtcConfiguration.iceTransportPolicy = RTCIceTransportPolicy.All;
		rtcConfiguration.iceCandidatePoolSize = 1; // What does this do? idk...
		connection.rtcConnection.SetConfiguration(ref rtcConfiguration);

		return connection;
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

	private void GoToScene(string name)
	{
		var op = SceneManager.LoadSceneAsync(name);
		op.completed += (x) =>
		{
			OnSceneLoad();
		};
	}

	private void GoToLobby()
	{
		GoToScene("LobbyUI");
	}

	private void GoToIntro()
	{
		GoToScene("Beta_Intro");
	}

	private void GoToPrompt()
	{
		GoToScene("PromptCreator");
	}

	private void GoToPhotos()
	{
		GoToScene("Beta_Photos");
	}

	private void GoToNews()
	{
		GoToScene("Beta_News");
	}
	#endregion

	#region Host
	public async void HostLobby()
	{
		await InitWebSocket();

		// Send a message to the main server to create a lobby
		await SendObjectToServer(new LobbyPacket(LobbyPacketType.request));

		// Wait for confirmation
		LobbyPacketResponse packet = new();
		await ReceiveObjectFromServer(packet, new byte[64]);

		if (packet.type == LobbyPacketType.response)
		{
			if (packet.success)
			{
				Debug.Log("Sucessfully created a lobby!");
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

	public async void HostStartGame()
	{
		Debug.Log("Starting game and connecting peers");
		await ConnectP2P();
	}

	public void HostEndIntro()
	{
		SendDataToAllClients("GOTO:PROMPT");
		GoToPrompt();
	}

	// Connect host to each client
	private async Task ConnectP2P()
	{
		// Tell the server to start
		await SendObjectToServer(new LobbyStartPacket(hostCode));

		// Get number of player to connect to
		LobbySizePacket packet = new(-1);
		await ReceiveObjectFromServer(packet, new byte[64]);

		int lobbySize = packet.size;

		Disconnect();

		connections = new RtcConnection[lobbySize];

		for (int i = 0; i < lobbySize; i++)
		{
			connections[i] = InitRtc(i);
		}

		HostLoop();

		// Connect to each player
		for (int i = 0; i < lobbySize; i++)
		{
			RtcConnection con = connections[i];

			con.sendChannel = con.rtcConnection.CreateDataChannel("sendChannel");
			con.sendChannel.OnOpen = () =>
			{
				Debug.Log("Open");
				con.sendOpen = true;
				con.sendChannel.Send("TEST WOWOWOWOWO!!!");
			};
			int numCpy = i; // Unless we do this stuff breaks
			con.sendChannel.OnMessage = (message) =>
			{
				OnMessageFromClient(message, numCpy, con);
			};

			StartCoroutine(HostCreateOffer(con, i));

			// await Task.Delay(5000);
		}

		StartCoroutine(WaitTillAllClientsDone());
	}

	private void OnMessageFromClient(byte[] message, int playerNum, RtcConnection con)
	{
		string s = Encoding.UTF8.GetString(message);
		DeconstructedMessage m = GetMessageType(s);

		switch (m.type)
		{
			case "PROMPT":
				HostAddPrompt(playerNum, m.message);
				break;
			case "LOADED_PHOTO_PROMPT":
				host.OnConfirmImagePrompt();
				break;
			case "IMAGE":
				host.AddUrl(playerNum, m.message);
				break;
		}
	}

	IEnumerator WaitTillAllClientsDone()
	{
		while (true)
		{
			bool notConnected = false;
			foreach (var connection in connections)
			{
				notConnected |= connection.NotFullyConnected();
			}

			if (!notConnected) break;

			yield return null;
		}
		Debug.Log("All connected");

		host = new Host();
		host.Init(connections.Length + 1);

		SendDataToAllClients("GOTO:INTRO");
		GoToIntro();
	}

	public void SendDataToAllClients(string message)
	{
		Debug.Log(message);

		foreach (var connection in connections)
		{
			connection.sendChannel.Send(message);
		}
	}

	public void SendDataToClient(int id, string message)
	{
		Debug.Log(id);
		Debug.Log(message);

		connections[id].sendChannel.Send(message);
	}

	private IEnumerator HostCreateOffer(RtcConnection connection, int playerNum)
	{
		// Create offer
		var op1 = connection.rtcConnection.CreateOffer();
		yield return op1;

		// Set local desc
		RTCSessionDescription desc = op1.Desc;
		yield return connection.rtcConnection.SetLocalDescription(ref desc);

		// Send offer description
		Debug.Log(playerNum);
		Task send = SendObjectToServer(new RtcOfferPacket(hostCode, playerNum, desc.type, desc.sdp));
		yield return new WaitUntil(() => send.IsCompleted);

		Debug.Log("Host sent offer");
	}

	private void HostAcceptAnswer(RtcAnswerPacket packet, RtcConnection connection)
	{
		RTCSessionDescription desc = new();
		desc.type = packet.rtcType;
		desc.sdp = packet.sdp;
		connection.rtcConnection.SetRemoteDescription(ref desc);

		Debug.Log("Host accepted answer");
	}

	private async void HostLoop()
	{
		while (true)
		{
			// Wait for answer or ice
			LobbyPacket packet = new();
			byte[] data = new byte[2048];
			await ReceiveObjectFromServer(packet, data);

			switch (packet.type)
			{
				case LobbyPacketType.rtcAnswer:
					RtcAnswerPacket answerPacket = new();
					GetPacket(data, answerPacket);
					Debug.Log(answerPacket.player);
					HostAcceptAnswer(answerPacket, connections[answerPacket.player]);
					break;
				case LobbyPacketType.rtcICE:
					RtcIcePacket icePacket = new();
					GetPacket(data, icePacket);
					Debug.Log(icePacket.player);

					OnReceiveICE(icePacket, connections[icePacket.player]);
					break;
			}

			if (connections.Length == 0) break;
		}

		Debug.Log("Host loop done");
	}

	private void HostAddPrompt(int playerID, string prompt)
	{
		Debug.Log(string.Format("Got prompt {0} from {1}", prompt, playerID));

		host.AddPrompt(playerID, prompt);
	}

	private void HostAddUrl(int playerID, string url)
	{
		Debug.Log(string.Format("Got image {0} from {1}", url, playerID));

		host.AddUrl(playerID, url);
	}

	public void HostAllPromptsDone()
	{
		host.AssignPhototgraphers();
	}

	public void HostAllImagesDone()
	{
		SendDataToAllClients("GOTO:NEWS");
		GoToNews();
	}

	public void HostLoadIntoPhotos()
	{
		Debug.Log("Photos");
		SendDataToAllClients("GOTO:PHOTOS");
		GoToPhotos();
	}
	#endregion

	#region Client
	public async void ConnectClient()
	{
		await InitWebSocket();

		hostCode = lobbyInput.text.ToUpper();
		Debug.Log("Connecting to lobby: " + hostCode);
		await SendObjectToServer(new LobbyConnectRequest(hostCode));

		// Wait for confirmation
		LobbyConnectResponse connectPacket = new();
		await ReceiveObjectFromServer(connectPacket, new byte[64]);

		if (connectPacket.type == LobbyPacketType.connectRes)
		{
			if (connectPacket.success)
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

		Disconnect();

		RtcConnection connection = InitRtc();
		connection.rtcConnection.OnDataChannel = (channel) =>
		{
			Debug.Log("Data channel");
			connection.sendChannel = channel;
			connection.sendOpen = true;
			connection.sendChannel.OnMessage = (message) =>
			{
				OnMessageFromServer(message);
			};

			connection.sendChannel.Send("TEST 2 YAYAYAYYAY!");
		};

		connections = new RtcConnection[1];
		connections[0] = connection;
		ClientLoop(connection);
	}

	private async void ClientLoop(RtcConnection connection)
	{
		while (connection.rtcConnection.ConnectionState == RTCPeerConnectionState.Connecting
			|| connection.rtcConnection.ConnectionState == RTCPeerConnectionState.New)
		{
			// Wait for ice candidates
			LobbyPacket packet = new();
			byte[] data = new byte[2048];
			await ReceiveObjectFromServer(packet, data);

			switch (packet.type)
			{
				case LobbyPacketType.rtcOffer:
					Debug.Log("Connecting to host");
					RtcOfferPacket offerPacket = new();
					GetPacket(data, offerPacket);

					// Get my player num
					clientPlayerNum = offerPacket.player;
					Debug.Log("I'm player #" + offerPacket.player);

					RTCSessionDescription desc = new()
					{
						type = offerPacket.rtcType,
						sdp = offerPacket.sdp
					};

					StartCoroutine(ClientAcceptOffer(desc, connection));
					break;
				case LobbyPacketType.rtcICE:
					Debug.Log("Ice");
					RtcIcePacket icePacket = new();
					GetPacket(data, icePacket);
					OnReceiveICE(icePacket, connection);
					break;
			}
		}

		Debug.Log("Client loop done");
	}

	private IEnumerator ClientAcceptOffer(RTCSessionDescription desc, RtcConnection connection)
	{
		Debug.Log("Client accepted offer");

		// Set remote desc
		yield return connection.rtcConnection.SetRemoteDescription(ref desc);

		// Create answer
		var op1 = connection.rtcConnection.CreateAnswer();
		yield return op1;

		// Set local desc
		RTCSessionDescription desc2 = op1.Desc;
		yield return connection.rtcConnection.SetLocalDescription(ref desc2);

		// Send answer to server
		Task send = SendObjectToServer(new RtcAnswerPacket(hostCode, clientPlayerNum, desc2.type, desc2.sdp));
		yield return new WaitUntil(() => send.IsCompleted);

		Debug.Log("Client sent answer");
	}

	private void OnMessageFromServer(byte[] message)
	{
		string s = Encoding.UTF8.GetString(message);
		Debug.Log(s);

		DeconstructedMessage m = GetMessageType(s);
		Debug.Log(m.type);
		Debug.Log(m.message);

		switch (m.type)
		{
			case "GOTO":
				switch (m.message)
				{
					case "INTRO":
						GoToIntro();
						break;
					case "PROMPT":
						GoToPrompt();
						break;
					case "PHOTOS":
						GoToPhotos();
						break;
					case "NEWS":
						GoToNews();
						break;
				}
				break;
			case "PHOTOGRAPHER":
				SetPhotoPrompt(m.message);
				break;
		}
	}

	public void SetPhotoPrompt(string prompt)
	{
		photoPrompt = prompt;

		ClientSendToServer("LOADED_PHOTO_PROMPT:");
	}

	public void ClientSendToServer(string message)
	{
		connections[0].sendChannel.Send(Encoding.UTF8.GetBytes(message));
	}

	private void ClientSendPrompt(string prompt)
	{
		ClientSendToServer("PROMPT:" + prompt);
	}

	private void ClientSendUrl(string url)
	{
		ClientSendToServer("IMAGE:" + url);
	}
	#endregion

	public void SendPrompt(string prompt)
	{
		if (!isHost)
		{
			ClientSendPrompt(prompt);
		}
		else
		{
			HostAddPrompt(-1, prompt);
		}
	}

	public void SendChosenUrl(string url)
	{
		if (!isHost)
		{
			ClientSendUrl(url);
		}
		else
		{
			HostAddUrl(-1, url);
		}
	}

	public string GetLobbyCode()
	{
		return hostCode;
	}

	public string GetPhotoPrompt()
	{
		return photoPrompt;
	}

	#region Net
	struct DeconstructedMessage
	{
		public string type;
		public string message;
	}

	private DeconstructedMessage GetMessageType(string message)
	{
		int index = message.IndexOf(':');
		Debug.Log(message);
		Debug.Log(index);

		var m = new DeconstructedMessage();

		if (index == -1)
		{
			m.type = "";
			m.message = message;
			return m;
		}

		m.type = message[..index];
		m.message = message[(index + 1)..];

		return m;
	}

	private void OnReceiveICE(RtcIcePacket packet, RtcConnection connection)
	{
		RTCIceCandidateInit init = new()
		{
			sdpMid = packet.sdpMid,
			candidate = packet.candidate,
			sdpMLineIndex = packet.sdpMLineIndex
		};

		RTCIceCandidate can = new(init);

		connection.rtcConnection.AddIceCandidate(can);
	}

	private async void SendICE(RTCIceCandidate candidate, int playerNum)
	{
		await SendObjectToServer(new RtcIcePacket(candidate, hostCode, playerNum, !isHost));
	}


	private void GetPacket<T>(byte[] bytes, T packet)
	{
		JsonUtility.FromJsonOverwrite(Encoding.UTF8.GetString(bytes), packet);
	}

	private async Task ReceiveObjectFromServer(LobbyPacket packet, byte[] buffer)
	{
		await webSocket.ReceiveAsync(buffer, CancellationToken.None);

		// Don't ptry to parse null strings
		if (buffer.Length == 0 || buffer[0] == 0)
		{
			return;
		}

		// Debug.Log(Encoding.UTF8.GetString(buffer));

		GetPacket(buffer, packet);
	}

	private async Task SendObjectToServer(object obj)
	{
		var encoded = Encoding.UTF8.GetBytes(JsonUtility.ToJson(obj));
		var buffer = new ArraySegment<byte>(encoded, 0, encoded.Length);
		await webSocket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
	}
	#endregion
}
