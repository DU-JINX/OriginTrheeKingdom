using UnityEngine;
using System.Collections;

public class HowToPlay : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
		if (Misc.GetBack()) {
			UnityEngine.SceneManagement.SceneManager.LoadScene(0);
		}
	}
}
