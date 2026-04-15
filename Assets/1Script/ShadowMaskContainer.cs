using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShadowMaskContainer : MonoBehaviour
{
    public Direction CurrentDirection;

    CameraVisible cameraVisible;


    RawImage[] shadowMasks;

    int _currentIndex = -1;

    public int CurrentIndex
    {
        get { return _currentIndex; }
        set
        {
            Debug.Log("ShadowMaskContainer - CurrentIndex: " + value);
            if (value >= 0 && value < shadowMasks.Length)
            {
                Debug.Log("ShadowMaskContainer - CurrentIndex: " + value);

                _currentIndex = value;
                ShowShadowMask(_currentIndex);
            }
        }
    }


    private void Awake()
    {
        shadowMasks = GetComponentsInChildren<RawImage>();
    }

    void Start()
    {
        PageController.Instance.OnReset += Reset;

        cameraVisible = FindAnyObjectByType<CameraVisible>();
    }


    public void ShowShadowMask(int index)
    {

        for (int i = 0; i < shadowMasks.Length; i++)
        {
            shadowMasks[i].enabled = (i == index);
        }

    }

    public void HideShadowMasks()
    {
        if (CurrentDirection == Direction.Left)
        {
            cameraVisible.CameraOffLeft();
        }
        else if (CurrentDirection == Direction.Right)
        {
            cameraVisible.CameraOffRight();
        }

        for (int i = 0; i < shadowMasks.Length; i++)
        {
            shadowMasks[i].enabled = false;
        }
    }

    public void Reset()
    {
        _currentIndex = -1;
        for (int i = 0; i < shadowMasks.Length; i++)
        {
            shadowMasks[i].enabled = false;
        }

    }



}
