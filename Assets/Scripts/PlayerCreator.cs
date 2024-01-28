using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
public class PlayerCreator : MonoBehaviour
{
    public GameObject playerPrefab;
    public Canvas canvas;

    private void Start()
    {
        Vector2 randomPos = new Vector2(UnityEngine.Random.Range(-5, 5), UnityEngine.Random.Range(-5, 5));
        GameObject player = PhotonNetwork.Instantiate(playerPrefab.name, randomPos, Quaternion.identity);
        player.transform.SetParent(canvas.transform, false);
    }
}
