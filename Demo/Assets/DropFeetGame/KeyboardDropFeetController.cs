using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KeyboardDropFeetController : AbstractDropFeetController
{
    public KeyCode feetButton;
    public KeyCode dropButton;
    bool shouldDrop;
    bool shouldFeed;

    bool shouldUpdate = false;
    public override bool DropButtonDown()
    {
        bool temp = shouldDrop;
        shouldDrop = false;
        return temp;
    }

    public override bool FeetButtonDown()
    {
        
        bool temp = shouldFeed;
        shouldFeed = false;

        return temp;

    }

    public override bool HoldDelayedKick()
    {
        return true;
    }

    public override void UpdateButtons()
    {
        if(shouldUpdate)
        {

            shouldDrop = Input.GetKeyDown(dropButton);
            

            shouldFeed = Input.GetKeyDown(feetButton);
            shouldUpdate = false;
        }
    }

    

    private void Update()
    {
        shouldUpdate = true;

    }
}
