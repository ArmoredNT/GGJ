using Unity.WebRTC;

[System.Serializable]
public enum LobbyPacketType
{
	unknown,
	request,
	response,
	connectReq,
	connectRes,
	start,
	lobbySize,
	rtcOffer,
	rtcAnswer,
	rtcICE,
	lobbyUpdate
}

[System.Serializable]
public class LobbyPacket
{
	public LobbyPacketType type = LobbyPacketType.unknown;

	public LobbyPacket(LobbyPacketType type)
	{
		this.type = type;
	}

	public LobbyPacket()
	{

	}
}

[System.Serializable]
public class LobbyPacketRequest : LobbyPacket
{
	public string name;

	public LobbyPacketRequest(string name) : base(LobbyPacketType.request)
	{
		this.name = name;
	}
}

[System.Serializable]
public class LobbyPacketResponse : LobbyPacket
{
	public bool success = false;
	public string code = "";

	public LobbyPacketResponse() : base(LobbyPacketType.response)
	{

	}
}

[System.Serializable]
public class LobbyConnectRequest : LobbyPacket
{
	public string code;
	public string name;

	public LobbyConnectRequest(string code, string name) : base(LobbyPacketType.connectReq)
	{
		this.code = code;
		this.name = name;
	}
}

[System.Serializable]
public class LobbyConnectResponse : LobbyPacket
{
	public bool success = false;

	public LobbyConnectResponse() : base(LobbyPacketType.connectRes)
	{

	}
}

[System.Serializable]
public class LobbyStartPacket : LobbyPacket
{
	public string code;

	public LobbyStartPacket(string code) : base(LobbyPacketType.start)
	{
		this.code = code;
	}
}

[System.Serializable]
public class LobbySizePacket : LobbyPacket
{
	public int size;

	public LobbySizePacket(int size) : base(LobbyPacketType.lobbySize)
	{
		this.size = size;
	}
}

[System.Serializable]
public class RtcOfferPacket : LobbyPacket
{
	public string code;
	public int player;
	public RTCSdpType rtcType;
	public string sdp;
	public bool toHost;

	public RtcOfferPacket()
	{
	}

	public RtcOfferPacket(string code, int player, bool toHost, RTCSdpType rtcType, string sdp) : base(LobbyPacketType.rtcOffer)
	{
		this.code = code;
		this.rtcType = rtcType;
		this.sdp = sdp;
		this.player = player;
		this.toHost = toHost;
	}
}

[System.Serializable]
public class RtcAnswerPacket : LobbyPacket
{
	public string code;
	public RTCSdpType rtcType;
	public string sdp;
	public int player;
	public bool toHost = false;

	public RtcAnswerPacket()
	{
	}

	public RtcAnswerPacket(string code, int player, bool toHost, RTCSdpType rtcType, string sdp) : base(LobbyPacketType.rtcAnswer)
	{
		this.rtcType = rtcType;
		this.sdp = sdp;
		this.code = code;
		this.player = player;
		this.toHost = toHost;
	}
}

[System.Serializable]
public class RtcIcePacket : LobbyPacket
{
	public string candidate;
	public string sdpMid;
	public int? sdpMLineIndex;
	public string code;
	public int player;
	public bool toHost = false;

	public RtcIcePacket()
	{
	}

	public RtcIcePacket(RTCIceCandidate candidate, string code, int player, bool toHost) : base(LobbyPacketType.rtcICE)
	{
		this.candidate = candidate.Candidate;
		this.sdpMid = candidate.SdpMid;
		this.sdpMLineIndex = candidate.SdpMLineIndex;
		this.code = code;
		this.player = player;
		this.toHost = toHost;
	}
}

[System.Serializable]
public class LobbyUpdatePacket : LobbyPacket
{
	public string[] players;
}
