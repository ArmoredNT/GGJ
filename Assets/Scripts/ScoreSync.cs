using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;

public class ScoreSync : MonoBehaviourPunCallbacks, IPunObservable
{
    private int playerScore = 0;
    private TextMeshProUGUI _text;

    private void Start()
    {
        _text = GetComponent<TextMeshProUGUI>();
        // Initialize the score
        playerScore = 0;

        // Make sure the PhotonView is observing this script
        if (photonView.IsMine)
        {
            // Set the player's unique score on start
            photonView.RPC("InitializeScore", RpcTarget.AllBuffered, playerScore);
            _text.text = playerScore.ToString();
        }
    }

    private void Update()
    {
        // Example: Increase the score when the player presses a button
        if (photonView.IsMine && Input.GetKeyDown(KeyCode.Space))
        {
            playerScore++;
            photonView.RPC("UpdateScore", RpcTarget.AllBuffered, playerScore);
            _text.text = playerScore.ToString();
        }
    }

    [PunRPC]
    void InitializeScore(int newScore)
    {
        _text = GetComponent<TextMeshProUGUI>();
        playerScore = newScore;
        _text.text = playerScore.ToString();
    }

    [PunRPC]
    void UpdateScore(int newScore)
    {
        playerScore = newScore;
        _text.text = playerScore.ToString();
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
