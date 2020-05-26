using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ComponentToggler : MonoBehaviour
{
    public SpriteRenderer togglingSprite;
    public Behaviour[] ComponentsToToggle;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    internal void SetActive(bool v)
    {
        togglingSprite.enabled = v;
        foreach(Behaviour c in ComponentsToToggle)
        {
            c.enabled = v;
        }
    }
}
