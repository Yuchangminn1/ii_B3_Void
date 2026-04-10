using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScoreText : MonoBehaviour
{
    Text _text;
    protected string originText = "";

    int _currentScore = 0;

    void Start()
    {
        _text = GetComponent<Text>();

        StepDataManager.Instance.OnScoreDataChanged += SetScoreText;

        PageController.Instance.OnReset += Reset;
        originText = _text.text;

    }

    void Reset()
    {
        _currentScore = 0;
    }
    void OnEnable()
    {
        if (originText != "")
            _text.text = originText.Replace("Score", _currentScore.ToString());

    }


    public void SetScoreText(int score)
    {
        _currentScore = score;
        _text.text = originText.Replace("Score", _currentScore.ToString());

    }




}
