using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PreambleHelper : MonoBehaviour
{
    public TMP_InputField commentText;
    
    public void FinishedNo()
    {
        Testupload.uploader.SetCommentsAndUpload(false,"");
    }

    public void FinishedYes()
    {
        Testupload.uploader.SetCommentsAndUpload(true,commentText.text);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
