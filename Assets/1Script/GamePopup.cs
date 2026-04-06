using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GamePopup : MonoBehaviour
{

    public RawImage[] waitPopup;
    public Text[] waitPopupText;


    public RawImage[] failPopup;
    public Text[] failPopupText;


    public void ShowWaitPopupLeft()
    {
        FadeManager.Instance.SetAlphaZero(failPopup);
        FadeManager.Instance.SetAlphaZero(failPopupText);

        FadeManager.Instance.SetAlphaOne(waitPopup);
        FadeManager.Instance.SetAlphaOne(waitPopupText);
    }

    public void ShowFailPopupLeft()
    {
        FadeManager.Instance.SetAlphaZero(waitPopup);
        FadeManager.Instance.SetAlphaZero(waitPopupText);

        FadeManager.Instance.SetAlphaOne(failPopup);
        FadeManager.Instance.SetAlphaOne(failPopupText);
    }

    public void ShowWaitPopupRight()
    {
        FadeManager.Instance.SetAlphaZero(failPopup);
        FadeManager.Instance.SetAlphaZero(failPopupText);

        FadeManager.Instance.SetAlphaOne(waitPopup);
        FadeManager.Instance.SetAlphaOne(waitPopupText);
    }

    public void ShowFailPopupRight()
    {
        FadeManager.Instance.SetAlphaZero(waitPopup);
        FadeManager.Instance.SetAlphaZero(waitPopupText);

        FadeManager.Instance.SetAlphaOne(failPopup);
        FadeManager.Instance.SetAlphaOne(failPopupText);
    }

    public void Reset()
    {
        FadeManager.Instance.SetAlphaZero(waitPopup);
        FadeManager.Instance.SetAlphaZero(waitPopupText);
        FadeManager.Instance.SetAlphaZero(failPopup);
        FadeManager.Instance.SetAlphaZero(failPopupText);
    }

    void Start()
    {
        PageController.Instance.OnReset += Reset;
    }


}
