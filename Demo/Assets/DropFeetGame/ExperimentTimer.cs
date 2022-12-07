using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ExperimentTimer : MonoBehaviour
{
    public TextMeshProUGUI timerText;

    private float timeLeft = 30;

    public ReplayRecorder _replayRecorder;

    public bool isDone;

    // Update is called once per frame
    void FixedUpdate()
    {
        timeLeft = Mathf.Max(0, timeLeft - Time.fixedDeltaTime);
        timerText.text = timeLeft.ToString("F1");

        if (!isDone && timeLeft == 0)
        {
            var replay =_replayRecorder.GetReplayStream();
            var stats = _replayRecorder.gameInstance.GetGameStats();
            
            Testupload.uploader.SetGameplayData(replay,stats);
            isDone = true;
            SceneManager.LoadScene("ExperimentPostamble", LoadSceneMode.Single);

        }
    }
}
