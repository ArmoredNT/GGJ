using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerNameManager : MonoBehaviour
{

    public GameObject playerItemPrefab;
   
    public Transform contentObject;

    public string[] names;
    
    private void Start()
    {
        //names = new string[10];
        //names[0] = "name";
        //names[1] = "name";
        //UpdatePlayerList(names);
    }

    private void Awake()
    {
        NetworkManager2.Instance.onLoaded.AddListener((names) =>
        {
            UpdatePlayerList(names);
        });
    }
    
    public void UpdatePlayerList(string[] list)
    {

        
        foreach (string player in list)
        {
            GameObject go = Instantiate(playerItemPrefab, contentObject);
            go.GetComponent<Text>().text = player;


        }
    }
    

    
}
