[System.Serializable]
public enum LobbyPacketType
{
    unknown,
    request,
    response
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

	public LobbyPacketResponse() : base(LobbyPacketType.response)
    {
		
    }
}
