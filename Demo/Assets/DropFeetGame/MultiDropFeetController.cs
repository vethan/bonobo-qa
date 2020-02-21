using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MultiDropFeetController : AbstractDropFeetController
{
    List<AbstractDropFeetController> controllers = new List<AbstractDropFeetController>();


    private void Awake()
    {
        controllers = new List<AbstractDropFeetController>(GetComponents<AbstractDropFeetController>());

        controllers.Remove(this);
    }

    public override bool DropButtonDown()
    {
        return !controllers.TrueForAll(x => !x.DropButtonDown());
    }

    public override bool FeetButtonDown()
    {
        return !controllers.TrueForAll(x => !x.FeetButtonDown());
    }

    public override bool HoldDelayedKick()
    {
        return true;
    }

    public override void UpdateButtons()
    {
        foreach(var v in controllers)
        {
            v.UpdateButtons();
        }
    }

    

    private void Update()
    {


    }
}
