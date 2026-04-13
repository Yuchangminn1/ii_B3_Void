using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CameraVisible : MonoBehaviour
{

    public CanvasGroup[] CameraCanvasGroup;


    public Graphic[] Camera_Graphics;

    public CameraValue[] CameraValues;



    Coroutine _cameraDelayOpenCoroutine = null;


    float _cameraOpenDelay = 1.5f;


    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            TogleCameraCnavas();
        }

    }

    void TogleCameraCnavas()
    {
        if (_cameraDelayOpenCoroutine == null)
        {
            FadeManager.Instance.SetAlphaZero(Camera_Graphics);

            if (CameraCanvasGroup[0].alpha == 1)
            {
                CameraOff();
            }
            else
            {
                CameraOn();
            }

        }


    }

    IEnumerator CameraDelayOpen()
    {
        float starttime = Time.time;

        while (Time.time - starttime < _cameraOpenDelay)
        {
            yield return CoroutineReturnManager.GetWaitForSeconds(0.1f);
        }
        FadeManager.Instance.SetAlphaOne(Camera_Graphics);

        _cameraDelayOpenCoroutine = null;

    }
    public void CameraOn()
    {
        if (_cameraDelayOpenCoroutine == null)
        {
            FadeManager.Instance.SetAlphaZero(Camera_Graphics);

            for (int i = 0; i < CameraCanvasGroup.Length; i++)
            {
                CameraCanvasGroup[i].alpha = 1;
                CameraCanvasGroup[i].blocksRaycasts = true;

                CameraValues[i].IsRendered = true;

            }
            _cameraDelayOpenCoroutine = StartCoroutine(CameraDelayOpen());
        }


    }

    public void CameraOnLeft()
    {
        CameraCanvasGroup[0].alpha = 1;
        CameraCanvasGroup[0].blocksRaycasts = true;
        CameraValues[0].IsRendered = true;

    }
    public void CameraOnRight()
    {
        CameraCanvasGroup[1].alpha = 1;
        CameraCanvasGroup[1].blocksRaycasts = true;
        CameraValues[1].IsRendered = true;

    }

    public void CameraOffLeft()
    {
        CameraCanvasGroup[0].alpha = 0;
        CameraCanvasGroup[0].blocksRaycasts = false;
        CameraValues[0].IsRendered = false;

    }
    public void CameraOffRight()
    {
        CameraCanvasGroup[1].alpha = 0;
        CameraCanvasGroup[1].blocksRaycasts = false;
        CameraValues[1].IsRendered = false;
    }






    public void CameraOff()
    {
        for (int i = 0; i < CameraCanvasGroup.Length; i++)
        {
            CameraCanvasGroup[i].alpha = 0;
            CameraCanvasGroup[i].blocksRaycasts = false;
            CameraValues[i].IsRendered = false;
        }
    }


}
