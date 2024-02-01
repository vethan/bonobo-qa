using System.Collections;
using System.Collections.Generic;
using SharpNeat.Phenomes;
using UnityEngine;

public abstract class AbstractChaserController
{
    public abstract float GetXAxis();
    public abstract float GetYAxis();

    public abstract void UpdateButtons();
}

public class KeyboardChaserController : AbstractChaserController
{
    public override float GetXAxis()
    {
        return (Input.GetKey(KeyCode.D) ? 1 : 0) + (Input.GetKey(KeyCode.A) ? -1 : 0);
    }

    public override float GetYAxis()
    {
        return (Input.GetKey(KeyCode.W) ? 1 : 0) + (Input.GetKey(KeyCode.S) ? -1 : 0);
    }

    public override void UpdateButtons()
    {
    }
}

public class EvolvedChaserController : AbstractChaserController
{
    private readonly ChaserBrain me;
    private readonly ChaserEnemy enemy;
    private readonly ChaserGameInstance gameInstance;

    public EvolvedChaserController(ChaserBrain me, ChaserEnemy enemy, ChaserGameInstance gameInstance)
    {
        this.me = me;
        this.enemy = enemy;
        this.gameInstance = gameInstance;
    }

    IBlackBox brain;

    public void SetBrain(IBlackBox newBrain)
    {
        brain = newBrain;
        
    }

    public override float GetXAxis()
    {
        return xAxis;
    }

    public override float GetYAxis()
    {
        return yAxis;
    }

    double[] inputSignals = new double[7];

    void AssignInputs()
    {
        var temp = enemy.GetLocalPhysicsPosition() - me.GetLocalPhysicsPosition();
        // var dist = temp.magnitude; 
        temp.Normalize();
        //Direction to enemy
        inputSignals[0] = temp.x / 2 + 0.5f;
        inputSignals[1] = temp.y / 2 + 0.5f;

        //Is sighted?
        inputSignals[2] = enemy.visionCone.PlayerVisible ? 1 : 0;

        //Which direction the enemy wants to face
        inputSignals[3] = enemy.targetVector.x/ 2 + 0.5f;
        inputSignals[4] = enemy.targetVector.y/ 2 + 0.5f;

        
        temp = (Vector2)gameInstance.originalPickup.mTransform.localPosition - me.GetLocalPhysicsPosition();
        
        temp.Normalize();
        // Direction to target
        inputSignals[5] = temp.x / 2 + 0.5f;
        inputSignals[6] = temp.y / 2 + 0.5f;
        brain.InputSignalArray.CopyFrom(inputSignals, 0);

    }


    private float xAxis;
    private float yAxis;
    private double[] outputs = new double[4];
    public override void UpdateButtons()
    {
        AssignInputs();
        brain.Activate();
        brain.OutputSignalArray.CopyTo(outputs,0);
        xAxis = (outputs[0] > 0.5f ? 1 : 0) + (outputs[1] > 0.5f ? -1 : 0);
        yAxis = (outputs[2] > 0.5f ? 1 : 0) + (outputs[3] > 0.5f ? -1 : 0);
    }

    public void Reset()
    {
        brain.ResetState();
        
    }
}