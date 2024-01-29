using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VisionCone : MonoBehaviour
{
    private Collider2D _trigger;

    public ChaserBrain player;

    public bool PlayerVisible = false;

// Start is called before the first frame update
    void OnEnable()
    {
        _trigger = GetComponent<Collider2D>();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject != player.gameObject) return;
        PlayerVisible = false;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.gameObject != player.gameObject)
        {
           // PlayerVisible = false;
            return;
        }

        var position = transform.parent.position;
        var hit = Physics2D.Raycast(position, other.transform.position - position);
        PlayerVisible = hit.transform.gameObject == player.gameObject;
    }

}