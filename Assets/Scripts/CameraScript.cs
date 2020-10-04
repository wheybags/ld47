using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraScript : MonoBehaviour
{
    private GameManager gameManager;

    // Start is called before the first frame update
    void Start()
    {
        gameManager = FindObjectOfType<GameManager>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnPreRender()
    {
        foreach (var robot in gameManager.robots)
            robot.CustomOnPreRender();

        foreach (var fruit in gameManager.fruits)
            fruit.CustomOnPreRender();
    }
}
