using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CamraVisible : MonoBehaviour
{

    public CanvasGroup[] CanvasGroup;


    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            TogleCameraCnavas();
        }

    }

    void TogleCameraCnavas()
    {
        for (int i = 0; i < CanvasGroup.Length; i++)
        {
            if (CanvasGroup[i].alpha == 1)
            {
                CanvasGroup[i].alpha = 0;
                CanvasGroup[i].blocksRaycasts = false;
            }
            else
            {
                CanvasGroup[i].alpha = 1;
                CanvasGroup[i].blocksRaycasts = true;
            }
        }
    }
    public void CameraOn()
    {
        for (int i = 0; i < CanvasGroup.Length; i++)
        {
            CanvasGroup[i].alpha = 1;
            CanvasGroup[i].blocksRaycasts = true;
        }
    }
    public void CameraOff()
    {
        for (int i = 0; i < CanvasGroup.Length; i++)
        {
            CanvasGroup[i].alpha = 0;
            CanvasGroup[i].blocksRaycasts = false;
        }
    }


}
