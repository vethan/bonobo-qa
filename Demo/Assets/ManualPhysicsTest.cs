using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ManualPhysicsTest : MonoBehaviour
{
    public bool physicsStep = false;
    // Start is called before the first frame update
    void Start()
    {
        Physics.autoSimulation = false;
    }
    void FixedUpdate()
    {
        //Debug.Log("FixedUpdate: " + Time.f);
    }
    // Update is called once per frame
    void Update()
    {
        if(physicsStep)
        {
            Physics.Simulate(.01f);
            physicsStep = false;
            Debug.Log("PhysicsStepped");
        }
    }
}
