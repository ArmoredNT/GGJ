using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
public class ScoreSync : MonoBehaviourPunCallbacks, IPunObservable
{
    private int playerScore = 0;

    private void Start()
    {
        // Initialize the score
        playerScore = 0;

        // Make sure the PhotonView is observing this script
        if (photonView.IsMine)
        {
            // Set the player's unique score on start
            photonView.RPC("InitializeScore", RpcTarget.AllBuffered, playerScore);
        }
    }

    private void Update()
    {
        // Example: Increase the score when the player presses a button
        if (photonView.IsMine && Input.GetKeyDown(KeyCode.Space))
        {
            playerScore++;
            photonView.RPC("UpdateScore", RpcTarget.AllBuffered, playerScore);
        }
    }

    [PunRPC]
    void InitializeScore(int newScore)
    {
        playerScore = newScore;
    }

    [PunRPC]
    void UpdateScore(int newScore)
    {
        playerScore = newScore;
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        // Use this to synchronize data (if needed) between players
        if (stream.IsWriting)
        {
            // Sending data to other players
            stream.SendNext(playerScore);
        }
        else
        {
            // Receiving data from the owner player
            playerScore = (int)stream.ReceiveNext();
        }
    }
}
