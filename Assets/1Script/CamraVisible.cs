using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CameraVisible : MonoBehaviour
{

    public CanvasGroup[] CameraCanvasGroup;

    public Graphic[] Camera_Graphics;

    Coroutine _cameraDelayOpenCoroutine = null;


    float _cameraOpenDelay = 1f;


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
            for (int i = 0; i < CameraCanvasGroup.Length; i++)
            {
                if (CameraCanvasGroup[i].alpha == 1)
                {
                    CameraCanvasGroup[i].alpha = 0;
                    CameraCanvasGroup[i].blocksRaycasts = false;
                }
                else
                {
                    CameraCanvasGroup[i].alpha = 1;
                    CameraCanvasGroup[i].blocksRaycasts = true;

                    _cameraDelayOpenCoroutine = StartCoroutine(CameraDelayOpen());
                }
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
            }
            _cameraDelayOpenCoroutine = StartCoroutine(CameraDelayOpen());
        }


    }

    public void CameraOnLeft()
    {
        CameraCanvasGroup[0].alpha = 1;
        CameraCanvasGroup[0].blocksRaycasts = true;
    }
    public void CameraOnRight()
    {
        CameraCanvasGroup[1].alpha = 1;
        CameraCanvasGroup[1].blocksRaycasts = true;
    }

    public void CameraOffLeft()
    {
        CameraCanvasGroup[0].alpha = 0;
        CameraCanvasGroup[0].blocksRaycasts = false;
    }
    public void CameraOffRight()
    {
        CameraCanvasGroup[1].alpha = 0;
        CameraCanvasGroup[1].blocksRaycasts = false;
    }






    public void CameraOff()
    {
        for (int i = 0; i < CameraCanvasGroup.Length; i++)
        {
            CameraCanvasGroup[i].alpha = 0;
            CameraCanvasGroup[i].blocksRaycasts = false;
        }
    }


}
