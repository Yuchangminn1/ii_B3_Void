using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScoreStampContainer : ResultStampContainer
{
    int currentStampIndex = -1;

    override protected void Start()
    {
        base.Start();

        defaultColor = new Color32(0, 0, 0, 255);

        getStampColor = new Color32(13, 119, 160, 255);
    }

    override public void Reset()
    {
        base.Reset();
        currentStampIndex = -1;
    }
    public void ScoreStamp(int value)
    {

        if (value != 0 && value % 2 == 0)
        {
            currentStampIndex = (value / 2) - 1;

            UserDataManager.Instance.GetPlayer().AddPiece = currentStampIndex + 1;

            Debug.Log($"점수: {value}, 현재 스탬프 인덱스: {currentStampIndex}");

            answerStamps[currentStampIndex].color = getStampColor;

            // UserDataManager.Instance.GetPlayer().AddPiece = currentStampIndex + 1;
        }


    }
}
