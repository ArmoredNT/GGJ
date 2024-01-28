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

	public void Close()
	{
		sendChannel?.Close();
		rtcConnection?.Close();
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

	bool isHost = false;
	int lobbySize = 0;
	string hostCode;

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
		connection.rtcConnection.OnDataChannel = (channel) =>
		{
			Debug.Log("Data channel");
			connection.sendChannel = channel;
			channel.OnMessage = (message) =>
			{
				Debug.Log(Encoding.UTF8.GetString(message));
			};

			channel.Send("TEST 2 YAYAYAYYAY!");
		};

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

	// Connect host to each client
	private async Task ConnectP2P()
	{
		// Tell the server to start
		await SendObjectToServer(new LobbyStartPacket(hostCode));

		// Get number of player to connect to
		LobbySizePacket packet = new(-1);
		await ReceiveObjectFromServer(packet, new byte[64]);

		lobbySize = packet.size;

		Disconnect();

		connections = new RtcConnection[lobbySize];

		for (int i = 0; i < lobbySize; i++)
		{
			RtcConnection connection = InitRtc(i);
			connections[i] = connection;
		}

		HostLoop();

		// Connect to each player
		for (int i = 0; i < lobbySize; i++)
		{
			connections[i].sendChannel = connections[i].rtcConnection.CreateDataChannel("sendChannel");
			int numCpy = i;
			connections[i].sendChannel.OnOpen = () =>
			{
				connections[numCpy].sendChannel.Send("TEST WOWOWOWOWO!!!");
			};
			connections[i].sendChannel.OnMessage = (message) =>
			{
				Debug.Log(Encoding.UTF8.GetString(message));
			};

			StartCoroutine(HostCreateOffer(connections[i], i));

			// await Task.Delay(5000);
		}
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

			bool br = true;
			foreach (var connection in connections)
			{
				if (connection.rtcConnection.ConnectionState == RTCPeerConnectionState.Connecting
					|| connection.rtcConnection.ConnectionState == RTCPeerConnectionState.New)
				{
					br = false;
					break;
				}
			}
			// Exit loop when we've connected
			if (br) break;
		}

		Debug.Log("Host loop done");
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
	#endregion

	#region Net
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
