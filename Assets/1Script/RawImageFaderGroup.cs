using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class RawImageFaderGroup : MonoBehaviour
{
    public RawImage[] _rawImages;

    public float customFadeTime = -1f;


    void Start()
    {
        var rawImages = GetComponentsInChildren<RawImage>();

        var list = new List<RawImage>(rawImages.Length);
        for (int i = 0; i < rawImages.Length; i++)
        {
            var r = rawImages[i];
            if (r.gameObject != gameObject)
            {
                list.Add(r);
            }
        }
        _rawImages = list.ToArray();

    }


    public void AllLEDOn()
    {
        if (customFadeTime < 0f)
            FadeManager.Instance.TargetFade(_rawImages, 1f);
        else
            FadeManager.Instance.TargetFade(_rawImages, 1f, customFadeTime);

    }

    public void AllLEDOff()
    {
        if (customFadeTime < 0f)
            FadeManager.Instance.TargetFade(_rawImages, 0f);
        else
            FadeManager.Instance.TargetFade(_rawImages, 0f, customFadeTime);
    }

    public void LEDOn(int index)
    {
        if (customFadeTime < 0f)
            FadeManager.Instance.TargetFade(_rawImages[index], 1f);
        else
            FadeManager.Instance.TargetFade(_rawImages[index], 1f, customFadeTime);
    }

    public void LEDOff(int index)
    {
        if (customFadeTime < 0f)
            FadeManager.Instance.TargetFade(_rawImages[index], 0f);
        else
            FadeManager.Instance.TargetFade(_rawImages[index], 0f, customFadeTime);
    }

    public void LEDOnOnly(List<int> index)
    {
        index.Sort();

        int count = 0;

        for (int i = 0; i < index.Count; i++)
        {
            if (index[count] != i)
            {
                LEDOff(i);
            }
            else
            {
                LEDOn(i);
                count++;
            }
        }
    }




}
