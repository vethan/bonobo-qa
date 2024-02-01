using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class VisionCone : MonoBehaviour
{
    private Collider2D _trigger;

    public ChaserBrain player;

    public bool PlayerVisible = false;

    public bool PlayerInCone = false;

// Start is called before the first frame update
    void OnEnable()
    {
        _trigger = GetComponent<PolygonCollider2D>();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject != player.gameObject) return;
        PlayerInCone = false;
        PlayerVisible = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject != player.gameObject)
        {
            // PlayerVisible = false;
            return;
        }

        PlayerInCone = true;
        var position = transform.parent.position;
        var hit = Physics2D.Raycast(position, other.transform.position - position);
        PlayerVisible = hit.transform.gameObject == player.gameObject;
    }

    private void FixedUpdate()
    {
        if (PlayerInCone)
        {
            var position = transform.parent.position;
            var hit = Physics2D.Raycast(position, player.mTrans.position - position);
            PlayerVisible = hit.transform.gameObject == player.gameObject;
        }
    }
}