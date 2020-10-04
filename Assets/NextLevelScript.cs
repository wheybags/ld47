using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NextLevelScript : MonoBehaviour {
    public string nextSceneName;

    public void OnClick() {
        SceneManager.LoadScene(nextSceneName);
    }
}
