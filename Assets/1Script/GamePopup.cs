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

    bool waitingPopupLeft = false;
    bool waitingPopupRight = false;


    public void ShowWaitPopupLeft()
    {
        if (waitingPopupRight)
        {
            return;
        }
        waitingPopupLeft = true;

        FadeManager.Instance.SetAlphaZero(failPopup[0]);
        FadeManager.Instance.SetAlphaZero(failPopupText[0]);

        FadeManager.Instance.SetAlphaOne(waitPopup[0]);
        FadeManager.Instance.SetAlphaOne(waitPopupText[0]);
    }

    public void ShowFailPopupLeft()
    {

        FadeManager.Instance.SetAlphaZero(waitPopup[0]);
        FadeManager.Instance.SetAlphaZero(waitPopupText[0]);

        FadeManager.Instance.SetAlphaOne(failPopup[0]);
        FadeManager.Instance.SetAlphaOne(failPopupText[0]);
    }

    public void ShowWaitPopupRight()
    {
        if (waitingPopupLeft)
        {
            return;
        }

        waitingPopupRight = true;


        FadeManager.Instance.SetAlphaZero(failPopup[1]);
        FadeManager.Instance.SetAlphaZero(failPopupText[1]);

        FadeManager.Instance.SetAlphaOne(waitPopup[1]);
        FadeManager.Instance.SetAlphaOne(waitPopupText[1]);
    }

    public void ShowFailPopupRight()
    {
        FadeManager.Instance.SetAlphaZero(waitPopup[1]);
        FadeManager.Instance.SetAlphaZero(waitPopupText[1]);

        FadeManager.Instance.SetAlphaOne(failPopup[1]);
        FadeManager.Instance.SetAlphaOne(failPopupText[1]);
    }

    public void Reset()
    {
        waitingPopupLeft = false;
        waitingPopupRight = false;

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
