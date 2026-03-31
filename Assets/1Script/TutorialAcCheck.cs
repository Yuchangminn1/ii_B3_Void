
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TutorialAcCheck : AcCheck
{

    public Texture2D _defaultTexture;

    public Texture2D _clearTexture;

    RawImage _rawImage;


    void Awake()
    {
        _rawImage = GetComponent<RawImage>();
    }

    // public void ShowCheckTexture()
    // {
    //     _rawImage.texture = _defaultTexture;
    //     FadeManager.Instance.SetAlphaOne(_rawImage);
    // }

    public void DisableRawImage()
    {
        FadeManager.Instance.SetAlphaZero(_rawImage);
    }




    public override void StartCheck()
    {
        _rawImage.texture = _defaultTexture;

        base.StartCheck();

    }


    protected override IEnumerator DelayOnClear()
    {
        _rawImage.texture = _clearTexture;

        yield return CoroutineReturnManager.GetWaitForSeconds(1f);

        onClear?.Invoke();
    }


}
