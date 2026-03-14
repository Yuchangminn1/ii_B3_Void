using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
public class SequenceFade : SequenceScript
{
    [Tooltip("컷 효과를 적용할 Graphic 배열 (예: UI 이미지 등)")]

    public List<Graphic> CutGraphics;

    public List<Graphic> FadeInGraphics;

    public List<Graphic> FadeOutGraphics;

    public List<CanvasGroup> FadeInCanvasGroups;


    public List<CanvasGroup> FadeOutCanvasGroups;


    public float _waitforStartDelay = -1f;




    [Tooltip("페이드 효과의 지속 시간 (초)")] public float fadeDuration = 1f;

    WaitForSeconds _fadeWait;
    WaitForSeconds _fadeOutDelay = new WaitForSeconds(0.1f);

    WaitForSeconds _startDelayTime;

    public UnityEvent OnFadeStart;




    protected override void AwakeSetup()
    {
        Initialize();

    }

    private void Initialize()
    {
        _fadeWait = new WaitForSeconds(fadeDuration);
        if (_waitforStartDelay > 0f)
            _startDelayTime = new WaitForSeconds(_waitforStartDelay);

    }

    protected override IEnumerator RunSequence()
    {
        if (_waitforStartDelay > 0f)
            yield return _startDelayTime;
        OnFadeStart?.Invoke();
        StartCutEffect();


        StartFadeEffect(FadeInGraphics, FadeInCanvasGroups);
        yield return _fadeWait;
        yield return _fadeOutDelay;


        StartFadeEffect(FadeOutGraphics, FadeOutCanvasGroups);
        yield return _fadeWait;

        // 모든 페이드 효과가 완료될 때까지 기다립니다.
    }



    private void StartFadeEffect(List<Graphic> graphics, List<CanvasGroup> canvasGroups)
    {
        if (graphics.Count > 0)
        {
            for (int i = 0; i < graphics.Count; i++)
            {
                FadeManager.Instance.ToggleFade(graphics[i], fadeDuration);
            }
        }
        // 모든 그래픽에 대해 페이드 효과를 동시에 시작합니다.

        if (canvasGroups.Count > 0)
        {
            for (int i = 0; i < canvasGroups.Count; i++)
            {
                FadeManager.Instance.ToggleFade(canvasGroups[i], fadeDuration);
            }
        }

    }

    private void StartCutEffect()
    {
        if (CutGraphics.Count < 1)
        {
            //Debug.Log("graphics is Null ");
            return;
        }

        for (int i = 0; i < CutGraphics.Count; i++)
        {
            FadeManager.Instance.ToggleCut(CutGraphics[i]);
        }
    }

}