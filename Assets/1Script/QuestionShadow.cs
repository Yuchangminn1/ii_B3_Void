using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class QuestionShadow : MonoBehaviour
{
    public RawImage[] rawImages;

    public CameraVisible cameraVisible;


    AcCheck acCheck;

    Action onClear;

    bool _isClear = false;

    public bool IsClear => _isClear;


    // Start is called before the first frame update
    void Start()
    {
        rawImages = GetComponentsInChildren<RawImage>();
        acCheck = GetComponent<AcCheck>();
    }

    public void AddOnClearListener(Action action)
    {
        onClear += action;
    }

    public void Clear()
    {
        acCheck.RemoveOnClearListener(Clear);
        _isClear = true;

        onClear?.Invoke();
    }


    public void ShowShadow(int index)
    {
        //_timer.AddOnEndListener(HideShadow);
        if (cameraVisible != null)
            cameraVisible.CameraOn();
        if (index < rawImages.Length)
        {
            acCheck.targetRawImage = rawImages[index];
            for (int i = 0; i < rawImages.Length; i++)
            {
                if (i == index)
                {
                    FadeManager.Instance.SetAlphaOne(rawImages[i]);
                }
                else
                {
                    FadeManager.Instance.SetAlphaZero(rawImages[i]);
                }
            }
        }

        _isClear = false;
        acCheck.AddOnClearListener(Clear);

        acCheck.StartCheck();
    }



    public void HideShadow()
    {
        // if (cameraVisible != null)

        //     cameraVisible.CameraOffLeft();
        for (int i = 0; i < rawImages.Length; i++)
        {
            FadeManager.Instance.SetAlphaZero(rawImages[i]);
        }
        acCheck.StopCheck();
    }



}
