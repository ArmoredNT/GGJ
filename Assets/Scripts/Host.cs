using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Host
{
    Dictionary<int, string> allPrompts = new();
    int playerCount;

    public void Init(int playerCount)
    {
        this.playerCount = playerCount;
        Debug.Log(this.playerCount + " players");
    }

    public void AddPrompt(int playerID, string prompt)
    {
        allPrompts.Add(playerID, prompt);

        if (allPrompts.Count == playerCount)
        {
            Debug.Log("All prompts in!");
        }
    }
}
