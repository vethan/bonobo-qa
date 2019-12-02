using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoNothingDropFeetController : AbstractDropFeetController
{
    public override bool DropButtonDown()
    {
        return false;
    }

    public override bool FeetButtonDown()
    {
        return false;
    }

    public override void UpdateButtons()
    {

    }
}
