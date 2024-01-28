using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
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

            NetworkManager2.Instance.SendPrompt(prompt);
        }
    }

    public void LoadNextScene()
    {
        SceneManager.LoadScene("PhotoSelecter");
    }
}
