using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public string SceneToLoad;
    bool pressed = false;
    public void LoadScene()
    {
        if (pressed)
            return;
        pressed = true;
        SceneManager.LoadScene(SceneToLoad, LoadSceneMode.Single);
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
