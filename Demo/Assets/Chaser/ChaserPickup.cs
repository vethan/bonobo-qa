using System.Collections;
using System.Collections.Generic;
using SpriteGlow;
using UnityEngine;

public class ChaserPickup : MonoBehaviour
{
    public System.Action<GameObject> OnCollision =(_) => { };

    private SpriteGlowEffect glow;

    private SpriteRenderer renderer;
    public Rigidbody2D rigid;

    public Transform mTransform;
    // Start is called before the first frame update
    void Awake()
    {
        glow = GetComponent<SpriteGlowEffect>();
        renderer = GetComponent<SpriteRenderer>();
        mTransform = transform;
    }

    public void SetColor(Color c)
    {
        glow.GlowColor = c;
        renderer.color = c;
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        OnCollision(collision.gameObject);
    }



    public void WarpTo(Vector2 getRandomPosition)
    {
        mTransform.localPosition = getRandomPosition;
    }
}
