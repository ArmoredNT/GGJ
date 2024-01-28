using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using Unity.WebRTC;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;
public class ClientCheck : MonoBehaviour
{
    private void Start()
    {
        NetworkManager2.Instance.ClientSendToServer("LOADED_NEWSROOM:");
    }
    
}
