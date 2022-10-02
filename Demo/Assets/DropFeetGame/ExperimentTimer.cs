using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;

public class ExperimentTimer : MonoBehaviour
{
    public TextMeshProUGUI timerText;

    private float timeLeft = 30;

    public ReplayRecorder _replayRecorder;

    public bool isDone;

    // Update is called once per frame
    void Update()
    {
        timeLeft = Mathf.Max(0, timeLeft - Time.deltaTime);
        timerText.text = timeLeft.ToString("F1");

        if (!isDone && timeLeft == 0)
        {
            var replay =_replayRecorder.GetReplayStream();
            var stats = _replayRecorder.gameInstance.GetGameStats();
            isDone = true;
        }
    }
}
