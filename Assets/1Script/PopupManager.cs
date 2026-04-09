using UnityEngine;
using UnityEngine.UI;
using System.Collections;
public enum PopupType
{
    None,
    PleaseInput,
    ResetNotice,
}
public enum InputType
{
    Touch,
    Button

}
public class PopupManager : Singleton<PopupManager>
{
    public Graphic[] FirstGraphics;

    public Text[] firstTexts;

    public Graphic[] SecondGraphics;



    public float ResetPopupDelay = 10f;
    readonly float ResetPopupTime = 3f;

    [Header("Touch = 0 \n Button = 1")]
    public InputType currentInputType = InputType.Touch;

    string[] popupText = new string[] { "답변해 주세요.", "답변해 주세요." };

    PopupType _currentPopupType = PopupType.None;

    public void SetInputType(int inputType)
    {
        currentInputType = (InputType)inputType;
    }

    public void SetInputType(InputType inputType)
    {
        currentInputType = inputType;
    }


    public PopupType CurrentPopupType
    {
        get { return _currentPopupType; }
        set { _currentPopupType = value; }
    }

    public CanvasGroup[] PopupCanvasGroups;

    Coroutine _popupCoroutine = null;

    override protected void Awake()
    {
        base.Awake();
    }

    void Start()
    {

    }


    public void SetPleaseInputText(PopupType popupType)
    {
        if (popupType == PopupType.PleaseInput)
        {
            SoundManager.Instance.PlayEffectSound(EffectSoundNum.PopupSound);

            FadeManager.Instance.SetAlphaOne(FirstGraphics);

            FadeManager.Instance.SetAlphaZero(SecondGraphics);
            return;
        }
        else if (popupType == PopupType.ResetNotice)
        {
            SoundManager.Instance.PlayEffectSound(EffectSoundNum.PopupSound);

            FadeManager.Instance.SetAlphaOne(SecondGraphics);

            FadeManager.Instance.SetAlphaZero(FirstGraphics);
        }
        else if (popupType == PopupType.None)
        {
            FadeManager.Instance.SetAlphaZero(FirstGraphics);
            FadeManager.Instance.SetAlphaZero(SecondGraphics);
        }
    }

    public void ResetPopUpOpen()
    {
        if (_popupCoroutine != null)
            StopCoroutine(_popupCoroutine);
        _popupCoroutine = StartCoroutine(PopUpCoroutine());

    }
    public IEnumerator PopUpCoroutine()
    {
        CurrentPopupType = PopupType.PleaseInput;
        SetPleaseInputText(CurrentPopupType);
        float startTime = Time.time;
        if (currentInputType == InputType.Touch)
        {
            foreach (var graphic in FirstGraphics)
            {
                FadeManager.Instance.TargetFade(graphic, 1f);
            }
            foreach (var text in firstTexts)
            {
                text.text = popupText[(int)InputType.Touch];
            }

        }

        else if (currentInputType == InputType.Button)
        {
            foreach (var text in firstTexts)
            {
                text.text = popupText[(int)InputType.Button];
            }

        }

        if (CurrentPopupType != PopupType.None)
        {
            foreach (var canvasGroup in PopupCanvasGroups)
            {
                FadeManager.Instance.TargetFade(canvasGroup, 1f);
            }

            foreach (var canvasGroup in PopupCanvasGroups)
            {
                FadeManager.Instance.TargetFade(canvasGroup, 1f);
            }
            while (Time.time - startTime < ResetPopupTime)
            {
                yield return CoroutineReturnManager.GetWaitForSeconds(0.1f);
            }
            foreach (var canvasGroup in PopupCanvasGroups)
            {
                FadeManager.Instance.TargetFade(canvasGroup, 0f);
            }
        }
        yield return CoroutineReturnManager.WaitForFixedUpdate;

        startTime = Time.time;
        while (Time.time - startTime < ResetPopupDelay)
        {
            yield return CoroutineReturnManager.GetWaitForSeconds(0.1f);
        }

        CurrentPopupType = PopupType.ResetNotice;
        SetPleaseInputText(CurrentPopupType);
        if (CurrentPopupType != PopupType.None)
        {
            foreach (var canvasGroup in PopupCanvasGroups)
            {
                FadeManager.Instance.TargetFade(canvasGroup, 1f);
            }
            startTime = Time.time;
            while (Time.time - startTime < ResetPopupTime)
            {
                yield return CoroutineReturnManager.GetWaitForSeconds(0.1f);
            }
            foreach (var canvasGroup in PopupCanvasGroups)
            {
                FadeManager.Instance.TargetFade(canvasGroup, 0f);
            }
        }
        yield return CoroutineReturnManager.WaitForFixedUpdate;


        startTime = Time.time;
        while (Time.time - startTime < 1f) //사라지고 여유시간 ? 임시로 
        {
            yield return CoroutineReturnManager.GetWaitForSeconds(0.1f);
        }
        UserDataManager.Instance.UserResetRequest();

        _popupCoroutine = null;
    }

    public void ClosePopup()
    {
        if (_popupCoroutine != null)
            StopCoroutine(_popupCoroutine);
        _popupCoroutine = null;
        CurrentPopupType = PopupType.None;
        foreach (var canvasGroup in PopupCanvasGroups)
        {
            FadeManager.Instance.TargetFade(canvasGroup, 0f);
        }
    }

}
