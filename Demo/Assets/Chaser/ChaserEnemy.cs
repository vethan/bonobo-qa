using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChaserEnemy : MonoBehaviour
{
    public enum State
    {
        Chasing,
        Patrolling,
        Searching
    }

    private State currentState = State.Patrolling;
    public GameObject PatrolWaypoints;
    public VisionCone visionCone;

    private Transform[] waypointPositions;
    private Rigidbody2D rigid;
    int currentWaypoint = 0;
    private float stayStillTimer = 0;

    private Vector2 targetPosition;

    private Transform _transform;
    private Transform _parentTransform;
    
    public const float SEARCH_TIME = 6.0f;
    private Vector2 StartSearchDirection;
    private Vector2 localPhysicsPos;
    public Vector2 targetVector;
    private float timeSearching = 0;
    private float timeChasing = 0;
    private float timePatrolling = 0;
    private float timeStill = 0;
    
    private int catches = 0;
    // Start is called before the first frame update
    void Awake()
    {
        _transform = GetComponent<Transform>();
        _parentTransform = _transform.parent;
        rigid = GetComponent<Rigidbody2D>();
        waypointPositions = new Transform[PatrolWaypoints.transform.childCount];
        for (int i = 0; i < PatrolWaypoints.transform.childCount; i++)
        {
            waypointPositions[i] = PatrolWaypoints.transform.GetChild(i);
        }
    }

    public Vector2 GetLocalPhysicsPosition()
    {
        return localPhysicsPos;
    }

    void UpdateLocalPhysicsPosition()
    {
        localPhysicsPos = _parentTransform.InverseTransformPoint(rigid.position);
    }

    void UpdatePatrol()
    {
        //   rigid.MoveRotation(rigid.rotation + 10);
        targetPosition = waypointPositions[currentWaypoint].position;
        if ((rigid.position - targetPosition).sqrMagnitude < 0.1f * _parentTransform.lossyScale.x)
        {
            stayStillTimer = 1.0f;
            currentWaypoint = (currentWaypoint + 1) % waypointPositions.Length;
        }

        if (visionCone.PlayerVisible)
        {
            currentState = State.Chasing;
        }
    }

    private void OnCollisionEnter2D(Collision2D col)
    {
        if (col.gameObject == visionCone.player.gameObject)
        {
            catches++;
            visionCone.player.Respawn();
            currentState = State.Patrolling;
            stayStillTimer = 0;
            currentWaypoint = 0;
            catches = 0;
            _transform.localPosition = Vector3.zero;
            localPhysicsPos = Vector2.zero;
            visionCone.PlayerVisible = false;
        }
    }

    void UpdateChasing()
    {
        var playerPos = visionCone.player.mTrans.position;

        if (visionCone.PlayerVisible)
        {
            targetPosition = playerPos;
            return;
        }


        if (!((rigid.position - targetPosition).sqrMagnitude < 0.1f * _parentTransform.lossyScale.x)) return;
        stayStillTimer = SEARCH_TIME;

        currentState = State.Searching;
        StartSearchDirection = ((Vector2)playerPos - rigid.position).normalized;
    }

    void UpdateSearching()
    {
        //Possible bug:  This code is missing, so doesnt chase while searching
        if (visionCone.PlayerVisible)
        {
            currentState = State.Chasing;
            stayStillTimer = 0; //Possible bug: don't reset this timer.

            return;
        }

        switch (stayStillTimer)
        {
            case <= 0:
                currentState = State.Patrolling;
                return;
            case < SEARCH_TIME / 3:
                targetPosition = rigid.position + Vector2.Perpendicular(StartSearchDirection);
                break;
            case < 2 * SEARCH_TIME / 3:
                targetPosition = rigid.position - Vector2.Perpendicular(StartSearchDirection);
                break;
            default:
                targetPosition = rigid.position + StartSearchDirection;
                break;
        }
    }



    private void FixedUpdate()
    {
        switch (currentState)
        {
            case State.Chasing:
                UpdateChasing();
                timeChasing += Time.fixedDeltaTime;
                break;
            case State.Patrolling:
                UpdatePatrol();
                timePatrolling += Time.fixedDeltaTime;
                break;
            case State.Searching:
                UpdateSearching();
                timeSearching += Time.fixedDeltaTime;
                break;

            default:
                UpdatePatrol();
                break;
        }


        targetVector = rigid.position - targetPosition;
        targetVector.Normalize();
        float targetValue = Vector2.SignedAngle(Vector2.down, targetVector);
        float change = targetValue - rigid.rotation;
        if (change > 180)
        {
            change -= 360;
        }

        if (change < -180)
        {
            change += 360;
        }

        const int rotationSpeed = 180;
        var clamped = Mathf.Clamp(change, -rotationSpeed * Time.fixedDeltaTime, rotationSpeed * Time.fixedDeltaTime);

        rigid.MoveRotation(rigid.rotation + clamped);

        if (stayStillTimer <= 0)
            rigid.MovePosition(rigid.position - (targetVector * ( _parentTransform.lossyScale.x * Time.fixedDeltaTime)));
        else
        {
            stayStillTimer -= Time.fixedDeltaTime;
            timeStill += Time.fixedDeltaTime;
        }
        
        UpdateLocalPhysicsPosition();
    }

    // Update is called once per frame
    public void Reset()
    {
        currentState = State.Patrolling;
        timeSearching = 0;
        timeStill = 0;
        timeChasing = 0;
        timePatrolling = 0;
        stayStillTimer = 0;
        currentWaypoint = 0;
        catches = 0;
        _transform.localPosition = Vector3.zero;
        localPhysicsPos = Vector2.zero;
        visionCone.PlayerVisible = false;
        rigid.rotation = 0;
    }

    public Dictionary<string, float> GetStats()
    {
        return new Dictionary<string, float>
        {
            {"Caught",catches},
            {"TimePartrolling",timePatrolling},
            {"TimeSearching",timeSearching},
            {"TimeChasing",timeChasing},
            {"TimeStill",timeStill}
        };
        
    }
}