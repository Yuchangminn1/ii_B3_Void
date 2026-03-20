using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class RingLEDController : MonoBehaviour
{
    public RingLEDHand[] ringLEDHands;

    Color32[] colors = new Color32[]
       {
        new Color32(166, 166, 166, 255),
        new Color32(82, 190, 70, 255)
       };

    bool[] prevState = new bool[2] { false, false };

    public Direction CurrentDirection = Direction.Left;

    protected virtual void Start()
    {
        LEDData.Instance.onAddPlayerLEDIndex += SetLEDState;

        if (CurrentDirection == Direction.Left)
        {
            ArduinoTouchManager.Instance.OnPlayerLeftTouch += SetLEDState;
        }
        else
        {
            ArduinoTouchManager.Instance.OnPlayerRightTouch += SetLEDState;
        }

        ArduinoTouchManager.Instance.OnAllPlayerTouchStateChanged += PlayTouchSound;

    }

    protected virtual void OnEnable()
    {
        ;
    }


    public void SetLEDState(int[] ledPair)
    {
        int leftIndex = ledPair[0] - 1;
        int rightIndex = ledPair[1] - 1; // led 번호(1-base) -> 배열 인덱스(0-base)

        for (int i = 0; i < ringLEDHands.Length; i++)
        {
            if (i == leftIndex || i == rightIndex)
            {
                ringLEDHands[i].LEDOn(colors[0]);
            }
            else
            {
                ringLEDHands[i].LEDOff();
            }
        }
    }

    public void AllLEDOff()
    {
        for (int i = 0; i < ringLEDHands.Length; i++)
        {
            ringLEDHands[i].LEDOff();
        }
    }

    public void SetLEDState(bool isLeft, bool isRight)
    {
        if (gameObject.activeInHierarchy == false || GameManager.Instance.IsStarted == false)
        {
            return;
        }
        if (prevState[0] == isLeft && prevState[1] == isRight)
        {
            return; // 상태가 변경되지 않았으므로 업데이트하지 않음
        }
        if (isLeft)
        {
            ringLEDHands[LEDData.Instance.GetPlayerLEDPair()[0] - 1].LEDOn(colors[1]);
        }
        else
        {
            ringLEDHands[LEDData.Instance.GetPlayerLEDPair()[0] - 1].LEDOn(colors[0]);

        }
        if (isRight)
        {
            ringLEDHands[LEDData.Instance.GetPlayerLEDPair()[1] - 1].LEDOn(colors[1]);
        }
        else
        {
            ringLEDHands[LEDData.Instance.GetPlayerLEDPair()[1] - 1].LEDOn(colors[0]);
        }
        prevState[0] = isLeft;
        prevState[1] = isRight;
    }


    public void PlayTouchSound(bool isLeft, bool isRight)
    {
        if (gameObject.activeInHierarchy == false || GameManager.Instance.IsStarted == false)
        {
            return;
        }
        if (CurrentDirection == Direction.Left)
        {
            if (isLeft && isRight)
            {
                SoundManager.Instance.PlayEffectSound(EffectSoundNum.GreenHandSound);
            }
        }

    }



}







