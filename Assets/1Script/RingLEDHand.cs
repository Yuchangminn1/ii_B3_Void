using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RingLEDHand : MonoBehaviour
{

    Graphic[] graphics;


    void Start()
    {
        graphics = GetComponentsInChildren<Graphic>();
    }


    public void LEDOn(Color32 color)
    {
        FadeManager.Instance.SetAlphaOne(graphics[0]);



        graphics[1].color = color;
    }
    public void LEDOff()
    {
        FadeManager.Instance.SetAlphaZero(graphics);
    }

}
