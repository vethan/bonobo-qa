using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ActivateOnHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public GameObject activeObject;
    Button b;

    public void OnPointerEnter(PointerEventData eventData)
    {
        activeObject.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        activeObject.SetActive(false);
    }


    // Start is called before the first frame update
    void Awake()
    {
        activeObject.SetActive(false);
    }


}
