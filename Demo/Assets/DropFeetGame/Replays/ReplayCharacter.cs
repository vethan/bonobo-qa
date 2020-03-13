using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReplayCharacter : MonoBehaviour
{
    public GameObject standSprites;
    public GameObject diveSprites;
    public GameObject jumpSprites;
    public Transform flipper;
    ReplayPlayer player;
    ReplayCharacter opponent;

    public bool isOnFloor = true;
    public bool dropping = false;
    // Start is called before the first frame update
    void Start()
    {
        player = GetComponentInParent<ReplayPlayer>();
        var players = player.GetComponentsInChildren<ReplayCharacter>();

        foreach (var player in players)
        {
            if (player == this)
            {
                continue;
            }
            opponent = player;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (isOnFloor)
        {
            float xScale = transform.localPosition.x - opponent.transform.localPosition.x > 0 ? -1 : 1;
            flipper.localScale = new Vector3(xScale, 1, 1);
        }

        standSprites.SetActive(isOnFloor);
        diveSprites.SetActive(dropping && !isOnFloor);
        jumpSprites.SetActive(!isOnFloor && !dropping);
    }
}
