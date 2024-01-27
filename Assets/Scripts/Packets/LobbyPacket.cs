[System.Serializable]
public enum LobbyPacketType
{
    unknown,
    request,
    response,
    connectReq,
    connectRes
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
