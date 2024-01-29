using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerItem : MonoBehaviour
{
    public Text playerName;
    PlayerNameManager manager;

    private void Start()
    {
        manager = FindObjectOfType<PlayerNameManager>();
    }


    public void SetPlayerName(string _playerName)
    {
        playerName.text = _playerName;
    }

    public void OnClickItem()
    {
        
    }
}
