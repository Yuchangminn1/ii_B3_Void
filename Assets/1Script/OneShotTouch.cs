using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OneShotTouch : MonoBehaviour
{
    public Direction currentDirection = Direction.Left;

    public bool IsTagging;

    public WaitCheck waitCheck;



    bool _isCheck = false;

    void Start()
    {

        if (currentDirection == Direction.Left)
            ArduinoTouchManager.Instance.OnPlayerLeftTouch += SetTagLED;
        else
            ArduinoTouchManager.Instance.OnPlayerRightTouch += SetTagLED;



        if (currentDirection == Direction.Left)
            ArduinoTouchManager.Instance.OnPlayerLeftTouch += SetIsTag;
        else
            ArduinoTouchManager.Instance.OnPlayerRightTouch += SetIsTag;

    }

    public void TagStart()
    {
        Debug.Log("StartTag");

        ArduinoTouchManager.Instance.UseTouchInput = true;

        _isCheck = true;

        if (currentDirection == Direction.Left)
            LEDData.Instance.AddPlayerLEDIndex();


    }

    public void SetIsTag(bool value1, bool value2)
    {
        if (gameObject.activeInHierarchy == false)
        {
            return;
        }

        if (value1 && value2)
        {
            waitCheck?.Checking(currentDirection);
            _isCheck = false;


            ArduinoLEDManager.Instance.SendLEDGreenMessage(LEDData.Instance.GetPlayerLEDPair(), currentDirection);
        }

    }


    void OnDisable()
    {

    }
    public virtual void SetTagLED(bool value1, bool value2)
    {
        if (gameObject.activeInHierarchy == false)
        {
            return;
        }
        if (value1 && value2)
        {
            ArduinoLEDManager.Instance.SendLEDGreenMessage(LEDData.Instance.GetPlayerLEDPair(), currentDirection);
            SoundManager.Instance.PlayEffectSound(EffectSoundNum.GreenHandSound);
        }

        else if (value1)
        {
            int[] q = new int[1];
            q[0] = LEDData.Instance.GetPlayerLEDPair()[0];
            ArduinoLEDManager.Instance.SendLEDGreenMessage(q, currentDirection);
        }
        else if (value2)
        {
            int[] q = new int[1];
            q[0] = LEDData.Instance.GetPlayerLEDPair()[1];
            ArduinoLEDManager.Instance.SendLEDGreenMessage(q, currentDirection);
        }
        else
        {
            ArduinoLEDManager.Instance.SendLEDWhiteMessage(LEDData.Instance.GetPlayerLEDPair(), currentDirection);
        }

    }

}
