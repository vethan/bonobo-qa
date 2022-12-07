using System.Collections;
using System.Collections.Generic;
using System.Security.Permissions;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class TutorialManager : MonoBehaviour
{
    public TextMeshProUGUI timerText;

    public PlayerCharacter _character;

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(Tutorial());
    }

    IEnumerator WaitForPoint()
    {
        var kills = _character.kicksHit;
        while (true)
        {
            yield return null;
            if (_character.kicksHit > kills)
            {
                yield break;
            }
        }
    }

    IEnumerator Tutorial()
    {
        var counter = _character.jumpsMade;
        int diff = 0;
        while (true)
        {
            yield return null;
            diff = _character.jumpsMade - counter;
            timerText.text = "Left Click or press W to jump. Jump " + (3 - diff) + " more times to continue";

            if (diff >= 3)
            {
                break;
            }
        }

        counter = _character.divekicksMade;
        while (true)
        {
            yield return null;
            diff = _character.divekicksMade - counter;
            timerText.text = "When in the air, right click or press Q to divekick. Divekick " + (3 - diff) +
                             " more times to continue";

            if (diff >= 3)
            {
                break;
            }
        }


        counter = _character.backhopsMade;
        while (true)
        {
            yield return null;
            diff = _character.backhopsMade - counter;
            timerText.text = "When on the ground, right click or press Q to hop backwards. Hop " + (3 - diff) +
                             " more times to continue";

            if (diff >= 3)
            {
                break;
            }
        }


        timerText.text = "Hit your opponent with a divekick to score a point";
        yield return WaitForPoint();


        timerText.text = "You are about to play against an AI opponent that has been created by a developer.\n(Score a point to continue)";
        yield return WaitForPoint();
        
        timerText.text = "The developer wants to know if the opponent is working correctly.\n(Score a point to continue)";
        yield return WaitForPoint();
        
        timerText.text = "You will be given 30 seconds to evaluate the opponent, then a chance to give feedback\n(Score a point to continue)";
        yield return WaitForPoint();
        
        timerText.text = "After you have submitted feedback, you may either evaluate for another 30 seconds, or quit!\n(Score a point to continue)";
        yield return WaitForPoint();
        
        timerText.text = "Feel free to play as many sessions as you would like before exiting the survey\n(Score a point to continue)";
        yield return WaitForPoint();
        
        timerText.text = "Score one more point to begin your first evaluation session.";
        yield return WaitForPoint();

        SceneManager.LoadScene("PlayerVsAIDropFeet", LoadSceneMode.Single);

    }

    // Update is called once per frame
    void Update()
    {
    }
}