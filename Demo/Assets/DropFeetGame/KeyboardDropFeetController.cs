using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KeyboardDropFeetController : AbstractDropFeetController
{
    public KeyCode feetButton;
    public KeyCode dropButton;

    public override bool DropButtonDown()
    {
        return Input.GetKeyDown(dropButton); ;
    }

    public override bool FeetButtonDown()
    {        
        return Input.GetKeyDown(feetButton); ;

    }

    public override bool HoldDelayedKick()
    {
        return true;
    }

    public override void UpdateButtons()
    {
       
    }
}
