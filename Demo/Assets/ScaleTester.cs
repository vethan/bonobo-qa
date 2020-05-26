using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScaleTester : MonoBehaviour
{
    public UtilityDropFeetController one;
    public UtilityDropFeetController other;

    // Start is called before the first frame update
    void Start()
    {
        var things = FindObjectsOfType<UtilityDropFeetController>();
        one = things[0];
        other = things[1];
    }

    // Update is called once per frame
    void LateUpdate()
    {
        foreach(var kvp in one.lastUtility)
        {
            var oneUtility = kvp.Value;
            var otherUtility = other.lastUtility[kvp.Key];
            if(Mathf.Abs(oneUtility.value - otherUtility.value) > 0.01f)
            {
                Debug.LogError(string.Format("{0} should be the same but isnt: {1} vs {2}", kvp.Key,oneUtility.value,otherUtility.value));
            }
        }
    }
}
