using System.Collections;
using UnityEngine;

public class IntroEnder : MonoBehaviour
{
    [SerializeField] float introLength = 10;

    public void Start()
    {
        StartCoroutine(WaitForIntro());
    }

    IEnumerator WaitForIntro()
    {
        yield return new WaitForSeconds(introLength);
		NetworkManager2.Instance.HostEndIntro();
	}
}
