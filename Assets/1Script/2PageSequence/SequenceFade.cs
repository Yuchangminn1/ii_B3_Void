using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
public class SequenceFade : SequenceScript
{
    [Tooltip("컷 효과를 적용할 Graphic 배열 (예: UI 이미지 등)")]

    public List<Graphic> CutGraphics;
    public List<CanvasGroup> CutCanvasGroups;


    public List<Graphic> FadeInGraphics;

    public List<Graphic> FadeOutGraphics;

    [Header("In 나타남")]

    public List<CanvasGroup> FadeInCanvasGroups;
    [Header("Out 사라짐")]


    public List<CanvasGroup> FadeOutCanvasGroups;


    [Header("0보다 클 경우 이 스크립트만 따로 시간 적용")] public float CustomFadeDuration = -1f;



    protected override void Initialize()
    {
        base.Initialize();
        if (CustomFadeDuration < 0)
        {
            CustomFadeDuration = FadeManager.Instance.FadeDuration;
        }
    }

    protected override IEnumerator RunSequence()
    {
        StartCutEffect();



        yield return StartFadeEffect(FadeOutGraphics, FadeOutCanvasGroups, 0f);

        yield return StartFadeEffect(FadeInGraphics, FadeInCanvasGroups, 1f);


        // 모든 페이드 효과가 완료될 때까지 기다립니다.
    }



    private IEnumerator StartFadeEffect(List<Graphic> graphics, List<CanvasGroup> canvasGroups, float targetAlpha)
    {
        if ((graphics == null || graphics.Count == 0) && (canvasGroups == null || canvasGroups.Count == 0))
        {
            yield break;
        }
        else
        {
            if (CustomFadeDuration > 0f)
            {
                if (graphics.Count > 0)
                {
                    for (int i = 0; i < graphics.Count; i++)
                        FadeManager.Instance.TargetFade(graphics[i], targetAlpha, CustomFadeDuration);
                }
                // 모든 그래픽에 대해 페이드 효과를 동시에 시작합니다.

                if (canvasGroups.Count > 0)
                {
                    for (int i = 0; i < canvasGroups.Count; i++)
                        FadeManager.Instance.TargetFade(canvasGroups[i], targetAlpha, CustomFadeDuration);
                }
                yield return CoroutineReturnManager.GetWaitForSeconds(CustomFadeDuration);
            }
            else
            {
                if (graphics.Count > 0)
                {
                    for (int i = 0; i < graphics.Count; i++)
                        FadeManager.Instance.TargetFade(graphics[i], targetAlpha);
                }
                // 모든 그래픽에 대해 페이드 효과를 동시에 시작합니다.

                if (canvasGroups.Count > 0)
                {
                    for (int i = 0; i < canvasGroups.Count; i++)
                        FadeManager.Instance.TargetFade(canvasGroups[i], targetAlpha);
                }
                yield return CoroutineReturnManager.GetWaitForSeconds(FadeManager.Instance.FadeDuration);

            }
        }

    }

    private void StartCutEffect()
    {
        if (CutGraphics.Count < 1 && CutCanvasGroups.Count < 1)
        {
            //Debug.Log("graphics is Null ");
            return;
        }

        for (int i = 0; i < CutGraphics.Count; i++)
        {
            FadeManager.Instance.ToggleCut(CutGraphics[i]);
        }
        for (int i = 0; i < CutCanvasGroups.Count; i++)
        {
            FadeManager.Instance.ToggleCut(CutCanvasGroups[i]);
        }
    }

}