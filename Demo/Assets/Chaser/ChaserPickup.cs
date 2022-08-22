using System.Collections;
using System.Collections.Generic;
using SpriteGlow;
using UnityEngine;

public class ChaserPickup : MonoBehaviour
{
    public System.Action<GameObject> OnCollision =(_) => { };

    private SpriteGlowEffect glow;

    private SpriteRenderer renderer;
    
    // Start is called before the first frame update
    void Awake()
    {
        glow = GetComponent<SpriteGlowEffect>();
        renderer = GetComponent<SpriteRenderer>();
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


    // Update is called once per frame
    void Update()
    {
        
    }

    public void WarpTo(Vector2 getRandomPosition)
    {
        transform.localPosition = getRandomPosition;
    }
}
