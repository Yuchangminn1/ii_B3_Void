using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AnswerStamp : MonoBehaviour
{
    public RawImage _emptyStampImage;
    public RawImage _correctStampImage;

    Color _correctStampImageColor = new Color(1f, 1f, 1f, 0.2f);

    public void SetTextures(Texture emptyStampTexture, Texture correctStampTexture)
    {
        if (_emptyStampImage == null || _correctStampImage == null)
            Debug.LogError($"빈 스탬프 이미지 또는 정답 스탬프 이미지가 할당되지 않았습니다. 빈 스탬프 이미지: {_emptyStampImage}, 정답 스탬프 이미지: {_correctStampImage}");
        _emptyStampImage.texture = emptyStampTexture;
        _correctStampImage.texture = correctStampTexture;
    }

    public void SetEmptyStamp()
    {
        FadeManager.Instance.SetAlphaZero(_correctStampImage);

    }

    public void SetCorrectStamp()
    {

        FadeManager.Instance.SetAlphaOne(_correctStampImage);

        SoundManager.Instance.PlayEffectSound(EffectSoundNum.SoulPieceSound);
    }

    void OnEnable()
    {
        if (GameManager.Instance.IsStarted)
        {
            StartCoroutine(DelayToSetEmptyStamp());
        }
    }

    IEnumerator DelayToSetEmptyStamp()
    {
        yield return new WaitForSeconds(0.5f);
        _emptyStampImage.color = _correctStampImageColor;
    }


    void Awake()
    {


        RawImage[] tmpRawImages = GetComponentsInChildren<RawImage>();
        tmpRawImages[0] = _emptyStampImage;
        tmpRawImages[1] = _correctStampImage;

    }

}
