using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ColorBallImage : MonoBehaviour
{

    RawImage _rawImage;
    public Direction PlayerDirection;


    public Texture[] ColorBallTextures;

    void Start()
    {
        _rawImage = GetComponentInChildren<RawImage>();
    }
    void OnEnable()
    {
        if (GameManager.Instance.IsStarted)
        {
            _rawImage.texture = ColorBallTextures[(int)UserDataManager.Instance.GetPlayer(PlayerDirection).ColorBallType];

        }
    }
}