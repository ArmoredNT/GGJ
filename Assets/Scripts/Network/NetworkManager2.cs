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

public class RtcConnection
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
	[SerializeField] TMP_InputField nameInput;

	private ClientWebSocket webSocket = null;

	RtcConnection[] connections;
	string[] playerNames;
	Host host;
	Client client;

	bool isHost = false;
	string lobbyCode;

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
		webSocket?.CloseAsync(WebSocketCloseStatus.NormalClosure, "Disconnect", CancellationToken.None);
		webSocket?.Dispose();
	}
	#endregion

	#region Scene
	public void HostSwitchScene(string name)
	{
		GoToScene(name);
		ServerSendToAllClients("SCENE", name);
	}

	public void ClientSetScene(string name)
	{
		GoToScene(name);
		// ClientSendToServer("SCENE_CONFIRM:" + name);
	}

	// TODO: Make a scene loader class
	private void OnSceneLoad()
	{
		GameObject[] hostOnlyObjects = GameObject.FindGameObjectsWithTag("HostOnly");
		foreach (GameObject hostObject in hostOnlyObjects)
		{
			hostObject.SetActive(isHost);
		}
	}

	private void GoToSceneAsync(string name)
	{
		var op = SceneManager.LoadSceneAsync(name);
		op.completed += (x) =>
		{
			OnSceneLoad();
		};
	}

	private void GoToScene(string name)
	{
		SceneManager.LoadScene(name);
		StartCoroutine(OnSceneLoadCoroutine());
	}

	private IEnumerator OnSceneLoadCoroutine()
	{
		yield return null;
		OnSceneLoad();
	}
	#endregion

	#region Net
	public struct DeconstructedMessage
	{
		public string type;
		public string message;
	}

	static public DeconstructedMessage GetMessageType(string message)
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

	public bool GetIsHost()
	{
		return isHost;
	}

	public Host GetHost()
	{
		return host;
	}

	public Client GetClient()
	{
		return client;
	}

	public string GetLobbyCode()
	{
		return lobbyCode;
	}
	#endregion

	#region WebSocket
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

	public async void HostLobby()
	{
		await InitWebSocket();

		string name = nameInput.text;

		// Send a message to the main server to create a lobby
		await WSSendObjectToServer(new LobbyPacketRequest(name));

		// Wait for confirmation
		LobbyPacketResponse packet = new();
		await WSReceiveObjectFromServer(packet, new byte[64]);

		if (packet.type == LobbyPacketType.response)
		{
			if (packet.success)
			{
				Debug.Log("Sucessfully created a lobby!");
				Debug.Log(packet.code);
				lobbyCode = packet.code;

				isHost = true;

				GoToScene("LobbyUi");
				HostLoop();
			}
			else
			{
				Debug.Log("Failed to create lobby");
			}
		}
	}

	private void GetPacket<T>(byte[] bytes, T packet)
	{
		JsonUtility.FromJsonOverwrite(Encoding.UTF8.GetString(bytes), packet);
	}

	private async Task WSReceiveObjectFromServer(LobbyPacket packet, byte[] buffer)
	{
		await webSocket.ReceiveAsync(buffer, CancellationToken.None);

		// Don't ptry to parse null strings
		if (buffer.Length == 0 || buffer[0] == 0)
		{
			return;
		}

		Debug.Log(Encoding.UTF8.GetString(buffer));

		GetPacket(buffer, packet);
	}

	private async Task WSSendObjectToServer(object obj)
	{
		var encoded = Encoding.UTF8.GetBytes(JsonUtility.ToJson(obj));
		var buffer = new ArraySegment<byte>(encoded, 0, encoded.Length);
		await webSocket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
	}
	#endregion

	#region Intermediate
	private async void SendICE(RTCIceCandidate candidate, int playerNum)
	{
		await WSSendObjectToServer(new RtcIcePacket(candidate, lobbyCode, playerNum, !isHost));
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
	#region Host
	public async void HostStartGame()
	{
		Debug.Log("Starting game and connecting peers");
		await ConnectP2P();
	}

	// Connect host to each client
	private async Task ConnectP2P()
	{
		string name = nameInput.text;

		// Tell the server to start
		await WSSendObjectToServer(new LobbyStartPacket(lobbyCode));

		host = new Host();
		client = new Client();
		host.Init(playerNames.Length);

		int lobbySize = playerNames.Length - 1;

		connections = new RtcConnection[lobbySize];

		for (int i = 0; i < lobbySize; i++)
		{
			connections[i] = InitRtc(i);
		}

		// Connect to each player
		for (int i = 0; i < lobbySize; i++)
		{
			RtcConnection con = connections[i];

			RTCDataChannelInit init = new()
			{
				ordered = true
			};
			con.sendChannel = con.rtcConnection.CreateDataChannel("sendChannel", init);
			con.sendChannel.OnOpen = () =>
			{
				Debug.Log("Open");
				con.sendOpen = true;
				con.sendChannel.Send("TEST WOWOWOWOWO!!!");
			};
			int numCpy = i; // Unless we do this stuff breaks
			con.sendChannel.OnMessage = (message) =>
			{
				host.ReceiveMessage(message, numCpy, con);
			};

			StartCoroutine(HostCreateOffer(con, i));
		}

		StartCoroutine(WaitTillAllClientsConnected());
	}

	private async void HostLoop()
	{
		while (true)
		{
			// Wait for answer or ice
			LobbyPacket packet = new();
			byte[] data = new byte[2048];

			// todo: because of this we get stuck forever in another thread
			// BUT WHO CARES
			await WSReceiveObjectFromServer(packet, data);

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
				case LobbyPacketType.lobbyUpdate:
					LobbyUpdatePacket updatePacket = new();
					GetPacket(data, updatePacket);
					Debug.Log(updatePacket.players);
					playerNames = updatePacket.players;
					UpdateLobbyUI();
					break;
			}

			if (connections != null && connections.Length == 0) break;
		}

		Debug.Log("Host loop done");
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
		Task send = WSSendObjectToServer(new RtcOfferPacket(lobbyCode, playerNum, desc.type, desc.sdp));
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

	IEnumerator WaitTillAllClientsConnected()
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

		yield return new WaitForSeconds(1); // debug

		HostSwitchScene("Beta_Intro");
	}
	#endregion
	#region Client
	public async void ConnectClient()
	{
		await InitWebSocket();

		lobbyCode = lobbyInput.text.ToUpper();
		string name = nameInput.text;
		Debug.Log("Connecting to lobby: " + lobbyCode);
		await WSSendObjectToServer(new LobbyConnectRequest(lobbyCode, name));

		// Wait for confirmation
		LobbyConnectResponse connectPacket = new();
		await WSReceiveObjectFromServer(connectPacket, new byte[64]);

		if (connectPacket.type == LobbyPacketType.connectRes)
		{
			if (connectPacket.success)
			{
				Debug.Log("Sucessfully joined lobbby!");
				GoToSceneAsync("LobbyUi");
			}
			else
			{
				Debug.Log("Failed to join lobby");
				return;
			}
		}

		client = new Client();

		RtcConnection connection = InitRtc();
		connection.rtcConnection.OnDataChannel = (channel) =>
		{
			Debug.Log("Data channel");
			connection.sendChannel = channel;
			connection.sendOpen = true;
			connection.sendChannel.OnMessage = client.ReceiveMessage;

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
			await WSReceiveObjectFromServer(packet, data);

			switch (packet.type)
			{
				case LobbyPacketType.rtcOffer:
					Debug.Log("Connecting to host");
					RtcOfferPacket offerPacket = new();
					GetPacket(data, offerPacket);

					// Get my player num
					client.SetPlayerId(offerPacket.player);
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
				case LobbyPacketType.lobbyUpdate:
					LobbyUpdatePacket updatePacket = new();
					GetPacket(data, updatePacket);
					Debug.Log(updatePacket.players);
					playerNames = updatePacket.players;
					UpdateLobbyUI();
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
		Task send = WSSendObjectToServer(new RtcAnswerPacket(lobbyCode, client.GetPlayerId(), desc2.type, desc2.sdp));
		yield return new WaitUntil(() => send.IsCompleted);

		Debug.Log("Client sent answer");
	}
	#endregion
	#endregion

	#region RTC
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
					SendICE(e, client.GetPlayerId());
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

	public void ServerSendToAllClients(string type, string message)
	{
		string full = type + ":" + message;
		Debug.Log(full);

		foreach (var connection in connections)
		{
			connection.sendChannel.Send(full);
		}
	}

	public void ServerSendToClient(int id, string type, string message)
	{
		string full = type + ":" + message;
		Debug.Log(id);
		Debug.Log(full);

		connections[id].sendChannel.Send(full);
	}

	public void ClientSendToServer(string type, string message)
	{
		string full = type + ":" + message;
		connections[0].sendChannel.Send(Encoding.UTF8.GetBytes(full));
	}
	#endregion

	public void StartCo(IEnumerator co)
	{
		StartCoroutine(co);
	}

	void UpdateLobbyUI()
	{

	}
}
