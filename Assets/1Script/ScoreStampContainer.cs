using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScoreStampContainer : ResultStampContainer
{
    int currentStampIndex = -1;
    //int score = 0;

    //public Arduino_Touch_Page4[] arduino_Touch_Page4s;



    override protected void Start()
    {
        base.Start();
        // arduino_Touch_Page4s = FindObjectsOfType<Arduino_Touch_Page4>();
        // foreach (var arduino in arduino_Touch_Page4s)
        // {
        //     arduino.AddOnscoreChange(ScoreStamp);
        // }

    }

    override public void Reset()
    {
        base.Reset();
        currentStampIndex = -1;
    }
    public void ScoreStamp(int value)
    {

        if (value > 9 && currentStampIndex != (value / 10) - 1)
        {
            currentStampIndex = (value / 10) - 1;
            UserDataManager.Instance.GetPlayer().AddPiece = (value / 10);

            Debug.Log($"점수: {value}, 현재 스탬프 인덱스: {currentStampIndex}");
            answerStamps[currentStampIndex].SetCorrectStamp();
            UserDataManager.Instance.GetPlayer().AddPiece = currentStampIndex + 1;
        }


    }
}
