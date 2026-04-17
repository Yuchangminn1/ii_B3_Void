using System.Collections;
using System.Collections.Generic;
using System.Data;
using UnityEngine;

public class ScoreStampContainer : ResultStampContainer
{
    int currentStampIndex = -1;

    const int PieceStampCount = 2;

    CanvasGroup canvasGroup;





    protected override void Start()
    {
        base.Start();

        canvasGroup = GetComponent<CanvasGroup>();

        defaultColor = new Color32(0, 0, 0, 255);

        getStampColor = new Color32(13, 119, 160, 255);
        if (StepDataManager.Instance != null)
        {
            StepDataManager.Instance.OnStampDataChanged += ScoreStamp;
        }
    }

    public override void Reset()
    {
        base.Reset();
        currentStampIndex = -1;

        foreach (var stamp in answerStamps)
        {
            stamp.color = defaultColor;
        }
    }
    public void ScoreStamp(int value)
    {
        currentStampIndex = value;
        if (currentStampIndex > 0 && currentStampIndex <= answerStamps.Length)
        {
            canvasGroup.alpha = 1f;

            answerStamps[currentStampIndex - 1].color = getStampColor;

            SoundManager.Instance.PlayEffectSound(EffectSoundNum.SoulPieceSound);
            StartCoroutine(DelayToHide());

        }
    }

    IEnumerator DelayToHide()
    {
        yield return new WaitForSeconds(1.5f);
        canvasGroup.alpha = 0f;
    }
}
