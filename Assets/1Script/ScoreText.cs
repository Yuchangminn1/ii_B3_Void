using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScoreText : MonoBehaviour
{
    Text _text;
    protected string originText = "";

    void Start()
    {
        _text = GetComponent<Text>();

        StepDataManager.Instance.OnScoreDataChanged += SetScoreText;

        originText = _text.text;

    }
    void OnEnable()
    {
        if (originText != "")
            _text.text = originText.Replace("Score", UserDataManager.Instance.GetPlayer().AddPiece.ToString());

    }


    public void SetScoreText(int score)
    {
        _text.text = originText.Replace("Score", score.ToString());

    }




}
