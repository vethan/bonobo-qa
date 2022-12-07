using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ExperimentRestarter : MonoBehaviour
{
    public GameObject continueButton;
    public GameObject prolificButton;
    public Text _textMeshPro;

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        if (Testupload.uploader.isUploading)
        {
            _textMeshPro.text = "Please Wait...";
            continueButton.SetActive(false);
            prolificButton.SetActive(false);
        }
        else
        {
            _textMeshPro.text =
                "Thanks for doing a playthrough.  Click the button below if you want to do another playthrough, or close the window if you are done.";
            continueButton.SetActive(true);
            prolificButton.SetActive(true);
        }
    }
}