using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Runtime.CompilerServices;
public class PromptHandler : MonoBehaviour
{

    [SerializeField] private TMP_InputField input;
    private bool called = false;

    private string prompt;
    public void FinishedPrompt()
    {
        if (!called)
        {
            called = true;
            prompt = input.text;

			if (!NetworkManager2.Instance.GetIsHost())
			{
				NetworkManager2.Instance.ClientSendToServer("PROMPT", prompt);
			}
			else
			{
				NetworkManager2.Instance.GetHost().ReceivePrompt(-1, prompt);
			}
		}
    }

    public void LoadNextScene()
    {
        SceneManager.LoadScene("PhotoSelecter");
    }
}
