using UnityEngine;
public class ClientCheck : MonoBehaviour
{
    private void Start()
    {
        NetworkManager2.Instance.ClientSendToServer("LOADED_NEWSROOM:");
    }
    
}
