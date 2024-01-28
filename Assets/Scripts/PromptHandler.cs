using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PromptHandler : MonoBehaviour
{

    [SerializeField] private InputField input;
    private bool called = false;

    private string prompt;
    public void FinishedPrompt()
    {
        if (!called)
        {
            called = true;
            prompt = input.text;
            print(prompt);
        }
    }

    public void LoadNextScene()
    {
        SceneManager.LoadScene("PhotoSelecter");
    }
}
