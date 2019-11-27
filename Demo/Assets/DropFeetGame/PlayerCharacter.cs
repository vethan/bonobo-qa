using UnityEngine;

public class PlayerCharacter : MonoBehaviour
{
    public enum KillType
    {
        Normal,
        Headshot,
        DoubleKill
    }

    public System.Action<PlayerCharacter,KillType> OnKill = (_,__)=>{};

    public GameObject standSprites;
    public GameObject diveSprites;
    public GameObject jumpSprites;

    AbstractDropFeetController controller;
    DropFeetGameInstance gameInstance;
    float xLimit = 7.82f;
    Transform floor;
    PlayerCharacter opponent;
    public Vector2 velocity { get; private set; }
    public bool dropping {get; private set; }
    Rigidbody2D rigid;
    float lastInAir;
    public bool debug = false;
    // Start is called before the first frame update
    void Awake()
    {
        controller = GetComponent<AbstractDropFeetController>();
        if(controller == null)
        {
            controller = gameObject.AddComponent<DoNothingDropFeetController>();
        }
        gameInstance = GetComponentInParent<DropFeetGameInstance>();
        floor = gameInstance.floor;
        rigid = GetComponent<Rigidbody2D>();
        var players = gameInstance.GetComponentsInChildren<PlayerCharacter>();

        foreach(var player in players)
        {
            if(player == this)
            {
                continue;
            }
            opponent = player;
        }
    }

    public bool isOnFloor
    {
        get
        {
            return transform.localPosition.y <= floor.localPosition.y + .01f && velocity.y <=0;
        }
    }
    float lastOnFloor;

    public Vector2 GetAttackVector()
    {
        return new Vector2(15 * transform.localScale.x, -15);
    }

    private void FixedUpdate()
    {
        if (gameInstance.isPausedForKill)
            return;
        if(debug)
            Debug.Log("isOnFloor:" + isOnFloor + "::dropping" + dropping + "::yVel" + velocity.y);
        if (isOnFloor)
        {
            lastInAir += Time.fixedDeltaTime;
            lastOnFloor = 0;
            dropping = false;
            velocity = Vector2.zero;
            if (transform.localPosition.y < floor.localPosition.y)
            {
                transform.localPosition = new Vector3(transform.localPosition.x, floor.localPosition.y, transform.localPosition.z);
            }
        }
        else
        {
            lastInAir = 0;
            lastOnFloor += Time.fixedDeltaTime;
            if (!dropping)
            {
                velocity -= new Vector2(0, 40) * Time.fixedDeltaTime;
            }
            if (dropping)
            {
                velocity = GetAttackVector();
            }
        }

        transform.localPosition += (Vector3)velocity*Time.fixedDeltaTime;
        transform.localPosition = new Vector3(Mathf.Clamp(transform.localPosition.x,-xLimit,xLimit),transform.localPosition.y,transform.localPosition.z);
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        Collider2D myCollider;
        Collider2D otherCollider;

        if(collision.rigidbody == rigid)
        {
            myCollider = collision.collider;
            otherCollider = collision.otherCollider;
        }
        else
        {
            otherCollider = collision.collider;
            myCollider = collision.otherCollider;
        }

        if(myCollider.tag != "Foot")
        {
            return;
        }

        if(otherCollider.tag == "Head")
        {
            OnKill(this, KillType.Headshot);
        }
        else if(otherCollider.tag == "Foot" && myCollider.GetInstanceID() < otherCollider.GetInstanceID())
        {
            OnKill(this, KillType.DoubleKill);
        }
        else
        {
            OnKill(this, KillType.Normal);
        }
        
    }

    public void SnapToFloorPosition(float xPoint) {
        transform.localPosition = new Vector3(xPoint, floor.localPosition.y, transform.localPosition.z);
        velocity = Vector2.zero;

    }

    // Update is called once per frame
    void Update()
    {
        if (debug)
            Debug.Log("Update Occurred");
        if (isOnFloor)
        {
            float xScale = transform.localPosition.x - opponent.transform.localPosition.x > 0 ? -1 : 1;
            transform.localScale = new Vector3(xScale, 1, 1);
            if (controller.DropButtonDown() && lastInAir > 0.1f)
            {
                velocity = new Vector2(0, 22);
            }
            if (controller.FeetButtonDown() && lastInAir > 0.1f)
            {
                velocity = new Vector2(3.5f * -transform.localScale.x, 13);
            }
        }
        else
        {
            if (!dropping)
            {
                if (lastOnFloor > 0.1f && controller.FeetButtonDown())
                {
                    velocity = GetAttackVector();
                    dropping = true;
                }
            }
        }
        standSprites.SetActive(isOnFloor);
        diveSprites.SetActive(dropping);
        jumpSprites.SetActive(!isOnFloor && !dropping);

    }
}
