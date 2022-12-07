using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ProlificIDStore : MonoBehaviour
{
    public TMPro.TMP_InputField input;
    // Start is called before the first frame update
    void Start()
    {
        
    }


    public void OpenCompletionURL()
    {
        Application.OpenURL("https://app.prolific.co/submissions/complete?cc=C1I6L4TC");
    }
    public void StoreID()
    {
        Testupload.prolificID = input.text;
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
