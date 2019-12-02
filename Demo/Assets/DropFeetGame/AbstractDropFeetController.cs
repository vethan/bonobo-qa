using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class AbstractDropFeetController : MonoBehaviour
{
    public abstract bool DropButtonDown();
    public abstract bool FeetButtonDown();

    public abstract void UpdateButtons();
}
