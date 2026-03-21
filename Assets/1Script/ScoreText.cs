using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScoreText : MonoBehaviour
{
    Text _text;

    readonly int maxScore = 50;
    //public Arduino_Touch_Page4[] arduino_Touch_Page4s;

    int currentScore = 0;
    // Start is called before the first frame update
    void Start()
    {
        _text = GetComponent<Text>();
        // arduino_Touch_Page4s = FindObjectsOfType<Arduino_Touch_Page4>();
        // foreach (var arduino in arduino_Touch_Page4s)
        // {
        //     arduino.AddOnscoreChange(ScoreStamp);
        // }
    }


    void OnEnable()
    {
        if (_text != null)
            _text.text = $"{0}/{maxScore}";

    }

    public void ScoreStamp(int value)
    {
        if (value > maxScore)
        {
            value = maxScore;
        }
        if (value != currentScore)
        {
            currentScore = value;
            _text.text = $"{currentScore}/{maxScore}";
        }

    }
}
