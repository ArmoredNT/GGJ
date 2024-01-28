using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using TMPro;

public class PlayerCreator : MonoBehaviour
{
    public GameObject playerPrefab;
    public Canvas canvas;

    private void Start()
    {
        Vector2 randomPos = new Vector2(UnityEngine.Random.Range(-3, 3), UnityEngine.Random.Range(-2, 2));
        GameObject player = PhotonNetwork.Instantiate(playerPrefab.name, randomPos, Quaternion.identity);
        player.transform.SetParent(canvas.transform, false);
        player.GetComponent<RectTransform>().position = randomPos;
    }
}
