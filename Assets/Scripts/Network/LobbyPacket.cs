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
	rtcAnswer
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

	public LobbyConnectRequest(string code) : base(LobbyPacketType.connectReq)
	{
		this.code = code;
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
	public int targetPlayer;
	public RTCSdpType rtcType;
	public string sdp;

	public RtcOfferPacket(string code, int targetPlayer, RTCSdpType rtcType, string sdp) : base(LobbyPacketType.rtcOffer)
	{
		this.code = code;
		this.rtcType = rtcType;
		this.sdp = sdp;
		this.targetPlayer = targetPlayer;
	}
}

[System.Serializable]
public class RtcAnswerPacket : LobbyPacket
{
	public string code;
	public RTCSdpType rtcType;
	public string sdp;

	public RtcAnswerPacket(string code, RTCSdpType rtcType, string sdp) : base(LobbyPacketType.rtcAnswer)
	{
		this.rtcType = rtcType;
		this.sdp = sdp;
		this.code = code;
	}
}
