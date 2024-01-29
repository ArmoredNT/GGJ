using UnityEngine;

public class SceneInitializer : MonoBehaviour
{
	private void Start()
	{
		Debug.Log("SCENE TEST");
		NetworkManager2.Instance.OnSceneLoad();
	}
}
