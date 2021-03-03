using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomDropFeetController : AbstractDropFeetController
{
    private readonly int callsBeforeChange = 4;
    private int timer = 0;
    private bool feet;
    private bool drop;
    public override bool DropButtonDown()
    {
        var temp = drop;
        drop = false;
        return temp;
    }

    public override bool FeetButtonDown()
    {
        var temp = feet;
        feet = false;
        return temp;
    }

    public override void UpdateButtons()
    {
        timer -= 1;
        if (timer <= 0)
        {
            timer = callsBeforeChange;
            feet = Random.value > .7;
            drop = Random.value > .7;
        }
    }
}
